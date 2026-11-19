#nullable enable

using DS4Windows.Core.Model;
using DS4Windows.Input.Devices;

namespace DS4Windows.Input.Reports
{
    /// <summary>
    /// DualSense の出力レポートへ書き込む値のまとめ（<see cref="DualSenseOutputReportBuilder"/> の入力）。
    /// 値の入れ物であり、組み立ての状態は持たない。
    /// </summary>
    public readonly struct DualSenseOutputState
    {
        /// <summary>振動の通常のエミュレーションを使うか（モーターの有効のフラグと、強さの書き込みを決める）。</summary>
        public bool UseRumble { get; init; }

        /// <summary>改良された（正確な）振動のエミュレーションを使うか。</summary>
        public bool UseAccurateRumble { get; init; }

        /// <summary>高周波（軽く速い）のモーターの強さ。</summary>
        public byte RumbleRightLightFast { get; init; }

        /// <summary>低周波（重く遅い）のモーターの強さ。</summary>
        public byte RumbleLeftHeavySlow { get; init; }

        public DS4Color LightbarColor { get; init; }

        /// <summary>触覚（振動全体）の強さ。</summary>
        public byte HapticPowerLevel { get; init; }

        /// <summary>マイクのミュート LED のバイト。</summary>
        public byte MuteLedByte { get; init; }

        /// <summary>プレイヤー LED の点灯の並び（ビットマスク）。</summary>
        public byte PlayerLedMask { get; init; }

        public DualSenseDevice.TriggerEffectData R2Effect { get; init; }

        public DualSenseDevice.TriggerEffectData L2Effect { get; init; }
    }
}
