using System.Collections.Generic;
using static DS4Windows.Input.DS4Devices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// DualSense（DualSense Edge を含む）のカートリッジ。対応する VID／PID の表と、デバイスの生成を持つ。
    /// 表は、<c>DS4Devices</c> の対応機器の表から、この種類の行をそのまま（順序・コメントとも）移したもの。
    /// </summary>
    public sealed class DualSenseDeviceCartridge : IInputDeviceCartridge
    {
        private static readonly VidPidInfo[] knownDevices =
        {
            new VidPidInfo(SONY_VID, 0x0CE6, "DualSense", InputDeviceType.DualSense, VidPidFeatureSet.DefaultDS4, DualSenseDevice.DetermineConnectionType),
            new VidPidInfo(SONY_VID, 0x0DF2, "DualSense Edge", InputDeviceType.DualSense, VidPidFeatureSet.DefaultDS4, DualSenseDevice.DetermineConnectionType),
        };

        public IReadOnlyList<VidPidInfo> KnownDevices => knownDevices;

        public bool Handles(InputDeviceType type)
        {
            return type == InputDeviceType.DualSense;
        }

        public DS4Device Create(InputDeviceType type, HidDevice hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new DualSenseDevice(hidDevice, displayName, featureSet);
        }
    }
}
