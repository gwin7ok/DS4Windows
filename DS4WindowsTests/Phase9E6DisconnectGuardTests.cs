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
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）: 切断・同期・シリアル変更の処理の切り出しの守り。
    /// 本体が <c>DeviceDisconnectHandler</c> へ移り、<c>ControlService</c> は委譲だけを持つこと。切断の後始末の順序（ガードレール: 切断時クリーンアップ）が変わっていないこと。
    /// </summary>
    public class Phase9E6DisconnectGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9E6DisconnectGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\.HandleDeviceDisconnect\(")]
        [InlineData(@"\.ReleaseSlot\(")]
        [InlineData(@"\.RemoveController\(")]
        [InlineData(@"\bControllerWasRemoved\b")]
        [InlineData(@"\bOnDeviceSerialChange\(")]
        [InlineData(@"\bReplaceOneEuroFilterPair\(")]
        [InlineData(@"\bIsRemoving\b")]
        public void ControlService_NoLongerContains_TheMovedDisconnectHandling(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6e で DeviceDisconnectHandler へ移した）");
        }

        [Fact]
        public void ControlService_NoLongerKeepsTheEventHandlers_AndTheConnectorSubscribesInTheOriginalOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            // Phase9 9-E E-6f1: イベントの購読先の委譲は不要になった（接続の組み立て［DeviceConnector］が、ハンドラーを直接購読する）
            Assert.DoesNotContain("On_DS4Removal", text);
            Assert.DoesNotContain("On_SyncChange", text);
            Assert.DoesNotContain("On_SerialChange", text);

            string connector = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceConnector.cs");
            if (connector == null) return;
            // イベントの購読の登録順は変えない（Removal: 自分 → レジストリ、SyncChange: 自分 → レジストリ）
            text = connector;
            int removal = text.IndexOf("device.Removal += _deviceDisconnect.OnRemoval;");
            int registryRemoval = text.IndexOf("device.Removal += _deviceRegistry.OnRemoval;");
            int sync = text.IndexOf("device.SyncChange += _deviceDisconnect.OnSyncChange;");
            int registrySync = text.IndexOf("device.SyncChange += _deviceRegistry.UpdateSerial;");
            Assert.True(removal > 0 && registryRemoval > removal && sync > registryRemoval && registrySync > sync);
        }

        [Fact]
        public void TheRemoval_KeepsTheCleanupOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceDisconnectHandler.cs");
            if (text == null) return;

            // 仮想パッドの取り外し → 合成入力のコミット → エンジンの切断 → スロットの解放 → 切断の通知 → 状態の初期化 → スロット管理の解除 → OSC の通知
            string[] order =
            {
                "_virtualPad.UnplugOutDev(ind, device);",
                "_mapping.Commit(ind, _profileSettings);",
                "_mapping.HandleDeviceDisconnect(ind);",
                "_profileSlotApplier.ReleaseSlot(ind);",
                "AppLogger.LogToTray(removed);",
                "device.IsRemoved = true;",
                "host.DS4Controllers[ind] = null;",
                "_oscService.ResetDeviceState(ind);",
                "host.SlotManager.RemoveController(device, ind);",
                "_oscService.SendPlug(ind, false);",
                "host.TouchPads[ind] = null;",
                "_latencyMonitor.Reset(ind);",
            };

            int last = -1;
            foreach (string step in order)
            {
                int index = text.IndexOf(step);
                Assert.True(index > last, $"切断の後始末の順序が変わっている（'{step}'）");
                last = index;
            }
        }

        [Fact]
        public void TheHandler_IsRegistered_TheHostIsTheControlService_AndThereAreNoStaticEntryPoints()
        {
            Assert.IsType<DeviceDisconnectHandler>(TestHost.GetService<IDeviceDisconnectHandler>());
            Assert.Same(TestHost.GetService<ControlService>(), TestHost.GetService<IDeviceSlotHost>());

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceDisconnectHandler.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bControlService\b"), "DeviceDisconnectHandler が ControlService を参照している");
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "DeviceDisconnectHandler が Global を参照している");
        }
    }
}
