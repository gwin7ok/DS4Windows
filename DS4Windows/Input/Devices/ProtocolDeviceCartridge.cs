using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// <see cref="IDeviceProtocol"/> で動く機種のカートリッジの土台。VID／PID の表と、プロトコルを渡すだけで、デバイスの生成とシリアルの読み方が決まる。
    /// 新しい単純な機種は、これを継承して <see cref="Family"/>、<see cref="KnownDevices"/>、<see cref="Protocol"/> を書く。
    /// </summary>
    public abstract class ProtocolDeviceCartridge : IInputDeviceCartridge
    {
        public abstract InputDeviceFamily Family { get; }

        public abstract IReadOnlyList<VidPidInfo> KnownDevices { get; }

        /// <summary>この機種のプロトコル（状態を持たないため、共有してよい）。</summary>
        protected abstract IDeviceProtocol Protocol { get; }

        /// <summary>既定は、特徴レポートから読む。読み方が違う機種は上書きする。</summary>
        public virtual string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            return hidDevice.ReadSerial(DS4Device.SERIAL_FEATURE_ID);
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new ProtocolDevice(Protocol, hidDevice, displayName, featureSet);
        }
    }
}
