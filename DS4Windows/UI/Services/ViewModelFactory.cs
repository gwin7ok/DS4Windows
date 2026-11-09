using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Services
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepo;
        private readonly IProfilePresetLoader _presetLoader;
        private readonly ISpecialActionRepository _actionRepo;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IAppSettingsService _appSettings;
        private readonly IAutoProfileService _autoProfileService;
        private readonly IMacroRecordingState _macroRecording;
        private readonly IProfileLoadTarget _loadTarget;   // Phase13-Step6 6-e: ProfileSettingsViewModel のプロファイル読込の窓口
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;   // Phase13-Step10 10-c: AutoProfilesViewModel の HidHide 登録の窓口
        private readonly IControllerSlotQuery _slots;
        private readonly IServiceLifecycle _lifecycle;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly ILogSettingsApplier _logSettingsApplier;
        private readonly IStartupEntryService _startupEntries;
        private readonly IChangelogService _changelogService;

        // Phase6-Step9-4: 依存はすべて必須引数（Pure DI。決定1＝S9-A）。
        // Phase13-Step6 6-c: ControlService は、Func による遅延解決（決定2＝C1。K7-1 対策）をやめ、直接受ける。K7-1 は 2026-10-17 に解消済みで、
        // このクラスはホスト構築後にしか解決されない。
        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public ViewModelFactory(DS4Windows.Core.Model.LightbarOverrides lightbar, 
            IProfileSettingsService profileSettings,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            ISpecialActionRepository actionRepo,
            IOutputSlotService outputSlotService,
            IEnvironmentService environmentService,
            IPathService pathService,
            IAppSettingsService appSettings,
            IAutoProfileService autoProfileService,
            IMacroRecordingState macroRecording,
            IProfileLoadTarget loadTarget,
            IRuntimeFeatureSwitch runtimeFeatures,
            IControllerSlotQuery slots,
            IServiceLifecycle lifecycle,
            IAppearanceSettingsService appearanceSettings,
            ILogSettingsApplier logSettingsApplier,
            IStartupEntryService startupEntries,
            IChangelogService changelogService)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _logSettingsApplier = logSettingsApplier ?? throw new ArgumentNullException(nameof(logSettingsApplier));
            _startupEntries = startupEntries ?? throw new ArgumentNullException(nameof(startupEntries));
            _changelogService = changelogService ?? throw new ArgumentNullException(nameof(changelogService));
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            _actionRepo = actionRepo ?? throw new ArgumentNullException(nameof(actionRepo));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            // Phase13-Step8 8-c: 旧 遅延取得デリゲート（Phase6-Step10-2-3: AutoProfileHolder は設定の場所が決まった後でなければ生成できない）。
            // 本ファクトリーは、メイン画面の生成時（設定の場所の確定後。App.ResolvePostHostService）に解決されるため、直接受ける。
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
            _macroRecording = macroRecording ?? throw new ArgumentNullException(nameof(macroRecording));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
        }

        public ChangelogViewModel CreateChangelogViewModel() => new ChangelogViewModel(_changelogService);

        public RenameProfileViewModel CreateRenameProfileViewModel() => new RenameProfileViewModel(_pathService);

        public PresetOptionViewModel CreatePresetOptionViewModel() => new PresetOptionViewModel(_presetLoader, _loadTarget);

        public FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions)
            => new FirstLauchUtilViewModel(deviceOptions);

        public UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion)
            => new UpdaterWindowViewModel(newVersion, _appSettings, _changelogService);

        public ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions)
            => new ControllerRegDeviceOptsViewModel(deviceOptions, _slots, _appSettings);

        public TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex)
            => new TouchButtonUserControlViewModel(deviceIndex, _profileSettings);

        public LanguagePackViewModel CreateLanguagePackViewModel() => new LanguagePackViewModel(_appSettings);

        public AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo)
            => new AxialStickControlViewModel(deadZoneInfo);

        public MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType)
            => new MappingListViewModel(deviceIndex, contType, _profileSettings);

        public LogViewModel CreateLogViewModel() => new LogViewModel(_environmentService);

        public TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher)
            => new TrayIconViewModel(_slots, _lifecycle, profileListHolder, _appearanceSettings, _profileRepo,
                _environmentService, _pathService, dispatcher);

        public BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice)
            => new BindingWindowViewModel(_lightbar, deviceIndex, settings, _profileSettings, _outputSlotService,
                _environmentService, targetDevice);

        public ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target)
            => new ControlActionEditorViewModel(deviceIndex, target);

        public MacroViewModel CreateMacroViewModel() => new MacroViewModel(_actionRepo);

        public LaunchProgramViewModel CreateLaunchProgramViewModel() => new LaunchProgramViewModel(_actionRepo);

        public LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList)
            => new LoadProfileViewModel(profileList, _actionRepo);

        public PressKeyViewModel CreatePressKeyViewModel(int deviceIndex)
        {
            var viewModel = new PressKeyViewModel(_actionRepo, _profileSettings, _outputSlotService);
            viewModel.SetDeviceNum(deviceIndex);
            return viewModel;
        }

        public SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex)
            => new SpecialActionViewModel(actionIndex, _actionRepo);

        public CheckBatteryViewModel CreateCheckBatteryViewModel()
            => new CheckBatteryViewModel(_lightbar, _actionRepo, _environmentService);

        public MultiActButtonViewModel CreateMultiActButtonViewModel() => new MultiActButtonViewModel(_actionRepo);

        public FocusWindowViewModel CreateFocusWindowViewModel() => new FocusWindowViewModel(_actionRepo);

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device {device}, targetDevice {targetDevice}");
            return new ProfileSettingsViewModel(_lightbar, device, _profileSettings, _loadTarget,
                _outputSlotService, _profileRepo, _presetLoader, _environmentService, _pathService, targetDevice);
        }

        public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool repeatable = false, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created RecordBoxViewModel for Device {device}, targetDevice {targetDevice}");
            return new RecordBoxViewModel(_lightbar, device, controlSettings, repeatable,
                _profileSettings, _environmentService, _macroRecording, targetDevice);
        }

        public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActEditorViewModel for Device {device}");
            return new SpecialActEditorViewModel(device, action, _actionRepo);
        }

        public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActionsListViewModel for Device {device}");
            return new SpecialActionsListViewModel(device, _actionRepo, _profileRepo);
        }

        public SettingsViewModel CreateSettingsViewModel()
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created SettingsViewModel");
            return new SettingsViewModel(_appSettings, _appearanceSettings, _environmentService, _pathService,
                _logSettingsApplier, _startupEntries);
        }

        public ControllerListViewModel CreateControllerListViewModel(ProfileList profileListHolder)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created ControllerListViewModel");
            return new ControllerListViewModel(_slots, _lifecycle, profileListHolder,
                _profileSettings, _profileRepo, _appSettings);
        }

        public CurrentOutDeviceViewModel CreateCurrentOutDeviceViewModel()
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created CurrentOutDeviceViewModel");
            return new CurrentOutDeviceViewModel(_outputSlotService);
        }

        public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created AutoProfilesViewModel");
            return new AutoProfilesViewModel(autoProfileHolder, profileList,
                _appSettings, _autoProfileService, _runtimeFeatures, _environmentService);
        }
    }
}
