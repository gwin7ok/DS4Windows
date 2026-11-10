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
            return InputDeviceTypeInfo.IsFamily(type, InputDeviceFamily.DualSense);
        }

        /// <summary>DualSense は、DS4 と別の特徴レポート番号から読む。</summary>
        public string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            return hidDevice.ReadSerial(DualSenseDevice.SERIAL_FEATURE_ID);
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new DualSenseDevice(hidDevice, displayName, featureSet);
        }
    }
}
