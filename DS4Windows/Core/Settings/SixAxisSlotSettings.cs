using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの 6 軸（加速度の X・Z）の設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class SixAxisSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public SixAxisSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        public double SXDeadZone
        {
            get => _settings.SXDeadzone[_slot];
            set => _settings.SXDeadzone[_slot] = value;
        }

        public double SZDeadZone
        {
            get => _settings.SZDeadzone[_slot];
            set => _settings.SZDeadzone[_slot] = value;
        }

        public double SXMaxZone
        {
            get => _settings.SXMaxzone[_slot];
            set => _settings.SXMaxzone[_slot] = value;
        }

        public double SZMaxZone
        {
            get => _settings.SZMaxzone[_slot];
            set => _settings.SZMaxzone[_slot] = value;
        }

        public double SXAntiDeadZone
        {
            get => _settings.SXAntiDeadzone[_slot];
            set => _settings.SXAntiDeadzone[_slot] = value;
        }

        public double SZAntiDeadZone
        {
            get => _settings.SZAntiDeadzone[_slot];
            set => _settings.SZAntiDeadzone[_slot] = value;
        }

        public double SXSens
        {
            get => _settings.SXSens[_slot];
            set => _settings.SXSens[_slot] = value;
        }

        public double SZSens
        {
            get => _settings.SZSens[_slot];
            set => _settings.SZSens[_slot] = value;
        }

        public int SXCurveMode
        {
            get => _settings.GetSxOutCurveMode(_slot);
            set => _settings.SetSxOutCurveMode(_slot, value);
        }

        public int SZCurveMode
        {
            get => _settings.GetSzOutCurveMode(_slot);
            set => _settings.SetSzOutCurveMode(_slot, value);
        }

        /// <summary>X の出力曲線のカスタム定義（中身を直接書き換える）。</summary>
        public BezierCurve SXCurve => _settings.SxOutBezierCurveObj[_slot];

        public BezierCurve SZCurve => _settings.SzOutBezierCurveObj[_slot];
    }
}
