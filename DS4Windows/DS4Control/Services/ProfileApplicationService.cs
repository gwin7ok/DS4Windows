using System;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileApplicationService : IProfileApplicationService
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileLoadTarget _control;   // Phase13-Step6 6-e: 旧 Func<ControlService>（遅延解決）。プロファイル読込の鎖が使う窓口を直接受ける
        private readonly IProfileRepository _profileRepo;
        // Phase10-Step4d W5c: Load Profile の切り替え先を、実行の直前に ID から現在の名前に引く。省略可（なければ、従来どおり details の名前）
        private readonly DS4Windows.DI.IProfileIdentityService _profileIdentity;
        // Phase6-Step10-4-2f（K1 是正）: スロット上限は、固定の 4 ではなく、環境（Windows 8 以上は 8）から取得する
        private readonly IEnvironmentService _environmentService;
        // Phase7-Step3c W3: 適用の核（旧 Global.ApplyProfile）に必要な依存
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IProfileActionProvider _profileActionProvider;
        private readonly BackingStore _config;
        // Phase7-Step8 W2（Q3＝A）: 旧 Mapping.ClearKeyButtonControllersForDevice の静的呼び出し
        private readonly IKeyButtonControllerRegistry _keyButtonControllers;
        // Phase8-Step5 W2（Q3＝A）: 束縛の登録簿と、ビルダーの遅延解決（ビルダー → IProfileSwitcher → 本サービス、の循環を避ける）
        private readonly DS4Windows.Actions.IActionRegistry _actionRegistry;
        // Phase13-Step8: 遅延取得のまま残す（意図的）。ActionBindingBuilder → IProfileSwitcher → IProfileApplicationService と循環する。
        // 再検討条件: 束縛の構築を ProfileApplicationService の外へ出したとき。許可リスト: Phase13FuncRatchetGuardTests
        private readonly Func<DS4Windows.Actions.IActionBindingBuilder> _bindingBuilderProvider;
        // Phase13-Step4: 旧 静的 ActionManager（ClearAllEntries／ClearDeviceState／PreallocateForProfileApply）の代わり
        private readonly DS4Windows.Actions.IManagedActionManager _actionManager;

        public ProfileApplicationService(
            IProfileSettingsService profileSettings,
            IProfileLoadTarget loadTarget,
            IProfileRepository profileRepo,
            IEnvironmentService environmentService,
            IProfileXmlStore profileXmlStore,
            IProfileActionProvider profileActionProvider,
            BackingStore config,
            IKeyButtonControllerRegistry keyButtonControllers,
            DS4Windows.Actions.IActionRegistry actionRegistry,
            Func<DS4Windows.Actions.IActionBindingBuilder> bindingBuilderProvider,
            DS4Windows.Actions.IManagedActionManager actionManager,
            DS4Windows.DI.IProfileIdentityService profileIdentity = null)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            _profileIdentity = profileIdentity;
            _actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
            _bindingBuilderProvider = bindingBuilderProvider ?? throw new ArgumentNullException(nameof(bindingBuilderProvider));
            _keyButtonControllers = keyButtonControllers ?? throw new ArgumentNullException(nameof(keyButtonControllers));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _profileActionProvider = profileActionProvider ?? throw new ArgumentNullException(nameof(profileActionProvider));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            // Phase7-Step10b W3: 未使用だった依存 IDeviceStateService（フィールドを保持するだけ。234e20f9 で DI コンテナ化の途中に追加）を外した。
            _control = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        /// <summary>
        /// 旧 静的 <c>ActionManager.PreallocateForProfileApply</c>（Phase13-Step4 で本サービスへ移した）。適用するプロファイルの特殊アクションについて、
        /// 状態とコントローラーを先に作る。ログは従来のまま。
        /// </summary>
        private void PreallocateForProfileApply(int device)
        {
            try
            {
                var profileActions = _profileActionProvider.GetProfileActionNames(device);
                if (profileActions == null) return;

                foreach (var actionName in profileActions)
                {
                    try
                    {
                        var sa = _profileActionProvider.GetProfileAction(device, actionName);
                        if (sa == null) continue;

                        try { _actionManager.GetStateFor(sa, device); } catch { }
                        try { _keyButtonControllers.GetOrCreateForAction(device, sa); } catch { }
                    }
                    catch { }
                }

                try { AppLogger.LogTrace($"ActionManager.PreallocateForProfileApply: preallocated actions/controllers for device {device}"); } catch { }
            }
            catch { }
        }

        public void ApplyFromAction(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit || action == null)
                return;

            if (!HasLoadableTarget(action))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyFromAction: target profile of '{action.name}' is empty or deleted; nothing to do (slot {deviceIndex})");
                return;
            }

            DS4Device device = _control.GetController(deviceIndex);

            // Phase10-Step4d W5c: 切り替え先の名前は、ID があれば ID から引く（プロファイルの名前を変えたあとも、SA を作り直さずに、現在の名前で切り替える）
            string target = ResolveTarget(action);

            string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile,
                (deviceIndex + 1).ToString(), target, device != null ? $"{device.Battery}" : "N/A");

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
                        ApplyCore(deviceIndex, target, action.IsTemporaryProfileAction, true, _control,
                            ProfileChangeSource.MappingAction, prolog);
                    });
                }
                else
                {
                    ApplyCore(deviceIndex, target, action.IsTemporaryProfileAction, true, _control,
                        ProfileChangeSource.MappingAction, prolog);
                }
            });
        }

        // Phase11 W6 K-W6-7: 切り替え先が空、または ID が引けず（プロファイルが削除された）名前でも見つからないときは、読み込めない。
        // ID のない古い SA は、判定できないため従来どおり（読み込める扱い）
        public bool HasLoadableTarget(SpecialAction action)
        {
            if (action == null) return false;

            string target = ResolveTarget(action);
            if (string.IsNullOrWhiteSpace(target)) return false;

            if (_profileIdentity != null && !string.IsNullOrEmpty(action.profileId)
                && _profileIdentity.GetName(action.profileId) == null && _profileIdentity.GetId(target) == null)
                return false;

            return true;
        }

        // Phase10-Step4d W5c: Load Profile の切り替え先。SA の切り替え先の ID（profileId）が引ければ、現在の名前を返す（details も同じ名前に直す）。
        // 引けない・ID がないときは、details の名前（従来どおり）
        public string ResolveTarget(SpecialAction action)
        {
            if (_profileIdentity != null && !string.IsNullOrEmpty(action.profileId))
            {
                string current = _profileIdentity.GetName(action.profileId);
                if (current != null)
                {
                    if (!string.Equals(action.details, current, StringComparison.Ordinal)) action.details = current;
                    return current;
                }
            }

            return action.details;
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
                DS4Device device = _control.GetController(deviceIndex);

                Action applyAction = () =>
                {
                    success = ApplyCore(deviceIndex, profileName, isTemp, launchProgram,
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

        // ---- Phase7-Step3c W3: 旧 Global.ApplyProfile／LoadProfile／CompleteProfileApplication の処理 ----
        // 手順・ログ・順序は旧 Global と同一。W4 で、呼び出し元（ProfileSlotApplier／Mapping／本サービスの既存の入口）をここへ切り替え、旧 Global の該当メンバーを削除した。
        // 静的な ActionManager／Mapping の呼び出しは、それぞれ Phase8／Phase7-Step7〜9 で解消するまで残す。

        private bool ApplyCore(int device, string profileName, bool isTemp, bool launchProgram,
            IProfileLoadTarget control, ProfileChangeSource source, string prolog)
        {
            // Debug: ApplyProfile呼び出しログ
            string stackTrace = new System.Diagnostics.StackTrace(1, true).ToString();
            AppLogger.LogDebug($"ApplyProfile CALLED: device={device}, profile={profileName}, isTemp={isTemp}, source={source}");
            AppLogger.LogTrace($"ApplyProfile CallStack:\n{stackTrace}");

            bool result;

            // プロファイル読み込み
            if (isTemp)
            {
                AppLogger.LogDebug($"ApplyProfile: Loading TEMP profile '{profileName}' for device {device}");
                result = _profileRepo.LoadTempProfile(device, profileName, launchProgram, control);
            }
            else
            {
                AppLogger.LogDebug($"ApplyProfile: Loading NORMAL profile '{profileName}' for device {device}");
                // 通常プロファイルの場合、ProfilePathを更新してからLoadProfile
                _profileRepo.ProfilePath[device] = profileName;
                result = LoadNormalProfile(device, launchProgram, control);
            }

            if (result)
                CompleteProfileApplication(device, profileName, isTemp, control, source, prolog);
            else
            {
                AppLogger.LogWarn($"ApplyProfile: Profile load FAILED for '{profileName}'");
            }

            AppLogger.LogDebug($"ApplyProfile COMPLETED: device={device}, profile={profileName}, result={result}");
            return result;
        }

        // 旧 Global.LoadProfile（ProfilePath[device] のプロファイルを読み込み、一時プロファイルの状態を戻す）
        private bool LoadNormalProfile(int device, bool launchProgram, IProfileLoadTarget control)
        {
            _config.loggedInvalidActions.Clear();
            bool result = _profileXmlStore.LoadProfileXml(device, launchProgram, control, "", true, true);
            _profileSettings.SetTempProfileName(device, string.Empty);
            _profileSettings.SetUseTempProfile(device, false);
            _profileSettings.SetTempProfileDistance(device, false);

            return result;
        }

        private void CompleteProfileApplication(int device, string profileName, bool isTemp,
            IProfileLoadTarget control, ProfileChangeSource source, string prolog)
        {
            AppLogger.LogDebug($"ApplyProfile: Profile loaded successfully. Updating state...");
            _profileRepo.SelectedProfile[device] = profileName;

            if (!isTemp)
            {
                _profileRepo.OlderProfilePath[device] = profileName;
                AppLogger.LogDebug($"ApplyProfile: OlderProfilePath updated to '{profileName}'");
            }
            else
            {
                AppLogger.LogDebug($"ApplyProfile: OlderProfilePath NOT updated (isTemp=true). Current value: '{_profileRepo.OlderProfilePath[device]}'");
            }

            if (prolog == null)
            {
                DS4Device ds4Device = control.GetController(device);
                string battery = ds4Device != null ? $"{ds4Device.Battery}" : "N/A";
                prolog = $"Controller {device + 1} is now using Profile \"{profileName}\" (Battery: {battery}%)";
            }

            AppLogger.LogToGui(prolog, false);
            AppLogger.LogDebug($"ApplyProfile: Calling LogProfileChanged...");
            try
            {
                AppLogger.LogProfileChanged(device, profileName, isTemp, source, prolog, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[Profile] Failed to log profile change: {ex.Message}");
            }

            AppLogger.LogDebug($"ApplyProfile: Raising SelectedProfileChanged event");
            _profileRepo.RaiseSelectedProfileChanged(device, profileName);

            try
            {
                try { _actionManager.ClearAllEntries(); } catch { }
                AppLogger.LogDebug($"ApplyProfile: Cleared ActionManager global entries to force re-creation of Action instances for new profile");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"ApplyProfile: Failed to clear ActionManager entries: {ex}");
            }

            try
            {
                _keyButtonControllers.ClearForDevice(device);
                try { _actionManager.ClearDeviceState(device); } catch { }
                AppLogger.LogDebug($"ApplyProfile: Cleared per-device SpecialAction controllers and ActionManager state for device {device}");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"ApplyProfile: Failed to clear per-device controllers: {ex}");
            }

            try { PreallocateForProfileApply(device); } catch { }

            try
            {
                bool foundToggle = false;
                bool foundPress = false;
                var profileActionNames = _profileRepo.ProfileActions[device];
                if (profileActionNames != null)
                {
                    foreach (var actionName in profileActionNames)
                    {
                        try
                        {
                            var sa = _profileActionProvider.GetProfileAction(device, actionName);
                            if (sa == null) continue;
                            if (sa.typeID == SpecialAction.ActionTypeId.Key || sa.typeID == SpecialAction.ActionTypeId.Button)
                            {
                                if (sa.KeyButtonSwitchMode.HasValue)
                                {
                                    if (sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle) foundToggle = true; else foundPress = true;
                                }
                                else
                                {
                                    bool isToggle = false;
                                    try { isToggle = sa.keyType.HasFlag(DS4KeyType.Toggle); } catch { }
                                    if (isToggle) foundToggle = true; else foundPress = true;
                                }
                            }
                        }
                        catch { }
                        if (foundToggle && foundPress) break;
                    }
                }

                AppLogger.LogDebug("ApplyProfile: Controller activation handled per-SpecialAction SwitchMode");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"ApplyProfile: Controller activation check failed: {ex}");
            }

            // Phase8-Step5 W2: 後始末（掃除）の最後に、このプロファイルの束縛を組み立てて登録簿へ差し替える。
            // 掃除の順序（ClearAllEntries → ClearForDevice → ClearDeviceState → PreallocateForProfileApply。Phase8Step1ApplyProfileCleanupOrderGuardTests）は変えない
            PublishBindings(device);
        }

        // Phase8-Step8 8d: 移行のスイッチは削除した。ここで差し替える束縛は、プロファイルの全種別の束縛。OnProfileApplied（適用時の抑制の集合の消去）の接続は、Step7.1 で判断する（Step5 計画書 Q4）。
        public void PublishBindings(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit)
                return;

            try
            {
                _actionRegistry.Replace(deviceIndex, _bindingBuilderProvider().Build(deviceIndex));
                AppLogger.LogDebug($"[DI] ProfileApplicationService.PublishBindings: replaced the bindings of device {deviceIndex}");
            }
            catch (Exception ex)
            {
                // Q5＝A: 組み立ての失敗で、プロファイルの適用を失敗にしない。そのデバイスの束縛は空にする
                AppLogger.LogError($"[DI] ProfileApplicationService.PublishBindings FAILED for device {deviceIndex}: {ex.Message}");
                try { _actionRegistry.Clear(deviceIndex); } catch { }
            }
        }

        /// <summary>
        /// 入力の読み取りを一時停止（Halt）せずに適用する。接続処理中など、呼び出し元がすでに停止している経路で使う
        /// （旧 <c>Global.ApplyProfileToSlot</c> と同じく Halt しない。例外は握らず、そのまま呼び出し元へ伝える）。
        /// </summary>
        public bool ApplyProfileWithoutHalt(int deviceIndex, string profileName, bool isTemp = false,
                    bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfileWithoutHalt FAILED: profileName is null or whitespace for slot {deviceIndex}");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= _environmentService.ControllerSlotLimit)
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfileWithoutHalt FAILED: deviceIndex {deviceIndex} is out of bounds");
                return false;
            }

            return ApplyCore(deviceIndex, profileName, isTemp, launchProgram, _control, source, prolog);
        }

    }
}