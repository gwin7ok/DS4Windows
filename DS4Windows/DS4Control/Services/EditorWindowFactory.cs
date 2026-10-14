using System;
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
        private readonly Func<ControlService> _controlServiceAccessor;

        public EditorWindowFactory(
            IProfileSettingsService profileSettings,
            IOutputSlotService outputSlotService,
            IPathService pathService,
            IViewModelFactory viewModelFactory,
            Func<ControlService> controlServiceAccessor)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _controlServiceAccessor = controlServiceAccessor ?? throw new ArgumentNullException(nameof(controlServiceAccessor));
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose = BindingWindow.ExposeMode.Full)
            => new BindingWindow(deviceNum, settings, targetDevice, this, _profileSettings, _outputSlotService,
                _controlServiceAccessor(), expose);

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, bool shift, int targetDevice,
            bool showscan = true, bool repeatable = true)
            => new RecordBox(deviceNum, controlSettings, shift, targetDevice, showscan, repeatable,
                _controlServiceAccessor(), _pathService, _viewModelFactory);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true)
            => new RecordBoxWindow(deviceNum, settings, targetDevice, this, repeatable);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow();

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel, _controlServiceAccessor());

        public SpecialActionEditor CreateSpecialActionEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null)
            => new SpecialActionEditor(deviceNum, profileList, targetDevice, this, specialAction);
    }
}
