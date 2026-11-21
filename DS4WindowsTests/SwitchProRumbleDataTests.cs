using System;
using System.Runtime.CompilerServices;
using System.Text;
using DS4Windows.Input.Devices;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Switch Pro の振動データ（左右の強さの比 → 8 バイトの振動データ）。実機がないため、組み立てを部品へ出す前の実装が出したバイト列を記録として固定する。
    /// </summary>
    public class SwitchProRumbleDataTests
    {
        private readonly ITestOutputHelper output;

        public SwitchProRumbleDataTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static readonly (double Left, double Right)[] Pairs =
        {
            (0.0, 0.0), (0.001, 0.002), (0.1, 0.9), (0.25, 0.25), (0.5, 0.0), (0.0, 0.5), (0.75, 0.33), (0.999, 1.0), (1.0, 1.0),
        };

        private static string Encode(double left, double right, int frames)
        {
            var device = new SwitchProDevice(new FakeHidTransport(), "Switch Pro");
            device.currentLeftAmpRatio = left;
            device.currentRightAmpRatio = right;
            var sb = new StringBuilder();
            for (int i = 0; i < frames; i++)
            {
                var buffer = new byte[10];
                device.PrepareRumbleData(buffer);
                sb.Append(Convert.ToHexString(buffer)).Append(' ');
            }

            return sb.ToString().TrimEnd();
        }

        [Theory]
        [InlineData(0, 0, "10000001604000016040 10010001604000016040 10020001604000016040")]
        [InlineData(0.001, 0.002, "10000001604000016040 10010001604000016040 10020001604000016040")]
        [InlineData(0.1, 0.9, "1000001BE04600A9606A 1001001BE04600A9606A 1002001BE04600A9606A")]
        [InlineData(0.25, 0.25, "10000039604E0039604E 10010039604E0039604E 10020039604E0039604E")]
        [InlineData(0.5, 0, "10000073E05C00016040 10010073E05C00016040 10020073E05C00016040")]
        [InlineData(0, 0.5, "1000000160400073E05C 1001000160400073E05C 1002000160400073E05C")]
        [InlineData(0.75, 0.33, "100000996066004D6053 100100996066004D6053 100200996066004D6053")]
        [InlineData(0.999, 1, "100000B3E06C00B3E06C 100100B3E06C00B3E06C 100200B3E06C00B3E06C")]
        [InlineData(1, 1, "100000B3E06C00B3E06C 100100B3E06C00B3E06C 100200B3E06C00B3E06C")]
        public void TheEncodedBytes_MatchTheRecordedOnes_AndTheFrameCounterAdvances(double left, double right, string expected)
        {
            Assert.Equal(expected, Encode(left, right, 3));
        }

        [Fact]
        public void TheFrameCounter_WrapsAround_AtSixteen()
        {
            var device = new SwitchProDevice(new FakeHidTransport(), "Switch Pro");
            var buffer = new byte[10];
            for (int i = 0; i < 17; i++) device.PrepareRumbleData(buffer);
            Assert.Equal(0x00, buffer[1]);   // 17 回目は、0 へ戻った番号（16 ≡ 0）
            Assert.Equal(1, device.FrameCount);
        }
    }
}
