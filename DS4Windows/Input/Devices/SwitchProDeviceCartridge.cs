using System.Collections.Generic;
using static DS4Windows.Input.DS4Devices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// Switch Pro コントローラーのカートリッジ。対応する VID／PID の表と、デバイスの生成を持つ。
    /// 表は、<c>DS4Devices</c> の対応機器の表から、この種類の行をそのまま（順序・コメントとも）移したもの。
    /// </summary>
    public sealed class SwitchProDeviceCartridge : IInputDeviceCartridge
    {
        private static readonly VidPidInfo[] knownDevices =
        {
            new VidPidInfo(NINTENDO_VENDOR_ID, SWITCH_PRO_PRODUCT_ID, "Switch Pro", InputDeviceType.SwitchPro, VidPidFeatureSet.DefaultDS4, checkConnection: SwitchProDevice.DetermineConnectionType),
        };

        public IReadOnlyList<VidPidInfo> KnownDevices => knownDevices;

        public bool Handles(InputDeviceType type)
        {
            return type == InputDeviceType.SwitchPro;
        }

        /// <summary>特徴レポートから読む。</summary>
        public string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            return hidDevice.ReadSerial(DS4Device.SERIAL_FEATURE_ID);
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new SwitchProDevice(hidDevice, displayName, featureSet);
        }
    }
}
