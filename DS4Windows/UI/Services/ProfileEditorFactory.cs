using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// プロファイル編集画面（メイン画面に埋め込む <see cref="ProfileEditor"/>）の生成。<see cref="EditorWindowFactory"/> が窓口として委譲する。
    /// 窓が別の窓を開くための窓口（<c>editors</c>）は、窓口自身を引数で受ける。
    /// </summary>
    public sealed class ProfileEditorFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly IProfileSwitcher _profileSwitcher;
        private readonly IControllerReadingsSource _readings;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IEnvironmentService _environmentService;
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;   // 振動の停止・入力の取得の窓口
        private readonly IControllerSlotCommands _slotCommands;

        public ProfileEditorFactory(IAppSettingsService appSettings, IProfileSettingsService profileSettings,
            IProfileRepository profileRepository, IProfileSwitcher profileSwitcher, IControllerReadingsSource readings,
            IViewModelFactory viewModelFactory, IEnvironmentService environmentService,
            IRuntimeFeatureSwitch runtimeFeatures, IControllerSlotCommands slotCommands)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
            _readings = readings ?? throw new ArgumentNullException(nameof(readings));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            _slotCommands = slotCommands ?? throw new ArgumentNullException(nameof(slotCommands));
        }

        public ProfileEditor CreateProfileEditor(int targetDevice, IEditorWindowFactory editors)
            => new ProfileEditor(targetDevice, _appSettings, _profileSettings, editors, _profileRepository,
                _profileSwitcher, _readings, _viewModelFactory, _environmentService, _runtimeFeatures, _slotCommands);
    }
}
