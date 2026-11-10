using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// メイン画面のトレイ・通知・テーマの切り替えを受け持つ（<see cref="MainWindow"/> から責務ごと移したもの。メソッドの本体は移動のみ）。
    /// 部品は窓を保持しない。トレイの ViewModel は、呼び出しのたびに引数で受ける。
    /// </summary>
    public sealed class TrayAppearanceController
    {
        private readonly INotificationService _notificationService;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IUiThemeAndLanguage _themeLanguage;
        private readonly IAppSettingsService _appSettings;

        public TrayAppearanceController(INotificationService notificationService,
            IAppearanceSettingsService appearanceSettings, IUiThemeAndLanguage themeLanguage,
            IAppSettingsService appSettings)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _themeLanguage = themeLanguage ?? throw new ArgumentNullException(nameof(themeLanguage));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>通知サービスのイベントを購読する（画面のイベント登録の時点で 1 回）。</summary>
        public void SubscribeNotifications()
        {
            _notificationService.NotificationTriggered += ShowSystemNotification;
        }

        private void ShowSystemNotification(object sender, NotificationEventArgs e)
        {
            ShowSystemNotification(e.Message, e.Warning);
        }

        // 通知実行の本体
        public void ShowSystemNotification(string message, bool isWarning)
        {
            int notifLevel = _appSettings.Notifications;
            bool levelPass = notifLevel == 2 || (notifLevel == 1 && isWarning);

            if (levelPass)
            {
                // TrayIconViewModel.balloonTitle の代わりに "DS4Windows" を直接指定
                string title = "DS4Windows";

                try
                {
                    AppNotificationRegistration.ShowModernToast(title, message);
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarn($"[Toast] ShowModernToast failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        // プロファイル切替通知の表示制御（通知仕様を一元管理）
        public void ShowProfileSwitchNotification(string message)
        {
            bool isProfileWinOn = _appSettings.ProfileChangedNotification;
            int notifLevel = _appSettings.Notifications;

            if (isProfileWinOn)
            {
                // ケース 1, 2, 3: チェックボックスが ON -> 独自デスクトップウィンドウを表示
                ProfileNotificationWindow.ShowNotification(message);
            }
            else if (notifLevel == 2)
            {
                // ケース 4: チェックボックスが OFF かつ 通知設定が「すべて(2)」 -> トースト通知を表示
                ShowSystemNotification(message, false);
            }
            else
            {
                // ケース 5, 6: 抑制（非表示）
            }
        }

        /// <summary>設定画面の ViewModel のプロパティ変更のうち、テーマとトレイのアイコンに関わるものを反映する。</summary>
        public void OnSettingsPropertyChanged(string propertyName, TrayIconViewModel trayIconVM)
        {
            switch (propertyName)
            {
                case nameof(SettingsViewModel.AppChoiceIndex): ApplyThemeChoice(trayIconVM); break;
                case nameof(SettingsViewModel.IconChoiceIndex): ApplyIconChoice(trayIconVM); break;
            }
        }

        private void ApplyThemeChoice(TrayIconViewModel trayIconVM)
        {
            AppThemeChoice choice = _appearanceSettings.UseCurrentTheme;
            _themeLanguage.ChangeTheme(choice);
            trayIconVM.PopulateContextMenu();
        }

        private void ApplyIconChoice(TrayIconViewModel trayIconVM)
        {
            trayIconVM.IconSource = TrayIconResources.iconChoiceResources[_appearanceSettings.UseIconChoice];
        }
    }
}
