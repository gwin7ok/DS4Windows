using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのジャイロの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// 実行中のスロットの状態へ反映する必要がある変更（デッドゾーンとトグル）は、反映先を渡す <c>Set…</c> のメソッドを通す。
    /// </summary>
    public class GyroSlotSettings
    {
        private readonly IEnvironmentService _environmentService;

        public GyroSlotSettings(int slot, IEnvironmentService environmentService)
        {
            _environmentService = environmentService ?? throw new System.ArgumentNullException(nameof(environmentService));
            Slot = slot;
            Mouse = new GyroMouseInfo();
            MouseStick = new GyroMouseStickInfo();
            Controls = new GyroControlsInfo();
            RemovedSwipe = new RemovedGyroSwipeSettings();
            ResetToDefaults();
        }

        public int Slot { get; }

        public GyroOutMode OutputMode { get; set; }

        public bool TriggerTurns { get; set; }

        public int Sensitivity { get; set; }

        public int SensVerticalScale { get; set; }

        public int MouseHorizontalAxis { get; set; }

        /// <summary>反転のビット（1＝縦、2＝横）。</summary>
        public int Invert { get; set; }

        public GyroMouseInfo Mouse { get; }

        public GyroMouseStickInfo MouseStick { get; }

        public GyroControlsInfo Controls { get; }

        public bool MouseStickTriggerTurns { get; set; }

        public int MouseStickHorizontalAxis { get; set; }

        public int MouseDeadZone { get; set; }

        public bool MouseToggle { get; set; }

        public bool MouseStickToggle { get; set; }

        public bool ControlsToggle => Controls.triggerToggle;

        // トグルとデッドゾーンは、実行中のスロットの状態（タッチパッド→マウスの処理）にも反映する必要があるため、反映先を渡す。
        // 接続していないスロット（スロット数の上限以上）や、処理がまだ無いときは、値だけ変える
        public void SetMouseDeadZone(int value, IProfileLoadTarget control)
        {
            MouseDeadZone = value;
            if (Slot < _environmentService.ControllerSlotLimit && control.GetTouchPad(Slot) != null)
                control.GetTouchPad(Slot).CursorGyroDead = value;
        }

        public void SetMouseToggle(bool value, IProfileLoadTarget control)
        {
            MouseToggle = value;
            if (Slot < _environmentService.ControllerSlotLimit && control.GetTouchPad(Slot) != null)
                control.GetTouchPad(Slot).ToggleGyroMouse = value;
        }

        public void SetMouseStickToggle(bool value, IProfileLoadTarget control)
        {
            MouseStickToggle = value;
            if (Slot < _environmentService.ControllerSlotLimit && control.GetTouchPad(Slot) != null)
                control.GetTouchPad(Slot).ToggleGyroStick = value;
        }

        public void SetControlsToggle(bool value, IProfileLoadTarget control)
        {
            Controls.triggerToggle = value;
            if (Slot < _environmentService.ControllerSlotLimit && control.GetTouchPad(Slot) != null)
                control.GetTouchPad(Slot).ToggleGyroControls = value;
        }

        /// <summary>マウスのトリガー条件。true＝すべて成立（and）、false＝いずれか（or）。</summary>
        public bool MouseTriggerCondAnd { get; set; }

        public bool MouseStickTriggerCondAnd { get; set; }

        /// <summary>カンマ区切りのトリガーの番号（-1＝常にオン）。</summary>
        public string MouseTriggers { get; set; }

        public string MouseStickTriggers { get; set; }

        /// <summary>廃止したジャイロのスワイプの設定（読み込み時に保持し、モードを選び直したら破棄する）。</summary>
        public RemovedGyroSwipeSettings RemovedSwipe { get; }

        /// <summary>
        /// 値を、新しく作ったときの既定値にする。<see cref="Mouse"/>／<see cref="MouseStick"/>／<see cref="Controls"/>／<see cref="RemovedSwipe"/> は、
        /// オブジェクトを保ったまま中身を戻す（保持している参照を無効にしないため）。
        /// </summary>
        public void ResetToDefaults()
        {
            OutputMode = BackingStore.DEFAULT_GYRO_OUT_MODE;
            TriggerTurns = BackingStore.DEFAULT_GYRO_TRIGGER_TURNS;
            Sensitivity = BackingStore.DEFAULT_GYRO_SENS;
            SensVerticalScale = BackingStore.DEFAULT_GYRO_SENS_VERTICAL_SCALE;
            MouseHorizontalAxis = 0;
            Invert = 0;
            MouseStickTriggerTurns = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGER_TURNS;
            MouseStickHorizontalAxis = 0;
            MouseDeadZone = DS4Windows.Processing.Touchpad.MouseCursor.GYRO_MOUSE_DEADZONE;
            MouseToggle = false;
            MouseStickToggle = false;
            MouseTriggerCondAnd = BackingStore.DEFAULT_SA_TRIGGER_COND;
            MouseStickTriggerCondAnd = BackingStore.DEFAULT_SA_MSTICK_TRIGGER_COND;
            MouseTriggers = BackingStore.DEFAULT_SA_TRIGGERS;
            MouseStickTriggers = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS;
        }
    }
}
