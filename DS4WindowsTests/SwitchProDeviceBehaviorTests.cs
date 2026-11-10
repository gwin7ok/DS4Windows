using System;
using System.Text;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Switch Pro（USB）の初期化の手順と読み取りループの特性化テスト。偽の通信路（<see cref="FakeHidTransport"/>）に、
    /// サブコマンドの応答（校正値を含む）と入力レポートの台本を与え、報告される状態の列、書き込みの列、台本が尽きたあとの切断を記録して、期待値と比べる。
    /// この機種は実時間で 5ms 未満の間隔のフレームを読み飛ばすため、偽の通信路に読み取りの遅延を入れる。経過時間は実時間なので記録しない。
    /// 共通のデバイスへ移す前後で、この記録が変わらないことを移行の合格条件にする。
    /// </summary>
    public class SwitchProDeviceBehaviorTests
    {
        private readonly ITestOutputHelper output;

        public SwitchProDeviceBehaviorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // SPI 読み出しの応答: [0]=0x21、[14]=サブコマンド、[20..28]=スティックの校正値（最大／中央／最小の X・Y を 12bit で詰めた 9 バイト）
        private static byte[] Response(byte subcommand, ushort[] calib = null)
        {
            var f = new byte[362];
            f[0] = 0x21;
            f[14] = subcommand;
            if (calib != null)
            {
                const int o = 20;
                for (int i = 0; i < 3; i++)
                {
                    ushort x = calib[i * 2], y = calib[i * 2 + 1];
                    f[o + i * 3] = (byte)x;
                    f[o + i * 3 + 1] = (byte)(((x >> 8) & 0x0F) | ((y & 0x0F) << 4));
                    f[o + i * 3 + 2] = (byte)(y >> 4);
                }
            }

            return f;
        }

        // 標準の入力レポート: [0]=0x30、[2]=電池と充電、[3..5]=ボタン、[6..8]=左スティック、[9..11]=右スティック（各 12bit の X・Y）
        private static byte[] Frame(byte battery, byte b3, byte b4, byte b5, ushort lx, ushort ly, ushort rx, ushort ry)
        {
            var f = new byte[362];
            f[0] = 0x30;
            f[2] = battery;
            f[3] = b3;
            f[4] = b4;
            f[5] = b5;
            f[6] = (byte)lx;
            f[7] = (byte)(((lx >> 8) & 0x0F) | ((ly & 0x0F) << 4));
            f[8] = (byte)(ly >> 4);
            f[9] = (byte)rx;
            f[10] = (byte)(((rx >> 8) & 0x0F) | ((ry & 0x0F) << 4));
            f[11] = (byte)(ry >> 4);
            return f;
        }

        private static string Snapshot(DS4State s)
        {
            return $"Ci={s.Circle} Cr={s.Cross} Sq={s.Square} R1={s.R1} L1={s.L1} Up={s.DpadUp} L2={s.L2} R2={s.R2} " +
                   $"LX={s.LX} LY={s.LY} RX={s.RX} RY={s.RY} Bat={s.Battery} Pkt={s.PacketCounter}";
        }

        private (string Reports, string Writes, int Battery, bool Removed) Run(params byte[][] frames)
        {
            var transport = new FakeHidTransport(inputReportByteLength: 362, outputReportByteLength: 49,
                vendorId: 0x057E, productId: 0x2009) { ReadDelayMs = 12 };

            // 初期化のサブコマンドの応答（USB のモード切替、低電力、HOME LED、プレイヤー LED、IMU 2 件、振動、通信間隔）
            for (int i = 0; i < 8; i++)
            {
                transport.EnqueueRead(HidReadStatus.Success, Response(0x03));
            }

            // 校正: 左の利用者の印（なし）→ 左の工場値、右の利用者の印（なし）→ 右の工場値
            var calib = new ushort[] { 1500, 1500, 2048, 2048, 1500, 1500 };
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10, calib));
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10, calib));

            // 動きセンサーの校正: 利用者の印（なし）→ 工場値（全て 0）
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
            transport.EnqueueRead(HidReadStatus.Success, Response(0x10));

            foreach (byte[] frame in frames)
            {
                transport.EnqueueRead(HidReadStatus.Success, frame);
            }

            var device = new SwitchProDevice(transport, "Switch Pro", VidPidFeatureSet.DefaultDS4);
            device.PostInit();

            var reports = new StringBuilder();
            device.Report += (sender, args) => reports.AppendLine(Snapshot(sender.getCurrentStateRef()));
            using var removed = new ManualResetEventSlim();
            device.Removal += (sender, args) => removed.Set();

            device.StartUpdate();
            bool wasRemoved = removed.Wait(8000);
            device.StopUpdate();

            var writes = new StringBuilder();
            foreach (FakeHidTransport.Write w in transport.Writes)
            {
                string sub = w.Data.Length > 10 && w.Data[0] == 0x01 ? $" sub={w.Data[10]:X2}" : string.Empty;
                writes.AppendLine($"{w.Kind} len={w.Data.Length} id={w.Data[0]:X2} b1={w.Data[1]:X2}{sub}");
            }

            return (reports.ToString(), writes.ToString(), device.Battery, wasRemoved);
        }

        [Fact]
        public void UsbScript_InitializesThenReportsEachFrame_AndRemovesOnTheEndOfTheScript()
        {
            var result = Run(
                Frame(0x80, 0x04, 0x00, 0x02, 2048, 2048, 2048, 2048),
                Frame(0x90, 0x09, 0x10, 0x42, 3500, 600, 1000, 3000),
                Frame(0xA0, 0x80, 0x00, 0x00, 2048, 2048, 2048, 2048));

            output.WriteLine(result.Reports);
            output.WriteLine(result.Writes);
            output.WriteLine("Battery=" + result.Battery + " Removed=" + result.Removed);
            Assert.True(result.Removed);
            Assert.Equal(Expected, (result.Reports + "--\n" + result.Writes + $"Battery={result.Battery}\n").Replace("\r\n", "\n"));
        }

        // 旧実装（SwitchProDevice）が出した記録そのもの。移行後の実装は、これと同じ記録を出すこと
        private const string Expected =
            "Ci=False Cr=True Sq=False R1=False L1=False Up=True L2=0 R2=0 LX=132 LY=122 RX=0 RY=255 Bat=100 Pkt=1\n" +
            "Ci=True Cr=False Sq=True R1=False L1=True Up=True L2=0 R2=0 LX=255 LY=252 RX=0 RY=0 Bat=100 Pkt=2\n" +
            "Ci=False Cr=False Sq=False R1=False L1=False Up=False L2=0 R2=255 LX=132 LY=122 RX=0 RY=255 Bat=100 Pkt=3\n" +
            "--\n" +
            "interrupt len=64 id=01 b1=00 sub=03\n" +
            "interrupt len=64 id=80 b1=01\n" +
            "interrupt len=64 id=80 b1=02\n" +
            "interrupt len=64 id=80 b1=03\n" +
            "interrupt len=64 id=80 b1=02\n" +
            "interrupt len=64 id=80 b1=04\n" +
            "interrupt len=64 id=01 b1=01 sub=08\n" +
            "interrupt len=64 id=01 b1=02 sub=38\n" +
            "interrupt len=64 id=01 b1=03 sub=30\n" +
            "interrupt len=64 id=01 b1=04 sub=40\n" +
            "interrupt len=64 id=01 b1=05 sub=41\n" +
            "interrupt len=64 id=01 b1=06 sub=48\n" +
            "interrupt len=64 id=01 b1=07 sub=03\n" +
            "interrupt len=64 id=10 b1=08\n" +
            "interrupt len=64 id=01 b1=09 sub=10\n" +
            "interrupt len=64 id=01 b1=0A sub=10\n" +
            "interrupt len=64 id=01 b1=0B sub=10\n" +
            "interrupt len=64 id=01 b1=0C sub=10\n" +
            "interrupt len=64 id=01 b1=0D sub=10\n" +
            "interrupt len=64 id=01 b1=0E sub=10\n" +
            "Battery=100\n";
    }
}
