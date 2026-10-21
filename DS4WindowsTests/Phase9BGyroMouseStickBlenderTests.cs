using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="GyroMouseStickBlender"/>（旧 <c>Mapping.TempMouseJoystick</c> の内側）の値の特性化。
    /// 出力スティック（左／右）と軸（XY／X／Y）ごとに、ジャイロ由来の値と通常のスティックの値のうち、中心（128）から遠い方を採ることを調べる。
    /// </summary>
    public class Phase9BGyroMouseStickBlenderTests
    {
        private static GyroMouseStickInfo Info(GyroMouseStickInfo.OutputStick stick, GyroMouseStickInfo.OutputStickAxes axes)
        {
            return new GyroMouseStickInfo { outputStick = stick, outputStickDir = axes };
        }

        [Fact]
        public void RightStick_TakesTheGyroValue_WhenTheNormalStickIsCentered()
        {
            var state = new DS4State();
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.RightStick, GyroMouseStickInfo.OutputStickAxes.XY), 200, 30, state, data);

            Assert.Equal(200, state.RX);
            Assert.Equal(30, state.RY);
            Assert.Equal(200, data.RX);
            Assert.Equal(30, data.RY);
            Assert.True(data.dirty);
            Assert.Equal(128, state.LX);   // 出力でないスティックは触らない
        }

        [Fact]
        public void TakesTheValueFartherFromCenter_PerAxis()
        {
            var state = new DS4State { RX = 250, RY = 120 };   // X: 通常の方が遠い（122 > 72）。Y: ジャイロの方が遠い（98 > 8）
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.RightStick, GyroMouseStickInfo.OutputStickAxes.XY), 200, 30, state, data);

            Assert.Equal(250, state.RX);
            Assert.Equal(30, state.RY);
            Assert.Equal(250, data.RX);
            Assert.Equal(30, data.RY);
        }

        [Fact]
        public void LeftStick_WritesTheLeftStickAndItsData()
        {
            var state = new DS4State();
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.LeftStick, GyroMouseStickInfo.OutputStickAxes.XY), 10, 240, state, data);

            Assert.Equal(10, state.LX);
            Assert.Equal(240, state.LY);
            Assert.Equal(10, data.LX);
            Assert.Equal(240, data.LY);
            Assert.True(data.dirty);
            Assert.Equal(128, state.RX);
        }

        [Fact]
        public void AxesX_LeavesTheVerticalAxisAlone()
        {
            var state = new DS4State();
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.RightStick, GyroMouseStickInfo.OutputStickAxes.X), 200, 30, state, data);

            Assert.Equal(200, state.RX);
            Assert.Equal(128, state.RY);
        }

        [Fact]
        public void AxesY_LeavesTheHorizontalAxisAlone()
        {
            var state = new DS4State();
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.RightStick, GyroMouseStickInfo.OutputStickAxes.Y), 200, 30, state, data);

            Assert.Equal(128, state.RX);
            Assert.Equal(30, state.RY);
        }

        [Fact]
        public void CenteredGyro_ChangesNothingAndIsNotDirty()
        {
            var state = new DS4State { RX = 77, RY = 99 };
            var data = new PostMapStickData();
            new GyroMouseStickBlender().Blend(Info(GyroMouseStickInfo.OutputStick.RightStick, GyroMouseStickInfo.OutputStickAxes.XY), 128, 128, state, data);

            Assert.Equal(77, state.RX);
            Assert.Equal(99, state.RY);
            Assert.False(data.dirty);
        }
    }
}
