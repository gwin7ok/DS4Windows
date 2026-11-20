using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="IEditorWindowFactory"/> の実装。補助ウィンドウの生成の窓口で、用途別の 3 つの工場
    /// （割り当て＝<see cref="AssignmentWindowFactory"/>、マクロ記録＝<see cref="MacroRecordingWindowFactory"/>、プロファイル編集＝<see cref="ProfileEditorFactory"/>）へ委譲する。
    /// 窓が別の窓を開くときの窓口は、この型自身を渡す。
    /// </summary>
    public class EditorWindowFactory : IEditorWindowFactory
    {
        private readonly AssignmentWindowFactory _assignments;
        private readonly MacroRecordingWindowFactory _recording;
        private readonly ProfileEditorFactory _profileEditors;

        public EditorWindowFactory(AssignmentWindowFactory assignments, MacroRecordingWindowFactory recording,
            ProfileEditorFactory profileEditors)
        {
            ArgumentNullException.ThrowIfNull(assignments);
            _assignments = assignments;
            ArgumentNullException.ThrowIfNull(recording);
            _recording = recording;
            ArgumentNullException.ThrowIfNull(profileEditors);
            _profileEditors = profileEditors;
        }

        public BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose)
            => _assignments.CreateBindingWindow(deviceNum, settings, targetDevice, expose, this);

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, int targetDevice,
            bool showscan = true, bool repeatable = true, bool showPlaybackOptions = true)
            => _recording.CreateRecordBox(deviceNum, controlSettings, targetDevice, showscan, repeatable, showPlaybackOptions);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true, bool showPlaybackOptions = true)
            => _recording.CreateRecordBoxWindow(deviceNum, settings, targetDevice, repeatable, showPlaybackOptions, this);

        public PresetOptionWindow CreatePresetOptionWindow()
            => _assignments.CreatePresetOptionWindow();

        public StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel)
            => _assignments.CreateStickCalibrationWindow(stick, device, profileSettingsViewModel);

        public AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null)
            => _assignments.CreateAssignmentEditor(deviceNum, profileList, targetDevice, specialAction, this);

        public AssignmentEditor CreateAssignmentEditorFor(int deviceNum, ProfileList profileList, int targetDevice, SpecialActionItem item)
            => _assignments.CreateAssignmentEditor(deviceNum, profileList, targetDevice, item?.SpecialAction, this);

        public AssignmentEditor CreateControlActionEditor(int deviceNum, MappedControl control, ProfileList profileList, int targetDevice)
            => _assignments.CreateControlActionEditor(deviceNum, control.Setting, profileList, targetDevice, this);

        public AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings,
            ProfileList profileList, int targetDevice)
            => _assignments.CreateControlActionEditor(deviceNum, controlSettings, profileList, targetDevice, this);

        public ProfileEditor CreateProfileEditor(int targetDevice)
            => _profileEditors.CreateProfileEditor(targetDevice, this);
    }
}
