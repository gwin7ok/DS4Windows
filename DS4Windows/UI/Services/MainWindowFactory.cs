using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="IMainWindowFactory"/> の実装。メイン画面の生成と、画面が必要とするサービスの受け渡しを集約する（Phase7-Step5 W5）。
    /// <see cref="IAutoProfileService"/> は、生成のたびに Func で遅延解決する
    /// （自動プロファイルの設定コレクションは、設定の場所が決まった後でなければ生成できないため。ViewModelFactory と同じ形）。
    /// Phase13-Step6 6-c: <see cref="ControlService"/> は Func をやめ、コンストラクターで直接受ける（ホスト構築後に解決されるため K7-1 の懸念はない）。
    /// </summary>
    public class MainWindowFactory : IMainWindowFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IEditorWindowFactory _editorWindowFactory;
        private readonly IPathService _pathService;
        private readonly IEnvironmentService _environmentService;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IProfileApplicationService _profileApplication;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IProfileRepository _profileRepository;
        private readonly INotificationService _notificationService;
        private readonly IProfileSlotApplier _profileSlotApplier;
        private readonly MainWindowsViewModel _mainWindowsViewModel;
        private readonly ILogSettingsApplier _logSettingsApplier;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IProfileIdentityService _profileIdentityService;
        private readonly ControlService _controlService;
        private readonly IIpcCommandProcessor _ipcProcessor;   // Phase13-Step11 11-c: IPC コマンドの実行
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;   // Phase13-Step10 10-c: OSC／UDP／HidHide／タッチパッドの切り替えの窓口
        private readonly IServiceLifecycle _lifecycle;   // Phase13-Step9: MainWindow が running／suspending を直接読み書きする
        private readonly IProfileLoadTarget _loadTarget;   // Phase13-Step6 6-e: IPC の LoadTempProfile に渡す窓口
        private readonly IAutoProfileService _autoProfileService;   // Phase13-Step8 8-c: 旧 遅延取得デリゲート。画面の生成時（設定の場所の確定後）に解決される

        public MainWindowFactory(
            IProfileSettingsService profileSettings,
            IAppSettingsService appSettings,
            IProfileXmlStore profileXmlStore,
            IEditorWindowFactory editorWindowFactory,
            IPathService pathService,
            IEnvironmentService environmentService,
            IAppearanceSettingsService appearanceSettings,
            IProfileApplicationService profileApplication,
            IOutputSlotService outputSlotService,
            IProfileRepository profileRepository,
            INotificationService notificationService,
            IProfileSlotApplier profileSlotApplier,
            MainWindowsViewModel mainWindowsViewModel,
            ILogSettingsApplier logSettingsApplier,
            IViewModelFactory viewModelFactory,
            IProfileIdentityService profileIdentityService,
            ControlService controlService,
            IServiceLifecycle lifecycle,
            IRuntimeFeatureSwitch runtimeFeatures,
            IIpcCommandProcessor ipcProcessor,
            IProfileLoadTarget loadTarget,
            IAutoProfileService autoProfileService)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _profileApplication = profileApplication ?? throw new ArgumentNullException(nameof(profileApplication));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _mainWindowsViewModel = mainWindowsViewModel ?? throw new ArgumentNullException(nameof(mainWindowsViewModel));
            _logSettingsApplier = logSettingsApplier ?? throw new ArgumentNullException(nameof(logSettingsApplier));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _profileIdentityService = profileIdentityService ?? throw new ArgumentNullException(nameof(profileIdentityService));
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            _ipcProcessor = ipcProcessor ?? throw new ArgumentNullException(nameof(ipcProcessor));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
        }

        public MainWindow Create(ArgumentParser parser)
            => new MainWindow(parser, _profileSettings, _appSettings, _profileXmlStore, _editorWindowFactory,
                _pathService, _environmentService, _appearanceSettings, _profileApplication, _outputSlotService,
                _profileRepository, _notificationService, _controlService, _lifecycle, _runtimeFeatures, _ipcProcessor, _loadTarget, _profileSlotApplier,
                _mainWindowsViewModel, _logSettingsApplier, _autoProfileService, _viewModelFactory, _profileIdentityService);
    }
}
