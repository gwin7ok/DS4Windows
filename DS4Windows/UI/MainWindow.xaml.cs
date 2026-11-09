/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using NonFormTimer = System.Timers.Timer;
using System.Runtime.InteropServices;
using System.ComponentModel;
using HttpProgress;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Input.Hid;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Translations;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;


namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public partial class MainWindow : Window
    {
        private const int POWER_RESUME = 7;
        private const int POWER_SUSPEND = 4;

        private MainWindowsViewModel mainWinVM;
        private StatusLogMsg lastLogMsg = new StatusLogMsg();
        private ProfileList profileListHolder;
        private LogViewModel logvm;
        private ControllerListViewModel conLvViewModel;
        private TrayIconViewModel trayIconVM;
        private SettingsViewModel settingsWrapVM;
        private IntPtr regHandle = new IntPtr();
        private bool showAppInTaskbar = false;
        private ManagementEventWatcher managementEvWatcher;
        private bool wasrunning = false;
        private AutoProfileHolder autoProfileHolder;
        private NonFormTimer hotkeysTimer;
        private NonFormTimer autoProfilesTimer;
        // [Phase 5 Step 13-9] 廃止: DIコンテナ注入の _autoProfileService を直接利用
        private readonly IAutoProfileService _autoProfileService;
        private readonly DS4Windows.Core.Abstractions.IAppSettingsService appSettingsService;
        // Phase6-Step10-2-4a: ProfileEditor へ渡す、補助ウィンドウの生成の窓口
        private readonly DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory;
        private readonly DS4Windows.Core.Abstractions.IPathService pathService;
        private readonly DS4Windows.Core.Abstractions.IEnvironmentService environmentService;
        private readonly DS4Windows.Core.Abstractions.IAppearanceSettingsService appearanceSettingsService;
        private readonly DS4Windows.Processing.Abstractions.IProfileApplicationService profileAppService;
        private readonly DS4Windows.Core.Abstractions.IProfileRepository profileRepo;
        // Phase6-Step10-4-5: Global.ApplyProfileToSlot の直接呼び出しの置換先（Global への薄い委譲）
        private readonly DS4Windows.Processing.Abstractions.IProfileSlotApplier profileSlotApplier;
        private readonly DS4Windows.Core.Abstractions.INotificationService notificationService;
        private readonly DS4Windows.UI.Abstractions.IUiThemeAndLanguage themeLanguage;   // Phase14b-Step16（D11）
        private readonly DS4Windows.UI.Abstractions.IIpcResultWriter ipcResultWriter;
        private readonly DS4Windows.Core.Abstractions.IChangelogService changelogService;   // Phase14b-Step17（D10）: 旧 静的 Changelog   // Phase14b-Step16（旧 App.WriteIPCResultDataMMF）
        private readonly DS4Windows.Processing.Abstractions.IIpcCommandProcessor ipcProcessor;   // Phase13-Step11 11-c: 旧 MainWindow.Ipc.cs の Ipc*
        private readonly DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch runtimeFeatures;   // Phase13-Step10 10-c: 旧 ControlService の OSC／UDP／HidHide／TouchpadSlide の転送
        private readonly DS4Windows.Processing.Abstractions.IServiceLifecycle lifecycle;   // Phase13-Step9: 旧 ControlService.running／suspending の転送
        private ProfileEditor editor;
        private int previousTabIndex = 0;
#pragma warning disable CS0414 // preserveSize kept for behavior compatibility with older logic
        private bool preserveSize = true;
#pragma warning restore CS0414
        private Size oldSize;
        private bool contextclose;
        private bool startMinimized;

        public ProfileList ProfileListHolder { get => profileListHolder; }

        public bool IsInitialShow { get; set; }

        // Phase7-Step5 W5（Q4＝A）: 旧 AppHost.GetService の 17 件を、コンストラクター引数に替えた。
        // IMainWindowFactory（実装は MainWindowFactory）が DI から解決して渡す。
        private readonly DS4Windows.Processing.Abstractions.IControllerSlotQuery slots;

        public MainWindow(ArgumentParser parser,
            DS4Windows.Core.Abstractions.IAppSettingsService appSettingsService,
            DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory,
            DS4Windows.Core.Abstractions.IPathService pathService,
            DS4Windows.Core.Abstractions.IEnvironmentService environmentService,
            DS4Windows.Core.Abstractions.IAppearanceSettingsService appearanceSettingsService,
            DS4Windows.Processing.Abstractions.IProfileApplicationService profileAppService,
            DS4Windows.Core.Abstractions.IProfileRepository profileRepo,
            DS4Windows.Core.Abstractions.INotificationService notificationService,
            DS4Windows.Processing.Abstractions.IServiceLifecycle lifecycle,
            DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch runtimeFeatures,
            DS4Windows.Processing.Abstractions.IIpcCommandProcessor ipcProcessor,
            DS4Windows.Processing.Abstractions.IProfileSlotApplier profileSlotApplier,
            MainWindowsViewModel mainWindowsViewModel,
            IAutoProfileService autoProfileService,
            DS4Windows.UI.Abstractions.IViewModelFactory viewModelFactory,
            DS4Windows.Core.Abstractions.IProfileIdentityService profileIdentityService,
            DS4Windows.UI.Abstractions.IUiThemeAndLanguage themeLanguage,
            DS4Windows.UI.Abstractions.IIpcResultWriter ipcResultWriter,
            DS4Windows.Core.Abstractions.IChangelogService changelogService,
            DS4Windows.Processing.Abstractions.IControllerSlotQuery slots)
        {
            this.slots = slots ?? throw new System.ArgumentNullException(nameof(slots));
            this.changelogService = changelogService ?? throw new ArgumentNullException(nameof(changelogService));   // Phase14b-Step17（D10）
            this.ipcResultWriter = ipcResultWriter ?? throw new ArgumentNullException(nameof(ipcResultWriter));   // Phase14b-Step16: CLI の問い合わせの結果の書き込み
            // Phase14b-Step16（決定 D11）: テーマ・言語の切り替え（旧: Application.Current as App の直接呼び出し）
            this.themeLanguage = themeLanguage ?? throw new ArgumentNullException(nameof(themeLanguage));
            // XAML初期化前にウィンドウサイズを設定（WindowLayoutDefaults から）
            Width = WindowLayoutDefaults.MAIN_WINDOW_WIDTH;
            Height = WindowLayoutDefaults.MAIN_WINDOW_HEIGHT;

            InitializeComponent();
            // Phase14b-Step16（D11）: XAML の資源 ImgLocPaths（画像のパス）に、テーマの切り替えの通知元を渡す（旧: 自分のコンストラクターで App.ThemeChanged を購読）
            (TryFindResource("ImgLocPaths") as ImageLocationPaths)?.AttachThemeSource(themeLanguage);
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            this.editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            profileListHolder = new ProfileList(pathService);
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this.appearanceSettingsService = appearanceSettingsService ?? throw new ArgumentNullException(nameof(appearanceSettingsService));
            this.profileAppService = profileAppService ?? throw new ArgumentNullException(nameof(profileAppService));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            this.ipcProcessor = ipcProcessor ?? throw new ArgumentNullException(nameof(ipcProcessor));
            this.profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));

            // Initialize log settings ComboBox
            logMinLevelComboBox.ItemsSource = new string[] { "Trace", "Debug", "Info", "Warn", "Error", "Fatal" };
            logMinLevelComboBox.SelectedValue = appSettingsService.LogMinLevel;

            // Phase7-Step5 W5: 旧 AppHost 解決の失敗時の new MainWindowsViewModel(...) のフォールバックは削除した（必須引数）
            mainWinVM = mainWindowsViewModel ?? throw new ArgumentNullException(nameof(mainWindowsViewModel));
            DataContext = mainWinVM;

            // Phase6-Step10-1-5（決定D7＝A）: ログ設定の反映の窓口（App が LoggerHolder の生成直後に接続する）
            settingsWrapVM = viewModelFactory.CreateSettingsViewModel();
            settingsTab.DataContext = settingsWrapVM;
            // Phase6-Step10-1-4: XAML から生成される LanguagePackControl へ、保持済みのサービスを渡す（Pure DI）。
            languagePackControl.Initialize(appSettingsService, themeLanguage);
            logvm = new LogViewModel(environmentService);
            //logListView.ItemsSource = logvm.LogItems;
            logListView.DataContext = logvm;
            lastMsgLb.DataContext = lastLogMsg;
            ProcessPriorityComboBox.ItemsSource = DS4Windows.Core.Platform.ProcessPriorityOptions.Classes;

            profileListHolder.Refresh();
            profilesListBox.ItemsSource = profileListHolder.ProfileListCol;

            StartStopBtn.Content = lifecycle.Running ? Translations.Strings.StopText :
                Translations.Strings.StartText;

            conLvViewModel = viewModelFactory.CreateControllerListViewModel(profileListHolder);
            controllerLV.DataContext = conLvViewModel;
            controllerLV.ItemsSource = conLvViewModel.ControllerCol;
            ChangeControllerPanel();

            // Sort device by input slot number
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(controllerLV.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("DevIndex", ListSortDirection.Ascending));
            view.Refresh();

            trayIconVM = new TrayIconViewModel(slots, lifecycle, profileListHolder,
                appearanceSettingsService, profileRepo, environmentService, pathService, Dispatcher);

            // Need to define before calling TaskbarIcon.ForceCreate
            notifyIcon.DataContext = trayIconVM;
            notifyIcon.CustomName = pathService.ExecutablePath;

            // Remove TaskbarIcon from visual tree so Loaded and Unloaded events
            // are not fired for TaskbarIcon instance. Ignores early Dispose calls
            // when scaling changes or an RDP session is activated
            var parent = VisualTreeHelper.GetParent(notifyIcon) as Panel;
            if (parent != null)
            {
                parent.Children.Remove(notifyIcon);
                // Since Loaded event will not get fired from Window, need to
                // create the tray icon explicitly here
                try
                {
                    // Loaded event handler has enablesEfficiencyMode default to false so
                    // do the same here
                    notifyIcon.ForceCreate(enablesEfficiencyMode: false);
                }
                catch (Exception)
                {
                    // Ignore exception
                }
            }

            startMinimized = appSettingsService.StartMinimized || parser.Mini;

            bool isElevated = environmentService.IsAdministrator();
            if (isElevated)
            {
                uacImg.Visibility = Visibility.Collapsed;
            }

            noContLb.Content = string.Format(Strings.NoControllersConnected,
                environmentService.ControllerSlotLimit);

            // [Phase 5 Step 13-9] 二重実体化防止: 注入済み _autoProfileService を直接利用するため撤去
            // Phase6-Step10-2-3: AutoProfiles の Initialize に渡すため、解決を前へ移した（解決先は同じ）
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));

            // Phase6-Step10-2-3: XAML から生成されるコントロールへ、保持済みのサービスを渡す（AutoProfileHolder を読む前に呼ぶ）
            autoProfControl.Initialize(pathService, appSettingsService, _autoProfileService,
                viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory)), environmentService);
            dupBox.Initialize(pathService, profileIdentityService ?? throw new ArgumentNullException(nameof(profileIdentityService)));
            autoProfileHolder = autoProfControl.AutoProfileHolder;
            autoProfControl.SetupDataContext(profileListHolder);

            slotManControl.SetupDataContext(viewModelFactory.CreateCurrentOutDeviceViewModel());

            SetupEvents();

            // Don't tie timers to main thread
            Thread timerThread = new Thread(() =>
            {
                hotkeysTimer = new NonFormTimer();
                hotkeysTimer.Interval = 20;
                hotkeysTimer.AutoReset = false;

                autoProfilesTimer = new NonFormTimer();
                autoProfilesTimer.Interval = 1000;
                autoProfilesTimer.AutoReset = false;
            });
            timerThread.IsBackground = true;
            timerThread.Priority = ThreadPriority.Lowest;
            timerThread.Start();
            // Wait for thread tasks to finish before continuing
            timerThread.Join();
        }

        public void LateChecks(ArgumentParser parser)
        {
            Task tempTask = Task.Run(() =>
            {
                mainWinVM.CheckDrivers();
                if (!parser.Stop)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StartStopBtn.IsEnabled = false;
                    }));
                    Thread.Sleep(1000);
                    lifecycle.Start();
                    //root.rootHubtest.Start();
                }
            });

            // Log exceptions that might occur
            Util.LogAssistBackgroundTask(tempTask);
#if !BETA_VERSION
            tempTask = Task.Delay(100).ContinueWith(_ =>
            {
                // Use explicit startup-check settings persisted in Profiles.xml
                if (appSettingsService.CheckUpdateStartupEnabled)
                {
                    int everyVal = appSettingsService.CheckEveryValue;
                    int everyUnit = appSettingsService.CheckEveryUnit; // 0=hours, 1=days
                    double hoursToWait = (everyUnit == 0) ? everyVal : everyVal * 24.0;

                    if (everyVal == 0 || DateTime.Now >= appSettingsService.LastChecked + TimeSpan.FromHours(hoursToWait))
                    {
                        try
                        {
                            if (changelogService.CheckNewerVersionExists(out var version, appSettingsService.LastVersionChecked, false, true))
                            {
                                DisplayUpdaterWindow(version.ToString());
                            }
                        }
                        catch
                        {
                            Dispatcher.Invoke(() => MessageBox.Show(Strings.FailedToRetrieveLatestVersion, "DS4Windows Updater"));
                            // bubble the exception up to allow to see what's wrong in the log
                            throw;
                        }

                        appSettingsService.LastChecked = DateTime.Now;
                    }
                }

                // Check if main window closing was requested from app update.
                // Quit task early
                //if (contextclose)
                //{
                //    return;
                //}
            });
#endif
            Util.LogAssistBackgroundTask(tempTask);
        }

        private void DisplayUpdaterWindow(string version)
        {
            MessageBoxResult result = MessageBoxResult.No;
            Dispatcher.Invoke(() =>
            {
                var updaterWin = new UpdaterWindow(version, environmentService, appSettingsService, changelogService);
                updaterWin.ShowDialog();
                result = updaterWin.Result;
            });
        }

        private void Check_Version(bool showstatus = false)
        {
            string version = environmentService.ApplicationVersion;
            string newversion = string.Empty;
            string versionFilePath = Path.Combine(pathService.AppDataPath, "version.txt");
            ulong lastVersionNum = appSettingsService.LastVersionCheckedNum;
            //ulong lastVersion = Global.CompileVersionNumberFromString("2.1.1");

            bool versionFileExists = File.Exists(versionFilePath);
            if (versionFileExists)
            {
                newversion = File.ReadAllText(versionFilePath).Trim();
                //newversion = "2.1.3";
            }

            // Delegate newest-version detection (and skip handling) to Changelog helper
            try
            {
                if (!string.IsNullOrWhiteSpace(newversion) && version.CompareTo(newversion) != 0)
                {
                    // Ensure CheckNewerVersionExists compares against the provided file version
                    Version parsedVersion;
                    if (Version.TryParse(newversion, out parsedVersion))
                    {
                        // Temporarily set cached/latest if needed - call helper allowing fresh check
                        if (changelogService.CheckNewerVersionExists(out var latest, appSettingsService.LastVersionChecked, allowCached: false))
                        {
                            Dispatcher.Invoke(() => DisplayUpdaterWindow(latest.ToString()));
                        }
                        else
                        {
                            if (showstatus)
                                Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
                        }
                    }
                }
                else
                {
                    if (showstatus)
                        Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
                }
            }
            catch
            {
                if (showstatus)
                    Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
            }
            finally
            {
                if (versionFileExists)
                    File.Delete(versionFilePath);
            }
        }

        private void TrayIconVM_RequestMinimize(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TrayIconVM_ProfileSelected(TrayIconViewModel sender,
            ControllerHolder item, string profile)
        {
            int idx = item.Index;
            CompositeDeviceModel devitem = conLvViewModel.ControllerDict[idx];
            if (devitem != null && devitem.Device != null)
            {
                string prolog = string.Format(Properties.Resources.UsingProfile,
                    (idx + 1).ToString(), profile, $"{devitem.Device.Battery}");
                profileAppService.ApplyProfile(idx, profile, false, true,
                    DS4Windows.Core.Logging.ProfileChangeSource.Manual, prolog);
            }
        }

        // 構想1: 情報通知の本体。切替のきっかけは Global.Notifications のみ。outputDestination is modern toast only.
        // 1. イベント登録用のオーバーロード（購読先を INotificationService.NotificationTriggered へ変更、Phase5-Step14）
        private void ShowSystemNotification(object sender, DS4Windows.Core.Abstractions.NotificationEventArgs e)
        {
            ShowSystemNotification(e.Message, e.Warning);
        }

        // 2. 通知実行の本体（エラー CS0117 を解消）
        private void ShowSystemNotification(string message, bool isWarning)
        {
            int notifLevel = appSettingsService.Notifications;
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
                    DS4Windows.Core.Logging.AppLogger.LogWarn($"[Toast] ShowModernToast failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        // 機能2: プロファイル切替通知の表示制御（通知仕様を一元管理）
        private void ShowProfileSwitchNotification(string message)
        {
            bool isProfileWinOn = appSettingsService.ProfileChangedNotification;
            int notifLevel = appSettingsService.Notifications;

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
        private void SetupEvents()
        {
            lifecycle.ServiceStarted += ControlServiceStarted;
            lifecycle.RunningChanged += ControlServiceChanged;
            lifecycle.PreServiceStop += PrepareForServiceStop;
            //root.rootHubtest.RunningChanged += ControlServiceChanged;
            conLvViewModel.ControllerCol.CollectionChanged += ControllerCol_CollectionChanged;
            notificationService.NotificationTriggered += ShowSystemNotification;

            // 型付きプロファイル変更イベントを購読
            AppLogger.ProfileChanged += OnProfileChanged;

            AppLogger.GuiLog += UpdateLastStatusMessage;
            logvm.LogItems.CollectionChanged += LogItems_CollectionChanged;
            trayIconVM.RequestShutdown += TrayIconVM_RequestShutdown;
            trayIconVM.ProfileSelected += TrayIconVM_ProfileSelected;
            trayIconVM.RequestMinimize += TrayIconVM_RequestMinimize;
            trayIconVM.RequestOpen += TrayIconVM_RequestOpen;
            trayIconVM.RequestServiceChange += TrayIconVM_RequestServiceChange;
            settingsWrapVM.PropertyChanged += SettingsWrapVM_PropertyChanged;

            autoProfControl.AutoDebugChanged += AutoProfControl_AutoDebugChanged;
            _autoProfileService.RequestServiceChange += AutoprofileChecker_RequestServiceChange;
            // 自動プロファイルは接続中のスロットにだけ適用する。接続した直後に、最後の前景ウィンドウで照合し直す
            slots.SlotChanged += ControlService_HotplugControllerRecheckAutoProfile;
            autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
            //autoProfControl.AutoProfVM.AutoProfileSystemChange += AutoProfVM_AutoProfileSystemChange;
            mainWinVM.FullTabsEnabledChanged += MainWinVM_FullTabsEnabledChanged;

            bool wmiConnected = false;
            WqlEventQuery q = new WqlEventQuery();
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            q.EventClassName = "Win32_PowerManagementEvent";

            try
            {
                scope.Connect();
            }
            catch (COMException) { }
            catch (ManagementException) { }

            if (scope.IsConnected)
            {
                wmiConnected = true;
                managementEvWatcher = new ManagementEventWatcher(scope, q);
                managementEvWatcher.EventArrived += PowerEventArrive;
                try
                {
                    managementEvWatcher.Start();
                }
                catch (ManagementException) { wmiConnected = false; }
                catch (COMException) { wmiConnected = false; }
            }

            if (!wmiConnected)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Ui_WmiFailed");
            }
        }

        private void SettingsWrapVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsViewModel.AppChoiceIndex): SettingsWrapVM_AppChoiceIndexChanged(sender, EventArgs.Empty); break;
                case nameof(SettingsViewModel.IconChoiceIndex): SettingsWrapVM_IconChoiceIndexChanged(sender, EventArgs.Empty); break;
            }
        }

        private void SettingsWrapVM_AppChoiceIndexChanged(object sender, EventArgs e)
        {
            AppThemeChoice choice = appearanceSettingsService.UseCurrentTheme;
            themeLanguage.ChangeTheme(choice);   // Phase14b-Step16（D11）: 旧 (App.Current as App).ChangeTheme
            trayIconVM.PopulateContextMenu();
        }

        private void SettingsWrapVM_IconChoiceIndexChanged(object sender, EventArgs e)
        {
            trayIconVM.IconSource = TrayIconResources.iconChoiceResources[appearanceSettingsService.UseIconChoice];
        }

        private void MainWinVM_FullTabsEnabledChanged(object sender, EventArgs e)
        {
            settingsWrapVM.ViewEnabled = mainWinVM.FullTabsEnabled;
        }

        private void TrayIconVM_RequestServiceChange(object sender, EventArgs e)
        {
            ChangeService();
        }

        private void LogItems_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    int count = logListView.Items.Count;
                    if (count > 0)
                    {
                        logListView.ScrollIntoView(logvm.LogItems[count - 1]);
                    }
                }));
            }
        }

        private void ControlServiceStarted(object sender, EventArgs e)
        {
            if (appSettingsService.SwipeProfiles)
            {
                ChangeHotkeysStatus(true);
            }

            CheckAutoProfileStatus();
        }

        // Phase6-Step10-4-4: 廃止済みの AutoProfileChecker を引数に持つオーバーロードを削除し、1 つに統合した
        private void AutoprofileChecker_RequestServiceChange(bool state)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ChangeService();
            }));
        }

        private void AutoProfVM_AutoProfileSystemChange(AutoProfilesViewModel sender, bool state)
        {
            if (state)
            {
                ChangeAutoProfilesStatus(true);
                autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
            }
            else
            {
                ChangeAutoProfilesStatus(false);
                autoProfileHolder.AutoProfileColl.CollectionChanged -= AutoProfileColl_CollectionChanged;
            }
        }

        private void AutoProfileColl_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CheckAutoProfileStatus();
        }

        private void AutoProfControl_AutoDebugChanged(object sender, EventArgs e)
        {
            _autoProfileService.AutoProfileDebugLogLevel = autoProfControl.AutoDebug == true ? 1 : 0;
        }

        private void PowerEventArrive(object sender, EventArrivedEventArgs e)
        {
            short evType = Convert.ToInt16(e.NewEvent.GetPropertyValue("EventType"));
            switch (evType)
            {
                // Wakeup from Suspend
                case POWER_RESUME:
                    {
                        lifecycle.Suspending = false;

                        if (wasrunning)
                        {
                            wasrunning = false;
                            Dispatcher.Invoke(() =>
                            {
                                StartStopBtn.IsEnabled = false;
                            });

                            AppLogger.Log(AppLogLevel.Info, DS4Windows.Translations.Strings.WakeupFromSuspend, LogOptions.UserFacing);
                            //controlService.LogDebug($"{Thread.CurrentThread.ManagedThreadId}");

                            //Thread.Sleep(60000);
                            //controlService.Start();

                            //Task startupTask = Task.Run(() =>
                            Task startupTask = Task.Delay(5000).ContinueWith(t =>
                            {
                                lifecycle.Start();
                            });

                            // Log exceptions that might occur
                            Util.LogAssistBackgroundTask(startupTask);
                        }
                    }

                    break;
                // Entering Suspend
                case POWER_SUSPEND:
                    {
                        lifecycle.Suspending = true;

                        if (lifecycle.Running)
                        {
                            //Dispatcher.Invoke(() =>
                            //{
                            //    StartStopBtn.IsEnabled = false;
                            //});

                            lifecycle.Stop(immediateUnplug: true);
                            wasrunning = true;

                            Thread.Sleep(1000);
                        }
                    }

                    break;

                default: break;
            }
        }

        private void ChangeHotkeysStatus(bool state)
        {
            if (state)
            {
                hotkeysTimer.Elapsed += HotkeysTimer_Elapsed;
                hotkeysTimer.Start();
            }
            else
            {
                hotkeysTimer.Stop();
                hotkeysTimer.Elapsed -= HotkeysTimer_Elapsed;
            }
        }

        private void HotkeysTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            hotkeysTimer.Stop();

            if (appSettingsService.SwipeProfiles)
            {
                foreach (CompositeDeviceModel item in conLvViewModel.ControllerCol)
                //for (int i = 0; i < 4; i++)
                {
                    string slide = runtimeFeatures.TouchpadSlide(item.DevIndex);
                    if (slide == "left")
                    {
                        //int ind = i;
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            if (item.SelectedIndex <= 0)
                            {
                                item.SelectedIndex = item.ProfileListCol.Count - 1;
                            }
                            else
                            {
                                item.SelectedIndex--;
                            }
                        }));
                    }
                    else if (slide == "right")
                    {
                        //int ind = i;
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            if (item.SelectedIndex == (item.ProfileListCol.Count - 1))
                            {
                                item.SelectedIndex = 0;
                            }
                            else
                            {
                                item.SelectedIndex++;
                            }
                        }));
                    }

                    if (slide.Contains("t"))
                    {
                        //int ind = i;
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            string temp = string.Format(Properties.Resources.UsingProfile, (item.DevIndex + 1).ToString(), item.SelectedProfile, $"{item.Device.Battery}");
                            ShowHotkeyNotification(temp);
                        }));
                    }
                }
            }

            hotkeysTimer.Start();
        }

        private void ShowHotkeyNotification(string message)
        {
            // Phase5-Step14 フェーズF: 「通知を表示」レベルによるイベント発火抑制を撤廃。
            // プロファイル適用イベントは常に発火させ、表示可否は OnProfileChanged 側で
            // ProfileChangedNotification を基準に一元判定する。
            try
            {
                AppLogger.LogProfileChanged(-1, message, false, DS4Windows.Core.Logging.ProfileChangeSource.Hotkey);
            }
            catch
            {
                AppLogger.LogToTray(message);
            }
        }

        private void PrepareForServiceStop(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                trayIconVM.ClearContextMenu();
            }));

            ChangeHotkeysStatus(false);
        }

        private void TrayIconVM_RequestOpen(object sender, EventArgs e)
        {
            if (!showAppInTaskbar)
            {
                Show();
            }

            WindowState = WindowState.Normal;
        }

        private void TrayIconVM_RequestShutdown(object sender, EventArgs e)
        {
            contextclose = true;
            this.Close();
        }

        private void UpdateLastStatusMessage(object sender, DS4Windows.Core.Logging.DebugEventArgs e)
        {
            if (e.Temporary)
                return;

            if (!string.IsNullOrEmpty(e.Data) && (e.Data.StartsWith("[DI]") || e.Data.StartsWith("[Legacy]")))
                return;

            lastLogMsg.Message = e.Data;
            lastLogMsg.Warning = e.Warning;
        }

        private void OnProfileChanged(object sender, DS4Windows.Core.Logging.ProfileChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    int devIndex = e.DeviceIndex;
                    string prof = e.ProfileName ?? string.Empty;
                    // Phase11 電池残量の一本化: 残量は一覧のモデルを経由せず、スロットのデバイスから直接読む
                    // （再接続の直後は一覧の行がまだ無く、"0" と表示されていた）
                    string battery = "N/A";
                    DS4Windows.Processing.Abstractions.IControllerSlotView batteryDevice = slots.GetSlot(devIndex);
                    if (batteryDevice != null)
                        battery = $"{batteryDevice.Battery}";

                    CompositeDeviceModel item = null;
                    if (conLvViewModel != null && conLvViewModel.ControllerDict != null && conLvViewModel.ControllerDict.ContainsKey(devIndex))
                        item = conLvViewModel.ControllerDict[devIndex];

                    string prolog;
                    // If the event is from AutoProfile, use the AutoTempProfile resource
                    if (e.Source == DS4Windows.Core.Logging.ProfileChangeSource.AutoProfile)
                        prolog = string.Format(DS4Windows.Properties.Resources.UsingAutoTempProfile, (devIndex + 1).ToString(), prof);
                    else
                        prolog = string.Format(Properties.Resources.UsingProfile, (devIndex + 1).ToString(), prof, battery);

                    // 修正前:
                    // if (appSettingsService.ProfileChangedNotification)
                    // {
                    //     ShowProfileSwitchNotification(message);
                    // }

                    // 修正後（判定をメソッド側に委譲し、シンプルに1行で呼ぶ）:
                    // 2026-10-19: 自動プロファイル由来は AutoProfileSwitchNotifyChoice と接続の有無で判定する（AutoProfileNotificationPolicy）
                    if (DS4Windows.UI.Services.AutoProfileNotificationPolicy.ShouldNotify(
                            e.Source, _autoProfileService.AutoProfileSwitchNotifyChoice, item != null))
                        ShowProfileSwitchNotification(prolog);
                }
                catch { }
            }));
        }

        private void ChangeControllerPanel()
        {
            if (conLvViewModel.ControllerCol.Count == 0)
            {
                controllerLV.Visibility = Visibility.Hidden;
                noContLb.Visibility = Visibility.Visible;
            }
            else
            {
                controllerLV.Visibility = Visibility.Visible;
                noContLb.Visibility = Visibility.Hidden;
            }
        }

        private void ChangeAutoProfilesStatus(bool state)
        {
            if (state)
            {
                autoProfilesTimer.Elapsed += AutoProfilesTimer_Elapsed;
                autoProfilesTimer.Start();
                _autoProfileService.Running = true;
            }
            else
            {
                autoProfilesTimer.Stop();
                autoProfilesTimer.Elapsed -= AutoProfilesTimer_Elapsed;
                _autoProfileService.Running = false;
            }
        }

        private void CheckAutoProfileStatus()
        {
            int pathCount = autoProfileHolder.AutoProfileColl.Count;
            bool timerEnabled = _autoProfileService.Running;
            if (pathCount > 0 && !timerEnabled)
            {
                ChangeAutoProfilesStatus(true);
            }
            else if (pathCount == 0 && timerEnabled)
            {
                ChangeAutoProfilesStatus(false);
            }
        }

        private void ControlService_HotplugControllerRecheckAutoProfile(object sender, DS4Windows.Processing.Abstractions.ControllerSlotChangedEventArgs args)
        {
            if (!_autoProfileService.Running)
                return;

            // 接続処理（サービスのスレッド）を待たせず、別スレッドで照合する
            System.Threading.Tasks.Task.Run(() =>
            {
                try { _autoProfileService.CheckProfilesForConnectedController(); }
                catch (Exception ex) { DS4Windows.Core.Logging.AppLogger.LogError($"[DI] CheckProfilesForConnectedController failed: {ex.Message}"); }
            });
        }

        private void AutoProfilesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            autoProfilesTimer.Stop();
            //Console.WriteLine("Event triggered");
            _autoProfileService.CheckProfiles();

            if (_autoProfileService.Running)
            {
                autoProfilesTimer.Start();
            }
        }

        private void ControllerCol_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ChangeControllerPanel();
                System.Collections.IList newitems = e.NewItems;
                if (newitems != null)
                {
                    foreach (CompositeDeviceModel item in newitems)
                    {
                        item.LightContext = new ContextMenu();
                        item.AddLightContextItems();
                        item.Device.SyncChange += DS4Device_SyncChange;
                        item.RequestColorPicker += Item_RequestColorPicker;
                        //item.LightContext.Items.Add(new MenuItem() { Header = "Use Profile Color", IsChecked = !item.UseCustomColor });
                        //item.LightContext.Items.Add(new MenuItem() { Header = "Use Custom Color", IsChecked = item.UseCustomColor });
                    }
                }

                if (lifecycle.Running)
                    trayIconVM.PopulateContextMenu();
            }));
        }

        private void Item_RequestColorPicker(CompositeDeviceModel sender)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = this;
            dialog.colorPicker.SelectedColor = sender.CustomLightColor;
            dialog.ColorChanged += (sender2, color) =>
            {
                sender.UpdateCustomLightColor(color);
            };
            dialog.ShowDialog();
        }

        private void DS4Device_SyncChange(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                trayIconVM.PopulateContextMenu();
            }));
        }

        private void ControlServiceChanged(object sender, EventArgs e)
        {
            //Tester service = sender as Tester;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (lifecycle.Running)
                {
                    StartStopBtn.Content = Translations.Strings.StopText;
                }
                else
                {
                    StartStopBtn.Content = Translations.Strings.StartText;
                }

                StartStopBtn.IsEnabled = true;
                slotManControl.IsEnabled = lifecycle.Running;
            }));
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            About aboutWin = new About(environmentService);
            aboutWin.Owner = this;
            aboutWin.ShowDialog();
        }

        private void StartStopBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangeService();
        }

        public async void ChangeService()
        {
            StartStopBtn.IsEnabled = false;
            //Tester service = root.rootHubtest;
            Task serviceTask = Task.Run(() =>
            {
                if (lifecycle.Running)
                    lifecycle.Stop(immediateUnplug: true);
                else
                    lifecycle.Start();
            });

            // Log exceptions that might occur
            Util.LogAssistBackgroundTask(serviceTask);
            await serviceTask;
        }

        private void LogListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int idx = logListView.SelectedIndex;
            if (idx > -1)
            {
                LogItem temp = logvm.LogItems[idx];
                LogMessageDisplay msgBox = new LogMessageDisplay(temp.Message);
                msgBox.Owner = this;
                msgBox.ShowDialog();
                //MessageBox.Show(temp.Message, "Log");
            }
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            logvm.LogItems.Clear();
            // 最下部の 1 行メッセージ欄（lastMsgLb）も、ログと一緒に消す
            lastLogMsg.Message = string.Empty;
            lastLogMsg.Warning = false;
        }

        // Phase11 W6 K-W6-1: 旧 MainTabCon_SelectionChanged（タブの番号 4 のとき、最下部の 1 行メッセージ欄 lastMsgLb を隠していた）は削除した。
        // 本来は Log タブを隠す意図と思われるが、Output Slots タブが途中に入って番号がずれ、実際は Settings タブで隠れ、
        // Settings で出る警告が見えなかった。すべてのタブで表示する（ユーザー決定 2026-11-02）。

        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            newProfListBtn.IsEnabled = true;
            editProfBtn.IsEnabled = true;
            deleteProfBtn.IsEnabled = true;
            renameProfBtn.IsEnabled = true;
            dupProfBtn.IsEnabled = true;
            importProfBtn.IsEnabled = true;
            exportProfBtn.IsEnabled = true;
        }

        private void RunAtStartCk_Click(object sender, RoutedEventArgs e)
        {
            settingsWrapVM.ShowRunStartPanel = runAtStartCk.IsChecked == true ? Visibility.Visible :
                Visibility.Collapsed;
        }

        private void ContStatusImg_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            int tag = Convert.ToInt32(img.Tag);
            conLvViewModel.CurrentIndex = tag;
            CompositeDeviceModel item = conLvViewModel.CurrentItem;
            //CompositeDeviceModel item = conLvViewModel.ControllerDict[tag];
            if (item != null)
            {
                item.RequestDisconnect();
            }
        }

        private void ExportLogBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text Documents (*.txt)|*.txt";
            dialog.Title = "Select Export File";
            // TODO: Expose config dir
            dialog.InitialDirectory = pathService.AppDataPath;
            if (dialog.ShowDialog() == true)
            {
                LogWriter logWriter = new LogWriter(dialog.FileName, logvm.LogItems.ToList());
                logWriter.Process();
            }
        }

        private void IdColumnTxtB_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            TextBlock statusBk = sender as TextBlock;
            int idx = Convert.ToInt32(statusBk.Tag);
            if (idx >= 0)
            {
                CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];
                item.RequestUpdatedTooltipID();
            }
        }

        /// <summary>
        /// Clear and re-populate tray context menu
        /// </summary>
        private void NotifyIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            notifyIcon.ContextMenu = trayIconVM.ContextMenu;
        }

        /// <summary>
        /// Change profile based on selection
        /// </summary>
        private void SelectProfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = sender as ComboBox;
            if (box == null) return;

            int idx = Convert.ToInt32(box.Tag);
            if (idx > -1 && conLvViewModel.ControllerDict.ContainsKey(idx))
            {
                CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];

                // ★統合抑制フラグ: バックエンドからのプロファイル変更同期中は手動適用をスキップ（無限ループ防止）
                if (item.suppressSelectedIndexChanged)
                {
                    DS4Windows.Core.Logging.AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Suppressed for device {idx}");
                    return;
                }

                if (item.SelectedIndex > -1 && item.SelectedIndex < item.ProfileListCol.Count)
                {
                    string prof = item.ProfileListCol[item.SelectedIndex].Name;

                    // 既に同じプロファイルが適用されていればスキップ
                    if (profileRepo.SelectedProfile[idx] == prof)
                    {
                        DS4Windows.Core.Logging.AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Profile '{prof}' already applied for device {idx}, skipping");
                        return;
                    }

                    DS4Windows.Core.Logging.AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Applying profile '{prof}' for device {idx}");

                    // ★共通適用窓口 ApplyProfileToSlot を呼び出す（すべての切り替え手段を統一）
                    profileSlotApplier.ApplyToSlot(idx, prof, ProfileChangeSource.Manual);

                    trayIconVM.PopulateContextMenu();
                }
            }
        }

        /// <summary>
        /// Change linked profile based on selection
        /// </summary>
        private void LinkedProfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = sender as ComboBox;
            int idx = Convert.ToInt32(box.Tag);
            if (idx > -1 && conLvViewModel.ControllerDict.ContainsKey(idx))
            {
                // LinkedProfileNameプロパティのsetterで処理される
                // Link ON時のみLinkedProfiles.xmlに反映
            }
        }

        private void CustomColorPick_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

        }

        private void LightColorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int idx = Convert.ToInt32(button.Tag);
            CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];
            //(button.ContextMenu.Items[0] as MenuItem).IsChecked = conLvViewModel.ControllerCol[idx].UseCustomColor;
            //(button.ContextMenu.Items[1] as MenuItem).IsChecked = !conLvViewModel.ControllerCol[idx].UseCustomColor;
            button.ContextMenu = item.LightContext;
            button.ContextMenu.IsOpen = true;
        }

        private void MainDS4Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (editor != null)
            {
                editor.Close();
                e.Cancel = true;
                return;
            }
            else if (contextclose)
            {
                return;
            }
            else if (appSettingsService.CloseMinimizes)
            {
                WindowState = WindowState.Minimized;
                e.Cancel = true;
                return;
            }

            // If this method was called directly without sender object then skip the confirmation dialogbox
            if (sender != null && conLvViewModel.ControllerCol.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(Properties.Resources.CloseConfirm, Properties.Resources.Confirm,
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void MainDS4Window_Closed(object sender, EventArgs e)
        {
            hotkeysTimer.Stop();
            autoProfilesTimer.Stop();
            //autoProfileHolder.Save();
            Util.UnregisterNotify(regHandle);

            // Phase5-Watchpoints-Investigation-Report Watchpoint 2対応:
            // Singletonサービス(controlService/profileRepo, SystemEvents)へのイベント購読を
            // 確実に解除する。二重呼び出しされる経路(1251行目)があるため冪等性を確保している。
            conLvViewModel?.Dispose();
            settingsWrapVM?.Dispose();

            // Attempt to dispose of notify icon early
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            Application.Current.Shutdown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (!appSettingsService.FirstRun)
            {
                WindowPlacementHelper.ApplyPlacement(this, appSettingsService, startMinimized);

                // ApplyPlacement 後に WPF 側でサイズや位置が変更される可能性があるため、
                // レイアウト完了後に実際のウィンドウ矩形と WPF プロパティをログに残す。
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // WPF レベルのプロパティ
                        AppLogger.LogTrace($"PostApply: WPF Left={this.Left} Top={this.Top} Width={this.Width} Height={this.Height} ActualWidth={this.ActualWidth} ActualHeight={this.ActualHeight} WindowState={this.WindowState}");

                        // Win32 レベルの取得（物理ピクセル）
                        var phys = WindowPlacementHelper.GetPlacement(this);
                        AppLogger.LogTrace($"PostApply: GetPlacement physicalRect={phys}");

                        // 論理座標としての取得
                        var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                        AppLogger.LogTrace($"PostApply: GetLogicalPlacement => LogicalX={logical.LogicalX} LogicalY={logical.LogicalY} LogicalWidth={logical.LogicalWidth} LogicalHeight={logical.LogicalHeight}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogDebug($"PostApply: Failed to log post-apply window metrics: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }

            // Restore controller tab column widths
            RestoreControllerColumnWidths();

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            HookWindowMessages(source);
            source.AddHook(WndProc);
        }

        private bool inHotPlug = false;
        private int hotplugCounter = 0;
        private object hotplugCounterLock = new object();
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int WM_COPYDATA = 0x004A;
        private const int HOTPLUG_CHECK_DELAY = 2000;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
            IntPtr lParam, ref bool handled)
        {
            // Phase9 9-F F-2b: メッセージの種類ごとの処理は、メソッドへ分けた（順序・条件は従来のまま）
            switch (msg)
            {
                case Util.WM_DEVICECHANGE:
                    HandleDeviceChange(wParam);
                    break;
                case WM_COPYDATA:
                    HandleCopyData(lParam);   // MainWindow.Ipc.cs
                    break;
                default: break;
            }

            return IntPtr.Zero;
        }


        /// <summary>
        /// Phase9 9-F F-2b: デバイスの着脱（ホットプラグ）。カウンターのロックと、実行中のフラグ（inHotPlug）は、従来のまま。
        /// </summary>
        private void HandleDeviceChange(IntPtr wParam)
        {
            if (appSettingsService.RunHotPlug)
            {
                Int32 Type = wParam.ToInt32();
                if (Type == DBT_DEVICEARRIVAL ||
                    Type == DBT_DEVICEREMOVECOMPLETE)
                {
                    lock (hotplugCounterLock)
                    {
                        hotplugCounter++;
                    }

                    if (!inHotPlug)
                    {
                        inHotPlug = true;
                        Task hotplugTask = Task.Run(() => { InnerHotplug2(); });
                        // Log exceptions that might occur
                        Util.LogAssistBackgroundTask(hotplugTask);
                    }
                }
            }
        }

        private void InnerHotplug2()
        {
            inHotPlug = true;

            bool loopHotplug = false;

            lock (hotplugCounterLock)
            {
                loopHotplug = hotplugCounter > 0;
                hotplugCounter = 0;
            }

            runtimeFeatures.UpdateHidHiddenAttributes();
            while (loopHotplug == true)
            {
                Thread.Sleep(HOTPLUG_CHECK_DELAY);
                lifecycle.HotPlug();

                lock (hotplugCounterLock)
                {
                    loopHotplug = hotplugCounter > 0;
                    hotplugCounter = 0;
                }
            }

            inHotPlug = false;
        }

        private void HookWindowMessages(HwndSource source)
        {
            bool result = Util.RegisterNotify(source.Handle, environmentService.HidClassGuid, ref regHandle);
            if (!result)
            {
                Application.Current.Shutdown();   // Phase14b-Step16: 旧 App.Current（同じもの）
            }
        }

        private void ProfEditSBtn_Click(object sender, RoutedEventArgs e)
        {
            Control temp = sender as Control;
            int idx = Convert.ToInt32(temp.Tag);
            controllerLV.SelectedIndex = idx;
            CompositeDeviceModel item = conLvViewModel.CurrentItem;

            if (item != null && item.SelectedIndex != -1)
            {
                ProfileEntity entity = profileListHolder.ProfileListCol[item.SelectedIndex];
                ShowProfileEditor(idx, entity);
                mainTabCon.SelectedIndex = 1;
            }
        }

        private void NewProfBtn_Click(object sender, RoutedEventArgs e)
        {
            Control temp = sender as Control;
            int idx = Convert.ToInt32(temp.Tag);
            controllerLV.SelectedIndex = idx;
            ShowProfileEditor(idx, null);
            mainTabCon.SelectedIndex = 1;
            //controllerLV.Focus();
        }

        // Ex Mode Re-Enable
        private async void HideDS4ContCk_Click(object sender, RoutedEventArgs e)
        {
            StartStopBtn.IsEnabled = false;
            //bool checkStatus = hideDS4ContCk.IsChecked == true;
            hideDS4ContCk.IsEnabled = false;
            Task serviceTask = Task.Run(() =>
            {
                lifecycle.Stop();
                lifecycle.Start();
            });

            // Log exceptions that might occur
            Util.LogAssistBackgroundTask(serviceTask);
            await serviceTask;

            hideDS4ContCk.IsEnabled = true;
            StartStopBtn.IsEnabled = true;
        }

        private void UseOscServerCk_Click(object sender, RoutedEventArgs e)
        {
            bool status = useOscServerCk.IsChecked == true;
            runtimeFeatures.ChangeOscListenerStatus(status);
        }

        private void UseOscSenderCk_Click(object sender, RoutedEventArgs e)
        {
            bool status = useOscSenderCk.IsChecked == true;
            runtimeFeatures.ChangeOscSenderStatus(status);
        }

        private async void UseUdpServerCk_Click(object sender, RoutedEventArgs e)
        {
            bool status = useUdpServerCk.IsChecked == true;
            if (!status)
            {
                runtimeFeatures.ChangeMotionEventStatus(status);
                await Task.Delay(200).ContinueWith((t) =>
                {
                    runtimeFeatures.ChangeUdpStatus(status);
                });
            }
            else
            {
                runtimeFeatures.ChangeUdpStatus(status);
                await Task.Delay(200).ContinueWith((t) =>
                {
                    runtimeFeatures.ChangeMotionEventStatus(status);
                });
            }
        }

        private void ProfFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(pathService.AppDataPath + "\\Profiles");
            startInfo.UseShellExecute = true;
            try
            {
                using (Process temp = Process.Start(startInfo))
                {
                }
            }
            catch { }
        }

        private void ControlPanelBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("control", "joy.cpl");
        }

        private async void DriverSetupBtn_Click(object sender, RoutedEventArgs e)
        {
            StartStopBtn.IsEnabled = false;
            await Task.Run(() =>
            {
                if (lifecycle.Running)
                    lifecycle.Stop();
            });

            StartStopBtn.IsEnabled = true;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = pathService.ExecutablePath;
            startInfo.Arguments = "-driverinstall";
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            try
            {
                using (Process temp = Process.Start(startInfo))
                {
                    temp.WaitForExit();
                    environmentService.RefreshHidHideInfo();
                    environmentService.RefreshFakerInputInfo();

                    settingsWrapVM.DriverCheckRefresh();
                }
            }
            catch { }
        }

        private void CheckUpdatesBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    // Manual check should ignore any previously skipped version
                    if (changelogService.CheckNewerVersionExists(out var version, appSettingsService.LastVersionChecked, false, false))
                        DisplayUpdaterWindow(version.ToString());
                    else
                        Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
                }
                catch
                {
                    Dispatcher.Invoke(() => MessageBox.Show(Strings.FailedToRetrieveLatestVersion, "DS4Windows Updater"));
                    // bubble the exception up to allow to see what's wrong in the log
                    throw;
                }
            });
        }

        private void ImportProfBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".xml";
            dialog.Filter = "DS4Windows Profile (*.xml)|*.xml";
            dialog.Title = "Select Profile to Import File";
            if (pathService.AppDataPath != pathService.ExecutableDirectory)
                dialog.InitialDirectory = Path.Combine(pathService.AppDataPath, "Profiles");
            else
                dialog.InitialDirectory = pathService.ExecutableDirectory + @"\Profiles\";

            if (dialog.ShowDialog() == true)
            {
                string[] files = dialog.FileNames;
                for (int i = 0, arlen = files.Length; i < arlen; i++)
                {
                    string profilename = System.IO.Path.GetFileName(files[i]);
                    string basename = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                    File.Copy(dialog.FileNames[i], pathService.AppDataPath + "\\Profiles\\" + profilename, true);
                    profileListHolder.AddProfileSort(basename);
                }
            }
        }

        private void ExportProfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.AddExtension = true;
                dialog.DefaultExt = ".xml";
                dialog.Filter = "DS4Windows Profile (*.xml)|*.xml";
                dialog.Title = "Select Profile to Export File";
                Stream stream;
                int idx = profilesListBox.SelectedIndex;
                Stream profile = new StreamReader(pathService.AppDataPath + "\\Profiles\\" + profileListHolder.ProfileListCol[idx].Name + ".xml").BaseStream;
                if (dialog.ShowDialog() == true)
                {
                    if ((stream = dialog.OpenFile()) != null)
                    {
                        profile.CopyTo(stream);
                        profile.Close();
                        stream.Close();
                    }
                }
            }
        }

        private void DupProfBtn_Click(object sender, RoutedEventArgs e)
        {
            string filename = "";
            if (profilesListBox.SelectedIndex >= 0)
            {
                int idx = profilesListBox.SelectedIndex;
                filename = profileListHolder.ProfileListCol[idx].Name;
                dupBox.OldFilename = filename;
                dupBoxBar.Visibility = Visibility.Visible;
                dupBox.Save -= DupBox_Save;
                dupBox.Cancel -= DupBox_Cancel;
                dupBox.Save += DupBox_Save;
                dupBox.Cancel += DupBox_Cancel;
            }
        }

        private void DupBox_Cancel(object sender, EventArgs e)
        {
            dupBoxBar.Visibility = Visibility.Collapsed;
        }

        private void DupBox_Save(DupBox sender, string profilename)
        {
            profileListHolder.AddProfileSort(profilename);
            dupBoxBar.Visibility = Visibility.Collapsed;
        }

        private void DeleteProfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                int idx = profilesListBox.SelectedIndex;
                ProfileEntity entity = profileListHolder.ProfileListCol[idx];
                string filename = entity.Name;
                if (MessageBox.Show(Properties.Resources.ProfileCannotRestore.Replace("*Profile name*", "\"" + filename + "\""),
                    Properties.Resources.DeleteProfile,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    entity.DeleteFile();
                    profileListHolder.ProfileListCol.RemoveAt(idx);
                }
            }
        }

        private void SelectProfCombo_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void MainDS4Window_StateChanged(object _sender, EventArgs _e)
        {
            CheckMinStatus();
        }

        public void CheckMinStatus()
        {
            bool minToTask = appSettingsService.MinimizeToTaskbar;
            if (WindowState == WindowState.Minimized && !minToTask)
            {
                Hide();
                showAppInTaskbar = false;
            }
            else if (WindowState == WindowState.Normal && !minToTask)
            {
                Show();
                showAppInTaskbar = true;
            }
        }

        private void MainDS4Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // プロフィール編集画面が開いている場合はKeepsizeで制御
            if (WindowState != WindowState.Minimized && editor != null && editor.Keepsize && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                appSettingsService.FormWidth = logical.LogicalWidth;
                appSettingsService.FormHeight = logical.LogicalHeight;
                AppLogger.LogTrace($"MainWindow.SizeChanged: Saved Logical Width={appSettingsService.FormWidth} Height={appSettingsService.FormHeight}");
            }
            // 編集画面が開いていない場合は常に保存
            else if (WindowState != WindowState.Minimized && editor == null && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                appSettingsService.FormWidth = logical.LogicalWidth;
                appSettingsService.FormHeight = logical.LogicalHeight;
                AppLogger.LogTrace($"MainWindow.SizeChanged: Saved Logical Width={appSettingsService.FormWidth} Height={appSettingsService.FormHeight}");
            }
        }

        private void MainDS4Window_LocationChanged(object sender, EventArgs e)
        {
            // プロフィール編集画面が開いている場合はKeepsizeで制御
            if (WindowState != WindowState.Minimized && editor != null && editor.Keepsize && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                appSettingsService.FormLocationX = logical.LogicalX;
                appSettingsService.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.LocationChanged: Saved Logical X={appSettingsService.FormLocationX} Y={appSettingsService.FormLocationY}");
            }
            // 編集画面が開いていない場合は常に保存
            else if (WindowState != WindowState.Minimized && editor == null && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                appSettingsService.FormLocationX = logical.LogicalX;
                appSettingsService.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.LocationChanged: Saved Logical X={appSettingsService.FormLocationX} Y={appSettingsService.FormLocationY}");
            }
        }

        private void NotifyIcon_TrayMiddleMouseDown(object sender, RoutedEventArgs e)
        {
            contextclose = true;
            Close();
        }

        private void SwipeTouchCk_Click(object sender, RoutedEventArgs e)
        {
            bool status = swipeTouchCk.IsChecked == true;
            ChangeHotkeysStatus(status);
        }

        private void EditProfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                ProfileEntity entity = profileListHolder.ProfileListCol[profilesListBox.SelectedIndex];
                ShowProfileEditor(ProfileEditor.NoTargetDevice, entity);
            }
        }

        private void ProfileEditor_Closed(object sender, EventArgs e)
        {
            profDockPanel.Children.Remove(editor);
            profOptsToolbar.Visibility = Visibility.Visible;
            profilesListBox.Visibility = Visibility.Visible;
            // preserveSizeの操作は不要
            if (!editor.Keepsize)
            {
                this.Width = oldSize.Width;
                this.Height = oldSize.Height;
            }
            else
            {
                oldSize = new Size(Width, Height);
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                appSettingsService.FormWidth = logical.LogicalWidth;
                appSettingsService.FormHeight = logical.LogicalHeight;
                appSettingsService.FormLocationX = logical.LogicalX;
                appSettingsService.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.ProfileEditor_Closed: Saved Logical Rect X={appSettingsService.FormLocationX} Y={appSettingsService.FormLocationY} W={appSettingsService.FormWidth} H={appSettingsService.FormHeight}");
            }
            editor = null;
            // Restore the tab that was active before opening the profile editor
            try
            {
                if (previousTabIndex >= 0 && previousTabIndex < mainTabCon.Items.Count)
                    mainTabCon.SelectedIndex = previousTabIndex;
                else
                    mainTabCon.SelectedIndex = 0;
            }
            catch
            {
                mainTabCon.SelectedIndex = 0;
            }
            mainWinVM.FullTabsEnabled = true;
            //Task.Run(() => GC.Collect(0, GCCollectionMode.Forced, false));
        }

        private void NewProfListBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowProfileEditor(ProfileEditor.NoTargetDevice, null);
        }

        /// <summary>
        /// プロファイル編集画面を開く。
        /// Phase6-Step7b: 編集は常に作業スロット（Global.TEST_PROFILE_INDEX）に対して行い、保存・適用を押すまで
        /// コントローラーには反映しない。編集スロットは ProfileEditor の内部で決まり、呼び出し元は指定しない。
        /// </summary>
        /// <param name="targetDevice">
        /// Edit／New Profile ボタンを押したコントローラーのスロット番号（ランブルテスト・ライトバーのプレビュー・校正・適用先）。
        /// プロファイル一覧から開いた場合は ProfileEditor.NoTargetDevice
        /// </param>
        /// <param name="entity">編集するプロファイル。null は新規作成</param>
        private void ShowProfileEditor(int targetDevice, ProfileEntity entity)
        {
            if (editor == null)
            {
                // Remember current tab so we can restore it when editor closes
                try { previousTabIndex = mainTabCon.SelectedIndex; } catch { previousTabIndex = 0; }
                profOptsToolbar.Visibility = Visibility.Collapsed;
                profilesListBox.Visibility = Visibility.Collapsed;
                mainWinVM.FullTabsEnabled = false;

                // 編集画面を開く前のサイズを保存（閉じる時に元に戻すため）
                oldSize.Width = Width;
                oldSize.Height = Height;

                // 【仕様】プロフィール編集画面は開く直前のメインウィンドウと同じサイズ・ポジションで開く
                // サイズ変更処理は不要（以前の最小サイズチェックロジックは無効化）
                // if (this.Width < WindowLayoutDefaults.PROFILE_EDITOR_WIDTH)
                // {
                //     this.Width = WindowLayoutDefaults.PROFILE_EDITOR_WIDTH;
                // }
                //
                // if (this.Height < WindowLayoutDefaults.PROFILE_EDITOR_HEIGHT)
                // {
                //     this.Height = WindowLayoutDefaults.PROFILE_EDITOR_HEIGHT;
                // }

                editor = editorWindowFactory.CreateProfileEditor(targetDevice);
                editor.ProfileSaved += Editor_ProfileSaved;
                editor.ServiceRestartRequested += Editor_ServiceRestartRequested;
                editor.ProfilesUpdatedExternally += Editor_ProfilesUpdatedExternally;
                editor.Closed += ProfileEditor_Closed;
                profDockPanel.Children.Add(editor);
                editor.Reload(entity);

                // When the profile editor is opened, emit missing-action logs once
                // per editor-open. We temporarily clear the per-load suppression set
                // so the same missing-action messages will be logged every time the
                // editor is opened (user expectation).
                try
                {
                    // Force emitting missing-action logs for this editor open (ignore suppression).
                    // Pass explicit profile name to avoid confusion with device's currently-assigned profile.
                    string profileNameForLog = entity != null ? entity.Name : "(new profile)";
                    // Phase6-Step7b: 読み込んだプロファイルのアクションを調べるため、実機ではなく作業スロットを渡す
                    profileRepo.EmitMissingActionLogsForDevice(ControllerLimits.TestProfileIndex, true, profileNameForLog);
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"[MainWindow] Failed to emit missing action logs: {ex.Message}");
                }
            }

        }

        #region Profile List & Controllers Synchronization (4-Stage Transaction)


        /// <summary>
        /// プロファイル保存・更新・リネーム時の4段階トランザクション同期メソッド。
        /// ComboBoxの選択解除・イベント抑制を行い、ディスク正本からリストを安全に再構築した上で、
        /// 各スロットの選択復元および最新設定のホットリロードを一括実行します。
        /// </summary>
        /// <param name="oldProfile">Rename時の旧プロファイル名（保存・更新時はnull）</param>
        /// <param name="newProfile">Rename時の新プロファイル名（保存・更新時はnull）</param>
        /// <param name="targetProfiles">
        /// Phase6-Step10-3b-3: 複数のプロファイルの XML が書き換えられた場合（アクションの除去・名前変更）の対象一覧。
        /// 指定すると、これらのプロファイルを使っているスロットだけに再適用する（oldProfile／newProfile は null にする）
        /// </param>
        private void SyncProfileListAndControllers(string oldProfile = null, string newProfile = null,
            IReadOnlyCollection<string> targetProfiles = null)
        {
            int slotCount = environmentService.ControllerSlotLimit;

            string[] activeProfiles = new string[slotCount];
            try
            {
                for (int i = 0; i < slotCount; i++)
                {
                    activeProfiles[i] = profileRepo.ProfilePath[i] ?? string.Empty;
                }

                // ② 【SSOT 再構築 (Rebuild)】
                bool isRename = !string.IsNullOrWhiteSpace(oldProfile) && !string.IsNullOrWhiteSpace(newProfile);
                if (isRename)
                {
                    // Phase10-Step4d W7: 実行時の保持（スロットの現在・直前・選択、一時プロファイル、リンク、自動プロファイルのルール）は、プロファイルの固定 ID を保管する窓口のため
                    // （W5b〜W5d）、名前を変えても、読み出す名前が自動で現在の名前になる。ここで配列や一覧を書き換える処理は、不要になった。
                    // 保存先のファイルの名前と ID を、現在の状態で書き直す（名前変更の直後に、1 回）
                    profileRepo.SaveLinkedProfiles();
                    _autoProfileService?.Holder?.Save();
                    appSettingsService.Save();
                    // SA（Actions.xml）の切り替え先は、ID から現在の名前に引き直して保存する
                    profileRepo.RefreshProfileSwitchTargets(oldProfile, newProfile);
                    // Phase10-Step4 W6: Controls の Load Profile の切り替え先（各プロファイルの <Control><Special>）は、ID のない古いプロファイルのファイルのため、旧名を新名へ書き換える処理を残す
                    // （TODO（Phase10-Step4d W7）: 全プロファイルのファイルに ID を書き足す移行ができたら撤去する。ReplaceActionReference も同じ）
                    profileRepo.ReplaceControlProfileSwitchTarget(oldProfile, newProfile);
                }

                // ディスク全XMLを正（SSOT）としてプロファイル一覧を再構築
                // （※isProfileSyncing中のため、Clear()が走ってもTargetExceptionクラッシュは完全に防止される）
                ProfileListHolder.Refresh();

                // ③ 【復元・再選択 (Re-attach)】
                // メモリ上の ProfilePath を確定させ、ComboBox の選択状態を復元
                for (int i = 0; i < slotCount; i++)
                {
                    if (!string.IsNullOrEmpty(activeProfiles[i]))
                    {
                        profileRepo.ProfilePath[i] = activeProfiles[i];
                    }
                }
            }
            finally
            {
            }

            // ④ 【再適用・ホットリロード (Re-apply)】
            // 保存またはリネームの影響を受けたスロットに対し、共通窓口経由で再適用を実行
            string targetProfile = !string.IsNullOrWhiteSpace(newProfile) ? newProfile : oldProfile;
            for (int i = 0; i < slotCount; i++)
            {
                bool matches = targetProfiles != null
                    ? !string.IsNullOrEmpty(activeProfiles[i]) && targetProfiles.Any(p =>
                        string.Equals(p, activeProfiles[i], StringComparison.CurrentCultureIgnoreCase))
                    : string.IsNullOrEmpty(targetProfile) ||
                      string.Equals(activeProfiles[i], targetProfile, StringComparison.CurrentCultureIgnoreCase);
                // Phase11 W6 K-W6-2: コントローラーが接続されているスロットにだけ適用する。サービスの停止中は、ProfilePath が同じ名前のまま残っているため、
                // 接続していないスロットにも適用され、無意味なログと通知が出ていた。プロファイルの XML は保存済みで、次の接続時に読まれる
                if (matches && IsSlotConnected(i))
                {
                    profileSlotApplier.ApplyToSlot(i, activeProfiles[i], ProfileChangeSource.Manual);
                }
            }
        }

        // Phase11 W6 K-W6-2: そのスロットに、接続中のコントローラーがあるか（サービス停止中は、どのスロットも false）
        private bool IsSlotConnected(int slot)
        {
            return slots.GetSlot(slot) != null;
        }

        #endregion

        /// <summary>
        /// プロファイル編集画面で [保存] または [適用] が実行された際のコールバック。
        /// 安全な4段階同期メソッドを経由することで、クラッシュを防ぎつつリスト同期とホットリロードを一括実行します。
        /// </summary>
        private void Editor_ProfileSaved(ProfileEditor sender, string profile)
        {
            // 保存されたプロファイルを対象として、安全にリスト再同期＆該当スロットのホットリロードを実行
            SyncProfileListAndControllers(oldProfile: profile, newProfile: null);
        }

        // Phase11 W4（Issue #4 の 1）: 編集画面が保存のときに求める、サービスの再起動（DS3 のピッチ／ロールの擬似の設定が変わったとき）
        private void Editor_ServiceRestartRequested(object sender, EventArgs e)
        {
            RestartService();
        }

        /// <summary>
        /// Phase11 W4: サービスを、停止→開始の順に 1 つの処理で再起動する。旧 ProfileEditor.SaveBtn_Click は、ChangeService
        /// （停止／開始の切り替え）を 2 回続けて呼んでおり、1 回目の完了を待たなかった。ここでは切り替えを使わず、順に行う。
        /// 止まっているサービスは開始しない（ユーザーが止めたものを勝手に動かさない。設定は、次の開始で効く）。
        /// </summary>
        private async void RestartService()
        {
            if (!lifecycle.Running) { return; }

            AppLogger.LogDebug("[MainWindow] UseDs3PitchRollSim の変更を反映するため、サービスを再起動する");
            StartStopBtn.IsEnabled = false;
            Task serviceTask = Task.Run(() =>
            {
                lifecycle.Stop(immediateUnplug: true);
                lifecycle.Start();
            });

            // Log exceptions that might occur
            Util.LogAssistBackgroundTask(serviceTask);
            await serviceTask;
        }
        // Phase6-Step10-3b-3: アクションの除去・名前変更でプロファイルの XML が書き換わったときも、
        // 保存時と同じ経路（一覧の読み直し＋該当スロットへの再適用）を使う
        private void Editor_ProfilesUpdatedExternally(IReadOnlyList<string> profiles)
        {
            SyncProfileListAndControllers(targetProfiles: profiles);
        }

        private void ControllerLV_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Save column widths when user finishes resizing (similar to form size/position)
            SaveControllerColumnWidths();
        }

        private void SaveControllerColumnWidths()
        {
            var gridView = controllerLV.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 10)
            {
                appSettingsService.ControllerIndexColWidth = (int)gridView.Columns[0].Width;
                appSettingsService.ControllerIdColWidth = (int)gridView.Columns[1].Width;
                appSettingsService.ControllerStatusColWidth = (int)gridView.Columns[2].Width;
                appSettingsService.ControllerExclusiveColWidth = (int)gridView.Columns[3].Width;
                appSettingsService.ControllerBatteryColWidth = (int)gridView.Columns[4].Width;
                appSettingsService.ControllerSelectProfileColWidth = (int)gridView.Columns[5].Width;
                appSettingsService.ControllerEditColWidth = (int)gridView.Columns[6].Width;
                appSettingsService.ControllerLinkedProfileColWidth = (int)gridView.Columns[7].Width;
                appSettingsService.ControllerLinkProfIdColWidth = (int)gridView.Columns[8].Width;
                appSettingsService.ControllerCustomColorColWidth = (int)gridView.Columns[9].Width;
            }
        }

        private void RestoreControllerColumnWidths()
        {
            var gridView = controllerLV.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 10)
            {
                gridView.Columns[0].Width = appSettingsService.ControllerIndexColWidth;
                gridView.Columns[1].Width = appSettingsService.ControllerIdColWidth;
                gridView.Columns[2].Width = appSettingsService.ControllerStatusColWidth;
                gridView.Columns[3].Width = appSettingsService.ControllerExclusiveColWidth;
                gridView.Columns[4].Width = appSettingsService.ControllerBatteryColWidth;
                gridView.Columns[5].Width = appSettingsService.ControllerSelectProfileColWidth;
                gridView.Columns[6].Width = appSettingsService.ControllerEditColWidth;
                gridView.Columns[7].Width = appSettingsService.ControllerLinkedProfileColWidth;
                gridView.Columns[8].Width = appSettingsService.ControllerLinkProfIdColWidth;
                gridView.Columns[9].Width = appSettingsService.ControllerCustomColorColWidth;
            }
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!showAppInTaskbar)
            {
                Show();
            }

            WindowState = WindowState.Normal;
        }

        private void ProfilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                ProfileEntity entity = profileListHolder.ProfileListCol[profilesListBox.SelectedIndex];
                ShowProfileEditor(ProfileEditor.NoTargetDevice, entity);
            }
        }

        private void Html5GameBtn_Click(object sender, RoutedEventArgs e)
        {
            Util.StartProcessHelper("https://gamepad-tester.com/");
        }

        private void HidHideBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = Util.GetHidHideClientPath();
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(path);
                    startInfo.UseShellExecute = true;
                    using (Process proc = Process.Start(startInfo)) { }
                }
                catch { }
            }
        }

        private void FakeExeNameExplainBtn_Click(object sender, RoutedEventArgs e)
        {
            string message = Translations.Strings.CustomExeNameInfo;
            MessageBox.Show(message, "Custom Exe Name Info", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void XinputCheckerBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(pathService.ExecutableDirectory, "Tools",
                "XInputChecker", "XInputChecker.exe");

            if (File.Exists(path))
            {
                try
                {
                    using (Process proc = Process.Start(path)) { }
                }
                catch { }
            }
        }

        private void ChecklogViewBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangelogWindow changelogWin = new ChangelogWindow(changelogService);
            changelogWin.ShowDialog();
        }

        private void DeviceOptionSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ControllerRegisterOptionsWindow optsWindow =
                new ControllerRegisterOptionsWindow(appSettingsService.DeviceOptions, slots,
                    appSettingsService);

            optsWindow.Owner = this;
            optsWindow.Show();
        }

        private void RenameProfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                int idx = profilesListBox.SelectedIndex;
                ProfileEntity entity = profileListHolder.ProfileListCol[idx];
                string filename = Path.Combine(pathService.AppDataPath,
                    "Profiles", $"{entity.Name}.xml");

                // Disallow renaming Default profile
                if (entity.Name != "Default" &&
                    File.Exists(filename))
                {
                    RenameProfileWindow renameWin = new RenameProfileWindow(pathService);
                    renameWin.ChangeProfileName(entity.Name);
                    bool? result = renameWin.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        string oldProfileName = entity.Name;
                        string newProfileName = renameWin.RenameProfileVM.ProfileName;
                        entity.RenameProfile(newProfileName);
                        // Phase6-Step10-4-5b: 名前変更を、全スロットの現在のプロファイル名・リンクプロファイルへ反映し、再適用する
                        // （従来はファイルと一覧の名前だけを変えていたため、サービスの再開後に Selected Profile が空欄になった）
                        if (string.Equals(entity.Name, newProfileName, StringComparison.Ordinal) &&
                            !string.Equals(oldProfileName, newProfileName, StringComparison.Ordinal))
                        {
                            SyncProfileListAndControllers(oldProfile: oldProfileName, newProfile: newProfileName);
                        }

                        trayIconVM.PopulateContextMenu();
                    }
                }
            }
        }

        private void ProcessPriorityComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using var process = Process.GetCurrentProcess();
            var s = (ComboBox)sender;
            var selectedPriority = (ProcessPriorityClass)s.SelectedItem;
            if (!environmentService.IsAdministrator() && selectedPriority == ProcessPriorityClass.RealTime)
            {
                MessageBox.Show(Strings.RealTimeNoAdmin);
                selectedPriority = ProcessPriorityClass.High;
                settingsWrapVM.ProcessPriorityIndex = DS4Windows.Core.Platform.ProcessPriorityOptions.IndexOf(ProcessPriorityClass.High);
            }
            process.PriorityClass = selectedPriority;
        }
    }
}