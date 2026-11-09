using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのジャイロの設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// 利用者（ViewModel）は、全機能の契約ではなく、この領域だけに依存する。
    /// </summary>
    public class GyroSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public GyroSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        public GyroOutMode OutputMode
        {
            get => _settings.GyroOutputMode[_slot];
            set => _settings.GyroOutputMode[_slot] = value;
        }

        public bool TriggerTurns
        {
            get => _settings.GyroTriggerTurns[_slot];
            set => _settings.GyroTriggerTurns[_slot] = value;
        }

        public int Sensitivity
        {
            get => _settings.GyroSensitivity[_slot];
            set => _settings.GyroSensitivity[_slot] = value;
        }

        public int SensVerticalScale
        {
            get => _settings.GyroSensVerticalScale[_slot];
            set => _settings.GyroSensVerticalScale[_slot] = value;
        }

        public int MouseHorizontalAxis
        {
            get => _settings.GyroMouseHorizontalAxis[_slot];
            set => _settings.GyroMouseHorizontalAxis[_slot] = value;
        }

        /// <summary>反転のビット（1＝縦、2＝横）。</summary>
        public int Invert
        {
            get => _settings.GyroInvert[_slot];
            set => _settings.GyroInvert[_slot] = value;
        }

        public GyroMouseInfo Mouse => _settings.GyroMouseInfo[_slot];

        public GyroMouseStickInfo MouseStick => _settings.GyroMouseStickInf[_slot];

        public GyroControlsInfo Controls => _settings.GyroControlsInf[_slot];

        public bool MouseStickTriggerTurns
        {
            get => _settings.GyroMouseStickTriggerTurns[_slot];
            set => _settings.GyroMouseStickTriggerTurns[_slot] = value;
        }

        public int MouseStickHorizontalAxis
        {
            get => _settings.GyroMouseStickHorizontalAxis[_slot];
            set => _settings.GyroMouseStickHorizontalAxis[_slot] = value;
        }

        public int MouseDeadZone => _settings.GyroMouseDeadZone[_slot];
        public bool MouseToggle => _settings.GyroMouseToggle[_slot];
        public bool MouseStickToggle => _settings.GyroMouseStickToggle[_slot];
        public bool ControlsToggle => Controls.triggerToggle;

        // トグルとデッドゾーンは、実行中のスロットの状態も更新する必要があるため、読み込み先を渡す
        public void SetMouseDeadZone(int value, IProfileLoadTarget control) => _settings.SetGyroMouseDeadZone(_slot, value, control);
        public void SetMouseToggle(bool value, IProfileLoadTarget control) => _settings.SetGyroMouseToggle(_slot, value, control);
        public void SetMouseStickToggle(bool value, IProfileLoadTarget control) => _settings.SetGyroMouseStickToggle(_slot, value, control);
        public void SetControlsToggle(bool value, IProfileLoadTarget control) => _settings.SetGyroControlsToggle(_slot, value, control);

        /// <summary>マウスのトリガー条件。true＝すべて成立（and）、false＝いずれか（or）。</summary>
        public bool MouseTriggerCondAnd
        {
            get => _settings.GetSATriggerCond(_slot);
            set => _settings.SetSaTriggerCond(_slot, value ? "and" : "or");
        }

        public bool MouseStickTriggerCondAnd
        {
            get => _settings.GetSAMouseStickTriggerCond(_slot);
            set => _settings.SetSaMouseStickTriggerCond(_slot, value ? "and" : "or");
        }

        /// <summary>カンマ区切りのトリガーの番号（-1＝常にオン）。</summary>
        public string MouseTriggers
        {
            get => _settings.SATriggers[_slot];
            set => _settings.SATriggers[_slot] = value;
        }

        public string MouseStickTriggers
        {
            get => _settings.SAMousestickTriggers[_slot];
            set => _settings.SAMousestickTriggers[_slot] = value;
        }

        /// <summary>DS3 の傾きの擬似。プロファイル全体の設定（スロットごとではない）だが、ジャイロのタブにある。</summary>
        public bool UseDs3PitchRollSim
        {
            get => _settings.UseDs3PitchRollSim;
            set => _settings.UseDs3PitchRollSim = value;
        }

        /// <summary>廃止したジャイロのスワイプの設定（読み込み時に保持し、モードを選び直したら破棄する）。</summary>
        public RemovedGyroSwipeSettings RemovedSwipe => _settings.GetRemovedGyroSwipeSettings(_slot);
    }
}
