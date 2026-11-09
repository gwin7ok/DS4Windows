using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのトリガー（L2・R2）の設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class TriggerSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public TriggerSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        /// <summary>L2 のデッドゾーンなど（中身を直接書き換える）。</summary>
        public TriggerDeadZoneZInfo L2Mod => _settings.L2ModInfo[_slot];

        public TriggerDeadZoneZInfo R2Mod => _settings.R2ModInfo[_slot];

        public double L2Sens
        {
            get => _settings.L2Sens[_slot];
            set => _settings.L2Sens[_slot] = value;
        }

        public double R2Sens
        {
            get => _settings.R2Sens[_slot];
            set => _settings.R2Sens[_slot] = value;
        }

        /// <summary>二段トリガー・トリガー効果など（中身を直接書き換える）。</summary>
        public TriggerOutputSettings L2Output => _settings.L2OutputSettings[_slot];

        public TriggerOutputSettings R2Output => _settings.R2OutputSettings[_slot];

        public BezierCurve L2Bezier => _settings.L2OutBezierCurveObj[_slot];

        public BezierCurve R2Bezier => _settings.R2OutBezierCurveObj[_slot];

        public int L2CurveMode
        {
            get => _settings.GetL2OutCurveMode(_slot);
            set => _settings.SetL2OutCurveMode(_slot, value);
        }

        public int R2CurveMode
        {
            get => _settings.GetR2OutCurveMode(_slot);
            set => _settings.SetR2OutCurveMode(_slot, value);
        }
    }
}
