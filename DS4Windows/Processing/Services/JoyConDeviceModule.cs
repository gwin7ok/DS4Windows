using System.Collections.Generic;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6c: Joy-Con（L／R／Grip）の部品。設定の有効／無効、追加ボタン、結合（Joined）の初期値。
    /// 結合の初期値の判定は、<c>JoyConDevice</c> を作れなくても確かめられるよう、純粋な関数 <see cref="DecideJoin"/> に分けた
    /// （実機がなくても <c>Phase9E6DeviceModuleTests</c> の差分テストで担保する。Phase9-E6-Plan.md §3.2）。
    /// </summary>
    public sealed class JoyConDeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) =>
            type == InputDeviceType.JoyConL || type == InputDeviceType.JoyConR || type == InputDeviceType.JoyConGrip;

        public override bool IsEnabled(ControlServiceDeviceOptions options) => options.JoyConDeviceOpts.Enabled;

        public override List<DS4Controls> GetKnownExtraButtons(DS4Device device)
        {
            List<DS4Controls> result = new List<DS4Controls>();
            // 旧コードは L／R のみ（Grip は default: で空）
            if (device.DeviceType == InputDeviceType.JoyConL || device.DeviceType == InputDeviceType.JoyConR)
            {
                result.AddRange(new DS4Controls[] { DS4Controls.Capture, DS4Controls.SideL, DS4Controls.SideR, DS4Controls.FnL, DS4Controls.FnR });
            }

            return result;
        }

        public override void OnDeviceInit(DS4Device device, ControlServiceDeviceOptions options)
        {
            if (device.DeviceType == InputDeviceType.JoyConL ||
                device.DeviceType == InputDeviceType.JoyConR)
            {
                if (DecideJoin(device.DeviceType, options.JoyConDeviceOpts.LinkedMode, options.JoyConDeviceOpts.JoinGyroProv,
                    out bool primaryDevice, out bool outputMapGyro))
                {
                    InputDevices.JoyConDevice tempJoyDev = device as InputDevices.JoyConDevice;
                    tempJoyDev.PerformStateMerge = true;
                    tempJoyDev.PrimaryDevice = primaryDevice;
                    tempJoyDev.OutputMapGyro = outputMapGyro;
                }
            }
        }

        /// <summary>
        /// 結合（Joined）のときの、主デバイスか、ジャイロを出力に使うか。結合しない設定のときは false（何も設定しない）。
        /// L は主デバイス、R は副。ジャイロは、設定で選んだ側だけが出力に使う。
        /// </summary>
        public static bool DecideJoin(InputDeviceType type, JoyConDeviceOptions.LinkMode mode, JoyConDeviceOptions.JoinedGyroProvider gyroProvider,
            out bool primaryDevice, out bool outputMapGyro)
        {
            primaryDevice = false;
            outputMapGyro = false;
            if (mode != JoyConDeviceOptions.LinkMode.Joined)
            {
                return false;
            }

            if (type == InputDeviceType.JoyConL)
            {
                primaryDevice = true;
                outputMapGyro = gyroProvider == JoyConDeviceOptions.JoinedGyroProvider.JoyConL;
            }
            else
            {
                primaryDevice = false;
                outputMapGyro = gyroProvider == JoyConDeviceOptions.JoinedGyroProvider.JoyConR;
            }

            return true;
        }
    }
}
