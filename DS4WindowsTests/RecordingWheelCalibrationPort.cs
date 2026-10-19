using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.6d: <see cref="IWheelCalibrationPort"/> の記録用の実装。較正の状態を 1 つ持ち、書き込みを記録する。実機のコントローラーには触れない。
    /// <see cref="State"/> を -1 にすると、コントローラーがいない状態になる。
    /// </summary>
    public sealed class RecordingWheelCalibrationPort : IWheelCalibrationPort
    {
        public int State { get; set; }
        public readonly List<int> Writes = new List<int>();

        public int GetCalibrationState(int deviceIndex) => State;

        public void SetCalibrationState(int deviceIndex, int state)
        {
            Writes.Add(state);
            State = state;
        }
    }
}
