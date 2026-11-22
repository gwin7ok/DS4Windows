using DS4Windows.Input.Devices;

namespace DS4Windows.Input.Reports
{
    /// <summary>
    /// DualSense の出力レポートのバイト列の組み立て（USB と Bluetooth）。状態を持たず、渡されたバッファへ書くだけ（割り当てなし）。
    /// 2 つの書式は、Bluetooth が先頭にレポート ID（0x31）・連番とタグ（[1]）・固定値 0x10（[2]）の 3 バイトを足すだけで、以降の並びは同じ（USB の添字 + 2。
    /// Linux カーネルの hid-playstation.c と SDL の SDL_hidapi_ps5.c に合わせる）。
    /// 変化の判定、待機の計時、Bluetooth の CRC32 の付与、書き込みは、組み立ての外（デバイス）で行う。
    /// 音量（USB の 5〜8）は今は書かない（0 のまま）。
    /// </summary>
    public static class DualSenseOutputReportBuilder
    {
        private const int BluetoothReportLength = 78;
        private const byte ReportIdUsb = 0x02;
        private const byte ReportIdBluetooth = 0x31;
        private const byte ReportIdData = 0x02;

        /// <summary>Bluetooth の効果の構造体の直前にある、決まった値。</summary>
        private const byte BluetoothDataTag = 0x10;

        /// <summary>
        /// 出力レポートを組み立てる。振動の強さは、通常か正確なエミュレーションのどちらかが有効なときだけ書く（どちらも無効なら、バッファの値を触らない）。
        /// </summary>
        /// <param name="report">USB は 48 バイト以上、Bluetooth は 78 バイト（CRC の 4 バイトを含む）のバッファ。</param>
        public static void Fill(byte[] report, bool bluetooth, in DualSenseOutputState state)
        {
            int o = 0;   // USB の添字に足す値
            if (bluetooth)
            {
                report[0] = ReportIdBluetooth;
                report[1] = ReportIdData;
                report[2] = BluetoothDataTag;
                o = 2;
            }
            else
            {
                report[0] = ReportIdUsb;
            }

            // 0x01 主モーター（0x02 と組）、0x02 主モーター（0x01 と組）、0x04 右トリガーのモーター、0x08 左トリガーのモーター
            report[1 + o] = state.UseRumble ? (byte)0x0F : (byte)0x0C;

            // 0x01 マイク LED、0x04 タッチパッド脇の LED、0x10 プレイヤー LED、0x40 モーター・効果全体の強さの調整
            report[2 + o] = 0x55;

            if (state.UseRumble || state.UseAccurateRumble)
            {
                report[3 + o] = state.RumbleRightLightFast;
                report[4 + o] = state.RumbleLeftHeavySlow;
            }

            report[9 + o] = state.MuteLedByte;

            FillTrigger(report, 11 + o, state.R2Effect);
            FillTrigger(report, 22 + o, state.L2Effect);

            report[37 + o] = state.HapticPowerLevel;

            // プレイヤー LED と、改良された振動のフラグ
            report[39 + o] = state.UseAccurateRumble ? (byte)0x06 : (byte)0x02;

            report[42 + o] = 0x02;
            report[43 + o] = 0x02;
            report[44 + o] = state.PlayerLedMask;

            report[45 + o] = state.LightbarColor.red;
            report[46 + o] = state.LightbarColor.green;
            report[47 + o] = state.LightbarColor.blue;
        }

        // トリガーのモーター 1 つぶん。モード（0＝抵抗なし、1＝連続、2＝区間。0x20・0x04 は追加の効果）、抵抗の開始位置、力、範囲の力、
        // 離れ際・中ほど・押し込みの強さ（補助モード 4・20 が要る）、作動の周波数
        private static void FillTrigger(byte[] report, int pos, DualSenseDevice.TriggerEffectData effect)
        {
            report[pos] = effect.triggerMotorMode;
            report[pos + 1] = effect.triggerStartResistance;
            report[pos + 2] = effect.triggerEffectForce;
            report[pos + 3] = effect.triggerRangeForce;
            report[pos + 4] = effect.triggerNearReleaseStrength;
            report[pos + 5] = effect.triggerNearMiddleStrength;
            report[pos + 6] = effect.triggerPressedStrength;
            report[pos + 9] = effect.triggerActuationFrequency;
        }

        /// <summary>
        /// Bluetooth のレポートの末尾 4 バイト（74〜77）へ CRC32 を書く。<paramref name="compute"/> が false なら 0 を書く
        /// （出力しないフレームで、前回の値を残さないため）。
        /// </summary>
        public static void ApplyBluetoothCrc(byte[] report, byte[] head, bool compute)
        {
            uint calcCrc32 = 0;
            if (compute)
            {
                int crcOffset = 0;
                int crcpos = BluetoothReportLength - 4;
                calcCrc32 = ~Crc32Algorithm.Compute(head);
                calcCrc32 = ~Crc32Algorithm.CalculateFasterBT78Hash(ref calcCrc32, ref report, ref crcOffset, ref crcpos);
            }

            report[74] = (byte)calcCrc32;
            report[75] = (byte)(calcCrc32 >> 8);
            report[76] = (byte)(calcCrc32 >> 16);
            report[77] = (byte)(calcCrc32 >> 24);
        }
    }
}
