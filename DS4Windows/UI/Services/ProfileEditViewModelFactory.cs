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
    /// プロファイルの編集（割り当て・出力・ボタンの記録・出力ピッカー・プリセット）の ViewModel を作る。
    /// 窓口は <see cref="DS4Windows.UI.Abstractions.IProfileEditViewModelFactory"/>。依存は、ここで作る ViewModel が使うものだけを受ける。
    /// </summary>
    public sealed class ProfileEditViewModelFactory : DS4Windows.UI.Abstractions.IProfileEditViewModelFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepo;
        private readonly IProfilePresetLoader _presetLoader;
        private readonly ISpecialActionRepository _actionRepo;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IMacroRecordingState _macroRecording;
        private readonly IProfileLoadTarget _loadTarget;
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public ProfileEditViewModelFactory(IProfileSettingsService profileSettings,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            ISpecialActionRepository actionRepo,
            IOutputSlotService outputSlotService,
            IEnvironmentService environmentService,
            IPathService pathService,
            IMacroRecordingState macroRecording,
            IProfileLoadTarget loadTarget,
            DS4Windows.Core.Model.LightbarOverrides lightbar)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            _actionRepo = actionRepo ?? throw new ArgumentNullException(nameof(actionRepo));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _macroRecording = macroRecording ?? throw new ArgumentNullException(nameof(macroRecording));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
            _lightbar = lightbar ?? throw new ArgumentNullException(nameof(lightbar));
        }

        public PresetOptionViewModel CreatePresetOptionViewModel() => new PresetOptionViewModel(_presetLoader, _loadTarget);

        public TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex)
            => new TouchButtonUserControlViewModel(deviceIndex, _profileSettings);

        public AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo)
            => new AxialStickControlViewModel(deadZoneInfo);

        public MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType)
            => new MappingListViewModel(deviceIndex, contType, _profileSettings);

        public BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice)
            => new BindingWindowViewModel(_lightbar, deviceIndex, settings, _profileSettings, _outputSlotService,
                _environmentService, targetDevice);

        public ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target)
            => new ControlActionEditorViewModel(deviceIndex, target);

        public PressKeyViewModel CreatePressKeyViewModel(int deviceIndex)
        {
            var viewModel = new PressKeyViewModel(_actionRepo, _profileSettings, _outputSlotService);
            viewModel.SetDeviceNum(deviceIndex);
            return viewModel;
        }

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
    }
}
