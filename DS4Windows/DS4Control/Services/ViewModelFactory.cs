using System;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4Windows
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
        private readonly ControlService _controlService;
        private readonly IProfileLoadTarget _loadTarget;   // Phase13-Step6 6-e: ProfileSettingsViewModel のプロファイル読込の窓口

        // Phase6-Step9-4: 依存はすべて必須引数（Pure DI。決定1＝S9-A）。
        // Phase13-Step6 6-c: ControlService は、Func による遅延解決（決定2＝C1。K7-1 対策）をやめ、直接受ける。K7-1 は 2026-10-17 に解消済みで、
        // このクラスはホスト構築後にしか解決されない。
        public ViewModelFactory(
            IProfileSettingsService profileSettings,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            ISpecialActionRepository actionRepo,
            IOutputSlotService outputSlotService,
            IEnvironmentService environmentService,
            IPathService pathService,
            IAppSettingsService appSettings,
            IAutoProfileService autoProfileService,
            ControlService controlService,
            IProfileLoadTarget loadTarget)
        {
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
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device {device}, targetDevice {targetDevice}");
            return new ProfileSettingsViewModel(device, _profileSettings, _loadTarget,
                _outputSlotService, _profileRepo, _presetLoader, _environmentService, _pathService, targetDevice);
        }

        public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool repeatable = false, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created RecordBoxViewModel for Device {device}, targetDevice {targetDevice}");
            return new RecordBoxViewModel(device, controlSettings, repeatable,
                _profileSettings, _environmentService, _controlService, targetDevice);
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

        public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created AutoProfilesViewModel");
            return new AutoProfilesViewModel(autoProfileHolder, profileList,
                _appSettings, _autoProfileService, _controlService, _environmentService);
        }
    }
}
