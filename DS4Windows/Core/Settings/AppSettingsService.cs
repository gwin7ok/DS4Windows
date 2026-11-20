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
    public class AppSettingsService(BackingStore config, IProfileXmlStore? xmlStore = null, IPathService? pathService = null) : IAppSettingsService
    {
        // Global の転送プロパティ経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IProfileXmlStore? _xmlStore = xmlStore;
        private readonly IPathService? _pathService = pathService;

        // StartMinimized等はGlobal(m_Config/BackingStore)への
        // 正規シムとし、UI層(MainWindow.xaml.cs等)からGlobal直参照した場合と完全に同一の実体を
        // 参照するようにする。従来はここに独立したprivateフィールドを持ち、Global側と非連動という
        // 「孤立した重複状態」バグがあった(Step13投入時に発覚)。
        // 非連動プロパティ CheckWhenを削除した。

        public event EventHandler<string>? SettingChanged;

        // (※旧コンストラクタ public AppSettingsService(...) { ... } は削除)
        public bool Save()
        {
            bool success = _xmlStore!.SaveAppSettingsXml();   // 保存・読み込みを使う生成（DI の登録）では、必ず渡される
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
            bool success = _xmlStore!.LoadAppSettingsXml();   // 同上
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
        /// <summary>設定の領域のプロパティ用（プロパティは ref で渡せないため、現在の値と代入の処理を受ける）。</summary>
        private void SetIfChanged<T>(T current, Action<T> assign, T value, [CallerMemberName] string? settingName = null)
        {
            if (EqualityComparer<T>.Default.Equals(current, value)) return;
            assign(value);
            NotifyChanged(settingName!);   // CallerMemberName が、プロパティの中からの呼び出しで名前を埋める
        }

        private void SetIfChanged<T>(ref T field, T value, [CallerMemberName] string? settingName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            NotifyChanged(settingName!);   // CallerMemberName が、プロパティの中からの呼び出しで名前を埋める
        }

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

        public bool UseUdpServer
        {
            get => _config.Network.useUDPServ;
            set => SetIfChanged(_config.Network.useUDPServ, v => _config.Network.useUDPServ = v, value);
        }

        public int UdpServerPort
        {
            get => _config.Network.udpServPort;
            set => SetIfChanged(_config.Network.udpServPort, v => _config.Network.udpServPort = v, value);
        }

        public string UdpServerListenAddress
        {
            get => _config.Network.udpServListenAddress;
            set
            {
                if (_config.Network.udpServListenAddress != value)
                {
                    _config.Network.udpServListenAddress = value.Trim();
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
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

        // ---- ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        // いずれも m_Config(BackingStore) への薄い公開アクセサ(Global.X)への正規シム。
        public int FormWidth
        {
            get => _config.Layout.formWidth;
            set => SetIfChanged(_config.Layout.formWidth, v => _config.Layout.formWidth = v, value);
        }

        public int FormHeight
        {
            get => _config.Layout.formHeight;
            set => SetIfChanged(_config.Layout.formHeight, v => _config.Layout.formHeight = v, value);
        }

        public int FormLocationX
        {
            get => _config.Layout.formLocationX;
            set => SetIfChanged(_config.Layout.formLocationX, v => _config.Layout.formLocationX = v, value);
        }

        public int FormLocationY
        {
            get => _config.Layout.formLocationY;
            set => SetIfChanged(_config.Layout.formLocationY, v => _config.Layout.formLocationY = v, value);
        }

        public int ControllerIndexColWidth
        {
            get => _config.Layout.controllerIndexColWidth;
            set => SetIfChanged(_config.Layout.controllerIndexColWidth, v => _config.Layout.controllerIndexColWidth = v, value);
        }

        public int ControllerIdColWidth
        {
            get => _config.Layout.controllerIdColWidth;
            set => SetIfChanged(_config.Layout.controllerIdColWidth, v => _config.Layout.controllerIdColWidth = v, value);
        }

        public int ControllerStatusColWidth
        {
            get => _config.Layout.controllerStatusColWidth;
            set => SetIfChanged(_config.Layout.controllerStatusColWidth, v => _config.Layout.controllerStatusColWidth = v, value);
        }

        public int ControllerExclusiveColWidth
        {
            get => _config.Layout.controllerExclusiveColWidth;
            set => SetIfChanged(_config.Layout.controllerExclusiveColWidth, v => _config.Layout.controllerExclusiveColWidth = v, value);
        }

        public int ControllerBatteryColWidth
        {
            get => _config.Layout.controllerBatteryColWidth;
            set => SetIfChanged(_config.Layout.controllerBatteryColWidth, v => _config.Layout.controllerBatteryColWidth = v, value);
        }

        public int ControllerSelectProfileColWidth
        {
            get => _config.Layout.controllerSelectProfileColWidth;
            set => SetIfChanged(_config.Layout.controllerSelectProfileColWidth, v => _config.Layout.controllerSelectProfileColWidth = v, value);
        }

        public int ControllerEditColWidth
        {
            get => _config.Layout.controllerEditColWidth;
            set => SetIfChanged(_config.Layout.controllerEditColWidth, v => _config.Layout.controllerEditColWidth = v, value);
        }

        public int ControllerLinkedProfileColWidth
        {
            get => _config.Layout.controllerLinkedProfileColWidth;
            set => SetIfChanged(_config.Layout.controllerLinkedProfileColWidth, v => _config.Layout.controllerLinkedProfileColWidth = v, value);
        }

        public int ControllerLinkProfIdColWidth
        {
            get => _config.Layout.controllerLinkProfIdColWidth;
            set => SetIfChanged(_config.Layout.controllerLinkProfIdColWidth, v => _config.Layout.controllerLinkProfIdColWidth = v, value);
        }

        public int ControllerCustomColorColWidth
        {
            get => _config.Layout.controllerCustomColorColWidth;
            set => SetIfChanged(_config.Layout.controllerCustomColorColWidth, v => _config.Layout.controllerCustomColorColWidth = v, value);
        }

        public int OutputSlotNumColWidth
        {
            get => _config.Layout.outputSlotNumColWidth;
            set => SetIfChanged(_config.Layout.outputSlotNumColWidth, v => _config.Layout.outputSlotNumColWidth = v, value);
        }

        public int OutputSlotInputNumColWidth
        {
            get => _config.Layout.outputSlotInputNumColWidth;
            set => SetIfChanged(_config.Layout.outputSlotInputNumColWidth, v => _config.Layout.outputSlotInputNumColWidth = v, value);
        }

        public int OutputSlotInputIdColWidth
        {
            get => _config.Layout.outputSlotInputIdColWidth;
            set => SetIfChanged(_config.Layout.outputSlotInputIdColWidth, v => _config.Layout.outputSlotInputIdColWidth = v, value);
        }

        public int OutputSlotCurrentTypeColWidth
        {
            get => _config.Layout.outputSlotCurrentTypeColWidth;
            set => SetIfChanged(_config.Layout.outputSlotCurrentTypeColWidth, v => _config.Layout.outputSlotCurrentTypeColWidth = v, value);
        }

        public int OutputSlotReservationColWidth
        {
            get => _config.Layout.outputSlotReservationColWidth;
            set => SetIfChanged(_config.Layout.outputSlotReservationColWidth, v => _config.Layout.outputSlotReservationColWidth = v, value);
        }

        public int AutoProfileNameColWidth
        {
            get => _config.Layout.autoProfileNameColWidth;
            set => SetIfChanged(_config.Layout.autoProfileNameColWidth, v => _config.Layout.autoProfileNameColWidth = v, value);
        }

        public int AutoProfilePathColWidth
        {
            get => _config.Layout.autoProfilePathColWidth;
            set => SetIfChanged(_config.Layout.autoProfilePathColWidth, v => _config.Layout.autoProfilePathColWidth = v, value);
        }

        public int AutoProfileTitleColWidth
        {
            get => _config.Layout.autoProfileTitleColWidth;
            set => SetIfChanged(_config.Layout.autoProfileTitleColWidth, v => _config.Layout.autoProfileTitleColWidth = v, value);
        }

        public int LogTimeColWidth
        {
            get => _config.Layout.logTimeColWidth;
            set => SetIfChanged(_config.Layout.logTimeColWidth, v => _config.Layout.logTimeColWidth = v, value);
        }

        public int LogDataColWidth
        {
            get => _config.Layout.logDataColWidth;
            set => SetIfChanged(_config.Layout.logDataColWidth, v => _config.Layout.logDataColWidth = v, value);
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

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い公開アクセサへの正規シム。
        // Notifications 等と同一パターン（独立フィールドを持たない）。
        public string LogMinLevel
        {
            get => _config.Log.logMinLevel;
            set => SetIfChanged(_config.Log.logMinLevel, v => _config.Log.logMinLevel = v, value);
        }

        public bool CheckUpdateStartupEnabled
        {
            get => _config.UpdateCheck.checkUpdateStartup;
            set => SetIfChanged(_config.UpdateCheck.checkUpdateStartup, v => _config.UpdateCheck.checkUpdateStartup = v, value);
        }

        public int CheckEveryValue
        {
            get => _config.UpdateCheck.checkEveryValue;
            set => SetIfChanged(_config.UpdateCheck.checkEveryValue, v => _config.UpdateCheck.checkEveryValue = v, value);
        }

        public int CheckEveryUnit
        {
            get => _config.UpdateCheck.checkEveryUnit;
            set => SetIfChanged(_config.UpdateCheck.checkEveryUnit, v => _config.UpdateCheck.checkEveryUnit = v, value);
        }

        public DateTime LastChecked
        {
            get => _config.UpdateCheck.lastChecked;
            set => SetIfChanged(_config.UpdateCheck.lastChecked, v => _config.UpdateCheck.lastChecked = v, value);
        }

        // _config.UpdateCheck.lastVersionCheckedNum は読み取り専用（LastVersionChecked代入時に自動算出）のため、
        // 本シムも読み取り専用として公開する。
        public ulong LastVersionCheckedNum => _config.UpdateCheck.lastVersionCheckedNum;

        // 代入時に番号（LastVersionCheckedNum）を算出する処理を、Global のプロパティからここへ移した。
        public string LastVersionChecked
        {
            get => _config.UpdateCheck.lastVersionChecked;
            set
            {
                if (_config.UpdateCheck.lastVersionChecked != value)
                {
                    _config.UpdateCheck.lastVersionChecked = value;
                    _config.UpdateCheck.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(value);
                    NotifyChanged(nameof(LastVersionChecked));
                    NotifyChanged(nameof(LastVersionCheckedNum));
                }
            }
        }

        public void SetCulture(string culture) => UiCulture.Set(culture);

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
            get => _config.Appearance.useLang;
            set => SetIfChanged(_config.Appearance.useLang, v => _config.Appearance.useLang = v, value);
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
            get => _config.Layout.profileEditorLeftWidth;
            set => SetIfChanged(_config.Layout.profileEditorLeftWidth, v => _config.Layout.profileEditorLeftWidth = v, value);
        }

        public int ProfileEditorRightWidth
        {
            get => _config.Layout.profileEditorRightWidth;
            set => SetIfChanged(_config.Layout.profileEditorRightWidth, v => _config.Layout.profileEditorRightWidth = v, value);
        }

        public int SpecialActionNameColWidth
        {
            get => _config.Layout.specialActionNameColWidth;
            set => SetIfChanged(_config.Layout.specialActionNameColWidth, v => _config.Layout.specialActionNameColWidth = v, value);
        }

        public int SpecialActionTriggerColWidth
        {
            get => _config.Layout.specialActionTriggerColWidth;
            set => SetIfChanged(_config.Layout.specialActionTriggerColWidth, v => _config.Layout.specialActionTriggerColWidth = v, value);
        }

        public int SpecialActionDetailColWidth
        {
            get => _config.Layout.specialActionDetailColWidth;
            set => SetIfChanged(_config.Layout.specialActionDetailColWidth, v => _config.Layout.specialActionDetailColWidth = v, value);
        }

        // ---- ControlService の Global 直接参照解消（Global への薄い委譲）----

        public bool UseOscServer
        {
            get => _config.Network.useOSCServ;
            set => SetIfChanged(_config.Network.useOSCServ, v => _config.Network.useOSCServ = v, value);
        }

        public bool UseOscSender
        {
            get => _config.Network.useOSCSend;
            set => SetIfChanged(_config.Network.useOSCSend, v => _config.Network.useOSCSend = v, value);
        }

        public bool InterpretingOscMonitoring
        {
            get => _config.Network.interpretingOscMonitoring;
            set => SetIfChanged(_config.Network.interpretingOscMonitoring, v => _config.Network.interpretingOscMonitoring = v, value);
        }

        public int OscServerPort
        {
            get => _config.Network.oscServPort;
            set => SetIfChanged(_config.Network.oscServPort, v => _config.Network.oscServPort = v, value);
        }

        public string OscSenderAddress
        {
            get => _config.Network.oscSendAddress;
            set
            {
                if (_config.Network.oscSendAddress != value)
                {
                    _config.Network.oscSendAddress = value.Trim();
                    NotifyChanged(nameof(OscSenderAddress));
                }
            }
        }

        public int OscSenderPort
        {
            get => _config.Network.oscSendPort;
            set => SetIfChanged(_config.Network.oscSendPort, v => _config.Network.oscSendPort = v, value);
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

        public ControlServiceDeviceOptions DeviceOptions => _config.deviceOptions;

        // 静的イベントだった UDPServerSmoothing*Changed を、このサービスのインスタンスイベントにした
        // （購読者は ControlService のみ。発火の条件と順序は従来と同一で、発火元だけが Global の静的イベントから this に変わった）。
        public event EventHandler? UDPServerSmoothingMincutoffChanged;

        public event EventHandler? UDPServerSmoothingBetaChanged;

        // ---- UDP サーバ平滑化パラメータ ----
        public double UDPServerSmoothingMincutoff
        {
            get => _config.Network.udpSmoothingMincutoff;
            set
            {
                if (_config.Network.udpSmoothingMincutoff != value)
                {
                    _config.Network.udpSmoothingMincutoff = value;
                    UDPServerSmoothingMincutoffChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingMincutoff));
                }
            }
        }

        public double UDPServerSmoothingBeta
        {
            get => _config.Network.udpSmoothingBeta;
            set
            {
                if (_config.Network.udpSmoothingBeta != value)
                {
                    _config.Network.udpSmoothingBeta = value;
                    UDPServerSmoothingBetaChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingBeta));
                }
            }
        }

        // ---- 遅延警告時のライトバー点滅 ----
        public bool FlashWhenLate
        {
            get => _config.Behavior.flashWhenLate;
            set => SetIfChanged(_config.Behavior.flashWhenLate, v => _config.Behavior.flashWhenLate = v, value);
        }

        // ---- 入力処理ホットパスで参照する設定 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の値を直接返し、キャッシュ・割り当て・ログを行わない。
        public bool UseUdpServerSmoothing
        {
            get => _config.Network.useUdpSmoothing;
            set => SetIfChanged(_config.Network.useUdpSmoothing, v => _config.Network.useUdpSmoothing = v, value);
        }

        public int FlashWhenLateAt
        {
            get => _config.Behavior.flashWhenLateAt;
            set => SetIfChanged(_config.Behavior.flashWhenLateAt, v => _config.Behavior.flashWhenLateAt = v, value);
        }

        // ---- SettingsViewModel の Global 直接参照解消（Global への薄い委譲。独自フィールドは持たない）----
        public bool UseCustomSteamFolder
        {
            get => _config.Network.useCustomSteamFolder;
            set => SetIfChanged(_config.Network.useCustomSteamFolder, v => _config.Network.useCustomSteamFolder = v, value);
        }

        public string? CustomSteamFolder
        {
            get => _config.Network.customSteamFolder;
            set => SetIfChanged(_config.Network.customSteamFolder, v => _config.Network.customSteamFolder = v, value);
        }

        // ファイル名に使えない文字を含む値は黙って無視する（従来の Global.FakeExeName の setter の動作。Phase7-Step3 W4 でここへ移した）。
        // 値が実際に変わったときだけ通知する。
        public string FakeExeName
        {
            get => _config.Appearance.fakeExeFileName;
            set
            {
                if (_config.Appearance.fakeExeFileName != value)
                {
                    if (!(value.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0))
                    {
                        _config.Appearance.fakeExeFileName = value;
                    }

                    if (_config.Appearance.fakeExeFileName == value)
                    {
                        NotifyChanged(nameof(FakeExeName));
                    }
                }
            }
        }

        public int LogMaxArchiveFiles
        {
            get => _config.Log.logMaxArchiveFiles;
            set => SetIfChanged(_config.Log.logMaxArchiveFiles, v => _config.Log.logMaxArchiveFiles = v, value);
        }
    }
}