using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="ControlActivityReader"/>（旧 <c>Mapping.GetBoolMappingExternal</c>）の値の特性化。
    /// ボタン、トリガー（100 を超える）、スティックの方向（中心から 55 を超える）、ジャイロ（Controls モードのときだけ。しきい値 67）の境界を調べる。
    /// タッチパッド・スワイプは <c>Mouse</c> を作れないため、<c>tp == null</c>（常に false）だけを調べる。
    /// </summary>
    public class Phase9BControlActivityReaderTests
    {
        private const int Device = 7;   // 共有ホストの設定を一時的に書き換えるため、ほかのテストが使わない添字を使う

        private static readonly ControlActivityReader Reader = new ControlActivityReader();

        private static bool Active(DS4Controls control, DS4State state, DS4StateExposed exposed = null)
        {
            return Reader.IsActive(TestHost.GetService<IProfileSettingsService>(), Device, control, state, exposed ?? new DS4StateExposed(state), null);
        }

        [Fact]
        public void Buttons_FollowTheState()
        {
            var state = new DS4State { Cross = true, Square = false, L1 = true, DpadUp = true, PS = true, Share = false };
            Assert.True(Active(DS4Controls.Cross, state));
            Assert.False(Active(DS4Controls.Square, state));
            Assert.True(Active(DS4Controls.L1, state));
            Assert.True(Active(DS4Controls.DpadUp, state));
            Assert.True(Active(DS4Controls.PS, state));
            Assert.False(Active(DS4Controls.Share, state));
        }

        [Theory]
        [InlineData(100, false)]
        [InlineData(101, true)]
        public void Triggers_AreActiveAboveOneHundred(byte value, bool expected)
        {
            var state = new DS4State { L2 = value, R2 = value };
            Assert.Equal(expected, Active(DS4Controls.L2, state));
            Assert.Equal(expected, Active(DS4Controls.R2, state));
        }

        [Theory]
        [InlineData(73, false, false)]   // 128 - 55 = 73。未満で負方向が有効
        [InlineData(72, true, false)]
        [InlineData(183, false, false)]   // 128 + 55 = 183。超えると正方向が有効
        [InlineData(184, false, true)]
        public void StickDirections_UseAThresholdOfFiftyFiveFromCenter(byte axis, bool negative, bool positive)
        {
            var state = new DS4State { LX = axis, LY = axis, RX = axis, RY = axis };
            Assert.Equal(negative, Active(DS4Controls.LXNeg, state));
            Assert.Equal(negative, Active(DS4Controls.LYNeg, state));
            Assert.Equal(negative, Active(DS4Controls.RXNeg, state));
            Assert.Equal(negative, Active(DS4Controls.RYNeg, state));
            Assert.Equal(positive, Active(DS4Controls.LXPos, state));
            Assert.Equal(positive, Active(DS4Controls.LYPos, state));
            Assert.Equal(positive, Active(DS4Controls.RXPos, state));
            Assert.Equal(positive, Active(DS4Controls.RYPos, state));
        }

        [Fact]
        public void TouchAndSwipe_AreInactive_WhenThereIsNoTouchpad()
        {
            var state = new DS4State();
            Assert.False(Active(DS4Controls.TouchLeft, state));
            Assert.False(Active(DS4Controls.TouchMulti, state));
            Assert.False(Active(DS4Controls.SwipeUp, state));
            Assert.False(Active(DS4Controls.SwipeRight, state));
        }

        [Fact]
        public void Gyro_IsInactive_UnlessTheOutputModeIsControls()
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            GyroOutMode savedMode = settings.GyroOutputMode[Device];
            double savedSx = settings.SXSens[Device];
            try
            {
                settings.GyroOutputMode[Device] = GyroOutMode.None;
                settings.SXSens[Device] = 1.0;
                var state = new DS4State();
                state.Motion.accelX = -500;
                Assert.False(Active(DS4Controls.GyroXPos, state));
            }
            finally
            {
                settings.GyroOutputMode[Device] = savedMode;
                settings.SXSens[Device] = savedSx;
            }
        }

        [Theory]
        [InlineData(-68, true)]    // 1.0 * -(-68) = 68 > 67
        [InlineData(-67, false)]
        public void GyroXPos_UsesAThresholdOfSixtySeven_WhenTheOutputModeIsControls(int accelX, bool expected)
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            GyroOutMode savedMode = settings.GyroOutputMode[Device];
            double savedSx = settings.SXSens[Device];
            double savedSz = settings.SZSens[Device];
            try
            {
                settings.GyroOutputMode[Device] = GyroOutMode.Controls;
                settings.SXSens[Device] = 1.0;
                settings.SZSens[Device] = 1.0;
                var state = new DS4State();
                state.Motion.accelX = accelX;
                state.Motion.accelZ = -accelX;   // GyroZPos: SZ * AccelZ > 67
                Assert.Equal(expected, Active(DS4Controls.GyroXPos, state));
                Assert.Equal(expected, Active(DS4Controls.GyroZPos, state));
                Assert.False(Active(DS4Controls.GyroXNeg, state));
            }
            finally
            {
                settings.GyroOutputMode[Device] = savedMode;
                settings.SXSens[Device] = savedSx;
                settings.SZSens[Device] = savedSz;
            }
        }
    }
}
