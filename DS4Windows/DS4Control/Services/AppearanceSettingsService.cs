namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="DS4Windows.DI.IAppearanceSettingsService"/> の実装。
    /// Global(ScpUtil.cs) が保持する実体（テーマ・トレイアイコン設定）への薄いシムであり、
    /// 独自フィールドを持たない（Notifications 等と同一の Strangler Fig パターン）。
    /// </summary>
    public class AppearanceSettingsService : DS4Windows.DI.IAppearanceSettingsService
    {
        // Phase7-Step3 W5: Global 経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config;

        public AppearanceSettingsService(BackingStore config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public AppThemeChoice UseCurrentTheme
        {
            get => _config.useCurrentTheme;
            set => _config.useCurrentTheme = value;
        }

        public TrayIconChoice UseIconChoice
        {
            get => _config.useIconChoice;
            set => _config.useIconChoice = value;
        }

        public string GetIconResourcePath(TrayIconChoice choice) => DS4WinWPF.DS4Forms.TrayIconResources.iconChoiceResources[choice];

        // ---- Phase6-Step2-5 (PR-5): バッテリー残量の変化通知 ----
        // Phase7-Step10d W4: 旧 Global.BatteryChanged（static イベント）の実体をこのサービスへ移した。
        // 差: イベントの送信元が typeof(Global) から、このサービスのインスタンスになった（購読者は送信元を使っていない）。
        public void InvokeBatteryChanged(byte percentage) => BatteryChanged?.Invoke(this, percentage);

        public event System.EventHandler<byte> BatteryChanged;
    }
}