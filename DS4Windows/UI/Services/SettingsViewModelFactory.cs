#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.UI.ViewModels;

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
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(appearanceSettings);
            _appearanceSettings = appearanceSettings;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;
            ArgumentNullException.ThrowIfNull(logSettingsApplier);
            _logSettingsApplier = logSettingsApplier;
            ArgumentNullException.ThrowIfNull(startupEntries);
            _startupEntries = startupEntries;
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
