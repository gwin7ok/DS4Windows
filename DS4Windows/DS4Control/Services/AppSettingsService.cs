using System;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    // ★ IDE0290 根本解決: C# 12 プライマリコンストラクタ構文を適用
    public class AppSettingsService(BackingStore config, IProfileXmlStore xmlStore = null, IPathService pathService = null) : IAppSettingsService
    {
        // Phase7-Step3 W2: Global の転送プロパティ経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IProfileXmlStore _xmlStore = xmlStore;
        private readonly IPathService _pathService = pathService;

        // Phase5-Step13-7根本修正: StartMinimized等はGlobal(m_Config/BackingStore)への
        // 正規シムとし、UI層(MainWindow.xaml.cs等)からGlobal直参照した場合と完全に同一の実体を
        // 参照するようにする。従来はここに独立したprivateフィールドを持ち、Global側と非連動という
        // 「孤立した重複状態」バグがあった(Step13投入時に発覚)。
        // CheckWhenのみ、対応するGlobal側の概念が既に撤廃されている(コメント "Legacy CheckWhen
        // removed. Use explicit CheckEveryValue/CheckEveryUnit instead." 参照)ため、
        // 単体テストのみで参照される非連動プロパティとして現状維持する。
        private int _checkWhen;

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
                AppLogger.LogToGui("Failed to save application settings", true);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppSettingsService.Save failed");
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

        public bool StartMinimized
        {
            get => _config.startMinimized;
            set
            {
                if (_config.startMinimized != value)
                {
                    _config.startMinimized = value;
                    NotifyChanged(nameof(StartMinimized));
                }
            }
        }

        public bool MinimizeToTaskbar
        {
            get => _config.minToTaskbar;
            set
            {
                if (_config.minToTaskbar != value)
                {
                    _config.minToTaskbar = value;
                    NotifyChanged(nameof(MinimizeToTaskbar));
                }
            }
        }

        public bool CloseMinimizes
        {
            get => _config.closeMini;
            set
            {
                if (_config.closeMini != value)
                {
                    _config.closeMini = value;
                    NotifyChanged(nameof(CloseMinimizes));
                }
            }
        }

        public int CheckWhen
        {
            get => _checkWhen;
            set
            {
                if (_checkWhen != value)
                {
                    _checkWhen = value;
                    NotifyChanged(nameof(CheckWhen));
                }
            }
        }

        public bool UseUdpServer
        {
            get => Global.isUsingUDPServer();
            set
            {
                if (Global.isUsingUDPServer() != value)
                {
                    Global.setUsingUDPServer(value);
                    NotifyChanged(nameof(UseUdpServer));
                }
            }
        }

        public int UdpServerPort
        {
            get => Global.getUDPServerPortNum();
            set
            {
                if (Global.getUDPServerPortNum() != value)
                {
                    Global.setUDPServerPort(value);
                    NotifyChanged(nameof(UdpServerPort));
                }
            }
        }

        public string UdpServerListenAddress
        {
            get => Global.getUDPServerListenAddress();
            set
            {
                if (Global.getUDPServerListenAddress() != value)
                {
                    Global.setUDPServerListenAddress(value);
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
        }

        public bool UseExclusiveMode
        {
            get => _config.useExclusiveMode;
            set
            {
                if (_config.useExclusiveMode != value)
                {
                    _config.useExclusiveMode = value;
                    NotifyChanged(nameof(UseExclusiveMode));
                }
            }
        }

        public bool AutoProfileRevertDefaultProfile
        {
            get => _config.autoProfileRevertDefaultProfile;
            set
            {
                if (_config.autoProfileRevertDefaultProfile != value)
                {
                    _config.autoProfileRevertDefaultProfile = value;
                    NotifyChanged(nameof(AutoProfileRevertDefaultProfile));
                }
            }
        }

        // ---- Phase5-Step13-7: ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        // いずれも m_Config(BackingStore) への薄い公開アクセサ(Global.X)への正規シム。
        public int FormWidth
        {
            get => _config.formWidth;
            set { if (_config.formWidth != value) { _config.formWidth = value; NotifyChanged(nameof(FormWidth)); } }
        }

        public int FormHeight
        {
            get => _config.formHeight;
            set { if (_config.formHeight != value) { _config.formHeight = value; NotifyChanged(nameof(FormHeight)); } }
        }

        public int FormLocationX
        {
            get => _config.formLocationX;
            set { if (_config.formLocationX != value) { _config.formLocationX = value; NotifyChanged(nameof(FormLocationX)); } }
        }

        public int FormLocationY
        {
            get => _config.formLocationY;
            set { if (_config.formLocationY != value) { _config.formLocationY = value; NotifyChanged(nameof(FormLocationY)); } }
        }

        public int ControllerIndexColWidth
        {
            get => _config.controllerIndexColWidth;
            set { if (_config.controllerIndexColWidth != value) { _config.controllerIndexColWidth = value; NotifyChanged(nameof(ControllerIndexColWidth)); } }
        }

        public int ControllerIdColWidth
        {
            get => _config.controllerIdColWidth;
            set { if (_config.controllerIdColWidth != value) { _config.controllerIdColWidth = value; NotifyChanged(nameof(ControllerIdColWidth)); } }
        }

        public int ControllerStatusColWidth
        {
            get => _config.controllerStatusColWidth;
            set { if (_config.controllerStatusColWidth != value) { _config.controllerStatusColWidth = value; NotifyChanged(nameof(ControllerStatusColWidth)); } }
        }

        public int ControllerExclusiveColWidth
        {
            get => _config.controllerExclusiveColWidth;
            set { if (_config.controllerExclusiveColWidth != value) { _config.controllerExclusiveColWidth = value; NotifyChanged(nameof(ControllerExclusiveColWidth)); } }
        }

        public int ControllerBatteryColWidth
        {
            get => _config.controllerBatteryColWidth;
            set { if (_config.controllerBatteryColWidth != value) { _config.controllerBatteryColWidth = value; NotifyChanged(nameof(ControllerBatteryColWidth)); } }
        }

        public int ControllerSelectProfileColWidth
        {
            get => _config.controllerSelectProfileColWidth;
            set { if (_config.controllerSelectProfileColWidth != value) { _config.controllerSelectProfileColWidth = value; NotifyChanged(nameof(ControllerSelectProfileColWidth)); } }
        }

        public int ControllerEditColWidth
        {
            get => _config.controllerEditColWidth;
            set { if (_config.controllerEditColWidth != value) { _config.controllerEditColWidth = value; NotifyChanged(nameof(ControllerEditColWidth)); } }
        }

        public int ControllerLinkedProfileColWidth
        {
            get => _config.controllerLinkedProfileColWidth;
            set { if (_config.controllerLinkedProfileColWidth != value) { _config.controllerLinkedProfileColWidth = value; NotifyChanged(nameof(ControllerLinkedProfileColWidth)); } }
        }

        public int ControllerLinkProfIdColWidth
        {
            get => _config.controllerLinkProfIdColWidth;
            set { if (_config.controllerLinkProfIdColWidth != value) { _config.controllerLinkProfIdColWidth = value; NotifyChanged(nameof(ControllerLinkProfIdColWidth)); } }
        }

        public int ControllerCustomColorColWidth
        {
            get => _config.controllerCustomColorColWidth;
            set { if (_config.controllerCustomColorColWidth != value) { _config.controllerCustomColorColWidth = value; NotifyChanged(nameof(ControllerCustomColorColWidth)); } }
        }

        // ---- Phase5-Step13-7: 通知レベル・スワイプ操作設定 ----
        public int Notifications
        {
            get => _config.notifications;
            set { if (_config.notifications != value) { _config.notifications = value; NotifyChanged(nameof(Notifications)); } }
        }

        public bool ProfileChangedNotification
        {
            get => Global.ProfileChangedNotification;
            set
            {
                if (Global.ProfileChangedNotification == value) return;
                Global.ProfileChangedNotification = value;
                NotifyChanged(nameof(ProfileChangedNotification));
            }
        }
        public bool SwipeProfiles
        {
            get => _config.swipeProfiles;
            set { if (_config.swipeProfiles != value) { _config.swipeProfiles = value; NotifyChanged(nameof(SwipeProfiles)); } }
        }

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い公開アクセサへの正規シム。
        // Notifications 等と同一パターン（独立フィールドを持たない）。
        public string LogMinLevel
        {
            get => Global.LogMinLevel;
            set { if (Global.LogMinLevel != value) { Global.LogMinLevel = value; NotifyChanged(nameof(LogMinLevel)); } }
        }

        public bool CheckUpdateStartupEnabled
        {
            get => _config.checkUpdateStartup;
            set { if (_config.checkUpdateStartup != value) { _config.checkUpdateStartup = value; NotifyChanged(nameof(CheckUpdateStartupEnabled)); } }
        }

        public int CheckEveryValue
        {
            get => Global.CheckEveryValue;
            set { if (Global.CheckEveryValue != value) { Global.CheckEveryValue = value; NotifyChanged(nameof(CheckEveryValue)); } }
        }

        public int CheckEveryUnit
        {
            get => Global.CheckEveryUnit;
            set { if (Global.CheckEveryUnit != value) { Global.CheckEveryUnit = value; NotifyChanged(nameof(CheckEveryUnit)); } }
        }

        public DateTime LastChecked
        {
            get => Global.LastChecked;
            set { if (Global.LastChecked != value) { Global.LastChecked = value; NotifyChanged(nameof(LastChecked)); } }
        }

        // Global.LastVersionCheckedNum は読み取り専用（LastVersionChecked代入時に自動算出）のため、
        // 本シムも読み取り専用として公開する。
        public ulong LastVersionCheckedNum => Global.LastVersionCheckedNum;

        // ---- Phase6-Step10-1-1: Global への薄い委譲 ----
        public string LastVersionChecked
        {
            get => Global.LastVersionChecked;
            set
            {
                if (Global.LastVersionChecked != value)
                {
                    Global.LastVersionChecked = value;
                    NotifyChanged(nameof(LastVersionChecked));
                    NotifyChanged(nameof(LastVersionCheckedNum));
                }
            }
        }

        public void SetCulture(string culture) => Global.SetCulture(culture);

        // ---- Phase6-Step10-2-1: Global への薄い委譲 ----
        public bool UseMoonlight
        {
            get => Global.UseMoonlight;
            set { if (Global.UseMoonlight != value) { Global.UseMoonlight = value; NotifyChanged(nameof(UseMoonlight)); } }
        }

        public bool UseAdvancedMoonlight
        {
            get => Global.UseAdvancedMoonlight;
            set { if (Global.UseAdvancedMoonlight != value) { Global.UseAdvancedMoonlight = value; NotifyChanged(nameof(UseAdvancedMoonlight)); } }
        }

        public bool FirstRun
        {
            get => Global.firstRun;
            set { if (Global.firstRun != value) { Global.firstRun = value; NotifyChanged(nameof(FirstRun)); } }
        }

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // 他のプロパティと同じく Global(m_Config/BackingStore) への正規シム（独立フィールドを持たない）。
        public string UseLang
        {
            get => Global.UseLang;
            set { if (Global.UseLang != value) { Global.UseLang = value; NotifyChanged(nameof(UseLang)); } }
        }

        public bool RunHotPlug
        {
            get => Global.runHotPlug;
            set { if (Global.runHotPlug != value) { Global.runHotPlug = value; NotifyChanged(nameof(RunHotPlug)); } }
        }

        // ---- Phase5-Step14: プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        public int ProfileEditorLeftWidth
        {
            get => _config.profileEditorLeftWidth;
            set
            {
                if (_config.profileEditorLeftWidth != value)
                {
                    _config.profileEditorLeftWidth = value;
                    NotifyChanged(nameof(ProfileEditorLeftWidth));
                }
            }
        }

        public int ProfileEditorRightWidth
        {
            get => _config.profileEditorRightWidth;
            set
            {
                if (_config.profileEditorRightWidth != value)
                {
                    _config.profileEditorRightWidth = value;
                    NotifyChanged(nameof(ProfileEditorRightWidth));
                }
            }
        }

        public int SpecialActionNameColWidth
        {
            get => _config.specialActionNameColWidth;
            set
            {
                if (_config.specialActionNameColWidth != value)
                {
                    _config.specialActionNameColWidth = value;
                    NotifyChanged(nameof(SpecialActionNameColWidth));
                }
            }
        }

        public int SpecialActionTriggerColWidth
        {
            get => _config.specialActionTriggerColWidth;
            set
            {
                if (_config.specialActionTriggerColWidth != value)
                {
                    _config.specialActionTriggerColWidth = value;
                    NotifyChanged(nameof(SpecialActionTriggerColWidth));
                }
            }
        }

        public int SpecialActionDetailColWidth
        {
            get => _config.specialActionDetailColWidth;
            set
            {
                if (_config.specialActionDetailColWidth != value)
                {
                    _config.specialActionDetailColWidth = value;
                    NotifyChanged(nameof(SpecialActionDetailColWidth));
                }
            }
        }

        // ---- Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消（Global への薄い委譲）----

        public bool UseOscServer
        {
            get => Global.isUsingOSCServer();
            set
            {
                if (Global.isUsingOSCServer() != value)
                {
                    Global.setUsingOSCServer(value);
                    NotifyChanged(nameof(UseOscServer));
                }
            }
        }

        public bool UseOscSender
        {
            get => Global.isUsingOSCSender();
            set
            {
                if (Global.isUsingOSCSender() != value)
                {
                    Global.setUsingOSCSender(value);
                    NotifyChanged(nameof(UseOscSender));
                }
            }
        }

        public bool InterpretingOscMonitoring
        {
            get => Global.isInterpretingOscMonitoring();
            set
            {
                if (Global.isInterpretingOscMonitoring() != value)
                {
                    Global.setInterpretingOscMonitoring(value);
                    NotifyChanged(nameof(InterpretingOscMonitoring));
                }
            }
        }

        public int OscServerPort
        {
            get => Global.getOSCServerPortNum();
            set
            {
                if (Global.getOSCServerPortNum() != value)
                {
                    Global.setOSCServerPort(value);
                    NotifyChanged(nameof(OscServerPort));
                }
            }
        }

        public string OscSenderAddress
        {
            get => Global.getOSCSenderAddress();
            set
            {
                if (Global.getOSCSenderAddress() != value)
                {
                    Global.setOSCSenderAddress(value);
                    NotifyChanged(nameof(OscSenderAddress));
                }
            }
        }

        public int OscSenderPort
        {
            get => Global.getOSCSenderPortNum();
            set
            {
                if (Global.getOSCSenderPortNum() != value)
                {
                    Global.setOSCSenderPort(value);
                    NotifyChanged(nameof(OscSenderPort));
                }
            }
        }

        public bool QuickCharge
        {
            get => _config.quickCharge;
            set
            {
                if (_config.quickCharge != value)
                {
                    _config.quickCharge = value;
                    NotifyChanged(nameof(QuickCharge));
                }
            }
        }

        public bool DCBTatStop
        {
            get => _config.disconnectBTAtStop;
            set
            {
                if (_config.disconnectBTAtStop != value)
                {
                    _config.disconnectBTAtStop = value;
                    NotifyChanged(nameof(DCBTatStop));
                }
            }
        }

        public int ProcessPriority
        {
            get => _config.processPriority;
            set
            {
                if (_config.processPriority != value)
                {
                    _config.processPriority = value;
                    NotifyChanged(nameof(ProcessPriority));
                }
            }
        }

        public ControlServiceDeviceOptions DeviceOptions => Global.DeviceOptions;

        // Global 側の static イベントへ購読をそのまま転送する（発火元・引数は従来と完全に同一）。
        public event EventHandler UDPServerSmoothingMincutoffChanged
        {
            add => Global.UDPServerSmoothingMincutoffChanged += value;
            remove => Global.UDPServerSmoothingMincutoffChanged -= value;
        }

        public event EventHandler UDPServerSmoothingBetaChanged
        {
            add => Global.UDPServerSmoothingBetaChanged += value;
            remove => Global.UDPServerSmoothingBetaChanged -= value;
        }

        // ---- Phase6-Step2-2 (PR-2): UDP サーバ平滑化パラメータ ----
        // 値の変更は Global 側のプロパティが UDPServerSmoothing*Changed イベントを発火する（従来と同一）。
        public double UDPServerSmoothingMincutoff
        {
            get => Global.UDPServerSmoothingMincutoff;
            set
            {
                if (Global.UDPServerSmoothingMincutoff != value)
                {
                    Global.UDPServerSmoothingMincutoff = value;
                    NotifyChanged(nameof(UDPServerSmoothingMincutoff));
                }
            }
        }

        public double UDPServerSmoothingBeta
        {
            get => Global.UDPServerSmoothingBeta;
            set
            {
                if (Global.UDPServerSmoothingBeta != value)
                {
                    Global.UDPServerSmoothingBeta = value;
                    NotifyChanged(nameof(UDPServerSmoothingBeta));
                }
            }
        }

        // ---- Phase6-Step2-4 (PR-4): 遅延警告時のライトバー点滅 ----
        public bool FlashWhenLate
        {
            get => _config.flashWhenLate;
            set
            {
                if (_config.flashWhenLate != value)
                {
                    _config.flashWhenLate = value;
                    NotifyChanged(nameof(FlashWhenLate));
                }
            }
        }

        // ---- Phase6-Step2-5 (PR-5): 入力処理ホットパスで参照する設定 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の値を直接返し、キャッシュ・割り当て・ログを行わない。
        public bool UseUdpServerSmoothing
        {
            get => _config.useUdpSmoothing;
            set
            {
                if (_config.useUdpSmoothing != value)
                {
                    _config.useUdpSmoothing = value;
                    NotifyChanged(nameof(UseUdpServerSmoothing));
                }
            }
        }

        public int FlashWhenLateAt
        {
            get => _config.flashWhenLateAt;
            set
            {
                if (_config.flashWhenLateAt != value)
                {
                    _config.flashWhenLateAt = value;
                    NotifyChanged(nameof(FlashWhenLateAt));
                }
            }
        }

        // ---- Phase6-Step9-1: SettingsViewModel の Global 直接参照解消（Global への薄い委譲。独自フィールドは持たない）----
        public bool UseCustomSteamFolder
        {
            get => _config.useCustomSteamFolder;
            set
            {
                if (_config.useCustomSteamFolder != value)
                {
                    _config.useCustomSteamFolder = value;
                    NotifyChanged(nameof(UseCustomSteamFolder));
                }
            }
        }

        public string CustomSteamFolder
        {
            get => _config.customSteamFolder;
            set
            {
                if (_config.customSteamFolder != value)
                {
                    _config.customSteamFolder = value;
                    NotifyChanged(nameof(CustomSteamFolder));
                }
            }
        }

        // Global.FakeExeName の setter は、ファイル名に使えない文字を含む値を黙って無視する（従来の動作）。
        // 値が実際に変わったときだけ通知する。
        public string FakeExeName
        {
            get => Global.FakeExeName;
            set
            {
                if (Global.FakeExeName != value)
                {
                    Global.FakeExeName = value;
                    if (Global.FakeExeName == value)
                    {
                        NotifyChanged(nameof(FakeExeName));
                    }
                }
            }
        }

        public int LogMaxArchiveFiles
        {
            get => Global.LogMaxArchiveFiles;
            set
            {
                if (Global.LogMaxArchiveFiles != value)
                {
                    Global.LogMaxArchiveFiles = value;
                    NotifyChanged(nameof(LogMaxArchiveFiles));
                }
            }
        }
    }
}