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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Management;
using System.ComponentModel;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Utilities;
using DS4Windows.Translations;
using DS4Windows.UI.Abstractions;
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
        // 画面のスレッドへの受け渡し（ViewModel へ渡す。状態を持たない）
        private readonly IUiDispatcher uiDispatcher = new WpfUiDispatcher();
        // トレイの右クリックメニュー（ViewModel の項目 TrayMenuItemModel から組み立てる）
        private readonly ContextMenu trayContextMenu = new ContextMenu();
        private SettingsViewModel settingsWrapVM;
        private IntPtr regHandle = new IntPtr();
        private IntPtr regHandleXusb = new IntPtr();
        private bool showAppInTaskbar = false;
        private ManagementEventWatcher? managementEvWatcher;
        private bool wasrunning = false;
        private AutoProfileHolder autoProfileHolder;
        private System.Timers.Timer? hotkeysTimer;
        // タイマーの処理中に止められたとき（終了・サービス停止）、始め直さないための印
        private volatile bool hotkeysTimerActive;
        // 更新の確認の判断（画面に依存しない）
        private readonly UpdateCheckPolicy updateCheckPolicy;
        // スワイプでのプロファイル切替の規則（画面に依存しない。値の計算だけを担当する）
        private readonly TouchpadSwipeProfileRule swipeRule = new TouchpadSwipeProfileRule();
        // 廃止: DIコンテナ注入の _autoProfileService を直接利用
        private readonly AutoProfileRunner _autoProfileService;
        private readonly MainWindowLayoutViewModel layout;
        // トレイ・通知・テーマの切り替えと、プロファイルの選択・編集後の同期は、それぞれの部品が受け持つ
        private readonly TrayAppearanceController trayAppearance;
        private readonly ProfileSelectionCoordinator profileSelection;
        private readonly DS4Windows.UI.Abstractions.IUiThemeAndLanguage themeLanguage;   // Phase14b-Step16（D11）
        private readonly DS4Windows.UI.Abstractions.IShellViewModelFactory _viewModelFactory;   // 子画面の ViewModel の生成
        private readonly IpcCommandDispatcher ipcDispatcher;   // CLI のコマンドの実行
        private MainWindowIpcHost ipcHost;   // コマンドが画面に頼む操作（一覧の ViewModel ができてから作る）
        private ProfileEditor? editor;
        private int previousTabIndex = 0;
#pragma warning disable CS0414 // preserveSize kept for behavior compatibility with older logic
        private bool preserveSize = true;
#pragma warning restore CS0414
        private Size oldSize;
        private bool contextclose;
        private bool startMinimized;

        public ProfileList ProfileListHolder { get => profileListHolder; }

        public bool IsInitialShow { get; set; }

        // 旧 AppHost.GetService の 17 件を、コンストラクター引数に替えた。
        // IMainWindowFactory（実装は MainWindowFactory）が DI から解決して渡す。

        public MainWindow(ArgumentParser parser,
            MainWindowLayoutViewModel layout,
            IpcCommandDispatcher ipcDispatcher,
            MainWindowsViewModel mainWindowsViewModel,
            AutoProfileRunner autoProfileService,
            DS4Windows.UI.Abstractions.IShellViewModelFactory viewModelFactory,
            DS4Windows.UI.Abstractions.IUiThemeAndLanguage themeLanguage,
            UpdateCheckPolicy updateCheckPolicy,
            TrayAppearanceController trayAppearance,
            ProfileSelectionCoordinator profileSelection)
        {
            ArgumentNullException.ThrowIfNull(trayAppearance);
            this.trayAppearance = trayAppearance;
            ArgumentNullException.ThrowIfNull(profileSelection);
            this.profileSelection = profileSelection;
            ArgumentNullException.ThrowIfNull(updateCheckPolicy);
            this.updateCheckPolicy = updateCheckPolicy;
            // テーマ・言語の切り替え
            ArgumentNullException.ThrowIfNull(themeLanguage);
            this.themeLanguage = themeLanguage;
            // XAML初期化前にウィンドウサイズを設定（WindowLayoutDefaults から）
            Width = WindowLayoutDefaults.MAIN_WINDOW_WIDTH;
            Height = WindowLayoutDefaults.MAIN_WINDOW_HEIGHT;

            InitializeComponent();
            // XAML の資源 ImgLocPaths（画像のパス）に、テーマの切り替えの通知元を渡す
            (TryFindResource("ImgLocPaths") as ImageLocationPaths)?.AttachThemeSource(themeLanguage);
            ArgumentNullException.ThrowIfNull(mainWindowsViewModel);
            mainWinVM = mainWindowsViewModel;
            DataContext = mainWinVM;
            mainWinVM.Navigator = new MainWindowNavigator(this);
            ArgumentNullException.ThrowIfNull(layout);
            this.layout = layout;
            profileListHolder = mainWinVM.CreateProfileList();
            ArgumentNullException.ThrowIfNull(ipcDispatcher);
            this.ipcDispatcher = ipcDispatcher;

            // Initialize log settings ComboBox
            logMinLevelComboBox.SelectedValue = mainWinVM.LogMinLevel;


            // ログ設定の反映の窓口（App が LoggerHolder の生成直後に接続する）
            ArgumentNullException.ThrowIfNull(viewModelFactory);
            _viewModelFactory = viewModelFactory;
            settingsWrapVM = viewModelFactory.CreateSettingsViewModel();
            settingsTab.DataContext = settingsWrapVM;
            // XAML から生成される LanguagePackControl へ、保持済みのサービスを渡す（Pure DI）。
            languagePackControl.Initialize(layout.Save, viewModelFactory.CreateLanguagePackViewModel(), themeLanguage);
            logvm = viewModelFactory.CreateLogViewModel(uiDispatcher);
            //logListView.ItemsSource = logvm.LogItems;
            logListView.DataContext = logvm;
            lastMsgLb.DataContext = lastLogMsg;

            profileListHolder.Refresh();
            profilesListBox.ItemsSource = profileListHolder.ProfileListCol;

            UpdateStartStopText();

            conLvViewModel = viewModelFactory.CreateControllerListViewModel(profileListHolder, uiDispatcher, new WpfImageResourceLookup());
            ipcHost = new MainWindowIpcHost(() => { _ = ChangeService(); }, CloseApplicationForIpc, conLvViewModel, profileListHolder);
            controllerLV.DataContext = conLvViewModel;
            controllerLV.ItemsSource = conLvViewModel.ControllerCol;
            ChangeControllerPanel();

            // Sort device by input slot number
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(controllerLV.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("DevIndex", ListSortDirection.Ascending));
            view.Refresh();

            trayIconVM = _viewModelFactory.CreateTrayIconViewModel(profileListHolder, uiDispatcher);

            // Need to define before calling TaskbarIcon.ForceCreate
            notifyIcon.DataContext = trayIconVM;
            trayIconVM.MenuChanged += TrayIconVM_MenuChanged;
            TrayContextMenuBuilder.Rebuild(trayContextMenu, trayIconVM.MenuItems);
            notifyIcon.CustomName = mainWinVM.ExecutablePath;

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
                catch (Exception caught) {
                    // Ignore exception
                    AppLogger.LogDebug($"[MainWindow:199] ignored {caught.GetType().Name}: {caught.Message}");
                }
            }

            startMinimized = mainWinVM.StartMinimized || parser.Mini;

            bool isElevated = mainWinVM.IsAdministrator();
            if (isElevated)
            {
                uacImg.Visibility = Visibility.Collapsed;
            }

            noContLb.Content = string.Format(Strings.NoControllersConnected,
                mainWinVM.ControllerSlotLimit);

            // 二重実体化防止: 注入済み _autoProfileService を直接利用するため撤去
            // AutoProfiles の Initialize に渡すため、解決を前へ移した（解決先は同じ）
            ArgumentNullException.ThrowIfNull(autoProfileService);
            _autoProfileService = autoProfileService;

            // XAML から生成されるコントロールへ、保持済みのサービスを渡す（AutoProfileHolder を読む前に呼ぶ）
            string autoProfilesFile = mainWinVM.AppDataPath + "\\Auto Profiles.xml";
            _autoProfileService.EnsureFile(autoProfilesFile);

            string? steamGamesDir = mainWinVM.CustomSteamFolderIfValid
                ?? (Directory.Exists(AutoProfiles.steamCommx86Loc) ? AutoProfiles.steamCommx86Loc
                    : Directory.Exists(AutoProfiles.steamCommLoc) ? AutoProfiles.steamCommLoc : null);

            autoProfControl.Initialize(autoProfilesFile, steamGamesDir, mainWinVM.ControllerSlotLimit,
                _autoProfileService.Holder, viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory)),
                () => layout.AutoProfileColumnWidths,
                (name, path, title) => layout.AutoProfileColumnWidths = (name, path, title));
            autoProfileHolder = autoProfControl.AutoProfileHolder;
            autoProfControl.SetupDataContext(profileListHolder);

            slotManControl.SetupDataContext(viewModelFactory.CreateCurrentOutDeviceViewModel());
            slotManControl.SetColumnWidths(layout.OutputSlotColumnWidths);
            slotManControl.ColumnWidthsChanged += (s, e) => layout.OutputSlotColumnWidths = slotManControl.GetColumnWidths();

            SetupEvents();

            // Don't tie timers to main thread
            Thread timerThread = new Thread(() =>
            {
                hotkeysTimer = new System.Timers.Timer();
                hotkeysTimer.Interval = 20;
                hotkeysTimer.AutoReset = false;
            });
            timerThread.IsBackground = true;
            timerThread.Priority = ThreadPriority.Lowest;
            timerThread.Start();
            // Wait for thread tasks to finish before continuing
            timerThread.Join();
        }

        private void TrayIconVM_MenuChanged(object? sender, EventArgs e)
        {
            // 項目の変更は別のスレッドからも起きるため、UI のスレッドで組み直す
            if (Dispatcher.CheckAccess())
                TrayContextMenuBuilder.Rebuild(trayContextMenu, trayIconVM.MenuItems);
            else
                Dispatcher.BeginInvoke(new Action(() => TrayContextMenuBuilder.Rebuild(trayContextMenu, trayIconVM.MenuItems)));
        }

        private void TrayIconVM_RequestMinimize(object? sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TrayIconVM_ProfileSelected(TrayIconViewModel sender,
            ControllerHolder item, string? profile)
        {
            int idx = item.Index;
            CompositeDeviceModel devitem = conLvViewModel.ControllerDict[idx];
            profileSelection.ApplyFromTray(idx, profile, devitem);
        }

        private void SettingsWrapVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            trayAppearance.OnSettingsPropertyChanged(e.PropertyName ?? string.Empty, trayIconVM);
            if (e.PropertyName == nameof(SettingsViewModel.ProcessPriorityIndex))
            {
                ApplyProcessPriorityFromSettings();
            }
        }

        private void MainWinVM_FullTabsEnabledChanged(object? sender, EventArgs e)
        {
            settingsWrapVM.ViewEnabled = mainWinVM.FullTabsEnabled;
        }

        private async void TrayIconVM_RequestServiceChange(object? sender, EventArgs e)
        {
            await ChangeService();
        }

        // 廃止済みの AutoProfileChecker を引数に持つオーバーロードを削除し、1 つに統合した
        private void AutoprofileChecker_RequestServiceChange(bool state)
        {
            // ChangeService は、サービスの開始・停止の失敗を自分でログへ出す。ここでは完了を待たない。
            _ = Dispatcher.InvokeAsync(ChangeService);
        }

        private void AutoProfileColl_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CheckAutoProfileStatus();
        }

        private void AutoProfControl_AutoDebugChanged(object? sender, EventArgs e)
        {
            _autoProfileService.DebugLogLevel = autoProfControl.AutoDebug == true ? 1 : 0;
        }

        private void TrayIconVM_RequestShutdown(object? sender, EventArgs e)
        {
            contextclose = true;
            this.Close();
        }

        private void CheckAutoProfileStatus()
            => _autoProfileService.UpdateStatus(autoProfileHolder.AutoProfileColl.Count);

        private void DS4Device_SyncChange(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                trayIconVM.PopulateContextMenu();
            }));
        }

        /// <summary>
        /// 開始・停止ボタンの文字を、サービスの状態に合わせる。XAML の <c>lex:Loc</c> では状態を表せず、
        /// 言語の切り替えのたびに「開始」へ戻されるため、文字は常にここで決める（XAML では文字を持たない）。
        /// </summary>
        private void UpdateStartStopText()
        {
            mainWinVM.RefreshStartStopText();
        }

        private void OnLocalizationCultureChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)UpdateStartStopText);
        }

        /// <summary>サービスの開始・停止。ボタン・トレイ・IPC のどれから呼んでも、同じコマンドを通す（実行中の再入は拒む）。</summary>
        public Task ChangeService() => mainWinVM.ToggleServiceCommand.ExecuteAsync(null);

        internal void ClearLog()
        {
            logvm.LogItems.Clear();
            // 最下部の 1 行メッセージ欄（lastMsgLb）も、ログと一緒に消す
            lastLogMsg.Message = string.Empty;
            lastLogMsg.Warning = false;
        }

        // 本来は Log タブを隠す意図と思われるが、Output Slots タブが途中に入って番号がずれ、実際は Settings タブで隠れ、
        // Settings で出る警告が見えなかった。すべてのタブで表示する（ユーザー決定 2026-11-02）。

        /// <summary>
        /// Clear and re-populate tray context menu
        /// </summary>
        private void NotifyIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            notifyIcon.ContextMenu = trayContextMenu;
        }

        /// <summary>
        /// Change linked profile based on selection
        /// </summary>
        private void CustomColorPick_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

        }

        private bool inHotPlug = false;
        private int hotplugCounter = 0;
        private object hotplugCounterLock = new object();
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int WM_COPYDATA = 0x004A;
        private const int HOTPLUG_CHECK_DELAY = 2000;

        internal void NewDeviceProfile(int idx)
        {
            controllerLV.SelectedIndex = idx;
            ShowProfileEditor(idx, null);
            mainTabCon.SelectedIndex = 1;
            //controllerLV.Focus();
        }

        internal void ImportProfile()
        {
            profileSelection.ImportProfile(profileListHolder);
        }

        internal void ExportProfile()
        {
            profileSelection.ExportProfile(profileListHolder, profilesListBox.SelectedIndex);
        }

        private void DupBox_Cancel(object? sender, EventArgs e)
        {
            mainWinVM.IsDuplicateBarVisible = false;
        }

        private void DupBox_Save(DupBox sender, string profilename)
        {
            profileListHolder.AddProfileSort(profilename);
            mainWinVM.IsDuplicateBarVisible = false;
        }

        internal void DeleteProfile()
        {
            profileSelection.DeleteProfile(profileListHolder, profilesListBox.SelectedIndex);
        }

        private void SelectProfCombo_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void MainDS4Window_StateChanged(object _sender, EventArgs _e)
        {
            CheckMinStatus();
        }

        private void NotifyIcon_TrayMiddleMouseDown(object sender, RoutedEventArgs e)
        {
            contextclose = true;
            Close();
        }

        internal void EditProfile()
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                ProfileEntity entity = profileListHolder.ProfileListCol[profilesListBox.SelectedIndex];
                ShowProfileEditor(ProfileEditor.NoTargetDevice, entity);
            }
        }

        internal void NewProfile()
        {
            ShowProfileEditor(ProfileEditor.NoTargetDevice, null);
        }

        #region Profile List & Controllers Synchronization (4-Stage Transaction)

        private void SyncProfileListAndControllers(string? oldProfile = null, string? newProfile = null,
            IReadOnlyCollection<string>? targetProfiles = null)
            => profileSelection.SyncProfileListAndControllers(ProfileListHolder, () => _autoProfileService?.Holder?.Save(),
                oldProfile, newProfile, targetProfiles);

        #endregion

        // アクションの除去・名前変更でプロファイルの XML が書き換わったときも、
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

        private void LogListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var gridView = logListView.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 2)
            {
                layout.LogColumnWidths = new[] { (int)gridView.Columns[0].Width, (int)gridView.Columns[1].Width };
            }
        }
    }
}