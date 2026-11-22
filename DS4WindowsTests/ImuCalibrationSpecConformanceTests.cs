#nullable enable
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ジャイロ・加速度計の校正を、公開された別実装の仕様と突き合わせる。
    /// DS4・DualSense（<c>DS4SixAxis</c>）は Linux カーネル <c>hid-playstation.c</c> と SDL の <c>SDL_hidapi_ps4.c</c>・<c>SDL_hidapi_ps5.c</c>、
    /// Switch Pro（<c>SwitchProCalibrationDecoder</c>）は <c>hid-nintendo.c</c> と <c>SDL_hidapi_switch.c</c>（2026-11-22 に取得）。
    /// </summary>
    public class ImuCalibrationSpecConformanceTests
    {
        private const short PitchBias = 10, YawBias = -20, RollBias = 30;
        private const short Plus = 8010, Minus = -8000;   // ジャイロの + と − の校正点（バイアス 10 の軸で、差 16010）
        private const short Speed = 540;                  // gyro_speed_plus・gyro_speed_minus（度毎秒）
        private const short AccPlus = 8200, AccMinus = -8100;

        private static void Put(byte[] report, int offset, short value)
        {
            report[offset] = (byte)value;
            report[offset + 1] = (byte)(value >> 8);
        }

        // フィーチャーレポート 0x02（USB）・0x05（Bluetooth）。位置は SDL とカーネルの共通: バイアス 1・3・5、速度 19・21、加速度 23〜33。
        // + と − の並びだけが違う（USB: ピッチ＋・ピッチ−・ヨー＋…、Bluetooth: ピッチ＋・ヨー＋・ロール＋・ピッチ−…）
        private static byte[] Feature(bool usbLayout)
        {
            var r = new byte[41];
            r[0] = usbLayout ? (byte)0x02 : (byte)0x05;
            Put(r, 1, PitchBias);
            Put(r, 3, YawBias);
            Put(r, 5, RollBias);
            short[] plus = { (short)(Plus + PitchBias - 10), (short)(Plus + YawBias - 10), (short)(Plus + RollBias - 10) };
            short[] minus = { (short)(Minus + PitchBias - 10), (short)(Minus + YawBias - 10), (short)(Minus + RollBias - 10) };
            if (usbLayout)
            {
                for (int a = 0; a < 3; a++)
                {
                    Put(r, 7 + a * 4, plus[a]);
                    Put(r, 9 + a * 4, minus[a]);
                }
            }
            else
            {
                for (int a = 0; a < 3; a++)
                {
                    Put(r, 7 + a * 2, plus[a]);
                    Put(r, 13 + a * 2, minus[a]);
                }
            }

            Put(r, 19, Speed);
            Put(r, 21, Speed);
            for (int a = 0; a < 3; a++)
            {
                Put(r, 23 + a * 4, AccPlus);
                Put(r, 25 + a * 4, AccMinus);
            }

            return r;
        }

        private static SixAxis Motion(byte[] feature, bool alt, int yaw, int pitch, int roll, int ax, int ay, int az)
        {
            var sixAxis = new DS4SixAxis();
            sixAxis.StopContinuousCalibration();
            sixAxis.SetCalibrationData(ref feature, alt);
            SixAxis? seen = null;
            sixAxis.SixAccelMoved += (sender, args) => { };
            var state = new ControllerState();
            sixAxis.HandleSixaxisVals(yaw, pitch, roll, ax, ay, az, state, 0.004);
            seen = state.Motion;
            Assert.NotNull(seen);
            return seen!;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TheGyro_AtThePlusCalibrationPoint_ReadsTheSpeedPlus_OnBothFeatureLayouts(bool usbLayout)
        {
            // 校正: 角速度 = (生の値 − バイアス) × (speed_plus + speed_minus) ÷ (plus と minus の幅)。plus の点は、およそ speed_plus（540 度毎秒）になる。
            // 内部の単位は 1/16 度毎秒（SDL の gyro_denominator＝16）
            byte[] feature = Feature(usbLayout);
            SixAxis m = Motion(feature, alt: usbLayout, yaw: Plus + YawBias - 10, pitch: Plus + PitchBias - 10, roll: Plus + RollBias - 10, 0, 0, 0);

            Assert.InRange(m.angVelPitch, Speed - 2.0, Speed + 2.0);
            Assert.InRange(-m.angVelYaw, Speed - 2.0, Speed + 2.0);   // 符号は、Populate がヨーとロールを反転して持つ
            Assert.InRange(-m.angVelRoll, Speed - 2.0, Speed + 2.0);
        }

        [Fact]
        public void TheGyro_AtTheBias_ReadsZero()
        {
            SixAxis m = Motion(Feature(true), alt: true, yaw: YawBias, pitch: PitchBias, roll: RollBias, 0, 0, 0);

            Assert.Equal(0.0, m.angVelPitch, 3);
            Assert.Equal(0.0, m.angVelYaw, 3);
            Assert.Equal(0.0, m.angVelRoll, 3);
        }

        [Fact]
        public void TheAccel_AtThePlusAndMinusPoints_ReadsPlusAndMinusOneG()
        {
            // 加速度: bias = plus − 幅 ÷ 2、感度 = 2 × 8192 ÷ 幅。plus の点が +1 G、minus の点が −1 G（Populate は X・Y の符号を反転して持つ）
            SixAxis plus = Motion(Feature(true), true, 0, 0, 0, AccPlus, AccPlus, AccPlus);
            SixAxis minus = Motion(Feature(true), true, 0, 0, 0, AccMinus, AccMinus, AccMinus);

            Assert.Equal(-1.0, plus.accelXG, 2);
            Assert.Equal(-1.0, plus.accelYG, 2);
            Assert.Equal(1.0, plus.accelZG, 2);
            Assert.Equal(1.0, minus.accelXG, 2);
            Assert.Equal(-1.0, minus.accelZG, 2);
        }

        // ---- Switch Pro: IMU の校正（SPI 0x6020〜）。カーネルの配置: accel_offset[i]＝2i、accel_scale[i]＝6+2i、gyro_offset[i]＝12+2i、gyro_scale[i]＝18+2i（i＝X・Y・Z） ----
        [Fact]
        public void SwitchPro_TheImuCalibration_IsReadFromTheKernelLayout_WithTheAxesRenamedToTheDs4Convention()
        {
            var response = new byte[362];
            const int o = SwitchProCalibrationDecoder.SpiResponseOffset;
            // カーネルの X・Y・Z に別々の値を入れる
            short[] accelOffset = { 100, 200, 300 }, accelScale = { 4100, 4200, 4300 }, gyroOffset = { 10, 20, 30 }, gyroScale = { 13000, 13100, 13200 };
            for (int i = 0; i < 3; i++)
            {
                Put(response, o + 2 * i, accelOffset[i]);
                Put(response, o + 6 + 2 * i, accelScale[i]);
                Put(response, o + 12 + 2 * i, gyroOffset[i]);
                Put(response, o + 18 + 2 * i, gyroScale[i]);
            }

            var accelNeutral = new short[3];
            var accelSens = new short[3];
            var accelCoeff = new double[3];
            var gyroBias = new short[3];
            var gyroSens = new short[3];
            var gyroCoeff = new double[3];
            SwitchProCalibrationDecoder.ReadImu(response, accelNeutral, accelSens, accelCoeff, gyroBias, gyroSens, gyroCoeff);

            // このアプリの X・Y・Z（DS4 の向き）は、カーネルの Y・X・Z。ジャイロのヨー・ピッチ・ロールは、カーネルの Z・Y・X
            Assert.Equal(new short[] { 200, 100, 300 }, accelNeutral);
            Assert.Equal(new short[] { 4200, 4100, 4300 }, accelSens);
            Assert.Equal(new short[] { 30, 20, 10 }, gyroBias);
            Assert.Equal(new short[] { 13200, 13100, 13000 }, gyroSens);

            // 係数: 加速度は 4 ÷ (感度 − 中立)（カーネルの 16384 ÷ 4096 ÷ ...）、ジャイロは 936 ÷ (感度 − バイアス)（SDL の SWITCH_GYRO_SCALE_MULT）
            Assert.Equal(4.0 / (4200 - 200), accelCoeff[0], 9);
            Assert.Equal(4.0 / (4100 - 100), accelCoeff[1], 9);
            Assert.Equal(936.0 / (13200 - 30), gyroCoeff[0], 9);
            Assert.Equal(936.0 / (13000 - 10), gyroCoeff[2], 9);
        }
    }
}
