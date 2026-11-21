#nullable enable

namespace DS4Windows.Input.Reports
{
    /// <summary>
    /// DS4 の出力レポート（振動・ライトバー）のバイト列の組み立て。状態を持たず、渡されたバッファへ書くだけ（割り当てなし）。
    /// 変化の判定、CRC32 の付与、書き込みは、組み立ての外（デバイス）で行う。
    /// </summary>
    public static class DualShock4OutputReportBuilder
    {
        /// <summary>Bluetooth のレポート（0x11／0x15）の先頭 13 バイトを書く。</summary>
        public static void FillBluetooth(byte[] buffer, byte reportType, int btPollRate, byte featuresByte, in DS4HapticState hap)
        {
            buffer[0] = reportType;
            buffer[1] = (byte)(0xC0 | btPollRate); // 入力レポートの送信間隔

            // ヘッドホン音量 L（0x10）・R（0x20）、マイク音量（0x40）、スピーカー音量（0x80）
            // 振動（0x01）、ライトバー（0x02）、点滅（0x04）。既定は 0x07
            buffer[3] = featuresByte;
            buffer[4] = 0x04;

            buffer[6] = hap.rumbleState.RumbleMotorStrengthRightLightFast; // 速いモーター
            buffer[7] = hap.rumbleState.RumbleMotorStrengthLeftHeavySlow; // 遅いモーター
            buffer[8] = hap.lightbarState.LightBarColor.red;
            buffer[9] = hap.lightbarState.LightBarColor.green;
            buffer[10] = hap.lightbarState.LightBarColor.blue;
            buffer[11] = hap.lightbarState.LightBarFlashDurationOn;
            buffer[12] = hap.lightbarState.LightBarFlashDurationOff;
        }

        /// <summary>USB のレポート（0x05）の先頭 11 バイトを書く。</summary>
        public static void FillUsb(byte[] buffer, byte featuresByte, in DS4HapticState hap)
        {
            buffer[0] = 0x05;
            buffer[1] = featuresByte;
            buffer[2] = 0x04;
            buffer[4] = hap.rumbleState.RumbleMotorStrengthRightLightFast;
            buffer[5] = hap.rumbleState.RumbleMotorStrengthLeftHeavySlow;
            buffer[6] = hap.lightbarState.LightBarColor.red;
            buffer[7] = hap.lightbarState.LightBarColor.green;
            buffer[8] = hap.lightbarState.LightBarColor.blue;
            buffer[9] = hap.lightbarState.LightBarFlashDurationOn;
            buffer[10] = hap.lightbarState.LightBarFlashDurationOff;
        }
    }
}
