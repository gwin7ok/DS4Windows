using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Switch Pro を構成する部品（スロットの LED、較正のスケール、サブコマンドの送信）の単体の振る舞い。
    /// 偽の通信路で、送るバイト列とフレーム番号の進み方を固定する（実機は不要）。
    /// </summary>
    public class SwitchProPartsTests
    {
        [Theory]
        [InlineData(0, 0x01)]
        [InlineData(1, 0x02)]
        [InlineData(2, 0x04)]
        [InlineData(3, 0x08)]
        [InlineData(4, 0x03)]
        [InlineData(9, 0x0C)]
        [InlineData(14, 0x0F)]
        [InlineData(15, 0x00)]
        [InlineData(-1, 0x00)]
        public void SlotMask_MapsSlotNumberToLedBits(int slot, int expected)
        {
            Assert.Equal((byte)expected, SwitchProSlotMask.FromSlot(slot));
        }

        [Fact]
        public void Calibration_ScalesToByteRange_AndInvertsY()
        {
            var cal = new SwitchProCalibration();
            cal.LeftStickX.min = 500; cal.LeftStickX.max = 3300;
            cal.LeftStickY.min = 500; cal.LeftStickY.max = 3300;

            Assert.Equal(0, cal.ScaleLeftX(0));        // 範囲の下へ丸める
            Assert.Equal(255, cal.ScaleLeftX(5000));   // 範囲の上へ丸める
            Assert.Equal(127, cal.ScaleLeftX(1900));   // 中央（255 * 0.5 の切り捨て）
            Assert.Equal(255, cal.ScaleLeftY(500));    // Y は上下が逆
            Assert.Equal(0, cal.ScaleLeftY(3300));
        }

        [Fact]
        public void Subcommand_WritesHeaderFrameCountAndPayload_AndAdvancesFrameCountModSixteen()
        {
            var hid = new FakeHidTransport();
            var channel = new SwitchProCommandChannel(hid);

            for (int i = 0; i < 17; i++)
                channel.Subcommand(0x30, new byte[] { 0x05 }, 1);

            Assert.Equal(17, hid.Writes.Count);
            byte[] first = hid.Writes[0].Data;
            Assert.Equal(64, first.Length);
            Assert.Equal(0x01, first[0]);
            Assert.Equal(0x00, first[1]);
            Assert.Equal(new byte[] { 0x0, 0x1, 0x40, 0x40, 0x0, 0x1, 0x40, 0x40 }, first[2..10]);
            Assert.Equal(0x30, first[10]);
            Assert.Equal(0x05, first[11]);
            Assert.Equal(0x00, hid.Writes[16].Data[1]);   // 16 回目で 0 へ戻る
            Assert.Equal(1, channel.FrameCount);
        }
    }
}
