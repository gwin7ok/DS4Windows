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
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;

namespace DS4Windows.UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject, IDisposable
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;

        // Re-Enable Ex Mode
        public bool HideDS4Controller
        {
            get => _appSettings.UseExclusiveMode;
            set => _appSettings.UseExclusiveMode = value;
        }

        /// <summary>検出した Xbox コントローラーすべての、元の XInput を HidHide で隠すか（Xbox の系統の設定。変更するとサービスを再起動する）。</summary>
        // 機種ごとの設定の定義（XboxDeviceOptions の ControllerSetting）には入っているが、画面は設定タブの 1 項目で、
        // HidHide が未導入のときの案内（ShowHidHideMissingNotice）と連動するため、定義の WithUi では表さず、この ViewModel が定義の値を読み書きする。
        public bool HideXboxController
        {
            get => _appSettings.DeviceOptions.Get<DS4Windows.Core.Settings.XboxDeviceOptions>(DS4Windows.Core.Model.InputDeviceFamily.Xbox).HideUsbParent;
            set
            {
                _appSettings.DeviceOptions.Get<DS4Windows.Core.Settings.XboxDeviceOptions>(DS4Windows.Core.Model.InputDeviceFamily.Xbox).HideUsbParent = value;
                OnPropertyChanged(nameof(HideXboxController));
                OnPropertyChanged(nameof(ShowHidHideMissingNotice));
            }
        }

        /// <summary>
        /// 「Xbox コントローラーを隠す」がオンで、HidHide が導入されていないとき、導入を促す赤文字を出すか。
        /// 導入の有無は、起動時の確認（<see cref="IEnvironmentService.RefreshHidHideInfo"/>）で保存した値（<see cref="IEnvironmentService.HidHideInstalled"/>）を参照する。
        /// </summary>
        public bool ShowHidHideMissingNotice => HideXboxController && !_environmentService.HidHideInstalled;

        public bool SwipeTouchSwitchProfile
        {
            get => _appSettings.SwipeProfiles;
            set => _appSettings.SwipeProfiles = value;
        }

        public bool ProfileChangedNotification
        {
            get => _appSettings.ProfileChangedNotification;
            set => _appSettings.ProfileChangedNotification = value;
        }

        public int ShowNotificationsIndex
        {
            get => _appSettings.Notifications;
            set => _appSettings.Notifications = value;
        }

        public bool DisconnectBTStop { get => _appSettings.DCBTatStop; set => _appSettings.DCBTatStop = value; }
        public bool FlashHighLatency
        {
            get => _appSettings.FlashWhenLate;
            set
            {
                if (_appSettings.FlashWhenLate == value) return;
                _appSettings.FlashWhenLate = value;
                // この値で編集の可否が変わる設定（FlashHighLatencyAt）へ知らせる
                OnPropertyChanged(nameof(FlashHighLatency));
            }
        }
        public int FlashHighLatencyAt { get => _appSettings.FlashWhenLateAt; set => _appSettings.FlashWhenLateAt = value; }
        public bool StartMinimize { get => _appSettings.StartMinimized; set => _appSettings.StartMinimized = value; }
        public bool MinimizeToTaskbar { get => _appSettings.MinimizeToTaskbar; set => _appSettings.MinimizeToTaskbar = value; }
        public bool CloseMinimizes { get => _appSettings.CloseMinimizes; set => _appSettings.CloseMinimizes = value; }
        public bool QuickCharge { get => _appSettings.QuickCharge; set => _appSettings.QuickCharge = value; }

        public int IconChoiceIndex
        {
            get => (int)_appearanceSettings.UseIconChoice;
            set
            {
                int temp = (int)_appearanceSettings.UseIconChoice;
                if (temp == value) return;
                _appearanceSettings.UseIconChoice = (DS4Windows.Core.Settings.TrayIconChoice)value;
                OnPropertyChanged(nameof(IconChoiceIndex));
            }
        }
        public int AppChoiceIndex
        {
            get => (int)_appearanceSettings.UseCurrentTheme;
            set
            {
                int temp = (int)_appearanceSettings.UseCurrentTheme;
                if (temp == value) return;
                _appearanceSettings.UseCurrentTheme = (DS4Windows.Core.Settings.AppThemeChoice)value;
                OnPropertyChanged(nameof(AppChoiceIndex));
            }
        }
        // CheckForUpdates: controls whether update checking is enabled.
        // We represent "disabled" with -1. A value of 0 means "check every startup".
        public bool CheckForUpdates
        {
            get => _appSettings.CheckUpdateStartupEnabled;
            set
            {
                _appSettings.CheckUpdateStartupEnabled = value;
                CheckForNoUpdatesWhen();
                _appSettings?.Save();
            }
        }
        public int CheckEvery
        {
            get
            {
                // Use the explicit persisted numeric value. The legacy `CheckWhen`
                // stores hours and was used before separate fields existed.
                // Now the numeric control maps to `CheckEveryValue` directly.
                return _appSettings.CheckEveryValue;
            }
            set
            {
                // If update checks are disabled (represented by -1), do not modify
                // underlying `CheckWhen` from the numeric control. This prevents
                // the numeric binding from re-enabling checks when the checkbox
                // is unchecked.
                // 「OFF のとき数値が保存されない」のは仕様。OFF のときは、数値を入力する行
                // （MainWindow.xaml の checkEveryOptionsStack）ごと IsEnabled で無効になり、利用者は数値を操作できない。
                // ここで何もしないのは、無効な入力欄のバインディングが値を書き戻して、更新確認を ON に戻すのを防ぐ安全弁。
                // 意図は 対応するテスト で固定している。
                if (!_appSettings.CheckUpdateStartupEnabled)
                {
                    return;
                }

                if (_appSettings.CheckEveryValue != value)
                {
                    _appSettings.CheckEveryValue = value;
                    CheckForNoUpdatesWhen();
                    _appSettings?.Save();
                }
            }
        }
        private int checkEveryUnitIdx = 1;
        public int CheckEveryUnit
        {
            get
            {
                return checkEveryUnitIdx;
            }
            set
            {
                if (checkEveryUnitIdx == value) return;
                // Persist selection immediately to the backing store so saves include it.
                _appSettings.CheckEveryUnit = value;
                checkEveryUnitIdx = value;
                OnPropertyChanged(nameof(CheckEveryUnit));
                _appSettings?.Save();
            }
        }
        public bool UseOSCServer
        {
            get => _appSettings.UseOscServer;
            set
            {
                if (_appSettings.UseOscServer == value) return;
                _appSettings.UseOscServer = value;
                OnPropertyChanged(nameof(UseOSCServer));
            }
        }
        public int OscPort { get => _appSettings.OscServerPort; set => _appSettings.OscServerPort = value; }

        public bool InterpretingOscMonitoring { get => _appSettings.InterpretingOscMonitoring; set => _appSettings.InterpretingOscMonitoring = value; }

        public string OscSenderAddress
        {
            get => _appSettings.OscSenderAddress;
            set => _appSettings.OscSenderAddress = value;
        }

        // 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // App.logHolder（静的フィールド）の直接参照の置換先。必須引数（null は拒否する）。
        private readonly ILogSettingsApplier _logSettingsApplier;
        // 旧 静的クラス StartupMethods（ルート直下）の直接呼び出しの置換先。必須引数
        private readonly IStartupEntryService _startupEntries;

        public SettingsViewModel(IAppSettingsService appSettings, IAppearanceSettingsService appearanceSettings,
            IEnvironmentService environmentService, IPathService pathService, ILogSettingsApplier logSettingsApplier,
            IStartupEntryService startupEntries)
        {
            ArgumentNullException.ThrowIfNull(startupEntries);
            _startupEntries = startupEntries;
            ArgumentNullException.ThrowIfNull(logSettingsApplier);
            _logSettingsApplier = logSettingsApplier;
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(appearanceSettings);
            _appearanceSettings = appearanceSettings;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] SettingsViewModel: created");

            // Initialize unit index and numeric value from persistent settings
            checkEveryUnitIdx = _appSettings.CheckEveryUnit;
            // Ensure unit idx is valid
            if (checkEveryUnitIdx != 0 && checkEveryUnitIdx != 1) checkEveryUnitIdx = 1;
            // No longer derive from CheckWhen; numeric value is held separately

            CheckStartupOptions();

            runStartProg = _startupEntries.HasStartProgEntry();
            try
            {
                runStartTask = _startupEntries.HasTaskEntry();
            }
            catch (COMException ex)
            {
                DS4Windows.Core.Logging.AppLogger.LogUser(AppLogLevel.Error, "Log_Ui_TaskServiceError", ex.Message);
            }

            runAtStartup = runStartProg || runStartTask;
            canWriteTask = _environmentService.IsAdministrator();

            if (!runAtStartup)
            {
                runStartProg = true;
            }
            else if (runStartProg && runStartTask)
            {
                runStartProg = false;
                if (_startupEntries.CanWriteStartEntry())
                {
                    _startupEntries.DeleteStartProgEntry();
                }
            }

            if (runAtStartup && runStartProg)
            {
                bool locChange = _startupEntries.CheckStartupExeLocation(_pathService.ExecutablePath);
                if (locChange)
                {
                    if (_startupEntries.CanWriteStartEntry())
                    {
                        _startupEntries.DeleteStartProgEntry();
                        _startupEntries.WriteStartProgEntry(_pathService.ExecutablePath, _pathService.ProgramDirectory);
                    }
                    else
                    {
                        runAtStartup = false;
                        showRunStartPanel = false;
                    }
                }
            }
            else if (runAtStartup && runStartTask)
            {
                if (canWriteTask)
                {
                    _startupEntries.DeleteOldTaskEntry(_pathService.ProgramDirectory);
                    _startupEntries.WriteTaskEntry(_pathService.ProgramDirectory,
                        System.IO.Path.GetFileName(_pathService.ExecutablePath));
                }
            }

            if (runAtStartup)
            {
                showRunStartPanel = true;
            }

            PropertyChanged += OnOwnPropertyChanged;
            FakeExeNameChangeCompare += SettingsViewModel_FakeExeNameChangeCompare;
        }

        /// <summary>
        /// 自分の値が変わったときの副作用（スタートアップの登録、偽装 exe の作成、UDP の表示の連動）。
        /// 値を設定した直後に同期して実行する。
        /// </summary>
        private void OnOwnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RunAtStartup): SettingsViewModel_RunAtStartupChanged(sender, EventArgs.Empty); break;
                case nameof(RunStartProg): SettingsViewModel_RunStartProgChanged(sender, EventArgs.Empty); break;
                case nameof(RunStartTask): SettingsViewModel_RunStartTaskChanged(sender, EventArgs.Empty); break;
                case nameof(FakeExeName): SettingsViewModel_FakeExeNameChanged(sender, EventArgs.Empty); break;
                case nameof(UseUdpSmoothing): SettingsViewModel_UseUdpSmoothingChanged(sender, EventArgs.Empty); break;
                case nameof(UseUDPServer): SettingsViewModel_UseUDPServerChanged(sender, EventArgs.Empty); break;
            }
        }

        public void Dispose()
        {
        }

        private void SettingsViewModel_UseUDPServerChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UdpServerOneEuroPanelShown));
        }

        private void SettingsViewModel_UseUdpSmoothingChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UdpServerOneEuroPanelShown));
        }

        private void SettingsViewModel_FakeExeNameChangeCompare(SettingsViewModel sender,
            string oldvalue, string newvalue)
        {
            DeleteFakeExeFiles(_pathService.ProgramDirectory, oldvalue);
        }

        public void CreateFakeExe(string filename)
        {
            CreateFakeExeFiles(_pathService.ProgramDirectory, _pathService.ExecutablePath, filename);
        }

        public void DriverCheckRefresh()
        {
            OnPropertyChanged(nameof(HidHideClientFound));
            OnPropertyChanged(nameof(ShowHidHideMissingNotice));
        }

        public int LogMaxArchiveFiles
        {
            get => _appSettings.LogMaxArchiveFiles;
            set
            {
                if (_appSettings.LogMaxArchiveFiles != value)
                {
                    _appSettings.LogMaxArchiveFiles = value;
                    _logSettingsApplier.UpdateLogSettings();
                    _logSettingsApplier.UpdateNLogConfig(value);
                    _appSettings?.Save();
                    _logSettingsApplier.RestoreArchiveSetting(); // Restore after all updates
                    OnPropertyChanged(nameof(LogMaxArchiveFiles));
                }
            }
        }

        public string LogMinLevel
        {
            get => _appSettings.LogMinLevel;
            set
            {
                if (_appSettings.LogMinLevel != value)
                {
                    _appSettings.LogMinLevel = value;
                    _logSettingsApplier.UpdateLogSettings();
                    _appSettings?.Save();
                    _logSettingsApplier.RestoreArchiveSetting(); // Restore after all updates
                    OnPropertyChanged(nameof(LogMinLevel));
                }
            }
        }
    }
}