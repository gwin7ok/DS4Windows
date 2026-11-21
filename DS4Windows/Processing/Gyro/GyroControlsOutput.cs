#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Touchpad;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>ジャイロの出力モード Controls: 出力の許可（outputGyroControls）を、トリガーの判定で決める。</summary>
    internal sealed class GyroControlsOutput : IGyroModeOutput
    {
        public bool IsEnabled => true;

        public void Process(SixAxisEventArgs arg, ControllerState state, bool shouldOutput)
        {
            state.Motion.outputGyroControls = shouldOutput;
        }
    }
}
