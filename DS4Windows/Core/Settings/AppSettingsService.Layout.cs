namespace DS4Windows.Core.Settings
{
    /// <summary>画面の配置の設定（ウィンドウの位置・大きさ、エディターの幅、一覧の列の幅）の窓口。</summary>
    public partial class AppSettingsService
    {
        // ---- ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        // いずれも m_Config(BackingStore) への薄い公開アクセサ(Global.X)への正規シム。
        public int FormWidth
        {
            get => _config.Layout.formWidth;
            set => SetIfChanged(_config.Layout.formWidth, v => _config.Layout.formWidth = v, value);
        }

        public int FormHeight
        {
            get => _config.Layout.formHeight;
            set => SetIfChanged(_config.Layout.formHeight, v => _config.Layout.formHeight = v, value);
        }

        public int FormLocationX
        {
            get => _config.Layout.formLocationX;
            set => SetIfChanged(_config.Layout.formLocationX, v => _config.Layout.formLocationX = v, value);
        }

        public int FormLocationY
        {
            get => _config.Layout.formLocationY;
            set => SetIfChanged(_config.Layout.formLocationY, v => _config.Layout.formLocationY = v, value);
        }

        public int ControllerIndexColWidth
        {
            get => _config.Layout.controllerIndexColWidth;
            set => SetIfChanged(_config.Layout.controllerIndexColWidth, v => _config.Layout.controllerIndexColWidth = v, value);
        }

        public int ControllerIdColWidth
        {
            get => _config.Layout.controllerIdColWidth;
            set => SetIfChanged(_config.Layout.controllerIdColWidth, v => _config.Layout.controllerIdColWidth = v, value);
        }

        public int ControllerStatusColWidth
        {
            get => _config.Layout.controllerStatusColWidth;
            set => SetIfChanged(_config.Layout.controllerStatusColWidth, v => _config.Layout.controllerStatusColWidth = v, value);
        }

        public int ControllerExclusiveColWidth
        {
            get => _config.Layout.controllerExclusiveColWidth;
            set => SetIfChanged(_config.Layout.controllerExclusiveColWidth, v => _config.Layout.controllerExclusiveColWidth = v, value);
        }

        public int ControllerBatteryColWidth
        {
            get => _config.Layout.controllerBatteryColWidth;
            set => SetIfChanged(_config.Layout.controllerBatteryColWidth, v => _config.Layout.controllerBatteryColWidth = v, value);
        }

        public int ControllerSelectProfileColWidth
        {
            get => _config.Layout.controllerSelectProfileColWidth;
            set => SetIfChanged(_config.Layout.controllerSelectProfileColWidth, v => _config.Layout.controllerSelectProfileColWidth = v, value);
        }

        public int ControllerEditColWidth
        {
            get => _config.Layout.controllerEditColWidth;
            set => SetIfChanged(_config.Layout.controllerEditColWidth, v => _config.Layout.controllerEditColWidth = v, value);
        }

        public int ControllerLinkedProfileColWidth
        {
            get => _config.Layout.controllerLinkedProfileColWidth;
            set => SetIfChanged(_config.Layout.controllerLinkedProfileColWidth, v => _config.Layout.controllerLinkedProfileColWidth = v, value);
        }

        public int ControllerLinkProfIdColWidth
        {
            get => _config.Layout.controllerLinkProfIdColWidth;
            set => SetIfChanged(_config.Layout.controllerLinkProfIdColWidth, v => _config.Layout.controllerLinkProfIdColWidth = v, value);
        }

        public int ControllerCustomColorColWidth
        {
            get => _config.Layout.controllerCustomColorColWidth;
            set => SetIfChanged(_config.Layout.controllerCustomColorColWidth, v => _config.Layout.controllerCustomColorColWidth = v, value);
        }

        public int OutputSlotNumColWidth
        {
            get => _config.Layout.outputSlotNumColWidth;
            set => SetIfChanged(_config.Layout.outputSlotNumColWidth, v => _config.Layout.outputSlotNumColWidth = v, value);
        }

        public int OutputSlotInputNumColWidth
        {
            get => _config.Layout.outputSlotInputNumColWidth;
            set => SetIfChanged(_config.Layout.outputSlotInputNumColWidth, v => _config.Layout.outputSlotInputNumColWidth = v, value);
        }

        public int OutputSlotInputIdColWidth
        {
            get => _config.Layout.outputSlotInputIdColWidth;
            set => SetIfChanged(_config.Layout.outputSlotInputIdColWidth, v => _config.Layout.outputSlotInputIdColWidth = v, value);
        }

        public int OutputSlotCurrentTypeColWidth
        {
            get => _config.Layout.outputSlotCurrentTypeColWidth;
            set => SetIfChanged(_config.Layout.outputSlotCurrentTypeColWidth, v => _config.Layout.outputSlotCurrentTypeColWidth = v, value);
        }

        public int OutputSlotReservationColWidth
        {
            get => _config.Layout.outputSlotReservationColWidth;
            set => SetIfChanged(_config.Layout.outputSlotReservationColWidth, v => _config.Layout.outputSlotReservationColWidth = v, value);
        }

        public int AutoProfileNameColWidth
        {
            get => _config.Layout.autoProfileNameColWidth;
            set => SetIfChanged(_config.Layout.autoProfileNameColWidth, v => _config.Layout.autoProfileNameColWidth = v, value);
        }

        public int AutoProfilePathColWidth
        {
            get => _config.Layout.autoProfilePathColWidth;
            set => SetIfChanged(_config.Layout.autoProfilePathColWidth, v => _config.Layout.autoProfilePathColWidth = v, value);
        }

        public int AutoProfileTitleColWidth
        {
            get => _config.Layout.autoProfileTitleColWidth;
            set => SetIfChanged(_config.Layout.autoProfileTitleColWidth, v => _config.Layout.autoProfileTitleColWidth = v, value);
        }

        public int LogTimeColWidth
        {
            get => _config.Layout.logTimeColWidth;
            set => SetIfChanged(_config.Layout.logTimeColWidth, v => _config.Layout.logTimeColWidth = v, value);
        }

        public int LogDataColWidth
        {
            get => _config.Layout.logDataColWidth;
            set => SetIfChanged(_config.Layout.logDataColWidth, v => _config.Layout.logDataColWidth = v, value);
        }

        // ---- プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        public int ProfileEditorLeftWidth
        {
            get => _config.Layout.profileEditorLeftWidth;
            set => SetIfChanged(_config.Layout.profileEditorLeftWidth, v => _config.Layout.profileEditorLeftWidth = v, value);
        }

        public int ProfileEditorRightWidth
        {
            get => _config.Layout.profileEditorRightWidth;
            set => SetIfChanged(_config.Layout.profileEditorRightWidth, v => _config.Layout.profileEditorRightWidth = v, value);
        }

        public int SpecialActionNameColWidth
        {
            get => _config.Layout.specialActionNameColWidth;
            set => SetIfChanged(_config.Layout.specialActionNameColWidth, v => _config.Layout.specialActionNameColWidth = v, value);
        }

        public int SpecialActionTriggerColWidth
        {
            get => _config.Layout.specialActionTriggerColWidth;
            set => SetIfChanged(_config.Layout.specialActionTriggerColWidth, v => _config.Layout.specialActionTriggerColWidth = v, value);
        }

        public int SpecialActionDetailColWidth
        {
            get => _config.Layout.specialActionDetailColWidth;
            set => SetIfChanged(_config.Layout.specialActionDetailColWidth, v => _config.Layout.specialActionDetailColWidth = v, value);
        }
    }
}
