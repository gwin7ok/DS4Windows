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
    /// </summary>
    public class EditorWindowFactory : IEditorWindowFactory
    {
        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose = BindingWindow.ExposeMode.Full)
            => new BindingWindow(deviceNum, settings, targetDevice, this, expose);

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, bool shift, int targetDevice,
            bool showscan = true, bool repeatable = true)
            => new RecordBox(deviceNum, controlSettings, shift, targetDevice, showscan, repeatable);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true)
            => new RecordBoxWindow(deviceNum, settings, targetDevice, this, repeatable);

        public PresetOptionWindow CreatePresetOptionWindow()
            => new PresetOptionWindow();

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => new StickCalibrationWindow(stick, device, profileSettingsViewModel);

        public SpecialActionEditor CreateSpecialActionEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null)
            => new SpecialActionEditor(deviceNum, profileList, targetDevice, this, specialAction);
    }
}
