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
        public void TheRemoval_KeepsTheCleanupOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/Connection/DeviceDisconnectHandler.cs");
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
                "host.Controllers[ind] = null!;",
                "_oscService.ResetDeviceState(ind);",
                "host.SlotManager.RemoveController(device, ind);",
                "_oscService.SendPlug(ind, false);",
                "host.TouchPads[ind] = null!;",
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
        public void TheHandler_IsRegistered_AndThereAreNoStaticEntryPoints()
        {
            Assert.IsType<DeviceDisconnectHandler>(TestHost.GetService<IDeviceDisconnectHandler>());
            Assert.Null(typeof(IDeviceDisconnectHandler).Assembly.GetType("DS4Windows.DI.IDeviceSlotHost"));   // ホストの窓口は削除した
        }

        [Fact]
        public void TheOtherFiles_ThenConnectorSubscribesInTheOriginalOrder()
        {

            // イベントの購読先の委譲は不要になった（接続の組み立て［DeviceConnector］が呼ぶ DeviceLifecycleHooks が、ハンドラーを直接購読する）

            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/Connection/DeviceLifecycleHooks.cs");
            if (connector == null) return;
            // イベントの購読の登録順は変えない（Removal: 自分 → レジストリ、SyncChange: 自分 → レジストリ）
            string text = connector;
            int removal = text.IndexOf("device.Removal += _deviceDisconnect.OnRemoval;");
            int registryRemoval = text.IndexOf("device.Removal += _deviceRegistry.OnRemoval;");
            int sync = text.IndexOf("device.SyncChange += _deviceDisconnect.OnSyncChange;");
            int registrySync = text.IndexOf("device.SyncChange += _deviceRegistry.UpdateSerial;");
            Assert.True(removal > 0 && registryRemoval > removal && sync > registryRemoval && registrySync > sync);
        }
    }
}
