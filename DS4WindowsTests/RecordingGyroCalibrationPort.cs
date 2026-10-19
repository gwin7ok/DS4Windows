using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>Phase8-Step7 7.6a: <see cref="IGyroCalibrationPort"/> の記録用の実装。呼ばれたデバイスの添字だけを記録し、実機のコントローラーには触れない。</summary>
    public sealed class RecordingGyroCalibrationPort : IGyroCalibrationPort
    {
        public readonly List<int> Calls = new List<int>();

        public void ResetContinuousCalibration(int deviceIndex)
        {
            lock (Calls) Calls.Add(deviceIndex);
        }
    }
}
