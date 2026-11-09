using DS4Windows.Core.Abstractions;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのタッチパッドの設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class TouchpadSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public TouchpadSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        public TouchpadOutMode OutputMode
        {
            get => _settings.TouchOutMode[_slot];
            set => _settings.TouchOutMode[_slot] = value;
        }

        public TouchButtonActivationMode ButtonMode
        {
            get => _settings.TouchpadButtonMode[_slot];
            set => _settings.TouchpadButtonMode[_slot] = value;
        }

        public byte TouchSensitivity
        {
            get => _settings.TouchSensitivity[_slot];
            set => _settings.TouchSensitivity[_slot] = value;
        }

        public int ScrollSensitivity
        {
            get => _settings.ScrollSensitivity[_slot];
            set => _settings.ScrollSensitivity[_slot] = value;
        }

        public byte TapSensitivity
        {
            get => _settings.TapSensitivity[_slot];
            set => _settings.TapSensitivity[_slot] = value;
        }

        public bool DoubleTap
        {
            get => _settings.DoubleTap[_slot];
            set => _settings.DoubleTap[_slot] = value;
        }

        public bool JitterCompensation
        {
            get => _settings.TouchpadJitterCompensation[_slot];
            set => _settings.TouchpadJitterCompensation[_slot] = value;
        }

        public int Invert
        {
            get => _settings.TouchpadInvert[_slot];
            set => _settings.TouchpadInvert[_slot] = value;
        }

        public bool LowerRightAsRightClick
        {
            get => _settings.LowerRCOn[_slot];
            set => _settings.LowerRCOn[_slot] = value;
        }

        public bool ClickPassthru
        {
            get => _settings.TouchClickPassthru[_slot];
            set => _settings.TouchClickPassthru[_slot] = value;
        }

        public bool StartOff
        {
            get => _settings.StartTouchpadOff[_slot];
            set => _settings.StartTouchpadOff[_slot] = value;
        }

        public bool Trackball
        {
            get => _settings.TrackballMode[_slot];
            set => _settings.TrackballMode[_slot] = value;
        }

        public double TrackballFriction
        {
            get => _settings.TrackballFriction[_slot];
            set => _settings.TrackballFriction[_slot] = value;
        }

        /// <summary>反転を無効にするトリガーの番号の並び（-1 は「なし」）。</summary>
        public int[] DisInvertTriggers
        {
            get => _settings.TouchDisInvertTriggers[_slot];
            set => _settings.TouchDisInvertTriggers[_slot] = value;
        }

        /// <summary>相対マウスの設定（中身を直接書き換える）。</summary>
        public TouchpadRelMouseSettings RelMouse => _settings.TouchRelMouse[_slot];

        /// <summary>マウスジョイスティックの設定（中身を直接書き換える）。</summary>
        public TouchMouseStickInfo MouseStick => _settings.TouchMouseStickInf[_slot];
    }
}
