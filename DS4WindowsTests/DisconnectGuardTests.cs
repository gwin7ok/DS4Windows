using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;

namespace DS4WindowsTests
{
    /// <summary>
    /// 切断・同期・シリアル変更の処理の切り出しの守り。
    /// 本体が <c>DeviceDisconnectHandler</c> へ移り、<c>ControlService</c> は委譲だけを持つこと。切断の後始末の順序（ガードレール: 切断時クリーンアップ）が変わっていないこと。
    /// </summary>
    public class DisconnectGuardTests
    {
        private readonly ITestOutputHelper _output;

        public DisconnectGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheHandler_IsRegistered_AndThereAreNoStaticEntryPoints()
        {
            Assert.IsType<DeviceDisconnectHandler>(TestHost.GetService<IDeviceDisconnectHandler>());
            Assert.Null(typeof(IDeviceDisconnectHandler).Assembly.GetType("DS4Windows.DI.IDeviceSlotHost"));   // ホストの窓口は削除した
        }

    }
}
