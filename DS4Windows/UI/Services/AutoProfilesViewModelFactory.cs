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
    /// <summary>自動プロファイル画面の ViewModel を作る。<see cref="ShellViewModelFactory"/> が、生成をここへ委譲する。</summary>
    public sealed class AutoProfilesViewModelFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IAutoProfileService _autoProfileService;
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;
        private readonly IEnvironmentService _environmentService;

        public AutoProfilesViewModelFactory(IAppSettingsService appSettings, IAutoProfileService autoProfileService,
            IRuntimeFeatureSwitch runtimeFeatures, IEnvironmentService environmentService)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        public AutoProfilesViewModel Create(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created AutoProfilesViewModel");
            return new AutoProfilesViewModel(autoProfileHolder, profileList,
                _appSettings, _autoProfileService, _runtimeFeatures, _environmentService);
        }
    }
}
