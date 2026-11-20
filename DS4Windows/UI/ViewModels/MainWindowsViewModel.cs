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
using System.Diagnostics;
using DS4Windows.Core.Abstractions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// メイン画面の ViewModel。サービスの開始・停止（<see cref="ToggleServiceCommand"/>）と、設定の変更に伴う再起動のコマンド、
    /// そのあいだ操作を止める「忙しい」状態を持つ。ボタンの有効・無効は、コマンドの <c>CanExecute</c> と、状態のバインディングで表す。
    /// 変更通知はツールキットの <c>ObservableObject</c>（同じ名前空間に独自の同名の型があるため、完全修飾で書く）。
    /// </summary>
    public class MainWindowsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IServiceLifecycle _lifecycle;
        private readonly IShellLauncher _launcher;
        private readonly IProcessPriorityControl _priority;
        private readonly IAppSettingsService _appSettings;
        private readonly IRuntimeFeatureSwitch _features;
        private readonly IDialogService _dialogs;
        private readonly UpdateCheckPolicy _updateCheckPolicy;

        private bool _isServiceBusy;
        private int _selectedProfileIndex = -1;
        private bool _hideDs4ToggleEnabled = true;
        private bool _hideXboxToggleEnabled = true;

        private bool fullTabsEnabled = true;

        public bool FullTabsEnabled
        {
            get => fullTabsEnabled;
            set
            {
                fullTabsEnabled = value;
                FullTabsEnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler? FullTabsEnabledChanged;

        // 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // 旧 RunUpdaterCheck／DownloadUpstreamVersionInfo／LauchDS4Updater／IsNET8Available と updaterExe は、
        // 呼出元 0 件の上流の更新経路の残骸（2025-12-12 の b55d160b で呼び出しが消え、現行の更新処理は
        // UpdaterWindow が担う）のため、決定6＝D1 により削除した。
        public MainWindowsViewModel(IEnvironmentService environmentService, IPathService pathService, IServiceLifecycle lifecycle,
            IShellLauncher launcher, IDialogService dialogs, UpdateCheckPolicy updateCheckPolicy,
            IRuntimeFeatureSwitch features, IProcessPriorityControl priority,
            IAppSettingsService appSettings)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(priority);
            _priority = priority;
            ArgumentNullException.ThrowIfNull(features);
            _features = features;
            ArgumentNullException.ThrowIfNull(launcher);
            _launcher = launcher;
            ArgumentNullException.ThrowIfNull(dialogs);
            _dialogs = dialogs;
            ArgumentNullException.ThrowIfNull(updateCheckPolicy);
            _updateCheckPolicy = updateCheckPolicy;
            ArgumentNullException.ThrowIfNull(lifecycle);
            _lifecycle = lifecycle;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;

            ToggleServiceCommand = CommandGuard.Create(ToggleServiceAsync, () => !IsServiceBusy, nameof(ToggleServiceCommand));
            RestartForHideDs4Command = CommandGuard.Create(() => RestartServiceAsync(isXbox: false), name: nameof(RestartForHideDs4Command));
            RestartForHideXboxCommand = CommandGuard.Create(() => RestartServiceAsync(isXbox: true), name: nameof(RestartForHideXboxCommand));

            OpenProfilesFolderCommand = CommandGuard.CreateSync(() => _launcher.OpenPath(System.IO.Path.Combine(_pathService.AppDataPath, "Profiles")), name: nameof(OpenProfilesFolderCommand));
            OpenControlPanelCommand = CommandGuard.CreateSync(() => _launcher.RunProgram("control", "joy.cpl"), name: nameof(OpenControlPanelCommand));
            OpenGamepadTesterCommand = CommandGuard.CreateSync(() => _launcher.OpenUrl("https://gamepad-tester.com/"), name: nameof(OpenGamepadTesterCommand));
            OpenXInputCheckerCommand = CommandGuard.CreateSync(
                () => _launcher.RunProgramIfExists(System.IO.Path.Combine(_pathService.ExecutableDirectory, "Tools", "XInputChecker", "XInputChecker.exe")),
                name: nameof(OpenXInputCheckerCommand));
            OpenHidHideClientCommand = CommandGuard.CreateSync(OpenHidHideClient, name: nameof(OpenHidHideClientCommand));
            ExplainCustomExeNameCommand = CommandGuard.CreateSync(
                () => _dialogs.ShowMessage(DS4Windows.Translations.Strings.CustomExeNameInfo, "Custom Exe Name Info", DialogIcon.Information),
                name: nameof(ExplainCustomExeNameCommand));
            CheckUpdatesCommand = CommandGuard.Create(CheckUpdatesAsync, name: nameof(CheckUpdatesCommand));
            ShowAboutCommand = CommandGuard.CreateSync(() => Navigator?.ShowAbout(), name: nameof(ShowAboutCommand));
            ShowChangelogCommand = CommandGuard.CreateSync(() => Navigator?.ShowChangelog(), name: nameof(ShowChangelogCommand));
            ShowLogMessageCommand = CommandGuard.CreateSync(() => Navigator?.ShowLogMessage(), name: nameof(ShowLogMessageCommand));
            ShowDeviceOptionsCommand = CommandGuard.CreateSync(() => Navigator?.ShowDeviceOptions(), name: nameof(ShowDeviceOptionsCommand));

            SetOscListenerCommand = CommandGuard.CreateSync<bool>(on => _features.ChangeOscListenerStatus(on), name: nameof(SetOscListenerCommand));
            SetOscSenderCommand = CommandGuard.CreateSync<bool>(on => _features.ChangeOscSenderStatus(on), name: nameof(SetOscSenderCommand));
            SetUdpServerCommand = CommandGuard.Create<bool>(SetUdpServerAsync, name: nameof(SetUdpServerCommand));
            SetSwipeTouchCommand = CommandGuard.CreateSync<bool>(on => Navigator?.SetHotkeysActive(on), name: nameof(SetSwipeTouchCommand));
            SetRunAtStartPanelCommand = CommandGuard.CreateSync<bool>(on => Navigator?.SetRunStartPanelVisible(on), name: nameof(SetRunAtStartPanelCommand));
            ExportLogCommand = CommandGuard.CreateSync(() => Navigator?.ExportLog(), name: nameof(ExportLogCommand));
            ClearLogCommand = CommandGuard.CreateSync(() => Navigator?.ClearLog(), name: nameof(ClearLogCommand));
            DriverSetupCommand = CommandGuard.Create(DriverSetupAsync, name: nameof(DriverSetupCommand));

            bool HasSelection() => SelectedProfileIndex >= 0;
            NewProfileCommand = CommandGuard.CreateSync(() => Navigator?.NewProfile(), name: nameof(NewProfileCommand));
            EditProfileCommand = CommandGuard.CreateSync(() => Navigator?.EditProfile(), HasSelection, nameof(EditProfileCommand));
            DeleteProfileCommand = CommandGuard.CreateSync(() => Navigator?.DeleteProfile(), HasSelection, nameof(DeleteProfileCommand));
            RenameProfileCommand = CommandGuard.CreateSync(() => Navigator?.RenameProfile(), HasSelection, nameof(RenameProfileCommand));
            DuplicateProfileCommand = CommandGuard.CreateSync(() => Navigator?.DuplicateProfile(), HasSelection, nameof(DuplicateProfileCommand));
            ImportProfileCommand = CommandGuard.CreateSync(() => Navigator?.ImportProfile(), name: nameof(ImportProfileCommand));
            ExportProfileCommand = CommandGuard.CreateSync(() => Navigator?.ExportProfile(), HasSelection, nameof(ExportProfileCommand));
            EditDeviceProfileCommand = CommandGuard.CreateSync<int>(index => Navigator?.EditDeviceProfile(index), name: nameof(EditDeviceProfileCommand));
            NewDeviceProfileCommand = CommandGuard.CreateSync<int>(index => Navigator?.NewDeviceProfile(index), name: nameof(NewDeviceProfileCommand));

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] MainWindowsViewModel: created (IEnvironmentService, IPathService, IServiceLifecycle injected)");
        }

        /// <summary>サービスの開始・停止の最中か（次の状態の通知まで、開始・停止ボタンを押せない）。UI スレッドで設定する。</summary>
        public bool IsServiceBusy
        {
            get => _isServiceBusy;
            set
            {
                if (SetProperty(ref _isServiceBusy, value))
                {
                    ToggleServiceCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>「DS4 を隠す」のチェックを操作できるか（再起動のあいだは false）。</summary>
        public bool HideDs4ToggleEnabled
        {
            get => _hideDs4ToggleEnabled;
            private set => SetProperty(ref _hideDs4ToggleEnabled, value);
        }

        /// <summary>「Xbox を隠す」のチェックを操作できるか（再起動のあいだは false）。</summary>
        public bool HideXboxToggleEnabled
        {
            get => _hideXboxToggleEnabled;
            private set => SetProperty(ref _hideXboxToggleEnabled, value);
        }

        // ---- サービス（開始・停止・状態）。画面が契約（IServiceLifecycle・IRuntimeFeatureSwitch）を直接参照しないための窓口 ----

        public bool ServiceRunning => _lifecycle.Running;

        public bool Suspending
        {
            get => _lifecycle.Suspending;
            set => _lifecycle.Suspending = value;
        }

        public event EventHandler ServiceStarted
        {
            add => _lifecycle.ServiceStarted += value;
            remove => _lifecycle.ServiceStarted -= value;
        }

        public event EventHandler RunningChanged
        {
            add => _lifecycle.RunningChanged += value;
            remove => _lifecycle.RunningChanged -= value;
        }

        public event EventHandler PreServiceStop
        {
            add => _lifecycle.PreServiceStop += value;
            remove => _lifecycle.PreServiceStop -= value;
        }

        public bool StartService(bool showlog = true) => _lifecycle.Start(showlog);

        public bool StopService(bool showlog = true, bool immediateUnplug = false) => _lifecycle.Stop(showlog, immediateUnplug);

        public bool HotPlug() => _lifecycle.HotPlug();

        public string TouchpadSlide(int index) => _features.TouchpadSlide(index);

        public void UpdateHidHiddenAttributes() => _features.UpdateHidHiddenAttributes();

        // ---- 保存先・実行環境 ----

        public string AppDataPath => _pathService.AppDataPath;

        public string ExecutablePath => _pathService.ExecutablePath;

        public bool IsAdministrator() => _environmentService.IsAdministrator();

        public int ControllerSlotLimit => _environmentService.ControllerSlotLimit;

        public string? ApplicationVersion => _environmentService.ApplicationVersion;

        public Guid HidClassGuid => _environmentService.HidClassGuid;

        public Guid XusbClassGuid => _environmentService.XusbClassGuid;

        /// <summary>保存先のパスから、プロファイルの一覧の保持の入れ物を作る。</summary>
        public DS4Windows.Core.Profiles.ProfileList CreateProfileList() => new DS4Windows.Core.Profiles.ProfileList(_pathService);

        // ---- 画面の動きを決める設定 ----

        public string LogMinLevel => _appSettings.LogMinLevel;

        public bool StartMinimized => _appSettings.StartMinimized;

        public bool MinimizeToTaskbar => _appSettings.MinimizeToTaskbar;

        public bool CloseMinimizes => _appSettings.CloseMinimizes;

        public bool FirstRun => _appSettings.FirstRun;

        public bool RunHotPlug => _appSettings.RunHotPlug;

        public bool SwipeProfiles => _appSettings.SwipeProfiles;

        public ulong LastVersionCheckedNum => _appSettings.LastVersionCheckedNum;

        /// <summary>Steam のゲームのフォルダ。カスタムのフォルダが有効で存在するときだけ、その値（それ以外は null）。</summary>
        public string? CustomSteamFolderIfValid
            => _appSettings.UseCustomSteamFolder && System.IO.Directory.Exists(_appSettings.CustomSteamFolder) ? _appSettings.CustomSteamFolder : null;

        /// <summary>「バージョン情報」画面の ViewModel を作る。</summary>
        public AboutViewModel CreateAboutViewModel() => new AboutViewModel(_environmentService, _launcher);

        /// <summary>機器の登録の設定画面の ViewModel（設定の機器オプションを渡して作る）。</summary>
        public ControllerRegDeviceOptsViewModel CreateDeviceOptionsViewModel(IShellViewModelFactory factory)
            => factory.CreateControllerRegDeviceOptsViewModel(_appSettings.DeviceOptions);

        /// <summary>他の画面を開く窓口。画面（View）ができたあとに、画面が自分で結ぶ。</summary>
        public IShellNavigator? Navigator { get; set; }

        public RelayCommand OpenProfilesFolderCommand { get; }

        public RelayCommand OpenControlPanelCommand { get; }

        public RelayCommand OpenGamepadTesterCommand { get; }

        public RelayCommand OpenXInputCheckerCommand { get; }

        public RelayCommand OpenHidHideClientCommand { get; }

        public RelayCommand ExplainCustomExeNameCommand { get; }

        /// <summary>更新を手動で確認する。新しい版があれば更新の画面を、なければ「最新です」を、取れなければ失敗を知らせる。</summary>
        public AsyncRelayCommand CheckUpdatesCommand { get; }

        public RelayCommand ShowAboutCommand { get; }

        public RelayCommand ShowChangelogCommand { get; }

        public RelayCommand ShowDeviceOptionsCommand { get; }

        /// <summary>ログ一覧で選んだ行の全文を、別の窓で見せる。</summary>
        public RelayCommand ShowLogMessageCommand { get; }

        /// <summary>プロファイル一覧で選ばれている行（なければ -1）。一覧の選択とバインドする。</summary>
        public int SelectedProfileIndex
        {
            get => _selectedProfileIndex;
            set
            {
                if (SetProperty(ref _selectedProfileIndex, value))
                {
                    EditProfileCommand.NotifyCanExecuteChanged();
                    DeleteProfileCommand.NotifyCanExecuteChanged();
                    RenameProfileCommand.NotifyCanExecuteChanged();
                    DuplicateProfileCommand.NotifyCanExecuteChanged();
                    ExportProfileCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public RelayCommand NewProfileCommand { get; }

        /// <summary>OSC の受信の入・切（<c>CommandParameter</c> はチェックの状態）。</summary>
        public RelayCommand<bool> SetOscListenerCommand { get; }

        /// <summary>OSC の送信の入・切。</summary>
        public RelayCommand<bool> SetOscSenderCommand { get; }

        /// <summary>UDP サーバー（Cemuhook）の入・切。ポートの開閉とモーション送出の順序は、入れるときと切るときで逆にする。</summary>
        public AsyncRelayCommand<bool> SetUdpServerCommand { get; }

        /// <summary>タッチパッドのスワイプによるプロファイル切替の入・切。</summary>
        public RelayCommand<bool> SetSwipeTouchCommand { get; }

        /// <summary>「起動時に実行」の詳細の表示・非表示。</summary>
        public RelayCommand<bool> SetRunAtStartPanelCommand { get; }

        public RelayCommand ExportLogCommand { get; }

        public RelayCommand ClearLogCommand { get; }

        /// <summary>ドライバーの導入。サービスを止めてから、導入のプログラムを管理者で実行し、終わったらドライバーの状態を調べ直す。</summary>
        public AsyncRelayCommand DriverSetupCommand { get; }

        public RelayCommand EditProfileCommand { get; }

        public RelayCommand DeleteProfileCommand { get; }

        public RelayCommand RenameProfileCommand { get; }

        public RelayCommand DuplicateProfileCommand { get; }

        public RelayCommand ImportProfileCommand { get; }

        public RelayCommand ExportProfileCommand { get; }

        /// <summary>機器ごとの「編集」。<c>CommandParameter</c> は機器（スロット）の番号。</summary>
        public RelayCommand<int> EditDeviceProfileCommand { get; }

        /// <summary>機器ごとの「新しいプロファイル」。<c>CommandParameter</c> は機器（スロット）の番号。</summary>
        public RelayCommand<int> NewDeviceProfileCommand { get; }

        /// <summary>動作中なら停止、止まっていれば開始する。</summary>
        public AsyncRelayCommand ToggleServiceCommand { get; }

        /// <summary>設定（DS4 を隠す）の変更を反映するため、サービスを停止して開始し直す。</summary>
        public AsyncRelayCommand RestartForHideDs4Command { get; }

        /// <summary>設定（Xbox の USB の親を隠す）の変更を反映するため、サービスを停止して開始し直す。</summary>
        public AsyncRelayCommand RestartForHideXboxCommand { get; }

        /// <summary>
        /// 開始・停止ボタンの文字。サービスの状態で変わる。XAML の <c>lex:Loc</c> では状態を表せず、
        /// 言語の切り替えのたびに「開始」へ戻されるため、文字は常にここで決める（<see cref="RefreshStartStopText"/> で再通知する）。
        /// </summary>
        public string StartStopText => _lifecycle.Running ? Translations.Strings.StopText : Translations.Strings.StartText;

        /// <summary>サービスの状態・表示言語が変わったあとに、開始・停止ボタンの文字を読み直させる。</summary>
        public void RefreshStartStopText() => OnPropertyChanged(nameof(StartStopText));

        private bool _isDuplicateBarVisible;

        /// <summary>プロファイルの複製の名前の入力欄（バー）を出しているか。</summary>
        public bool IsDuplicateBarVisible
        {
            get => _isDuplicateBarVisible;
            set => SetProperty(ref _isDuplicateBarVisible, value);
        }

        /// <summary>サービスの状態が変わったとき（開始・停止の完了）に、画面から呼ぶ。開始・停止ボタンを再び押せるようにする。</summary>
        public void NotifyServiceStateChanged() => IsServiceBusy = false;

        private async Task SetUdpServerAsync(bool on)
        {
            // 切るときはモーション送出を先に止めてからポートを閉じ、入れるときはポートを開いてからモーション送出を始める
            if (!on)
            {
                _features.ChangeMotionEventStatus(on);
                await Task.Delay(200).ConfigureAwait(true);
                _features.ChangeUdpStatus(on);
            }
            else
            {
                _features.ChangeUdpStatus(on);
                await Task.Delay(200).ConfigureAwait(true);
                _features.ChangeMotionEventStatus(on);
            }
        }

        private async Task DriverSetupAsync()
        {
            IsServiceBusy = true;
            try
            {
                await Task.Run(() =>
                {
                    if (_lifecycle.Running)
                        _lifecycle.Stop();
                }).ConfigureAwait(true);
            }
            finally
            {
                IsServiceBusy = false;
            }

            await Task.Run(() => _launcher.RunElevatedAndWait(_pathService.ExecutablePath, "-driverinstall")).ConfigureAwait(true);
            _environmentService.RefreshHidHideInfo();
            _environmentService.RefreshFakerInputInfo();
            Navigator?.RefreshDriverCheck();
        }

        /// <summary>
        /// 設定の優先度の番号を、このアプリのプロセスへ反映する。管理者でなければ RealTime は使えないため、通知して High にする。
        /// 戻り値は、実際に反映した番号（設定の表示を合わせるために、呼び出し側が設定へ書き戻す）。
        /// </summary>
        public int ApplyProcessPriority(int index)
        {
            var classes = DS4Windows.Core.Platform.ProcessPriorityOptions.Classes;
            if (index < 0 || index >= classes.Count) return index;

            System.Diagnostics.ProcessPriorityClass selected = classes[index];
            if (!_environmentService.IsAdministrator() && selected == System.Diagnostics.ProcessPriorityClass.RealTime)
            {
                _dialogs.ShowMessage(DS4Windows.Translations.Strings.RealTimeNoAdmin);
                selected = System.Diagnostics.ProcessPriorityClass.High;
                index = DS4Windows.Core.Platform.ProcessPriorityOptions.IndexOf(selected);
            }

            _priority.Apply(selected);
            return index;
        }

        private void OpenHidHideClient()
        {
            string path = DS4Windows.Core.Utilities.Util.GetHidHideClientPath();
            if (!string.IsNullOrEmpty(path)) _launcher.OpenPath(path);
        }

        private async Task CheckUpdatesAsync()
        {
            const string title = "DS4Windows Updater";
            Version? version;
            bool found;
            try
            {
                // 手動の確認は、以前にスキップした版を無視する
                (found, version) = await Task.Run(() =>
                {
                    bool result = _updateCheckPolicy.TryFindNewerVersion(startup: false, out Version? latest);
                    return (result, latest);
                }).ConfigureAwait(true);
            }
            catch
            {
                _dialogs.ShowMessage(DS4Windows.Translations.Strings.FailedToRetrieveLatestVersion, title);
                // 原因をログで見られるよう、共通の窓口（CommandGuard）へ渡す
                throw;
            }

            if (found && version != null)
                Navigator?.ShowUpdater(version.ToString());
            else
                _dialogs.ShowMessage(DS4Windows.Properties.Resources.UpToDate, title);
        }

        private async Task ToggleServiceAsync()
        {
            // 開始・停止は、状態の変更の通知（NotifyServiceStateChanged）が来るまで、ボタンを押せなくする
            IsServiceBusy = true;
            try
            {
                await Task.Run(() =>
                {
                    if (_lifecycle.Running)
                        _lifecycle.Stop(immediateUnplug: true);
                    else
                        _lifecycle.Start();
                }).ConfigureAwait(true);
            }
            catch
            {
                // 失敗しても状態の通知が来ないことがあるため、ボタンを戻して、例外は共通の窓口（CommandGuard）へ渡す
                IsServiceBusy = false;
                throw;
            }
        }

        private async Task RestartServiceAsync(bool isXbox)
        {
            IsServiceBusy = true;
            if (isXbox) HideXboxToggleEnabled = false; else HideDs4ToggleEnabled = false;
            try
            {
                await Task.Run(() =>
                {
                    _lifecycle.Stop();
                    _lifecycle.Start();
                }).ConfigureAwait(true);
            }
            finally
            {
                if (isXbox) HideXboxToggleEnabled = true; else HideDs4ToggleEnabled = true;
                IsServiceBusy = false;
            }
        }

        public void CheckDrivers()
        {
            bool deriverinstalled = _environmentService.IsViGEmBusInstalled();
            if (!deriverinstalled || !_environmentService.IsRunningSupportedViGEmBus())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = $"{_pathService.ExecutablePath}";
                startInfo.Arguments = "-driverinstall";
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
                try
                {
                    using (Process? temp = Process.Start(startInfo))
                    {
                    }
                }
                catch (Exception caught) when (caught is System.ComponentModel.Win32Exception or System.InvalidOperationException or System.NotSupportedException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
            }
        }
    }
}
