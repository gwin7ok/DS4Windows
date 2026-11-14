using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Core.Settings;
using Xunit;
using DS4Windows.Processing.Services.Devices;

namespace DS4WindowsTests
{
    /// <summary>
    /// 1 つの具象を複数の契約で公開する登録（キャストを使わず、具象を 1 つ登録して契約がそれを返す形）が、
    /// 契約ごとに同じインスタンスを返すこと。別のインスタンスになると、状態が割れる。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class SingleInstanceRegistrationTests
    {
        public SingleInstanceRegistrationTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
        }

        [Fact]
        public void TheDeviceSlotState_IsTheSameInstanceForAllItsContracts()
        {
            var concrete = TestHost.GetService<DeviceSlotState>();
            Assert.Same(concrete, TestHost.GetService<IDeviceSlotState>());
            Assert.Same(concrete, TestHost.GetService<IDeviceStateAccessor>());
            Assert.Same(concrete, TestHost.GetService<IConfigurableDeviceLookup>());
        }

        [Fact]
        public void TheActionManager_IsTheSameInstanceForTheContractAndTheConcreteType()
        {
            Assert.Same(TestHost.GetService<DefaultActionManager>(), TestHost.GetService<IManagedActionManager>());
        }

        [Fact]
        public void TheProfileSettingsService_IsTheSameInstanceForTheContractAndTheConcreteType()
        {
            Assert.Same(TestHost.GetService<ProfileSettingsService>(), TestHost.GetService<IProfileSettingsService>());
        }
    }
}
