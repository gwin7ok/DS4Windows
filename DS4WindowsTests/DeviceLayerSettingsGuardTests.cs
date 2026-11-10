using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W3（Q4＝A）: 第 1 層（<c>DS4Devices</c>／<c>DS4Device</c>／<c>DS4SixAxis</c>／<c>HidDevices</c>）と出力デバイスが
    /// <c>Global</c> を参照せず、設定値を <see cref="IDeviceLayerSettings"/>／関数で受け取ることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class DeviceLayerSettingsGuardTests
    {

        private readonly ITestOutputHelper _output;

        public DeviceLayerSettingsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Container_ResolvesTheSingletonLayerSettings()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var first = TestHost.GetService<IDeviceLayerSettings>();
            Assert.NotNull(first);
            Assert.IsType<DeviceLayerSettings>(first);
            Assert.Same(first, TestHost.GetService<IDeviceLayerSettings>());
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            Assert.Throws<ArgumentNullException>(() => new DeviceLayerSettings(null, TestHost.GetService<IProfileSettingsService>()));
            Assert.Throws<ArgumentNullException>(() => new Ds4DeviceRegistryAdapter((DS4Devices)null));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotManager(null, 4));   // Phase9 Steering 削除 W3: 軸の取得口（第 2 引数）を削除した。バージョンの取得口の null を検査する
        }

        [Fact]
        public void RegistryAdapter_PassesTheSettingsToTheFirstLayer()
        {
            var fake = new FakeDeviceLayerSettings { UseMoonlight = true };
            var devices = new DS4Devices(fake);
            var adapter = new Ds4DeviceRegistryAdapter(devices);
            Assert.Same(fake, devices.LayerSettings);
            Assert.NotNull(adapter);
        }

        [Fact]
        public void SixAxis_HasNoSettingsUntilTheDeviceSetsThem()
        {
            var sixAxis = new DS4SixAxis();
            Assert.Null(sixAxis.LayerSettings);
        }
    }
}
