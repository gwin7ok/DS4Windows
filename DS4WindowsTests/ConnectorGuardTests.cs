using DS4Windows.Core.Model;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6f1（Phase9-E6-Plan.md §9.2）: 接続の組み立て（旧 <c>PrepareConnectedInputControllerSettingEvents</c> ほか）の切り出しの守り。
    /// 本体が <c>DeviceConnector</c> へ移り、<c>ControlService</c> は薄い委譲だけを持つこと。組み立ての順序が変わっていないこと。
    /// </summary>
    public class ConnectorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ConnectorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"\bLoadControllerConfigsForDevice\b")]
        [InlineData(@"\bLoadStoreSettings\(")]
        [InlineData(@"\.AddController\(")]
        [InlineData(@"\bnew Mouse\(")]
        [InlineData(@"\.StartUpdate\(\)")]
        [InlineData(@"\bChargingChanged\b")]
        [InlineData(@"\bRefreshExtrasButtons\(")]
        [InlineData(@"\bQuickCharge\b")]
        [InlineData(@"\bPrepareDeviceMotion\(")]
        public void ControlService_NoLongerContains_TheMovedConnection(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6f1 で DeviceConnector へ移した）");
        }

        [Fact]
        public void ControlService_NoLongerHoldsOnReport_AndTheLifecycleCallsTheConnector()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase9 9-E E-6f2: Start／HotPlug の本体が ServiceLifecycle へ移ったため、薄い委譲は不要になった（ライフサイクルが DeviceConnector を直接呼ぶ）
            Assert.DoesNotContain("PrepareConnectedInputControllerSettingEvents", text);
            Assert.DoesNotContain("BeginPrepareConnectedInputController", text);
            string lifecycle = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
            if (lifecycle == null) return;
            Assert.Contains("_deviceConnector.PrepareConnected(numControllers, device, index: i)", lifecycle);
            Assert.Contains("_deviceConnector.PrepareConnected(numControllers, device, Index)", lifecycle);
            // Phase13-Step5b: 入力レポートの処理（ホットパス）は InputReportProcessor へ移した。ControlService には窓口（IDeviceSlotHost）も残っていない
            Assert.DoesNotContain("On_Report", text);
            Assert.DoesNotContain("IDeviceSlotHost", text.Replace("IDeviceSlotHost（ActiveControllers", ""));
            string processor = SourceText.ReadCode("DS4Windows/Processing/Services/InputReportProcessor.cs");
            Assert.Contains("public void OnReport(DS4Device device, EventArgs e, int ind)", processor);
        }

        [Fact]
        public void TheConnection_KeepsTheOriginalOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (text == null) return;

            string[] order =
            {
                "_profileSettings.RefreshExtrasButtons(",
                "_profileXmlStore.LoadControllerConfigsForDevice(device);",
                "device.LoadStoreSettings();",
                "device.CheckControllerNumDeviceSettings(numControllers);",
                "slots.SlotManager.AddController(device, index);",   // Phase13-Step5: スロット状態は IDeviceSlotState（slots）から読む
                "_outputs.NotifyPlugged(index);",
                "device.Removal += _deviceDisconnect.OnRemoval;",
                "device.SyncChange += _deviceDisconnect.OnSyncChange;",
                "device.SerialChange += _deviceDisconnect.OnSerialChange;",
                "device.ChargingChanged += OnChargingChanged;",
                "touchPads[index] = _mouseFactory.Create(index, device);",
                "device.SixAxis.GyroControlsTriggerActive",
                "_connectionProfileSelector.ApplyOnConnect(",
                "_outputs.PlugVirtualPad(index, device);",
                "_deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);",
                "_deviceConnectionSetup.WireInitialHooks(index, device);",
                "device.Report += (sender, e) =>",
                "_outputs.PrepareMotion(tempIdx, device);",
                "device.StartUpdate();",
            };

            int last = -1;
            foreach (string step in order)
            {
                int index = text.IndexOf(step);
                Assert.True(index > last, $"接続の組み立ての順序が変わっている（'{step}'）");
                last = index;
            }
        }

        [Fact]
        public void TheReportSubscription_CallsTheProcessorOnReport_WithTheSlotIndex()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (text == null) return;

            Assert.Contains("inputReport.OnReport(sender, e, tempIdx);", text);   // Phase13-Step5b: 旧 host.OnReport
        }

        [Fact]
        public void TheConnector_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnector>(TestHost.GetService<IDeviceConnector>());
            Assert.Same(TestHost.GetService<IDeviceConnector>(), TestHost.GetService<IDeviceConnector>());

            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, @"\bControlService\b"), "DeviceConnector が ControlService を参照している");
        }
    }
}
