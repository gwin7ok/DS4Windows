#nullable enable
using System.Linq;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力レポート（振動・Bluetooth の枠と CRC）と、タッチパッドの読み方を、公開された別実装の仕様と突き合わせる。
    /// 出どころは、Linux カーネルの <c>hid-playstation.c</c> と SDL の <c>SDL_hidapi_ps4.c</c>・<c>SDL_hidapi_ps5.c</c>（2026-11-22 に取得。経緯は <c>PhaseRV38-Spec-Crosscheck.md</c>）。
    /// 本番のデバイスが偽の通信路へ書いた出力レポートを、独立した CRC 実装（<see cref="PsReportCrc"/>）で検算する。
    /// </summary>
    public class OutputReportSpecConformanceTests
    {
        private const byte Right = 100;
        private const byte Left = 200;

        private static byte[][] WriteWithRumble(VirtualPad pad, int reportId)
        {
            using VirtualPadDevice opened = pad.Open(new PadInput(), frames: 120);
            opened.Transport!.ReadDelayMs = 8;
            using var first = new ManualResetEventSlim();
            using var removed = new ManualResetEventSlim();
            opened.Device.Report += (sender, args) =>
            {
                if (first.IsSet) return;
                first.Set();
                sender.SetRumble(Right, Left);   // 右（小さく速い）、左（大きく遅い）
            };
            opened.Device.Removal += (sender, args) => removed.Set();
            try
            {
                opened.Device.StartUpdate();
                Assert.True(removed.Wait(15000));
            }
            finally
            {
                opened.Device.StopUpdate();
            }

            return opened.Transport.Writes.Where(w => w.Data[0] == reportId).Select(w => w.Data).ToArray();
        }

        [Fact]
        public void Ds4Bluetooth_OutputReport_FollowsTheSdlFraming_AndTheCrcUsesTheOutputSeed()
        {
            // SDL_hidapi_ps4.c: ID 0x11、data[1] の上位 2 ビットは HID と CRC の印（0xC0）、data[3] が有効フラグ、振動は右が data[6]・左が data[7]、
            // 末尾 78 バイト目までで、CRC は種 0xA2 を先に流して下位バイトから data[74..77] へ
            byte[][] reports = WriteWithRumble(VirtualPad.Ds4Bluetooth, 0x11);

            byte[] withRumble = reports.FirstOrDefault(r => r[6] == Right && r[7] == Left)
                ?? throw new Xunit.Sdk.XunitException("振動つきの出力レポートが書かれなかった");
            Assert.Equal(0xC0, withRumble[1] & 0xC0);
            Assert.NotEqual(0, withRumble[3] & 0x01);   // 振動の有効フラグ
            Assert.True(PsReportCrc.IsValid(withRumble, 78, PsReportCrc.OutputSeed), "出力の CRC（種 0xA2）が一致しない");
        }

        [Fact]
        public void DualSenseBluetooth_OutputReport_HasTheSpecIdAndCrc_ButTheCurrentFramingIsOneByteEarlierThanTheSpec()
        {
            // SDL_hidapi_ps5.c・hid-playstation.c: ID 0x31、data[1]＝タグと連番、data[2]＝0x10（決まった値）、効果の構造体は data[3] から
            // （有効フラグ data[3]・data[4]、振動は右 data[5]・左 data[6]）。CRC は種 0xA2、data[74..77]。
            // 現在の実装は、data[1]＝0x02、有効フラグ data[2]・data[3]、振動は右 data[4]・左 data[5] と、1 バイト早い（PhaseRV38-Spec-Crosscheck.md の食い違い 3）。
            // ここでは、仕様と一致する部分（ID と CRC）を固定し、食い違う部分は「いまの形」を固定する（実機確認のあとに直すときは、この検査も直す）。
            byte[][] reports = WriteWithRumble(VirtualPad.DualSenseBluetooth, 0x31);

            Assert.NotEmpty(reports);
            Assert.All(reports, r => Assert.True(PsReportCrc.IsValid(r, 78, PsReportCrc.OutputSeed), "出力の CRC（種 0xA2）が一致しない"));

            byte[] withRumble = reports.FirstOrDefault(r => r[4] == Right && r[5] == Left)
                ?? throw new Xunit.Sdk.XunitException("振動つきの出力レポートが書かれなかった");
            Assert.NotEqual(0x10, withRumble[2]);   // 仕様では data[2] が 0x10。いまは有効フラグが入っている
        }

        // ---- タッチパッド: 1 点が 4 バイト（カウンター、x の下位、x の上位 4 ビット＋y の下位 4 ビット、y の上位）。カウンターの最上位ビットが立っていると指なし ----
        private static byte[] TouchBytes(bool down, byte id, int x, int y) => new[]
        {
            (byte)((down ? 0 : 0x80) | id), (byte)x, (byte)(((x >> 8) & 0x0F) | ((y & 0x0F) << 4)), (byte)(y >> 4),
        };

        private static void AssertTouches(ControllerState s)
        {
            Assert.True(s.TrackPadTouch0.IsActive);
            Assert.Equal(1, s.TrackPadTouch0.Id);
            Assert.Equal(1234, s.TrackPadTouch0.X);
            Assert.Equal(567, s.TrackPadTouch0.Y);
            Assert.False(s.TrackPadTouch1.IsActive);
        }

        [Fact]
        public void Ds4Usb_TheTouchPoints_AreAtTheSdlOffsets_AndFollowTheTwelveBitPacking()
        {
            // SDL_hidapi_ps4.c: 1 点目のカウンターが struct の 34（USB のレポートでは 35）、2 点目が 38（USB では 39）
            var report = new byte[64];
            report[0] = 0x01;
            TouchBytes(true, 1, 1234, 567).CopyTo(report, 35);
            TouchBytes(false, 2, 0, 0).CopyTo(report, 39);
            var s = new ControllerState();
            new Ds4ReportParser().Parse(report, s);

            AssertTouches(s);
        }

        [Theory]
        [InlineData(0, 33)]   // USB: struct の 32 → レポートの 33、2 点目は 37
        [InlineData(1, 34)]   // BT 0x31: さらに 1 バイト後ろ
        public void DualSense_TheTouchPoints_AreAtTheSdlOffsets_AndFollowTheTwelveBitPacking(int parserOffset, int firstPoint)
        {
            var report = new byte[78];
            TouchBytes(true, 1, 1234, 567).CopyTo(report, firstPoint);
            TouchBytes(false, 2, 0, 0).CopyTo(report, firstPoint + 4);
            var s = new ControllerState();
            new DualSenseReportParser(parserOffset).Parse(report, s);

            AssertTouches(s);
        }
    }
}
