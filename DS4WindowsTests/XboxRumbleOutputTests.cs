using DS4Windows.Input;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Xbox の振動。仮想パッドから来る振動の強さ（左＝重く遅い、右＝軽く速い）を、通信路の出力レポート（[0]=ID、[1]=左、[2]=右）へ組み立てる。
    /// 通信路がそれを XInput の振動へ変える（変換は <c>XInputTransportTests</c>）。ライトバーはない。
    /// </summary>
    public class XboxRumbleOutputTests
    {
        [Fact]
        public void TheProtocol_PutsTheTwoMotorsIntoTheOutputReport()
        {
            var state = new DS4HapticState();
            state.rumbleState.RumbleMotorStrengthLeftHeavySlow = 200;
            state.rumbleState.RumbleMotorStrengthRightLightFast = 50;
            var report = new byte[XInputTransport.OutputReportLength];

            Assert.True(new XboxProtocol().TryBuildOutputReport(state, 100, report));

            Assert.Equal(new byte[] { 0, 200, 50 }, report);
        }

        [Fact]
        public void TheProtocol_BuildsAllZeros_ForAnEmptyState_SoStoppingSilencesTheMotors()
        {
            var report = new byte[] { 0, 9, 9 };

            Assert.True(new XboxProtocol().TryBuildOutputReport(new DS4HapticState(), 100, report));

            Assert.Equal(new byte[] { 0, 0, 0 }, report);
        }

        [Fact]
        public void TheProtocol_RefusesAReportThatIsTooShort()
        {
            Assert.False(new XboxProtocol().TryBuildOutputReport(new DS4HapticState(), 100, new byte[2]));
        }
    }
}
