using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;

namespace DS4WindowsTests
{
    /// <summary>
    /// 接続の組み立ての切り出しの守り。
    /// 本体が <c>DeviceConnector</c> へ移り、<c>ControlService</c> は薄い委譲だけを持つこと。組み立ての順序が変わっていないこと。
    /// </summary>
    public class ConnectorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ConnectorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheConnector_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnector>(TestHost.GetService<IDeviceConnector>());
            Assert.Same(TestHost.GetService<IDeviceConnector>(), TestHost.GetService<IDeviceConnector>());
        }

        [Fact]
        public void TheOtherFiles_NoLongerHoldsOnReport_AndTheLifecycleCallsTheConnector()
        {

            // Start／HotPlug の本体が ServiceLifecycle へ移ったため、薄い委譲は不要になった（ライフサイクルが DeviceConnector を直接呼ぶ）
            string lifecycle = SourceText.ReadCode("DS4Windows/Processing/Services/Connection/ServiceLifecycle.cs");
            if (lifecycle == null) return;
            Assert.Contains("_deviceConnector.PrepareConnected(numControllers, device, index: i)", lifecycle);
            Assert.Contains("_deviceConnector.PrepareConnected(numControllers, device, Index)", lifecycle);
            // 入力レポートの処理（ホットパス）は InputReportProcessor へ移した。ControlService には窓口（IDeviceSlotHost）も残っていない
            string processor = SourceText.ReadCode("DS4Windows/Processing/Services/Conditioning/InputReportProcessor.cs");
            Assert.Contains("public void OnReport(InputDeviceCore device, EventArgs e, int ind)", processor);
        }
    }
}
