#nullable enable
using System;
using DS4Windows.Input;

namespace DS4WindowsTests
{
    /// <summary>仮想コントローラーが作ったデバイス（破棄で、読み取りを止める）。</summary>
    internal sealed class VirtualPadDevice : IDisposable
    {
        private readonly Action? _disconnect;

        public VirtualPadDevice(InputDeviceCore device, FakeHidTransport? transport = null, Action? disconnect = null)
        {
            Device = device;
            Transport = transport;
            _disconnect = disconnect;
        }

        public InputDeviceCore Device { get; }

        /// <summary>HID の機種の偽の通信路（書き込みの記録を見る。Xbox は null）。</summary>
        public FakeHidTransport? Transport { get; }

        /// <summary>機器が抜けたことにする（Xbox のように、台本の終わりが切断にならない機種のため。HID の機種は何もしない）。</summary>
        public void Disconnect() => _disconnect?.Invoke();

        public void Dispose()
        {
            Device.StopUpdate();
        }
    }
}
