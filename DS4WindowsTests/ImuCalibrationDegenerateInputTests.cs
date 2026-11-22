#nullable enable
using DS4Windows.Core.Model;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 校正データが壊れている・読めなかったとき（全 0、一部の軸の幅が 0）に、ジャイロ・加速度の値が壊れないことを確かめる。
    /// 読み出しに失敗した機器は、全 0 のレポートを返すことがある。
    /// </summary>
    public class ImuCalibrationDegenerateInputTests
    {
        private static SixAxis Motion(byte[] feature, int yaw, int pitch, int roll, int ax, int ay, int az)
        {
            var sixAxis = new DS4SixAxis();
            sixAxis.StopContinuousCalibration();
            sixAxis.SetCalibrationData(ref feature, true);
            sixAxis.SixAccelMoved += (sender, args) => { };
            var state = new ControllerState();
            sixAxis.HandleSixaxisVals(yaw, pitch, roll, ax, ay, az, state, 0.004);
            return state.Motion;
        }

        private static void Put(byte[] report, int offset, short value)
        {
            report[offset] = (byte)value;
            report[offset + 1] = (byte)(value >> 8);
        }

        [Fact]
        public void AnAllZeroCalibrationReport_LeavesTheRawValuesAsTheyAre()
        {
            SixAxis m = Motion(new byte[41], 100, 200, 300, 4000, 5000, 6000);

            Assert.Equal(-100, m.gyroYawFull);
            Assert.Equal(200, m.gyroPitchFull);
            Assert.Equal(-300, m.gyroRollFull);
            Assert.Equal(-4000, m.accelXFull);
            Assert.Equal(-5000, m.accelYFull);
            Assert.Equal(6000, m.accelZFull);
        }

        [Theory]
        [InlineData(0)]   // X の幅が 0
        [InlineData(1)]   // Y の幅が 0
        public void AnAccelAxisWithAZeroRange_DoesNotProduceAGarbageValue(int zeroAxis)
        {
            // ジャイロは正常、加速度は、zeroAxis の軸だけ plus と minus が同じ（幅 0）、ほかは正常
            var r = new byte[41];
            Put(r, 7, 8000); Put(r, 9, -8000);
            Put(r, 11, 8000); Put(r, 13, -8000);
            Put(r, 15, 8000); Put(r, 17, -8000);
            Put(r, 19, 540); Put(r, 21, 540);
            for (int a = 0; a < 3; a++)
            {
                short plus = (short)(a == zeroAxis ? 100 : 8200);
                short minus = (short)(a == zeroAxis ? 100 : -8100);
                Put(r, 23 + a * 4, plus);
                Put(r, 25 + a * 4, minus);
            }

            SixAxis m = Motion(r, 0, 0, 0, 3000, 3000, 3000);

            // 幅 0 の軸は、校正をかけずに生の値のまま（または 0）にする。極端な値（int の端）にしない
            int zeroValue = zeroAxis == 0 ? m.accelXFull : m.accelYFull;
            Assert.InRange(zeroValue, -20000, 20000);
        }
    }
}
