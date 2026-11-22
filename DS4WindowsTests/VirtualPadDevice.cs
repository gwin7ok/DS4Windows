#nullable enable
using System;
using DS4Windows.Input;

namespace DS4WindowsTests
{
    /// <summary>仮想コントローラーが作ったデバイス（破棄で、読み取りを止める）。</summary>
    internal sealed class VirtualPadDevice : IDisposable
    {
        public VirtualPadDevice(InputDeviceCore device)
        {
            Device = device;
        }

        public InputDeviceCore Device { get; }

        public void Dispose()
        {
            Device.StopUpdate();
        }
    }
}
