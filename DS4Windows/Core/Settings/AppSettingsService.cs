using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    // ★ IDE0290 根本解決: C# 12 プライマリコンストラクタ構文を適用
    public class AppSettingsService(BackingStore config, IProfileXmlStore xmlStore = null, IPathService pathService = null) : IAppSettingsService
    {
        // Global の転送プロパティ経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IProfileXmlStore _xmlStore = xmlStore;
        private readonly IPathService _pathService = pathService;

        // StartMinimized等はGlobal(m_Config/BackingStore)への
        // 正規シムとし、UI層(MainWindow.xaml.cs等)からGlobal直参照した場合と完全に同一の実体を
        // 参照するようにする。従来はここに独立したprivateフィールドを持ち、Global側と非連動という
        // 「孤立した重複状態」バグがあった(Step13投入時に発覚)。
        // 非連動プロパティ CheckWhenを削除した。

        public event EventHandler<string> SettingChanged;

        // (※旧コンストラクタ public AppSettingsService(...) { ... } は削除)
        public bool Save()
        {
            bool success = _xmlStore.SaveAppSettingsXml();
            if (success)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppSettingsService.Save succeeded");
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Settings_SaveFailed");
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogDebug("[DI] AppSettingsService.Save failed");
            }
            return success;
        }

        public bool Load()
        {
            bool success = _xmlStore.LoadAppSettingsXml();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppSettingsService.Load: success={success}");
            return success;
        }

        private void NotifyChanged(string settingName)
        {
            SettingChanged?.Invoke(this, settingName);
        }

        /// <summary>
        /// 値が変わったときだけ、設定を書き換えて <see cref="SettingChanged"/> を出す（設定の後。通知の名前は呼び出したプロパティの名前）。
        /// 比較は <see cref="EqualityComparer{T}.Default"/>（bool・int・string・列挙型は、元の <c>!=</c> と同じ結果になる）。
        /// </summary>
        private void SetIfChanged<T>(ref T field, T value, [CallerMemberName] string settingName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            NotifyChanged(settingName);
        }

        public bool StartMinimized
        {
            get => _config.startMinimized;
            set => SetIfChanged(ref _config.startMinimized, value);
        }

        public bool MinimizeToTaskbar
        {
            get => _config.minToTaskbar;
            set => SetIfChanged(ref _config.minToTaskbar, value);
        }

        public bool CloseMinimizes
        {
            get => _config.closeMini;
            set => SetIfChanged(ref _config.closeMini, value);
        }

        public bool UseUdpServer
        {
            get => _config.useUDPServ;
            set => SetIfChanged(ref _config.useUDPServ, value);
        }

        public int UdpServerPort
        {
            get => _config.udpServPort;
            set => SetIfChanged(ref _config.udpServPort, value);
        }

        public string UdpServerListenAddress
        {
            get => _config.udpServListenAddress;
            set
            {
                if (_config.udpServListenAddress != value)
                {
                    _config.udpServListenAddress = value.Trim();
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
        }

        public bool UseExclusiveMode
        {
            get => _config.useExclusiveMode;
            set => SetIfChanged(ref _config.useExclusiveMode, value);
        }

        public bool AutoProfileRevertDefaultProfile
        {
            get => _config.autoProfileRevertDefaultProfile;
            set => SetIfChanged(ref _config.autoProfileRevertDefaultProfile, value);
        }

        // ---- ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        // いずれも m_Config(BackingStore) への薄い公開アクセサ(Global.X)への正規シム。
        public int FormWidth
        {
            get => _config.formWidth;
            set => SetIfChanged(ref _config.formWidth, value);
        }

        public int FormHeight
        {
            get => _config.formHeight;
            set => SetIfChanged(ref _config.formHeight, value);
        }

        public int FormLocationX
        {
            get => _config.formLocationX;
            set => SetIfChanged(ref _config.formLocationX, value);
        }

        public int FormLocationY
        {
            get => _config.formLocationY;
            set => SetIfChanged(ref _config.formLocationY, value);
        }

        public int ControllerIndexColWidth
        {
            get => _config.controllerIndexColWidth;
            set => SetIfChanged(ref _config.controllerIndexColWidth, value);
        }

        public int ControllerIdColWidth
        {
            get => _config.controllerIdColWidth;
            set => SetIfChanged(ref _config.controllerIdColWidth, value);
        }

        public int ControllerStatusColWidth
        {
            get => _config.controllerStatusColWidth;
            set => SetIfChanged(ref _config.controllerStatusColWidth, value);
        }

        public int ControllerExclusiveColWidth
        {
            get => _config.controllerExclusiveColWidth;
            set => SetIfChanged(ref _config.controllerExclusiveColWidth, value);
        }

        public int ControllerBatteryColWidth
        {
            get => _config.controllerBatteryColWidth;
            set => SetIfChanged(ref _config.controllerBatteryColWidth, value);
        }

        public int ControllerSelectProfileColWidth
        {
            get => _config.controllerSelectProfileColWidth;
            set => SetIfChanged(ref _config.controllerSelectProfileColWidth, value);
        }

        public int ControllerEditColWidth
        {
            get => _config.controllerEditColWidth;
            set => SetIfChanged(ref _config.controllerEditColWidth, value);
        }

        public int ControllerLinkedProfileColWidth
        {
            get => _config.controllerLinkedProfileColWidth;
            set => SetIfChanged(ref _config.controllerLinkedProfileColWidth, value);
        }

        public int ControllerLinkProfIdColWidth
        {
            get => _config.controllerLinkProfIdColWidth;
            set => SetIfChanged(ref _config.controllerLinkProfIdColWidth, value);
        }

        public int ControllerCustomColorColWidth
        {
            get => _config.controllerCustomColorColWidth;
            set => SetIfChanged(ref _config.controllerCustomColorColWidth, value);
        }

        public int OutputSlotNumColWidth
        {
            get => _config.outputSlotNumColWidth;
            set => SetIfChanged(ref _config.outputSlotNumColWidth, value);
        }

        public int OutputSlotInputNumColWidth
        {
            get => _config.outputSlotInputNumColWidth;
            set => SetIfChanged(ref _config.outputSlotInputNumColWidth, value);
        }

        public int OutputSlotInputIdColWidth
        {
            get => _config.outputSlotInputIdColWidth;
            set => SetIfChanged(ref _config.outputSlotInputIdColWidth, value);
        }

        public int OutputSlotCurrentTypeColWidth
        {
            get => _config.outputSlotCurrentTypeColWidth;
            set => SetIfChanged(ref _config.outputSlotCurrentTypeColWidth, value);
        }

        public int OutputSlotReservationColWidth
        {
            get => _config.outputSlotReservationColWidth;
            set => SetIfChanged(ref _config.outputSlotReservationColWidth, value);
        }

        public int AutoProfileNameColWidth
        {
            get => _config.autoProfileNameColWidth;
            set => SetIfChanged(ref _config.autoProfileNameColWidth, value);
        }

        public int AutoProfilePathColWidth
        {
            get => _config.autoProfilePathColWidth;
            set => SetIfChanged(ref _config.autoProfilePathColWidth, value);
        }

        public int AutoProfileTitleColWidth
        {
            get => _config.autoProfileTitleColWidth;
            set => SetIfChanged(ref _config.autoProfileTitleColWidth, value);
        }

        public int LogTimeColWidth
        {
            get => _config.logTimeColWidth;
            set => SetIfChanged(ref _config.logTimeColWidth, value);
        }

        public int LogDataColWidth
        {
            get => _config.logDataColWidth;
            set => SetIfChanged(ref _config.logDataColWidth, value);
        }

        // ---- 通知レベル・スワイプ操作設定 ----
        public int Notifications
        {
            get => _config.notifications;
            set => SetIfChanged(ref _config.notifications, value);
        }

        public bool ProfileChangedNotification
        {
            get => _config.profileChangedNotification;
            set
            {
                if (_config.profileChangedNotification == value) return;
                _config.profileChangedNotification = value;
                NotifyChanged(nameof(ProfileChangedNotification));
            }
        }
        public bool SwipeProfiles
        {
            get => _config.swipeProfiles;
            set => SetIfChanged(ref _config.swipeProfiles, value);
        }

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い公開アクセサへの正規シム。
        // Notifications 等と同一パターン（独立フィールドを持たない）。
        public string LogMinLevel
        {
            get => _config.logMinLevel;
            set => SetIfChanged(ref _config.logMinLevel, value);
        }

        public bool CheckUpdateStartupEnabled
        {
            get => _config.checkUpdateStartup;
            set => SetIfChanged(ref _config.checkUpdateStartup, value);
        }

        public int CheckEveryValue
        {
            get => _config.checkEveryValue;
            set => SetIfChanged(ref _config.checkEveryValue, value);
        }

        public int CheckEveryUnit
        {
            get => _config.checkEveryUnit;
            set => SetIfChanged(ref _config.checkEveryUnit, value);
        }

        public DateTime LastChecked
        {
            get => _config.lastChecked;
            set => SetIfChanged(ref _config.lastChecked, value);
        }

        // _config.lastVersionCheckedNum は読み取り専用（LastVersionChecked代入時に自動算出）のため、
        // 本シムも読み取り専用として公開する。
        public ulong LastVersionCheckedNum => _config.lastVersionCheckedNum;

        // 代入時に番号（LastVersionCheckedNum）を算出する処理を、Global のプロパティからここへ移した。
        public string LastVersionChecked
        {
            get => _config.lastVersionChecked;
            set
            {
                if (_config.lastVersionChecked != value)
                {
                    _config.lastVersionChecked = value;
                    _config.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(value);
                    NotifyChanged(nameof(LastVersionChecked));
                    NotifyChanged(nameof(LastVersionCheckedNum));
                }
            }
        }

        public void SetCulture(string culture) => UiCulture.Set(culture);

        // ---- Global への薄い委譲 ----
        public bool UseMoonlight
        {
            get => _config.useMoonlight;
            set => SetIfChanged(ref _config.useMoonlight, value);
        }

        public bool UseAdvancedMoonlight
        {
            get => _config.useAdvancedMoonlight;
            set => SetIfChanged(ref _config.useAdvancedMoonlight, value);
        }

        public bool FirstRun
        {
            // 旧 Global.firstRun。実体は PathService（FindConfigLocation が設定する）
            get => PathService.FirstRunState;
            set { if (PathService.FirstRunState != value) { PathService.FirstRunState = value; NotifyChanged(nameof(FirstRun)); } }
        }

        // ---- App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // 他のプロパティと同じく Global(m_Config/BackingStore) への正規シム（独立フィールドを持たない）。
        public string UseLang
        {
            get => _config.useLang;
            set => SetIfChanged(ref _config.useLang, value);
        }

        // 旧 Global.runHotPlug（保存しない実行時のフラグ）。このサービス（DI のシングルトン）のインスタンスの状態にした
        private bool _runHotPlug;

        public bool RunHotPlug
        {
            get => _runHotPlug;
            set => SetIfChanged(ref _runHotPlug, value);
        }

        // ---- プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        public int ProfileEditorLeftWidth
        {
            get => _config.profileEditorLeftWidth;
            set => SetIfChanged(ref _config.profileEditorLeftWidth, value);
        }

        public int ProfileEditorRightWidth
        {
            get => _config.profileEditorRightWidth;
            set => SetIfChanged(ref _config.profileEditorRightWidth, value);
        }

        public int SpecialActionNameColWidth
        {
            get => _config.specialActionNameColWidth;
            set => SetIfChanged(ref _config.specialActionNameColWidth, value);
        }

        public int SpecialActionTriggerColWidth
        {
            get => _config.specialActionTriggerColWidth;
            set => SetIfChanged(ref _config.specialActionTriggerColWidth, value);
        }

        public int SpecialActionDetailColWidth
        {
            get => _config.specialActionDetailColWidth;
            set => SetIfChanged(ref _config.specialActionDetailColWidth, value);
        }

        // ---- ControlService の Global 直接参照解消（Global への薄い委譲）----

        public bool UseOscServer
        {
            get => _config.useOSCServ;
            set => SetIfChanged(ref _config.useOSCServ, value);
        }

        public bool UseOscSender
        {
            get => _config.useOSCSend;
            set => SetIfChanged(ref _config.useOSCSend, value);
        }

        public bool InterpretingOscMonitoring
        {
            get => _config.interpretingOscMonitoring;
            set => SetIfChanged(ref _config.interpretingOscMonitoring, value);
        }

        public int OscServerPort
        {
            get => _config.oscServPort;
            set => SetIfChanged(ref _config.oscServPort, value);
        }

        public string OscSenderAddress
        {
            get => _config.oscSendAddress;
            set
            {
                if (_config.oscSendAddress != value)
                {
                    _config.oscSendAddress = value.Trim();
                    NotifyChanged(nameof(OscSenderAddress));
                }
            }
        }

        public int OscSenderPort
        {
            get => _config.oscSendPort;
            set => SetIfChanged(ref _config.oscSendPort, value);
        }

        public bool QuickCharge
        {
            get => _config.quickCharge;
            set => SetIfChanged(ref _config.quickCharge, value);
        }

        public bool DCBTatStop
        {
            get => _config.disconnectBTAtStop;
            set => SetIfChanged(ref _config.disconnectBTAtStop, value);
        }

        public int ProcessPriority
        {
            get => _config.processPriority;
            set => SetIfChanged(ref _config.processPriority, value);
        }

        public ControlServiceDeviceOptions DeviceOptions => _config.deviceOptions;

        // 静的イベントだった UDPServerSmoothing*Changed を、このサービスのインスタンスイベントにした
        // （購読者は ControlService のみ。発火の条件と順序は従来と同一で、発火元だけが Global の静的イベントから this に変わった）。
        public event EventHandler UDPServerSmoothingMincutoffChanged;

        public event EventHandler UDPServerSmoothingBetaChanged;

        // ---- UDP サーバ平滑化パラメータ ----
        public double UDPServerSmoothingMincutoff
        {
            get => _config.udpSmoothingMincutoff;
            set
            {
                if (_config.udpSmoothingMincutoff != value)
                {
                    _config.udpSmoothingMincutoff = value;
                    UDPServerSmoothingMincutoffChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingMincutoff));
                }
            }
        }

        public double UDPServerSmoothingBeta
        {
            get => _config.udpSmoothingBeta;
            set
            {
                if (_config.udpSmoothingBeta != value)
                {
                    _config.udpSmoothingBeta = value;
                    UDPServerSmoothingBetaChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingBeta));
                }
            }
        }

        // ---- 遅延警告時のライトバー点滅 ----
        public bool FlashWhenLate
        {
            get => _config.flashWhenLate;
            set => SetIfChanged(ref _config.flashWhenLate, value);
        }

        // ---- 入力処理ホットパスで参照する設定 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の値を直接返し、キャッシュ・割り当て・ログを行わない。
        public bool UseUdpServerSmoothing
        {
            get => _config.useUdpSmoothing;
            set => SetIfChanged(ref _config.useUdpSmoothing, value);
        }

        public int FlashWhenLateAt
        {
            get => _config.flashWhenLateAt;
            set => SetIfChanged(ref _config.flashWhenLateAt, value);
        }

        // ---- SettingsViewModel の Global 直接参照解消（Global への薄い委譲。独自フィールドは持たない）----
        public bool UseCustomSteamFolder
        {
            get => _config.useCustomSteamFolder;
            set => SetIfChanged(ref _config.useCustomSteamFolder, value);
        }

        public string CustomSteamFolder
        {
            get => _config.customSteamFolder;
            set => SetIfChanged(ref _config.customSteamFolder, value);
        }

        // ファイル名に使えない文字を含む値は黙って無視する（従来の Global.FakeExeName の setter の動作。Phase7-Step3 W4 でここへ移した）。
        // 値が実際に変わったときだけ通知する。
        public string FakeExeName
        {
            get => _config.fakeExeFileName;
            set
            {
                if (_config.fakeExeFileName != value)
                {
                    if (!(value.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0))
                    {
                        _config.fakeExeFileName = value;
                    }

                    if (_config.fakeExeFileName == value)
                    {
                        NotifyChanged(nameof(FakeExeName));
                    }
                }
            }
        }

        public int LogMaxArchiveFiles
        {
            get => _config.logMaxArchiveFiles;
            set => SetIfChanged(ref _config.logMaxArchiveFiles, value);
        }
    }
}