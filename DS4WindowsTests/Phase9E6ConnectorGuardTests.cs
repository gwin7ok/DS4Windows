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
    /// Phase9 9-E E-6f1（Phase9-E6-Plan.md §9.2）: 接続の組み立て（旧 <c>PrepareConnectedInputControllerSettingEvents</c> ほか）の切り出しの守り。
    /// 本体が <c>DeviceConnector</c> へ移り、<c>ControlService</c> は薄い委譲だけを持つこと。組み立ての順序が変わっていないこと。
    /// </summary>
    public class Phase9E6ConnectorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9E6ConnectorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
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
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6f1 で DeviceConnector へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForStartAndHotPlug()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("private void PrepareConnectedInputControllerSettingEvents(int numControllers, DS4Device device, int index)", text);
            Assert.Contains("_deviceConnector.PrepareConnected(numControllers, device, index);", text);
            Assert.Contains("private void BeginPrepareConnectedInputController(DS4Device device, bool showlog = false)", text);
            Assert.Contains("_deviceConnector.BeginPrepare(device, showlog);", text);
            // 入力レポートの処理（ホットパス）は ControlService に残り、窓口から呼ばれる
            Assert.Contains("protected void On_Report(DS4Device device, EventArgs e, int ind)", text);
            Assert.Contains("=> On_Report(device, e, ind);", text);
        }

        [Fact]
        public void TheConnection_KeepsTheOriginalOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceConnector.cs");
            if (text == null) return;

            string[] order =
            {
                "_profileSettings.RefreshExtrasButtons(",
                "_profileXmlStore.LoadControllerConfigsForDevice(device);",
                "device.LoadStoreSettings();",
                "device.CheckControllerNumDeviceSettings(numControllers);",
                "host.SlotManager.AddController(device, index);",
                "_oscService.SendPlug(index, true);",
                "device.Removal += _deviceDisconnect.OnRemoval;",
                "device.SyncChange += _deviceDisconnect.OnSyncChange;",
                "device.SerialChange += _deviceDisconnect.OnSerialChange;",
                "device.ChargingChanged += OnChargingChanged;",
                "touchPads[index] = new Mouse(",
                "device.SixAxis.GyroControlsTriggerActive",
                "_connectionProfileSelector.ApplyOnConnect(",
                "_virtualPad.PluginOutDev(index, device);",
                "_deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);",
                "_deviceConnectionSetup.WireInitialHooks(index, device);",
                "device.Report += (sender, e) =>",
                "_udpMotion.PrepareDeviceMotion(device, tempIdx);",
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
        public void TheReportSubscription_CallsTheHostOnReport_WithTheSlotIndex()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceConnector.cs");
            if (text == null) return;

            Assert.Contains("host.OnReport(sender, e, tempIdx);", text);
        }

        [Fact]
        public void TheConnector_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnector>(TestHost.GetService<IDeviceConnector>());
            Assert.Same(TestHost.GetService<IDeviceConnector>(), TestHost.GetService<IDeviceConnector>());

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceConnector.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bControlService\b"), "DeviceConnector が ControlService を参照している");
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "DeviceConnector が Global を参照している");
        }
    }
}
