using System;
using DS4Windows.InputDevices;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6d（Phase9-E6-Plan.md §3.2）: Joy-Con の結合（2 つのデバイスを 1 つの入力にする）が触る、最小のメンバーだけの窓口。
    /// <c>JoyConDevice</c>（実機が必要）の代わりに、テストでは偽物を差し込める。実機側は <c>JoinableDeviceAdapter</c> が包む。
    /// </summary>
    public interface IJoinableDevice
    {
        InputDeviceType DeviceType { get; }

        bool PerformStateMerge { get; }

        bool PrimaryDevice { get; }

        /// <summary>結合相手のスロット番号。結合していないときは -1。</summary>
        int JointDeviceSlotNumber { get; }

        /// <summary>結合相手を設定する（<c>null</c> で解除）。旧: <c>JoyConDevice.JointDevice = ...</c>。</summary>
        void SetJointDevice(IJoinableDevice other);

        /// <summary>結合した状態（<c>JointState</c>）を、相手のものにする。旧: <c>this.JointState = other.JointState</c>。</summary>
        void CopyJointStateFrom(IJoinableDevice other);

        /// <summary>デバイスの取り外し（<c>Removal</c>）のときに呼ぶ処理を足す。</summary>
        void OnRemoval(Action handler);
    }
}
