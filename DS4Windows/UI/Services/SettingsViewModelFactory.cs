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
    /// <summary>設定タブの ViewModel を作る。<see cref="ShellViewModelFactory"/> が、生成をここへ委譲する。</summary>
    public sealed class SettingsViewModelFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly ILogSettingsApplier _logSettingsApplier;
        private readonly IStartupEntryService _startupEntries;

        public SettingsViewModelFactory(IAppSettingsService appSettings, IAppearanceSettingsService appearanceSettings,
            IEnvironmentService environmentService, IPathService pathService, ILogSettingsApplier logSettingsApplier,
            IStartupEntryService startupEntries)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _logSettingsApplier = logSettingsApplier ?? throw new ArgumentNullException(nameof(logSettingsApplier));
            _startupEntries = startupEntries ?? throw new ArgumentNullException(nameof(startupEntries));
        }

        public SettingsViewModel Create()
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created SettingsViewModel");
            return new SettingsViewModel(_appSettings, _appearanceSettings, _environmentService, _pathService,
                _logSettingsApplier, _startupEntries);
        }
    }
}
