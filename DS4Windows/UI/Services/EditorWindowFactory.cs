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
    /// <see cref="IEditorWindowFactory"/> の実装。補助ウィンドウの生成を集約する。
    /// Phase6-Step10-2-4a: 生成箇所を窓口へ集約した（ウィンドウ側のコンストラクタは、窓口を必須引数で受け取るだけの変更）。
    /// Phase6-Step10-2-4b: 各ウィンドウが必要とするサービスを、本クラスの必須引数として保持して渡す。
    /// Phase13-Step6 6-c: ControlService は、生成のたびの Func による遅延解決をやめ、コンストラクターで直接受ける（このクラスは ControlService の依存グラフの外のため循環しない）。
    /// </summary>
    public class EditorWindowFactory : IEditorWindowFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IPathService _pathService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ISpecialActionRepository _specialActionRepository;
        private readonly IProfilePresetLoader _presetLoader;
        private readonly ControlService _controlService;
        private readonly IProfileLoadTarget _loadTarget;   // Phase13-Step6 6-e: プリセットの読込先の窓口
        // Phase7-Step5 W5: CreateProfileEditor が ProfileEditor へ渡すサービス
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly IProfileSwitcher _profileSwitcher;
        // Phase7-Step10e W2: 画面・ViewModel が上限を読むための環境サービス
        private readonly IEnvironmentService _environmentService;
        // Phase12-Step2 2i: FocusWindow の編集画面のターゲットマークが、ドロップしたウィンドウを取得するための窓口
        private readonly IWindowSystem _windowSystem;
        // Phase12-Step3 3e: マクロの記録で、画面全体のマウス入力（移動・ボタン）を記録するための監視
        private readonly IMouseInputHook _mouseHook;
        // Phase13-Step10 10-d: ProfileEditor の振動の停止・入力の取得の窓口（旧 ControlService.setRumble／GetActiveInputControl）
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;

        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public EditorWindowFactory(DS4Windows.Core.Model.LightbarOverrides lightbar, 
            IProfileSettingsService profileSettings,
            IOutputSlotService outputSlotService,
            IPathService pathService,
            IViewModelFactory viewModelFactory,
            ISpecialActionRepository specialActionRepository,
            IProfilePresetLoader presetLoader,
            ControlService controlService,
            IProfileLoadTarget loadTarget,
            IAppSettingsService appSettings,
            IProfileRepository profileRepository,
            IProfileSwitcher profileSwitcher,
            IEnvironmentService environmentService,
            IWindowSystem windowSystem,
            IMouseInputHook mouseHook,
            IRuntimeFeatureSwitch runtimeFeatures)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            _mouseHook = mouseHook ?? throw new ArgumentNullException(nameof(mouseHook));
            _runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            _presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose)
            => new BindingWindow(_lightbar, deviceNum, settings, targetDevice, this, _profileSettings, _outputSlotService,
                _environmentService, _controlService, expose);

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, int targetDevice,
            bool showscan = true, bool repeatable = true, bool showPlaybackOptions = true)
            => new RecordBox(deviceNum, controlSettings, targetDevice, showscan, repeatable, showPlaybackOptions,
                _controlService, _pathService, _viewModelFactory, _windowSystem, _mouseHook);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true, bool showPlaybackOptions = true)
            => new RecordBoxWindow(deviceNum, settings, targetDevice, this, repeatable, showPlaybackOptions);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow(_presetLoader, _loadTarget);

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel, _controlService);

        public AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null)
            => new AssignmentEditor(_lightbar, deviceNum, profileList, targetDevice, this, _viewModelFactory,
                _specialActionRepository, _profileSettings, _outputSlotService, _environmentService, _windowSystem, specialAction);

        // Phase10-Step3 W4: ProfileEditor.ShowControlAssignmentEditor が呼ぶ
        public AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings, ProfileList profileList, int targetDevice)
            => new AssignmentEditor(_lightbar, deviceNum, controlSettings, profileList, targetDevice, this, _viewModelFactory,
                _specialActionRepository, _profileSettings, _outputSlotService, _environmentService, _windowSystem);

        public ProfileEditor CreateProfileEditor(int targetDevice)
            => new ProfileEditor(targetDevice, _appSettings, _profileSettings, this, _profileRepository,
                _profileSwitcher, _controlService, _viewModelFactory, _environmentService, _runtimeFeatures);
    }
}
