using System;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileApplicationService : IProfileApplicationService
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IDeviceStateService _deviceState;
        private readonly Func<ControlService> _controlProvider;
        // Phase6-Step10-4-2a: ControlService は遅延解決（Func）で受け取る（Program.rootHub/AppHost 参照の廃止。決定 D4-3）
        private ControlService _control => _controlProvider();
        private readonly IProfileRepository _profileRepo;
        // Phase6-Step10-4-2f（K1 是正）: スロット上限は、固定の 4 ではなく、環境（Windows 8 以上は 8）から取得する
        private readonly IEnvironmentService _environmentService;

        public ProfileApplicationService(
            IProfileSettingsService profileSettings,
            IDeviceStateService deviceState,
            Func<ControlService> controlProvider,
            IProfileRepository profileRepo,
            IEnvironmentService environmentService)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _deviceState = deviceState ?? throw new ArgumentNullException(nameof(deviceState));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        public void ApplyFromAction(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit || action == null)
                return;

            DS4Device device = _control?.DS4Controllers?[deviceIndex];

            string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile,
                (deviceIndex + 1).ToString(), action.details, device != null ? $"{device.Battery}" : "N/A");

            Task.Run(() =>
            {
                if (device != null)
                {
                    // C案: HaltReporting の区間内ではプロファイルメモリ適用のみを高速実行（1〜2ms以内）。
                    // 通知表示（独自ウィンドウ／トースト）は CompleteProfileApplication が発火する
                    // LogProfileChanged イベント → MainWindow.OnProfileChanged（Dispatcher.BeginInvoke で
                    // 非同期化済み）→ ShowProfileSwitchNotification の単一経路のみが担う。
                    // 仕様: 独自ウィンドウ通知チェックがONなら独自ウィンドウのみ、OFFかつ通知レベルが
                    // 「すべて」ならトーストのみを表示する排他分岐であり、ここで別途 LogToTray を
                    // 呼び足すと同一切替に対して二重表示になるため、以前あった追加呼び出しは撤去した。
                    device.HaltReportingRunAction(() =>
                    {
                        // TODO(Phase7): Global.ApplyProfile は ProfileApplicationService の実装本体（Mapping の instance 化とあわせて本サービスへ移す）。
                        Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
                            ProfileChangeSource.MappingAction, prolog);
                    });
                }
                else
                {
                    // TODO(Phase7): Global.ApplyProfile は ProfileApplicationService の実装本体（Mapping の instance 化とあわせて本サービスへ移す）。
                    Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
                        ProfileChangeSource.MappingAction, prolog);
                }
            });
        }

        public bool RestoreFromAction(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit)
                return false;

            string previousProfile = _profileRepo.OlderProfilePath?[deviceIndex];
            if (string.IsNullOrWhiteSpace(previousProfile))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.RestoreFromAction: No OlderProfilePath for device {deviceIndex}");
                return false;
            }

            return ApplyProfile(deviceIndex, previousProfile, false, false, ProfileChangeSource.MappingAction);
        }

        public void ClearPendingRestore(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit)
                return;

            if (_profileRepo.OlderProfilePath != null && deviceIndex < _profileRepo.OlderProfilePath.Length)
            {
                _profileRepo.OlderProfilePath[deviceIndex] = string.Empty;
            }
        }

        public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false,
                    bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: profileName is null or whitespace for slot {deviceIndex}");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit)
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: deviceIndex {deviceIndex} is out of bounds");
                return false;
            }

            bool success = false;
            try
            {
                DS4Device device = _control?.DS4Controllers?[deviceIndex];

                Action applyAction = () =>
                {
                    // TODO(Phase7): Global.ApplyProfile は ProfileApplicationService の実装本体（Mapping の instance 化とあわせて本サービスへ移す）。
                    success = Global.ApplyProfile(deviceIndex, profileName, isTemp, launchProgram,
                        _control, source, prolog);
                };

                if (device != null)
                {
                    if (source == ProfileChangeSource.MappingAction)
                    {
                        applyAction();
                    }
                    else
                    {
                        device.HaltReportingRunAction(applyAction);
                    }
                }
                else
                {
                    applyAction();
                }

                AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyProfile: Slot {deviceIndex}, Profile '{profileName}', isTemp={isTemp}, success={success}");
                if (!success)
                {
                    AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile Result: FAILED for Slot {deviceIndex}, Profile '{profileName}', isTemp={isTemp}, source={source}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[DI] ProfileApplicationService.ApplyProfile failed: {ex}");
                success = false;
            }

            return success;
        }
    }
}