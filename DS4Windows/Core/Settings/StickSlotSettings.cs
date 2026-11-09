using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのスティック（LS・RS）の設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class StickSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public StickSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        // 中身を直接書き換える設定のまとまり
        public StickDeadZoneInfo LSMod => _settings.LSModInfo[_slot];
        public StickDeadZoneInfo RSMod => _settings.RSModInfo[_slot];
        public StickOutputSetting LSOutput => _settings.LSOutputSettings[_slot];
        public StickOutputSetting RSOutput => _settings.RSOutputSettings[_slot];
        public StickAntiSnapbackInfo LSAntiSnapback => _settings.LSAntiSnapbackInfo[_slot];
        public StickAntiSnapbackInfo RSAntiSnapback => _settings.RSAntiSnapbackInfo[_slot];
        public SquareStickInfo SquStick => _settings.SquStickInfo[_slot];
        public BezierCurve LsBezier => _settings.LsOutBezierCurveObj[_slot];
        public BezierCurve RsBezier => _settings.RsOutBezierCurveObj[_slot];

        public double LSSens
        {
            get => _settings.LSSens[_slot];
            set => _settings.LSSens[_slot] = value;
        }

        public double RSSens
        {
            get => _settings.RSSens[_slot];
            set => _settings.RSSens[_slot] = value;
        }

        /// <summary>回転（ラジアン）。</summary>
        public double LSRotation
        {
            get => _settings.LSRotation[_slot];
            set => _settings.LSRotation[_slot] = value;
        }

        public double RSRotation
        {
            get => _settings.RSRotation[_slot];
            set => _settings.RSRotation[_slot] = value;
        }

        public sbyte LeftStickDriftXAxis
        {
            get => _settings.LeftStickDriftXAxis[_slot];
            set => _settings.LeftStickDriftXAxis[_slot] = value;
        }

        public sbyte LeftStickDriftYAxis
        {
            get => _settings.LeftStickDriftYAxis[_slot];
            set => _settings.LeftStickDriftYAxis[_slot] = value;
        }

        public sbyte RightStickDriftXAxis
        {
            get => _settings.RightStickDriftXAxis[_slot];
            set => _settings.RightStickDriftXAxis[_slot] = value;
        }

        public sbyte RightStickDriftYAxis
        {
            get => _settings.RightStickDriftYAxis[_slot];
            set => _settings.RightStickDriftYAxis[_slot] = value;
        }

        public int LSCurveMode
        {
            get => _settings.GetLsOutCurveMode(_slot);
            set => _settings.SetLsOutCurveMode(_slot, value);
        }

        public int RSCurveMode
        {
            get => _settings.GetRsOutCurveMode(_slot);
            set => _settings.SetRsOutCurveMode(_slot, value);
        }
    }
}
