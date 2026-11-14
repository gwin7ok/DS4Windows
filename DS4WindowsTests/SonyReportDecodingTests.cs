using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>DS4 と DualSense の解析器が共有する、方向キーとタッチ 1 本の読み取り。</summary>
    public class SonyReportDecodingTests
    {
        [Theory]
        [InlineData(0, true, false, false, false)]
        [InlineData(1, true, false, false, true)]
        [InlineData(2, false, false, false, true)]
        [InlineData(3, false, true, false, true)]
        [InlineData(4, false, true, false, false)]
        [InlineData(5, false, true, true, false)]
        [InlineData(6, false, false, true, false)]
        [InlineData(7, true, false, true, false)]
        [InlineData(8, false, false, false, false)]
        [InlineData(15, false, false, false, false)]
        public void ApplyDpad_WritesTheFourDirections(byte value, bool up, bool down, bool left, bool right)
        {
            var s = new ControllerState { DpadUp = !up, DpadDown = !down, DpadLeft = !left, DpadRight = !right };
            SonyReportDecoding.ApplyDpad(s, value);
            Assert.Equal((up, down, left, right), (s.DpadUp, s.DpadDown, s.DpadLeft, s.DpadRight));
        }

        [Fact]
        public void ReadTouch_DecodesTheTrackingNumber_TheActiveFlag_AndThe12BitPosition()
        {
            // 追跡番号 0x05（接触中＝上位ビット 0）、X = 0x3A1、Y = 0x2B4
            byte[] report = { 0xEE, 0x05, 0xA1, 0x43, 0x2B };
            var s = new ControllerState();

            SonyReportDecoding.ReadTouch(report, 1, ref s.TrackPadTouch0);

            Assert.Equal(0x05, s.TrackPadTouch0.RawTrackingNum);
            Assert.Equal(0x05, s.TrackPadTouch0.Id);
            Assert.True(s.TrackPadTouch0.IsActive);
            Assert.Equal(0x3A1, s.TrackPadTouch0.X);
            Assert.Equal(0x2B4, s.TrackPadTouch0.Y);

            report[1] = 0x85;   // 上位ビット 1＝離れている
            SonyReportDecoding.ReadTouch(report, 1, ref s.TrackPadTouch0);
            Assert.False(s.TrackPadTouch0.IsActive);
            Assert.Equal(0x05, s.TrackPadTouch0.Id);
        }
    }
}
