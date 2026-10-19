using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.6b: <see cref="IControllerDisconnectPort"/> の記録用の実装。呼び出しの回数だけを記録し、実機のコントローラーには触れない。
    /// <see cref="BluetoothResults"/> に結果を積むと、その順に成功／失敗を返す（空になったら <see cref="DefaultBluetoothResult"/>）。
    /// </summary>
    public sealed class RecordingControllerDisconnectPort : IControllerDisconnectPort
    {
        public DisconnectTargetKind Kind { get; set; } = DisconnectTargetKind.Bluetooth;
        public bool DefaultBluetoothResult { get; set; } = true;
        public bool DongleSynced { get; set; } = true;
        public bool Idle { get; set; } = true;
        public readonly Queue<bool> BluetoothResults = new Queue<bool>();
        public int BluetoothCalls { get; private set; }
        public int DongleCalls { get; private set; }
        public readonly List<int> Devices = new List<int>();

        public DisconnectTargetKind GetTargetKind(int deviceIndex) => Kind;

        public bool DisconnectBluetooth(int deviceIndex)
        {
            BluetoothCalls++;
            Devices.Add(deviceIndex);
            return BluetoothResults.Count > 0 ? BluetoothResults.Dequeue() : DefaultBluetoothResult;
        }

        public bool IsDongleSynced(int deviceIndex) => DongleSynced;

        public bool IsIdle(int deviceIndex) => Idle;

        public bool DisconnectDongle(int deviceIndex)
        {
            DongleCalls++;
            return true;
        }
    }
}
