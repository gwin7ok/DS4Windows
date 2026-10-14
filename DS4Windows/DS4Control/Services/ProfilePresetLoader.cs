using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IProfilePresetLoader"/> の実装。<c>Global.Load…Profile</c> への薄い委譲（独自の状態を持たない）。
    /// </summary>
    public class ProfilePresetLoader : IProfilePresetLoader
    {
        public void LoadBlank(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadBlankDevProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadBlankDS4(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadBlankDS4Profile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultGamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultGamepadGyroProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultDS4GamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultDS4GamepadGyroProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultMixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultMixedControlsProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultDS4MixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultDS4MixedControlsProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultMixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultMixedGyroMouseProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultDS4MixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultDS4MixedGyroMouseProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultKBM(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultKBMProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);

        public void LoadDefaultKBMGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadDefaultKBMGyroMouseProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);
    }
}
