namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 通知・起動・動作の設定。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class BehaviorSettings
    {
        public int processPriority { get; set; }
        public bool useExclusiveMode { get; set; } = false; // Re-enable Ex Mode
        public bool startMinimized { get; set; } = false;
        public bool minToTaskbar { get; set; } = false;
        public int notifications { get; set; } = BackingStore.DEFAULT_NOTIFICATIONS;
        public bool profileChangedNotification { get; set; }
        public bool disconnectBTAtStop { get; set; } = false;
        public bool swipeProfiles { get; set; } = BackingStore.DEFAULT_SWIPE_PROFILES;
        public bool ds4Mapping { get; set; } = false;
        public bool quickCharge { get; set; } = false;
        public bool useMoonlight { get; set; } = false;
        public bool useAdvancedMoonlight { get; set; } = false;
        public bool closeMini { get; set; } = false;
        public bool flashWhenLate { get; set; } = BackingStore.DEFAULT_FLASH_WHEN_LATE;
        public int flashWhenLateAt { get; set; } = BackingStore.DEFAULT_FLASH_WHEN_LATE_AT;
        public bool autoProfileRevertDefaultProfile { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE;
    }
}
