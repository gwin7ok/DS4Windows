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
    /// <summary>
    /// メイン画面の枠（タブ、設定、機器一覧、出力スロット、自動プロファイル、トレイ）と、その子ダイアログの ViewModel を作る。
    /// 窓口は <see cref="DS4Windows.UI.Abstractions.IShellViewModelFactory"/>。依存は、ここで作る ViewModel が使うものだけを受ける。
    /// </summary>
    public sealed class ShellViewModelFactory : DS4Windows.UI.Abstractions.IShellViewModelFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepo;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IAppSettingsService _appSettings;
        private readonly IControllerSlotQuery _slots;
        private readonly IServiceLifecycle _lifecycle;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IChangelogService _changelogService;
        private readonly SettingsViewModelFactory _settingsFactory;
        private readonly AutoProfilesViewModelFactory _autoProfilesFactory;

        public ShellViewModelFactory(IProfileSettingsService profileSettings,
            IProfileRepository profileRepo,
            IOutputSlotService outputSlotService,
            IEnvironmentService environmentService,
            IPathService pathService,
            IAppSettingsService appSettings,
            IControllerSlotQuery slots,
            IServiceLifecycle lifecycle,
            IAppearanceSettingsService appearanceSettings,
            IChangelogService changelogService,
            SettingsViewModelFactory settingsFactory,
            AutoProfilesViewModelFactory autoProfilesFactory)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _changelogService = changelogService ?? throw new ArgumentNullException(nameof(changelogService));
            _settingsFactory = settingsFactory ?? throw new ArgumentNullException(nameof(settingsFactory));
            _autoProfilesFactory = autoProfilesFactory ?? throw new ArgumentNullException(nameof(autoProfilesFactory));
        }

        public ChangelogViewModel CreateChangelogViewModel() => new ChangelogViewModel(_changelogService);

        public RenameProfileViewModel CreateRenameProfileViewModel() => new RenameProfileViewModel(_pathService);

        public FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions)
            => new FirstLauchUtilViewModel(deviceOptions);

        public UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion)
            => new UpdaterWindowViewModel(newVersion, _appSettings, _changelogService);

        public ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions)
            => new ControllerRegDeviceOptsViewModel(deviceOptions, _slots, _appSettings);

        public LanguagePackViewModel CreateLanguagePackViewModel() => new LanguagePackViewModel(_appSettings);

        public LogViewModel CreateLogViewModel() => new LogViewModel(_environmentService);

        public TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher)
            => new TrayIconViewModel(_slots, _lifecycle, profileListHolder, _appearanceSettings, _profileRepo,
                _environmentService, _pathService, dispatcher);

        public SettingsViewModel CreateSettingsViewModel() => _settingsFactory.Create();

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
            => _autoProfilesFactory.Create(autoProfileHolder, profileList);
    }
}
