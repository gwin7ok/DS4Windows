using System.Collections.Generic;
using static DS4Windows.Input.DS4Devices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// DualShock 3のカートリッジ。対応する VID／PID の表と、デバイスの生成を持つ。
    /// 表は、<c>DS4Devices</c> の対応機器の表から、この種類の行をそのまま（順序・コメントとも）移したもの。
    /// </summary>
    public sealed class Ds3DeviceCartridge : IInputDeviceCartridge
    {
        private static readonly VidPidInfo[] knownDevices =
        {
            new VidPidInfo(0x054C, 0x0268, "DualShock 3 (SXS)", InputDeviceType.DS3, VidPidFeatureSet.DefaultDS4, checkConnection: DS3Device.DetermineConnectionType), // Sony DualShock 3 using DsHidMini driver (SXS) or Sony Sixaxis driver
        };

        public IReadOnlyList<VidPidInfo> KnownDevices => knownDevices;

        public InputDeviceFamily Family => InputDeviceFamily.DS3;

        /// <summary>DS3 は機器固有のシリアルを持たないため、擬似のシリアルを作る。</summary>
        public string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            return hidDevice.GenerateFakeHwSerial();
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new DS3Device(hidDevice, displayName, featureSet);
        }
    }
}
