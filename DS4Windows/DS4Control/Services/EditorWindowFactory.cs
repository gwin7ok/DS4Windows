using System;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IEditorWindowFactory"/> の実装。補助ウィンドウの生成を集約する。
    /// Phase6-Step10-2-4a: 生成箇所を窓口へ集約した（ウィンドウ側のコンストラクタは、窓口を必須引数で受け取るだけの変更）。
    /// Phase6-Step10-2-4b: 各ウィンドウが必要とするサービスを、本クラスの必須引数として保持して渡す。
    /// ControlService は、生成のたびに Func で遅延解決する（ViewModelFactory と同じ形。決定 Step9-4 C1）。
    /// </summary>
    public class EditorWindowFactory : IEditorWindowFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IPathService _pathService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ISpecialActionRepository _specialActionRepository;
        private readonly IProfilePresetLoader _presetLoader;
        private readonly Func<ControlService> _controlServiceAccessor;
        // Phase7-Step5 W5: CreateProfileEditor が ProfileEditor へ渡すサービス
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly IProfileSwitcher _profileSwitcher;
        // Phase7-Step10e W2: 画面・ViewModel が上限を読むための環境サービス
        private readonly IEnvironmentService _environmentService;

        public EditorWindowFactory(
            IProfileSettingsService profileSettings,
            IOutputSlotService outputSlotService,
            IPathService pathService,
            IViewModelFactory viewModelFactory,
            ISpecialActionRepository specialActionRepository,
            IProfilePresetLoader presetLoader,
            Func<ControlService> controlServiceAccessor,
            IAppSettingsService appSettings,
            IProfileRepository profileRepository,
            IProfileSwitcher profileSwitcher,
            IEnvironmentService environmentService)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            _presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            _controlServiceAccessor = controlServiceAccessor ?? throw new ArgumentNullException(nameof(controlServiceAccessor));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose)
            => new BindingWindow(deviceNum, settings, targetDevice, this, _profileSettings, _outputSlotService,
                _environmentService, _controlServiceAccessor(), expose);

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, int targetDevice,
            bool showscan = true, bool repeatable = true, bool showPlaybackOptions = true)
            => new RecordBox(deviceNum, controlSettings, targetDevice, showscan, repeatable, showPlaybackOptions,
                _controlServiceAccessor(), _pathService, _viewModelFactory);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true, bool showPlaybackOptions = true)
            => new RecordBoxWindow(deviceNum, settings, targetDevice, this, repeatable, showPlaybackOptions);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow(_presetLoader, _controlServiceAccessor());

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel, _controlServiceAccessor());

        public AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null)
            => new AssignmentEditor(deviceNum, profileList, targetDevice, this, _viewModelFactory,
                _specialActionRepository, _profileSettings, _outputSlotService, _environmentService, specialAction);

        // Phase10-Step3 W4: ProfileEditor.ShowControlAssignmentEditor が呼ぶ
        public AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings, ProfileList profileList, int targetDevice)
            => new AssignmentEditor(deviceNum, controlSettings, profileList, targetDevice, this, _viewModelFactory,
                _specialActionRepository, _profileSettings, _outputSlotService, _environmentService);

        public ProfileEditor CreateProfileEditor(int targetDevice)
            => new ProfileEditor(targetDevice, _appSettings, _profileSettings, this, _profileRepository,
                _profileSwitcher, _controlServiceAccessor(), _viewModelFactory, _environmentService);
    }
}
