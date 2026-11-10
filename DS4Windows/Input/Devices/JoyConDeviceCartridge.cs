using System.Collections.Generic;
using static DS4Windows.Input.DS4Devices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// Joy-Con（L／R／充電グリップ）のカートリッジ。対応する VID／PID の表と、デバイスの生成を持つ。
    /// 表は、<c>DS4Devices</c> の対応機器の表から、この種類の行をそのまま（順序・コメントとも）移したもの。
    /// </summary>
    public sealed class JoyConDeviceCartridge : IInputDeviceCartridge
    {
        private static readonly VidPidInfo[] knownDevices =
        {
            new VidPidInfo(NINTENDO_VENDOR_ID, JOYCON_L_PRODUCT_ID, "JoyCon (L)", InputDeviceType.JoyConL, VidPidFeatureSet.DefaultDS4, checkConnection: JoyConDevice.DetermineConnectionType),
            new VidPidInfo(NINTENDO_VENDOR_ID, JOYCON_R_PRODUCT_ID, "JoyCon (R)", InputDeviceType.JoyConR, VidPidFeatureSet.DefaultDS4, checkConnection: JoyConDevice.DetermineConnectionType),
            new VidPidInfo(NINTENDO_VENDOR_ID, JOYCON_CHARGING_GRIP_PRODUCT_ID, "JoyCon (Grip)", InputDeviceType.JoyConGrip, VidPidFeatureSet.DefaultDS4, checkConnection: JoyConDevice.DetermineConnectionType),
        };

        public IReadOnlyList<VidPidInfo> KnownDevices => knownDevices;

        public InputDeviceFamily Family => InputDeviceFamily.JoyCon;

        /// <summary>充電グリップに付いた Joy-Con は、USB のハンドシェイクでシリアルを読む。それ以外は特徴レポートから読む。</summary>
        public string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            if (info.inputDevType == InputDeviceType.JoyConGrip)
            {
                // 空のシリアルは、Joy-Con がグリップのどちらの側にも付いていないことを表す
                return JoyConDevice.ReadUSBSerial(hidDevice);
            }

            return hidDevice.ReadSerial(DS4Device.SERIAL_FEATURE_ID);
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new JoyConDevice(hidDevice, displayName, featureSet);
        }
    }
}
