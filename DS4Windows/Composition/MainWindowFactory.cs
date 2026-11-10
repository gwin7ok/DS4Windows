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
    /// </summary>
    public class MainWindowFactory : IMainWindowFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IEditorWindowFactory _editorWindowFactory;
        private readonly IPathService _pathService;
        private readonly IEnvironmentService _environmentService;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly INotificationService _notificationService;
        private readonly IProfileSlotApplier _profileSlotApplier;
        private readonly MainWindowsViewModel _mainWindowsViewModel;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IProfileIdentityService _profileIdentityService;
        private readonly IIpcCommandProcessor _ipcProcessor;   // Phase13-Step11 11-c: IPC コマンドの実行
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;   // Phase13-Step10 10-c: OSC／UDP／HidHide／タッチパッドの切り替えの窓口
        private readonly IServiceLifecycle _lifecycle;   // Phase13-Step9: MainWindow が running／suspending を直接読み書きする
        private readonly IUiThemeAndLanguage _themeLanguage;
        private readonly IChangelogService _changelogService;   // Phase14b-Step17（D10）: 更新の確認・変更履歴   // Phase14b-Step16: CLI の問い合わせの結果の書き込み   // Phase14b-Step16（D11）: テーマ・言語の切り替え   // Phase14b-Step15（D12）: 設定画面のスタートアップ登録
        private readonly IControllerSlotQuery _slots;
        private readonly IAutoProfileService _autoProfileService;   // Phase13-Step8 8-c: 旧 遅延取得デリゲート。画面の生成時（設定の場所の確定後）に解決される

        public MainWindowFactory(
            IAppSettingsService appSettings,
            IEditorWindowFactory editorWindowFactory,
            IPathService pathService,
            IEnvironmentService environmentService,
            IAppearanceSettingsService appearanceSettings,
            IProfileRepository profileRepository,
            INotificationService notificationService,
            IProfileSlotApplier profileSlotApplier,
            MainWindowsViewModel mainWindowsViewModel,
            IViewModelFactory viewModelFactory,
            IProfileIdentityService profileIdentityService,
            IServiceLifecycle lifecycle,
            IRuntimeFeatureSwitch runtimeFeatures,
            IIpcCommandProcessor ipcProcessor,
            IAutoProfileService autoProfileService,
            IUiThemeAndLanguage themeLanguage,
            IChangelogService changelogService,
            IControllerSlotQuery slots)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _changelogService = changelogService ?? throw new ArgumentNullException(nameof(changelogService));
            _themeLanguage = themeLanguage ?? throw new ArgumentNullException(nameof(themeLanguage));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _mainWindowsViewModel = mainWindowsViewModel ?? throw new ArgumentNullException(nameof(mainWindowsViewModel));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _profileIdentityService = profileIdentityService ?? throw new ArgumentNullException(nameof(profileIdentityService));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            _ipcProcessor = ipcProcessor ?? throw new ArgumentNullException(nameof(ipcProcessor));
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
        }

        public MainWindow Create(ArgumentParser parser)
        {
            var window = new MainWindow(parser, _appSettings, _editorWindowFactory,
                _pathService, _environmentService, _appearanceSettings,
                _profileRepository, _notificationService, _lifecycle, _runtimeFeatures, _ipcProcessor, _profileSlotApplier,
                _mainWindowsViewModel, _autoProfileService, _viewModelFactory, _themeLanguage, _changelogService, _slots);
            // 複製の名前入力欄が使うサービスは、窓の引数を増やさないよう工場から渡す
            window.dupBox.Initialize(_pathService, _profileIdentityService);
            return window;
        }
    }
}
