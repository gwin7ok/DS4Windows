using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 割り当ての編集に関する窓（出力ピッカー <see cref="BindingWindow"/>、割り当て編集画面 <see cref="AssignmentEditor"/>、プリセット、スティック校正）の生成。
    /// <see cref="EditorWindowFactory"/> が窓口として委譲する。窓が別の窓を開くための窓口（<c>editors</c>）は、窓口自身を引数で受ける。
    /// </summary>
    public sealed class AssignmentWindowFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IControllerSlotCommands _slotCommands;
        private readonly IProfileEditViewModelFactory _viewModelFactory;
        private readonly IActionViewModelFactory _actionViewModelFactory;
        private readonly ISpecialActionRepository _specialActionRepository;
        private readonly IEnvironmentService _environmentService;
        private readonly IWindowSystem _windowSystem;
        private readonly IControllerReadingsSource _readings;

        public AssignmentWindowFactory(IProfileSettingsService profileSettings, IControllerSlotCommands slotCommands,
            IProfileEditViewModelFactory viewModelFactory, IActionViewModelFactory actionViewModelFactory, ISpecialActionRepository specialActionRepository,
            IEnvironmentService environmentService, IWindowSystem windowSystem, IControllerReadingsSource readings)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(slotCommands);
            _slotCommands = slotCommands;
            ArgumentNullException.ThrowIfNull(actionViewModelFactory);
            _actionViewModelFactory = actionViewModelFactory;
            ArgumentNullException.ThrowIfNull(viewModelFactory);
            _viewModelFactory = viewModelFactory;
            ArgumentNullException.ThrowIfNull(specialActionRepository);
            _specialActionRepository = specialActionRepository;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(windowSystem);
            _windowSystem = windowSystem;
            ArgumentNullException.ThrowIfNull(readings);
            _readings = readings;
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose, IEditorWindowFactory editors)
            => new BindingWindow(deviceNum, targetDevice, editors, new ActionRumbleTestController(_slotCommands),
                _viewModelFactory.CreateBindingWindowViewModel(deviceNum, settings, targetDevice), expose);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow(_viewModelFactory.CreatePresetOptionViewModel());

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel, new ControllerReadingsFeedAdapter(_readings));

        public AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction, IEditorWindowFactory editors)
            => new AssignmentEditor(new AssignmentEditorSession(deviceNum, null, specialAction, profileList, targetDevice, editors, _viewModelFactory,
                _actionViewModelFactory, _specialActionRepository, _profileSettings, _environmentService, _windowSystem));

        public AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings,
            ProfileList profileList, int targetDevice, IEditorWindowFactory editors)
            => new AssignmentEditor(new AssignmentEditorSession(deviceNum, controlSettings ?? throw new ArgumentNullException(nameof(controlSettings)),
                null, profileList, targetDevice, editors, _viewModelFactory, _actionViewModelFactory, _specialActionRepository, _profileSettings,
                _environmentService, _windowSystem));
    }
}
