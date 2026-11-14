using DS4Windows.Input.Devices;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>Switch Pro の校正データ（SPI の応答のバイト列）の解釈。値は、バイトの並び（12 ビット 2 つ → 3 バイト）から手で求めた。</summary>
    public class SwitchProCalibrationDecoderTests
    {
        private const int Off = SwitchProCalibrationDecoder.SpiResponseOffset;

        // 12 ビットの X・Y を 3 バイトへ詰める（下位から X の 8 ビット、X の上位 4 ビット＋Y の下位 4 ビット、Y の上位 8 ビット）
        private static void Pack(byte[] r, int pos, int x, int y)
        {
            r[Off + pos] = (byte)(x & 0xFF);
            r[Off + pos + 1] = (byte)(((x >> 8) & 0x0F) | ((y & 0x0F) << 4));
            r[Off + pos + 2] = (byte)((y >> 4) & 0xFF);
        }

        [Fact]
        public void TheUserCalibrationMark_IsB2A1AtTheStartOfTheData()
        {
            var r = new byte[Off + 4];
            Assert.False(SwitchProCalibrationDecoder.HasUserCalibrationMark(r));
            r[Off] = 0xB2;
            r[Off + 1] = 0xA1;
            Assert.True(SwitchProCalibrationDecoder.HasUserCalibrationMark(r));
        }

        [Fact]
        public void TheLeftStick_IsReadAsMaxCenterMin_AndTheRightStickAsCenterMinMax_IntoTheSameOrder()
        {
            // 上限 (0x5A0, 0x6B1)、中心 (0x800, 0x7FF)、下限 (0x4C3, 0x5D2)
            var left = new byte[Off + 9];
            Pack(left, 0, 0x5A0, 0x6B1);
            Pack(left, 3, 0x800, 0x7FF);
            Pack(left, 6, 0x4C3, 0x5D2);
            var calib = new ushort[6];
            SwitchProCalibrationDecoder.ReadLeftStick(left, calib);
            Assert.Equal(new ushort[] { 0x5A0, 0x6B1, 0x800, 0x7FF, 0x4C3, 0x5D2 }, calib);

            // 右は、応答の並びが 中心、下限、上限
            var right = new byte[Off + 9];
            Pack(right, 0, 0x800, 0x7FF);
            Pack(right, 3, 0x4C3, 0x5D2);
            Pack(right, 6, 0x5A0, 0x6B1);
            var rcalib = new ushort[6];
            SwitchProCalibrationDecoder.ReadRightStick(right, rcalib);
            Assert.Equal(calib, rcalib);
        }

        [Fact]
        public void TheUserCalibration_UsesTheCenterAsIs()
        {
            var calib = new ushort[] { 1000, 1100, 2000, 2100, 700, 800 };
            var x = new SwitchProDevice.StickAxisData();
            var y = new SwitchProDevice.StickAxisData();
            SwitchProCalibrationDecoder.BuildStickAxes(calib, true, ref x, ref y);
            Assert.Equal((3000, 2000, 1300), ((int)x.max, (int)x.mid, (int)x.min));
            Assert.Equal((3200, 2100, 1300), ((int)y.max, (int)y.mid, (int)y.min));
        }

        [Fact]
        public void TheFactoryCalibration_NarrowsTheRange_AndPutsTheMiddleInTheMiddle()
        {
            var calib = new ushort[] { 1000, 1100, 2000, 2100, 700, 800 };
            var x = new SwitchProDevice.StickAxisData();
            var y = new SwitchProDevice.StickAxisData();
            SwitchProCalibrationDecoder.BuildStickAxes(calib, false, ref x, ref y);
            // 最大 = (1000 + 2000) × 0.96 = 2880、最小 = (2000 − 700) × 1.04 = 1352、中間 = (2880 − 1352) / 2 + 1352 = 2116
            Assert.Equal((2880, 2116, 1352), ((int)x.max, (int)x.mid, (int)x.min));
            // 最大 = (1100 + 2100) × 0.96 = 3072、最小 = (2100 − 800) × 1.04 = 1352、中間 = (3072 − 1352) / 2 + 1352 = 2212
            Assert.Equal((3072, 2212, 1352), ((int)y.max, (int)y.mid, (int)y.min));
        }

        [Fact]
        public void TheImuCalibration_ReadsOffsetsAndSensitivities_AndBuildsTheCoefficients()
        {
            var r = new byte[Off + 24];
            void Put(int pos, short v) { r[Off + pos] = (byte)v; r[Off + pos + 1] = (byte)(v >> 8); }
            Put(2, 100);    // Accel X 中立
            Put(0, 200);    // Accel Y 中立
            Put(4, -300);   // Accel Z 中立
            Put(8, 4100);   // Accel X 感度
            Put(6, 8200);   // Accel Y 感度
            Put(10, 3700);  // Accel Z 感度
            Put(16, 10);    // Gyro Yaw バイアス
            Put(14, -20);   // Gyro Pitch バイアス
            Put(12, 30);    // Gyro Roll バイアス
            Put(22, 946);   // Gyro Yaw 感度
            Put(20, 916);   // Gyro Pitch 感度
            Put(18, 1030);  // Gyro Roll 感度

            var an = new short[3]; var asn = new short[3]; var ac = new double[3];
            var gb = new short[3]; var gs = new short[3]; var gc = new double[3];
            SwitchProCalibrationDecoder.ReadImu(r, an, asn, ac, gb, gs, gc);

            Assert.Equal(new short[] { 100, 200, -300 }, an);
            Assert.Equal(new short[] { 4100, 8200, 3700 }, asn);
            Assert.Equal(4.0 / 4000, ac[0], 12);
            Assert.Equal(4.0 / 8000, ac[1], 12);
            Assert.Equal(4.0 / 4000, ac[2], 12);
            Assert.Equal(new short[] { 10, -20, 30 }, gb);
            Assert.Equal(new short[] { 946, 916, 1030 }, gs);
            Assert.Equal(936.0 / 936, gc[0], 12);
            Assert.Equal(936.0 / 936, gc[1], 12);
            Assert.Equal(936.0 / 1000, gc[2], 12);
        }
    }
}
