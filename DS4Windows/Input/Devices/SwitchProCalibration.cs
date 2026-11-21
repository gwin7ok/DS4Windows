#nullable enable

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// Switch Pro の較正の値（スティックの軸の範囲、IMU の補正）。初期化（<see cref="SwitchProSession"/>）が機器から読んで書き、
    /// 読み取りのループ（<see cref="SwitchProDevice"/>）が毎フレーム読む。値は、配列の中身を使い回して更新する（割り当てを増やさない）。
    /// </summary>
    public sealed class SwitchProCalibration
    {
        private SwitchProDevice.StickAxisData _leftStickX;
        private SwitchProDevice.StickAxisData _leftStickY;
        private SwitchProDevice.StickAxisData _rightStickX;
        private SwitchProDevice.StickAxisData _rightStickY;

        public ref SwitchProDevice.StickAxisData LeftStickX => ref _leftStickX;

        public ref SwitchProDevice.StickAxisData LeftStickY => ref _leftStickY;

        public ref SwitchProDevice.StickAxisData RightStickX => ref _rightStickX;

        public ref SwitchProDevice.StickAxisData RightStickY => ref _rightStickY;

        public ushort[] LeftStickCalib { get; } = new ushort[6];

        public ushort[] RightStickCalib { get; } = new ushort[6];

        /// <summary>スティックの中央の補正（機器から読んだ値を反映していない。0 のまま）。</summary>
        public ushort LeftStickOffsetX { get; }

        public ushort LeftStickOffsetY { get; }

        public ushort RightStickOffsetX { get; }

        public ushort RightStickOffsetY { get; }

        public short[] AccelNeutral { get; } = new short[3];

        public short[] AccelSens { get; } = new short[3];

        public double[] AccelCoeff { get; } = new double[3];

        public short[] GyroBias { get; } = new short[3];

        public short[] GyroSens { get; } = new short[3];

        /// <summary>ジャイロの較正の追加の補正（機器から読んだ値を反映していない。0 のまま）。</summary>
        public short[] GyroCalibOffsets { get; } = new short[3];

        public double[] GyroCoeff { get; } = new double[3];

        /// <summary>機器の生の値（12 ビット）を、較正の範囲へ収めて 0〜255 にする。Y は上下を反転する。</summary>
        public byte ScaleLeftX(int raw) => Scale(_leftStickX, raw - LeftStickOffsetX, invert: false);

        public byte ScaleLeftY(int raw) => Scale(_leftStickY, raw - LeftStickOffsetY, invert: true);

        public byte ScaleRightX(int raw) => Scale(_rightStickX, raw - RightStickOffsetX, invert: false);

        public byte ScaleRightY(int raw) => Scale(_rightStickY, raw - RightStickOffsetY, invert: true);

        private static byte Scale(SwitchProDevice.StickAxisData axis, int value, bool invert)
        {
            int clamped = value > axis.max ? axis.max : (value < axis.min ? axis.min : value);
            double ratio = (clamped - axis.min) / (double)(axis.max - axis.min);
            return invert ? (byte)(((ratio - 0.5) * -1.0 + 0.5) * 255) : (byte)(ratio * 255);
        }
    }
}
