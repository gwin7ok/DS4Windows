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
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _slotCommands = slotCommands ?? throw new ArgumentNullException(nameof(slotCommands));
            _actionViewModelFactory = actionViewModelFactory ?? throw new ArgumentNullException(nameof(actionViewModelFactory));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            _readings = readings ?? throw new ArgumentNullException(nameof(readings));
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose, IEditorWindowFactory editors)
            => new BindingWindow(deviceNum, settings, targetDevice, editors, _profileSettings, _slotCommands,
                _viewModelFactory.CreateBindingWindowViewModel(deviceNum, settings, targetDevice), expose);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow(_viewModelFactory.CreatePresetOptionViewModel());

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel, _readings);

        public AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction, IEditorWindowFactory editors)
            => new AssignmentEditor(deviceNum, profileList, targetDevice, editors, _viewModelFactory, _actionViewModelFactory,
                _specialActionRepository, _profileSettings, _environmentService, _windowSystem, specialAction);

        public AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings,
            ProfileList profileList, int targetDevice, IEditorWindowFactory editors)
            => new AssignmentEditor(deviceNum, controlSettings, profileList, targetDevice, editors, _viewModelFactory, _actionViewModelFactory,
                _specialActionRepository, _profileSettings, _environmentService, _windowSystem);
    }
}
