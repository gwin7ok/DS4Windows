using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W3: <see cref="IDeviceLayerSettings"/> の固定値のテスト用実装（DI を使わないテストで、第 1 層へ渡す）。
    /// </summary>
    internal class FakeDeviceLayerSettings : IDeviceLayerSettings
    {
        public bool UseMoonlight { get; set; }
        public bool UseAdvancedMoonlight { get; set; }
        public bool VerboseLogMessages { get; set; }
        public int DebouncingMs { get; set; }
        public bool UseDs3PitchRollSim { get; set; }

        public int GetDebouncingMs(int slot) => DebouncingMs;
    }
}
