using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W3（Q4＝A）: 第 1 層（<c>DS4Devices</c>／<c>DS4Device</c>／<c>DS4SixAxis</c>／<c>HidDevices</c>）と出力デバイスが
    /// <c>Global</c> を参照せず、設定値を <see cref="IDeviceLayerSettings"/>／関数で受け取ることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10dW3DeviceLayerSettingsGuardTests
    {
        private static readonly string[] LowerLayerFiles =
        {
            "DS4Windows/DS4Library/DS4Devices.cs",
            "DS4Windows/DS4Library/DS4Device.cs",
            "DS4Windows/DS4Library/DS4SixAxis.cs",
            "DS4Windows/HidLibrary/HidDevices.cs",
            "DS4Windows/DS4Control/OutputDevice.cs",
            "DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs",
            "DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs",
            "DS4Windows/DS4Control/Xbox360OutDevice.cs",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10dW3DeviceLayerSettingsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LowerLayerFiles_DoNotReferenceGlobal()
        {
            foreach (string rel in LowerLayerFiles)
            {
                string path = SourceFileLocator.Find(rel);
                if (path == null)
                {
                    _output.WriteLine(rel + " が見つからないため検査を省略した。");
                    continue;
                }

                string text = File.ReadAllText(path);
                text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
                text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
                Assert.False(Regex.IsMatch(text, @"(?<![\w.])Global\.\w+"), rel + " に Global の参照が残っている");
            }
        }

        [Fact]
        public void Container_ResolvesTheSingletonLayerSettings()
        {
            DS4WinWPF.AppHost.CreateHost();
            var first = DS4WinWPF.AppHost.GetService<IDeviceLayerSettings>();
            Assert.NotNull(first);
            Assert.IsType<DeviceLayerSettings>(first);
            Assert.Same(first, DS4WinWPF.AppHost.GetService<IDeviceLayerSettings>());
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            DS4WinWPF.AppHost.CreateHost();
            Assert.Throws<ArgumentNullException>(() => new DeviceLayerSettings(null, DS4WinWPF.AppHost.GetService<IProfileSettingsService>()));
            Assert.Throws<ArgumentNullException>(() => new Ds4DeviceRegistryAdapter(null));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotManager(() => new Version(1, 0), null, 4));
        }

        [Fact]
        public void RegistryAdapter_PassesTheSettingsToTheFirstLayer()
        {
            var original = DS4Devices.LayerSettings;
            try
            {
                var fake = new FakeDeviceLayerSettings { UseMoonlight = true };
                var adapter = new Ds4DeviceRegistryAdapter(fake);
                Assert.Same(fake, DS4Devices.LayerSettings);
                Assert.NotNull(adapter);
            }
            finally
            {
                DS4Devices.LayerSettings = original;
            }
        }

        [Fact]
        public void SixAxis_HasNoSettingsUntilTheDeviceSetsThem()
        {
            var sixAxis = new DS4SixAxis();
            Assert.Null(sixAxis.LayerSettings);
        }
    }
}
