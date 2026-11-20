using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Slots
{
    public class AutoProfileService : IAutoProfileService
    {
        private readonly object _syncLock = new object();
        private readonly AutoProfileHolder _holder;
        private readonly IProfileApplicationService _profileAppService;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProcessInspector _processInspector;
        // スロット上限（現在接続台数ではない）の取得元。
        private readonly IEnvironmentService _environmentService;

        private bool _turnOffTemp;
        private AutoProfileEntity? _tempAutoProfile;
        private bool _running;
        private int _autoProfileDebugLogLevel;

        public bool Running
        {
            get => _running;
            set => _running = value;
        }

        public int AutoProfileDebugLogLevel
        {
            get => _autoProfileDebugLogLevel;
            set => _autoProfileDebugLogLevel = value;
        }

        public AutoProfileHolder Holder => _holder;

        // Global 経由をやめ、注入された BackingStore を直接使う。
        private readonly BackingStore _config;

        public AutoProfileDisplayProfileSwitchChoices AutoProfileSwitchNotifyChoice
        {
            get => _config.autoProfileSwitchNotifyChoice;
            set => _config.autoProfileSwitchNotifyChoice = value;
        }

        // 画面（AutoProfiles）が使う。Phase7-Step10d W4: 旧 Global.CreateAutoProfiles の本体をここへ移した（内容は同一）
        public bool CreateAutoProfilesFile(string path)
        {
            bool Saved = true;

            try
            {
                System.Xml.XmlNode Node;
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

                Node = doc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                doc.AppendChild(Node);

                Node = doc.CreateComment(string.Format(" Auto-Profile Configuration Data. {0} ", DateTime.Now));
                doc.AppendChild(Node);

                Node = doc.CreateWhitespace("\r\n");
                doc.AppendChild(Node);

                Node = doc.CreateNode(System.Xml.XmlNodeType.Element, "Programs", "");
                doc.AppendChild(Node);
                doc.Save(path);
            }
            catch { Saved = false; }

            return Saved;
        }

        public event Action<bool>? RequestServiceChange;

        // 全ての依存を必須化（new／AppHost／Program.rootHub／Global の設定参照を廃止）。
        // 旧 Func<ControlService>（決定 D4-3）。使うのは running と Controllers だけだったため、IServiceLifecycle と IDeviceStateAccessor を直接受ける
        private readonly IServiceLifecycle _lifecycle;
        private readonly IDeviceStateAccessor _deviceState;
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileRepository _profileRepository;

        public AutoProfileService(AutoProfileHolder holder,
            IProfileApplicationService profileAppService,
            IProfileSettingsService profileSettings,
            IProcessInspector processInspector,
            IEnvironmentService environmentService,
            IAppSettingsService appSettings,
            IProfileRepository profileRepository,
            IServiceLifecycle lifecycle,
            IDeviceStateAccessor deviceState,
            BackingStore config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _config = config;
            ArgumentNullException.ThrowIfNull(holder);
            _holder = holder;
            ArgumentNullException.ThrowIfNull(profileAppService);
            _profileAppService = profileAppService;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(processInspector);
            _processInspector = processInspector;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(profileRepository);
            _profileRepository = profileRepository;
            ArgumentNullException.ThrowIfNull(lifecycle);
            _lifecycle = lifecycle;
            ArgumentNullException.ThrowIfNull(deviceState);
            _deviceState = deviceState;
        }

        // 2026-10-19（AutoProfile-NoController-Investigation.md 案B）: 自動プロファイルは、接続中のスロットにだけ適用する。
        // テストなどで差し替える。未設定（null）なら Controllers[slot] が null でないことを接続とみなす。
        public Func<int, bool>? SlotConnectedProbe { get; set; }

        private bool IsSlotConnected(int slot)
        {
            if (SlotConnectedProbe != null)
                return SlotConnectedProbe(slot);

            return _deviceState.GetController(slot) != null;   // 範囲外は null
        }

        // S9-5 の確認で判明: 元のプロファイルへ戻すときの通知が、バッテリー残量を常に「N/A」と出していた。
        // 手動の切り替え・SA の Load Profile と同じく、接続中のコントローラーの Battery を使う（未接続のときだけ「N/A」）
        private string BatteryText(int slot)
        {
            var device = _deviceState.GetController(slot);
            return device != null ? $"{device.Battery}" : "N/A";
        }

        // 最後に取得できた前景ウィンドウの情報。前景が変わらないと GetForegroundProcessInfo が false を返すため、
        // コントローラーの接続時の再確認（CheckProfilesForConnectedController）はこの値を使う。
        private string? _lastProcessName;
        private string? _lastWindowTitle;
        private bool _hasLastForeground;

        public void CheckProfiles()
        {
            lock (_syncLock)
            {
                if (_holder == null || _processInspector == null || _profileAppService == null)
                    return;

                string topProcessName, topWindowTitle;
                if (!_processInspector.GetForegroundProcessInfo(out topProcessName, out topWindowTitle))
                    return;

                _lastProcessName = topProcessName;
                _lastWindowTitle = topWindowTitle;
                _hasLastForeground = true;

                CheckProfilesCore(topProcessName, topWindowTitle);
            }
        }

        public void CheckProfilesForConnectedController()
        {
            lock (_syncLock)
            {
                if (_holder == null || _profileAppService == null || !_running || !_hasLastForeground)
                    return;

                AppLogger.LogDebug("[DI] AutoProfileService.CheckProfilesForConnectedController: re-checking with the last foreground window");
                CheckProfilesCore(_lastProcessName!, _lastWindowTitle!); // _hasLastForeground が true のときだけ来る（前景ウィンドウを記録済み）
            }
        }

        // _syncLock を保持して呼ぶこと。
        private void CheckProfilesCore(string topProcessName, string topWindowTitle)
        {
            {
                bool turnOffDS4WinApp = false;
                AutoProfileEntity? matchedProfileEntity = null;

                for (int i = 0, pathsLen = _holder.AutoProfileColl.Count; i < pathsLen; i++)
                {
                    AutoProfileEntity tempEntity = _holder.AutoProfileColl[i];
                    if (tempEntity.IsMatch(topProcessName, topWindowTitle))
                    {
                        if (AutoProfileDebugLogLevel > 0)
                            AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. Rule#{i + 1} Path={tempEntity.path} Title={tempEntity.title}", LogOptions.Temporary);

                        turnOffDS4WinApp = tempEntity.Turnoff;
                        matchedProfileEntity = tempEntity;
                        break;
                    }
                }

                if (matchedProfileEntity != null)
                {
                    bool forceLoadProfile = false;
                    if (!turnOffDS4WinApp && _turnOffTemp)
                    {
                        _turnOffTemp = false;
                        SetAndWaitServiceStatus(true);
                        forceLoadProfile = true;
                    }

                    for (int j = 0; j < _environmentService.ControllerSlotLimit; j++)
                    {
                        string tempname = matchedProfileEntity.ProfileNames[j];
                        if (!string.IsNullOrEmpty(tempname) && tempname != "(none)")
                        {
                            if (!IsSlotConnected(j))
                            {
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. Controller {j + 1} is not connected. Skipping LoadProfile {tempname}", LogOptions.Temporary);
                                continue;
                            }

                            bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                            string currentTemp = _profileSettings?.GetTempProfileName(j) ?? string.Empty;
                            string currentProfile = _profileRepository.ProfilePath[j];

                            if ((useTemp && tempname != currentTemp) ||
                                (!useTemp && tempname != currentProfile) ||
                                forceLoadProfile)
                            {
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. LoadProfile Controller {j + 1}={tempname}", LogOptions.Temporary);

                                string prolog = string.Format(DS4Windows.Properties.Resources.UsingAutoTempProfile, (j + 1).ToString(), tempname);

                                // Step 3 & Step 4 申し送り事項:
                                // IProfileApplicationService.ApplyProfile を呼び出す。
                                // Halt ガード（§5.2）はサービス内に内包され、ControlService の直参照は排除。
                                // displayNotification は null を指定して _profileSettings.ProfileChangedNotification を自動解決（Step 4）。
                                _profileAppService.ApplyProfile(j, tempname, isTemp: true, launchProgram: true,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog);
                            }
                            else if (AutoProfileDebugLogLevel > 0)
                            {
                                AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. LoadProfile Controller {j + 1}={tempname} (already loaded)", LogOptions.Temporary);
                            }
                        }
                    }

                    if (turnOffDS4WinApp)
                    {
                        _turnOffTemp = true;
                        if (_lifecycle.Running)
                        {
                            if (AutoProfileDebugLogLevel > 0)
                                AppLogger.Log(AppLogLevel.Info, "DEBUG: Auto-Profile. Turning DS4Windows temporarily off", LogOptions.Temporary);
                            SetAndWaitServiceStatus(false);
                        }
                    }

                    _tempAutoProfile = matchedProfileEntity;
                }
                else if (_tempAutoProfile != null)
                {
                    if (_turnOffTemp && _appSettings.AutoProfileRevertDefaultProfile)
                    {
                        _turnOffTemp = false;
                        if (!_lifecycle.Running)
                        {
                            if (AutoProfileDebugLogLevel > 0)
                                AppLogger.Log(AppLogLevel.Info, "DEBUG: Auto-Profile. Turning DS4Windows on before reverting to default profile", LogOptions.Temporary);
                            SetAndWaitServiceStatus(true);
                        }
                    }

                    _tempAutoProfile = null;
                    for (int j = 0; j < _environmentService.ControllerSlotLimit; j++)
                    {
                        bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                        if (useTemp)
                        {
                            if (_appSettings.AutoProfileRevertDefaultProfile)
                            {
                                string defaultProfile = _profileRepository.ProfilePath[j];
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. Unknown process. Reverting to default profile. Controller {j + 1}={defaultProfile} (default)", LogOptions.Temporary);

                                string prolog = string.Format(DS4Windows.Properties.Resources.UsingProfile, (j + 1).ToString(), defaultProfile, BatteryText(j));

                                // Step 3 & Step 4 申し送り事項:
                                // 一時プロファイル解除復帰（isTemp: false）、displayNotification: null で自動解決
                                _profileAppService.ApplyProfile(j, defaultProfile, isTemp: false, launchProgram: false,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog);
                            }
                            else if (AutoProfileDebugLogLevel > 0)
                            {
                                string tempName = _profileSettings?.GetTempProfileName(j) ?? string.Empty;
                                AppLogger.Log(AppLogLevel.Info, $"DEBUG: Auto-Profile. Unknown process. Existing profile left as active. Controller {j + 1}={tempName}", LogOptions.Temporary);
                            }
                        }
                    }
                }
            }
        }

        private void SetAndWaitServiceStatus(bool serviceRunningStatus)
        {
            if (_lifecycle.Running != serviceRunningStatus)
            {
                RequestServiceChange?.Invoke(serviceRunningStatus);
                Stopwatch sw = Stopwatch.StartNew();
                while (_lifecycle.Running != serviceRunningStatus && sw.Elapsed.TotalSeconds < 10)
                {
                    Thread.SpinWait(1000);
                }
                Thread.SpinWait(1000);
            }
        }
    }
}