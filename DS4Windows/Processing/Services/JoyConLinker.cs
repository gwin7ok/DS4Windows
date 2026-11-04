using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6d（Phase9-E6-Plan.md §3.2）: <see cref="IJoyConLinker"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService.Start</c>／<c>HotPlug</c> の Joy-Con の結合の手順を、そのまま移したもの。
    /// 実機（Joy-Con）がなくても、偽物の <see cref="IJoinableDevice"/> を使った差分テスト（<c>Phase9E6JoyConLinkerTests</c>）で守る。
    /// </summary>
    public sealed class JoyConLinker : IJoyConLinker
    {
        public IJoinableDevice LinkOnStart(IJoinableDevice device, IJoinableDevice pendingPrimary, JoyConDeviceOptions.LinkMode mode)
        {
            if (mode == JoyConDeviceOptions.LinkMode.Joined)
            {
                if (IsJoyConLeftOrRight(device) && device.PerformStateMerge)
                {
                    if (pendingPrimary == null)
                    {
                        pendingPrimary = device;
                    }
                    else
                    {
                        Join(pendingPrimary, device);

                        pendingPrimary = null;
                    }
                }
            }

            return pendingPrimary;
        }

        public void FindPendingForHotPlug(IEnumerable<IJoinableDevice> devices, JoyConDeviceOptions.LinkMode mode,
            out IJoinableDevice pendingPrimary, out IJoinableDevice pendingSecondary)
        {
            pendingPrimary = null;
            pendingSecondary = null;
            if (mode == JoyConDeviceOptions.LinkMode.Joined)
            {
                foreach (IJoinableDevice d in devices)
                {
                    if (IsJoyConLeftOrRight(d) && d.PrimaryDevice && d.JointDeviceSlotNumber == -1)
                    {
                        pendingPrimary = d;
                        break;
                    }
                }

                foreach (IJoinableDevice d in devices)
                {
                    if (IsJoyConLeftOrRight(d) && !d.PrimaryDevice && d.JointDeviceSlotNumber == -1)
                    {
                        pendingSecondary = d;
                        break;
                    }
                }
            }
        }

        public void LinkOnHotPlug(IJoinableDevice device, ref IJoinableDevice pendingPrimary, ref IJoinableDevice pendingSecondary,
            JoyConDeviceOptions.LinkMode mode)
        {
            if (mode == JoyConDeviceOptions.LinkMode.Joined)
            {
                if (IsJoyConLeftOrRight(device) && device.PerformStateMerge)
                {
                    if (device.PrimaryDevice &&
                        pendingSecondary != null)
                    {
                        Join(pendingSecondary, device);

                        pendingSecondary = null;
                        pendingPrimary = null;
                    }
                    else if (!device.PrimaryDevice &&
                        pendingPrimary != null)
                    {
                        Join(pendingPrimary, device);

                        pendingPrimary = null;
                    }
                }
            }
        }

        private static bool IsJoyConLeftOrRight(IJoinableDevice device)
        {
            return device.DeviceType == InputDeviceType.JoyConL || device.DeviceType == InputDeviceType.JoyConR;
        }

        // 旧コードの 3 か所（Start、HotPlug の 2 つの枝）で同じだった組み立て。順序もそのまま:
        // 相互に結合相手を設定 → 親の JointState を相手のものにする → 取り外しで、相手の結合相手を解除する処理を両方に足す
        private static void Join(IJoinableDevice parent, IJoinableDevice current)
        {
            parent.SetJointDevice(current);
            current.SetJointDevice(parent);

            parent.CopyJointStateFrom(current);

            parent.OnRemoval(() => current.SetJointDevice(null));
            current.OnRemoval(() => parent.SetJointDevice(null));
        }
    }
}
