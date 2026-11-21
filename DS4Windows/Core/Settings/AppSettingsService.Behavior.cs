namespace DS4Windows.Core.Settings
{
    /// <summary>通知・起動・動作の設定の窓口。</summary>
    public partial class AppSettingsService
    {
        public bool StartMinimized
        {
            get => _config.Behavior.startMinimized;
            set => SetIfChanged(_config.Behavior.startMinimized, v => _config.Behavior.startMinimized = v, value);
        }

        public bool MinimizeToTaskbar
        {
            get => _config.Behavior.minToTaskbar;
            set => SetIfChanged(_config.Behavior.minToTaskbar, v => _config.Behavior.minToTaskbar = v, value);
        }

        public bool CloseMinimizes
        {
            get => _config.Behavior.closeMini;
            set => SetIfChanged(_config.Behavior.closeMini, v => _config.Behavior.closeMini = v, value);
        }

        public bool UseExclusiveMode
        {
            get => _config.Behavior.useExclusiveMode;
            set => SetIfChanged(_config.Behavior.useExclusiveMode, v => _config.Behavior.useExclusiveMode = v, value);
        }

        public bool AutoProfileRevertDefaultProfile
        {
            get => _config.Behavior.autoProfileRevertDefaultProfile;
            set => SetIfChanged(_config.Behavior.autoProfileRevertDefaultProfile, v => _config.Behavior.autoProfileRevertDefaultProfile = v, value);
        }

        // ---- 通知レベル・スワイプ操作設定 ----
        public int Notifications
        {
            get => _config.Behavior.notifications;
            set => SetIfChanged(_config.Behavior.notifications, v => _config.Behavior.notifications = v, value);
        }

        public bool ProfileChangedNotification
        {
            get => _config.Behavior.profileChangedNotification;
            set
            {
                if (_config.Behavior.profileChangedNotification == value) return;
                _config.Behavior.profileChangedNotification = value;
                NotifyChanged(nameof(ProfileChangedNotification));
            }
        }
        public bool SwipeProfiles
        {
            get => _config.Behavior.swipeProfiles;
            set => SetIfChanged(_config.Behavior.swipeProfiles, v => _config.Behavior.swipeProfiles = v, value);
        }

        // ---- Global への薄い委譲 ----
        public bool UseMoonlight
        {
            get => _config.Behavior.useMoonlight;
            set => SetIfChanged(_config.Behavior.useMoonlight, v => _config.Behavior.useMoonlight = v, value);
        }

        public bool UseAdvancedMoonlight
        {
            get => _config.Behavior.useAdvancedMoonlight;
            set => SetIfChanged(_config.Behavior.useAdvancedMoonlight, v => _config.Behavior.useAdvancedMoonlight = v, value);
        }

        public bool QuickCharge
        {
            get => _config.Behavior.quickCharge;
            set => SetIfChanged(_config.Behavior.quickCharge, v => _config.Behavior.quickCharge = v, value);
        }

        public bool DCBTatStop
        {
            get => _config.Behavior.disconnectBTAtStop;
            set => SetIfChanged(_config.Behavior.disconnectBTAtStop, v => _config.Behavior.disconnectBTAtStop = v, value);
        }

        public int ProcessPriority
        {
            get => _config.Behavior.processPriority;
            set => SetIfChanged(_config.Behavior.processPriority, v => _config.Behavior.processPriority = v, value);
        }

        // ---- 遅延警告時のライトバー点滅 ----
        public bool FlashWhenLate
        {
            get => _config.Behavior.flashWhenLate;
            set => SetIfChanged(_config.Behavior.flashWhenLate, v => _config.Behavior.flashWhenLate = v, value);
        }

        public int FlashWhenLateAt
        {
            get => _config.Behavior.flashWhenLateAt;
            set => SetIfChanged(_config.Behavior.flashWhenLateAt, v => _config.Behavior.flashWhenLateAt = v, value);
        }
    }
}
