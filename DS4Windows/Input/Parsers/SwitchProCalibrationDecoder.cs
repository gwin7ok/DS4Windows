using System;
using DS4Windows.Input.Devices;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// Switch Pro の校正データ（SPI フラッシュを読んだ応答のバイト列）の解釈。状態を持たない。
    /// 読み出しのやりとり（サブコマンドの送受信、ユーザーの校正か工場の校正かの選択）は、デバイスに残す。
    /// 応答の先頭 20 バイトはヘッダーで、データはその後ろから始まる。
    /// </summary>
    public static class SwitchProCalibrationDecoder
    {
        /// <summary>応答のバイト列で、SPI のデータが始まる位置。</summary>
        public const int SpiResponseOffset = 20;

        private const double StickAxisMaxCutoff = 0.96;
        private const double StickAxisMinCutoff = 1.04;

        /// <summary>ユーザーの校正が保存されているか（2 バイトの印 0xB2 0xA1）。</summary>
        public static bool HasUserCalibrationMark(ReadOnlySpan<byte> response)
        {
            return response[SpiResponseOffset] == 0xB2 && response[SpiResponseOffset + 1] == 0xA1;
        }

        // 12 ビットの値 2 つが 3 バイトに詰められている（下位から X、Y）
        private static ushort ReadX(ReadOnlySpan<byte> response, int pos)
        {
            return (ushort)(((response[pos + 1 + SpiResponseOffset] << 8) & 0xF00) | response[pos + SpiResponseOffset]);
        }

        private static ushort ReadY(ReadOnlySpan<byte> response, int pos)
        {
            return (ushort)((response[pos + 2 + SpiResponseOffset] << 4) | (response[pos + 1 + SpiResponseOffset] >> 4));
        }

        /// <summary>
        /// 左スティックの校正値を読む。並びは、上限（X、Y）、中心（X、Y）、下限（X、Y）の順に、<paramref name="calib"/>［0〜5］へ。
        /// </summary>
        public static void ReadLeftStick(ReadOnlySpan<byte> response, ushort[] calib)
        {
            calib[0] = ReadX(response, 0);   // X Axis Max above center
            calib[1] = ReadY(response, 0);   // Y Axis Max above center
            calib[2] = ReadX(response, 3);   // X Axis Center
            calib[3] = ReadY(response, 3);   // Y Axis Center
            calib[4] = ReadX(response, 6);   // X Axis Min below center
            calib[5] = ReadY(response, 6);   // Y Axis Min below center
        }

        /// <summary>
        /// 右スティックの校正値を読む。応答の並びは、中心、下限、上限の順（左と違う）。<paramref name="calib"/> へは、左と同じ並び（上限、中心、下限）で入れる。
        /// </summary>
        public static void ReadRightStick(ReadOnlySpan<byte> response, ushort[] calib)
        {
            calib[2] = ReadX(response, 0);   // X Axis Center
            calib[3] = ReadY(response, 0);   // Y Axis Center
            calib[4] = ReadX(response, 3);   // X Axis Min below center
            calib[5] = ReadY(response, 3);   // Y Axis Min below center
            calib[0] = ReadX(response, 6);   // X Axis Max above center
            calib[1] = ReadY(response, 6);   // Y Axis Max above center
        }

        /// <summary>
        /// 校正値から、軸ごとの最大・中間・最小を作る。ユーザーの校正は中心をそのまま使う。
        /// 工場の校正は、端の遊びを避けるために範囲を内側へ詰め（最大 × 0.96、最小 × 1.04）、中間は詰めた範囲の真ん中にする。
        /// </summary>
        public static void BuildStickAxes(ushort[] calib, bool userCalibration,
            ref SwitchProDevice.StickAxisData x, ref SwitchProDevice.StickAxisData y)
        {
            if (userCalibration)
            {
                x.max = (ushort)(calib[0] + calib[2]);
                x.mid = calib[2];
                x.min = (ushort)(calib[2] - calib[4]);

                y.max = (ushort)(calib[1] + calib[3]);
                y.mid = calib[3];
                y.min = (ushort)(calib[3] - calib[5]);
            }
            else
            {
                x.max = (ushort)((calib[0] + calib[2]) * StickAxisMaxCutoff);
                x.min = (ushort)((calib[2] - calib[4]) * StickAxisMinCutoff);
                x.mid = (ushort)((x.max - x.min) / 2.0 + x.min);

                y.max = (ushort)((calib[1] + calib[3]) * StickAxisMaxCutoff);
                y.min = (ushort)((calib[3] - calib[5]) * StickAxisMinCutoff);
                y.mid = (ushort)((y.max - y.min) / 2.0 + y.min);
            }
        }

        private static short ReadShort(ReadOnlySpan<byte> response, int pos)
        {
            return (short)((response[pos + 1 + SpiResponseOffset] << 8) & 0xFF00 | response[pos + SpiResponseOffset]);
        }

        /// <summary>
        /// 加速度・ジャイロの校正値（中立、感度）と、そこから作る係数を読む。配列の添字は X・Y・Z（ジャイロは Yaw・Pitch・Roll）。
        /// 加速度の係数は 4 ÷ (感度 − 中立)、ジャイロの係数は 936 ÷ (感度 − バイアス)。
        /// </summary>
        public static void ReadImu(ReadOnlySpan<byte> response,
            short[] accelNeutral, short[] accelSens, double[] accelCoeff,
            short[] gyroBias, short[] gyroSens, double[] gyroCoeff)
        {
            const int X = SwitchProDevice.IMU_XAXIS_IDX;
            const int Y = SwitchProDevice.IMU_YAXIS_IDX;
            const int Z = SwitchProDevice.IMU_ZAXIS_IDX;
            const int Yaw = SwitchProDevice.IMU_YAW_IDX;
            const int Pitch = SwitchProDevice.IMU_PITCH_IDX;
            const int Roll = SwitchProDevice.IMU_ROLL_IDX;

            accelNeutral[X] = ReadShort(response, 2);   // Accel X Offset
            accelNeutral[Y] = ReadShort(response, 0);   // Accel Y Offset
            accelNeutral[Z] = ReadShort(response, 4);   // Accel Z Offset

            accelSens[X] = ReadShort(response, 8);      // Accel X Sens
            accelSens[Y] = ReadShort(response, 6);      // Accel Y Sens
            accelSens[Z] = ReadShort(response, 10);     // Accel Z Sens

            accelCoeff[X] = 1.0 / (accelSens[X] - accelNeutral[X]) * 4.0;
            accelCoeff[Y] = 1.0 / (accelSens[Y] - accelNeutral[Y]) * 4.0;
            accelCoeff[Z] = 1.0 / (accelSens[Z] - accelNeutral[Z]) * 4.0;

            gyroBias[0] = ReadShort(response, 16);      // Gyro Yaw Offset
            gyroBias[1] = ReadShort(response, 14);      // Gyro Pitch Offset
            gyroBias[2] = ReadShort(response, 12);      // Gyro Roll Offset

            gyroSens[Yaw] = ReadShort(response, 22);    // Gyro Yaw Sens
            gyroSens[Pitch] = ReadShort(response, 20);  // Gyro Pitch Sens
            gyroSens[Roll] = ReadShort(response, 18);   // Gyro Roll Sens

            gyroCoeff[Yaw] = 936.0 / (gyroSens[Yaw] - gyroBias[Yaw]);
            gyroCoeff[Pitch] = 936.0 / (gyroSens[Pitch] - gyroBias[Pitch]);
            gyroCoeff[Roll] = 936.0 / (gyroSens[Roll] - gyroBias[Roll]);
        }
    }
}
