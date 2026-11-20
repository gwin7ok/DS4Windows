#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>自動プロファイル画面の ViewModel を作る。<see cref="ShellViewModelFactory"/> が、生成をここへ委譲する。</summary>
    public sealed class AutoProfilesViewModelFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IAutoProfileService _autoProfileService;
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;
        private readonly IEnvironmentService _environmentService;
        private readonly DS4Windows.UI.Abstractions.IUiDispatcher _uiDispatcher;
        private readonly DS4Windows.UI.Abstractions.IDialogService _dialogs;

        public AutoProfilesViewModelFactory(IAppSettingsService appSettings, IAutoProfileService autoProfileService,
            IRuntimeFeatureSwitch runtimeFeatures, IEnvironmentService environmentService,
            DS4Windows.UI.Abstractions.IUiDispatcher uiDispatcher, DS4Windows.UI.Abstractions.IDialogService dialogs)
        {
            ArgumentNullException.ThrowIfNull(dialogs);
            _dialogs = dialogs;
            ArgumentNullException.ThrowIfNull(uiDispatcher);
            _uiDispatcher = uiDispatcher;
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(autoProfileService);
            _autoProfileService = autoProfileService;
            ArgumentNullException.ThrowIfNull(runtimeFeatures);
            _runtimeFeatures = runtimeFeatures;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
        }

        public AutoProfilesViewModel Create(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created AutoProfilesViewModel");
            return new AutoProfilesViewModel(autoProfileHolder, profileList,
                _appSettings, _autoProfileService, _runtimeFeatures, _environmentService, _uiDispatcher, _dialogs);
        }
    }
}
