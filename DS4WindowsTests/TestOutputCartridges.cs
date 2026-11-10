using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Output.Abstractions;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Devices;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>出力のカートリッジ（Xbox 360／DS4）を、テストで組み立てる。仮想パッドの生成は、カートリッジの Create を直接呼ぶ。</summary>
    internal static class TestOutputCartridges
    {
        private sealed class NoRumble : IDeviceRumbleService
        {
            public void SetDevRumble(InputDeviceCore device, byte heavyMotor, byte lightMotor, int deviceNum) { }
        }

        public static IOutputDeviceCartridge[] For(IProfileSettingsService settings, IDeviceRumbleService rumble)
        {
            return new IOutputDeviceCartridge[] { new Xbox360OutputCartridge(rumble), new Ds4OutputCartridge(settings) };
        }

        public static IOutputDeviceCartridge[] Default()
        {
            return For(TestHost.GetService<IProfileSettingsService>(), new NoRumble());
        }

        /// <summary>種類のカートリッジで仮想パッドを作る（None は null）。<paramref name="busVersion"/> の既定は 1.0（XInput のスロット番号なし）。</summary>
        public static OutputDevice Allocate(OutContType type, IVirtualPadBackend backend, Version busVersion = null)
        {
            foreach (IOutputDeviceCartridge cartridge in Default())
            {
                if (cartridge.Type == type) return cartridge.Create(backend, busVersion ?? new Version(1, 0));
            }

            return null;
        }
    }
}
