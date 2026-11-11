using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6（Phase9-E6-Plan.md）: ライフサイクルの切り出しの守り（段階ごとに検査を足していく）。
    /// E-6a: 接続時のプロファイルの選択と適用が <c>ConnectionProfileSelector</c> へ移り、<c>ControlService</c> は委譲だけを持つこと。
    /// E-6b: 設定の反映とイベントの配線が <c>DeviceConnectionSetup</c> へ移り、<c>ControlService</c> は薄い委譲だけを持つこと。
    /// E-6c: デバイスの種類ごとの分岐が <c>IInputDeviceModule</c>（カートリッジ）へ移り、<c>ControlService</c> には種類ごとの分岐が残らないこと。
    /// E-6d: Joy-Con の結合の手順が <c>JoyConLinker</c> へ移り、<c>ControlService</c> は呼び出しだけを持つこと。
    /// </summary>
    public class ConnectionGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ConnectionGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ControlService_DelegatesTheSelection_BeforeThePluginStep()
        {
            // Phase9 9-E E-6f1: 接続の組み立ては DeviceConnector へ移った
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (text == null) return;

            int select = text.IndexOf("_connectionProfileSelector.ApplyOnConnect(");
            int plugin = text.IndexOf("PlugVirtualPad(index, device);", select < 0 ? 0 : select);
            Assert.True(select > 0 && plugin > select, "プロファイルの選択（ApplyOnConnect）は、仮想パッドの接続（PluginOutDev）より前に呼ばれる必要がある");
        }

        [Fact]
        public void ConnectionProfileSelector_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<ConnectionProfileSelector>(TestHost.GetService<IConnectionProfileSelector>());
            Assert.Same(TestHost.GetService<IConnectionProfileSelector>(), TestHost.GetService<IConnectionProfileSelector>());

            string text = SourceText.ReadCode("DS4Windows/Processing/Services/ConnectionProfileSelector.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, @"(?<!ProfileChangeSource\.)\bControlService\b"), "ConnectionProfileSelector が ControlService を参照している");
        }
    
        [Fact]
        public void DeviceConnectionSetup_IsRegistered_KeepsTheTouchpadLookupAtEventTime_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnectionSetup>(TestHost.GetService<IDeviceConnectionSetup>());
            Assert.Same(TestHost.GetService<IDeviceConnectionSetup>(), TestHost.GetService<IDeviceConnectionSetup>());

            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnectionSetup.cs");
            if (text == null) return;

            // 旧 TouchPadOn と同じく、発火のたびに配列からマウスを引き直す
            Assert.Contains("touchPads[ind].populatePriorButtonStates()", text);
            Assert.False(Regex.IsMatch(text, @"(?<!ProfileChangeSource\.)\bControlService\b"), "DeviceConnectionSetup が ControlService を参照している");
        }
    
        [Fact]
        public void ControlService_DelegatesToTheDeviceModules()
        {
            // 登録簿へ結ぶフックは DeviceRegistryHooks が持つ（ControlService は Attach を呼ぶだけ）
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceRegistryHooks.cs");
            if (text == null) return;

            Assert.Contains("_deviceModules.Resolve(device.DeviceType).OnDeviceInit(device, _deviceOptions);", text);
            Assert.Contains("_deviceModules.Resolve(metaInfo.inputDevType).IsEnabled(_deviceOptions);", text);

            // 接続の組み立て（E-6f1 で DeviceConnector へ移った）が呼ぶもの
            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (connector == null) return;
            Assert.Contains("_deviceModules.Resolve(device.DeviceType).HookSettings(device, () => _slots.ActiveControllers);", connector);
            Assert.Contains("_deviceModules.Resolve(device.DeviceType).GetKnownExtraButtons(device)", connector);
        }

        [Fact]
        public void TheDeviceModules_AreRegistered_AllFourOfThem_AndHaveNoStaticEntryPoints()
        {
            var modules = new System.Collections.Generic.List<IInputDeviceModule>(
                (System.Collections.Generic.IEnumerable<IInputDeviceModule>)TestHost.GetService(typeof(System.Collections.Generic.IEnumerable<IInputDeviceModule>)));
            Assert.Equal(4, modules.Count);
            Assert.IsType<InputDeviceModuleRegistry>(TestHost.GetService<IInputDeviceModuleRegistry>());

            foreach (string file in new[] { "Ds4DeviceModule", "DualSenseDeviceModule", "SwitchProDeviceModule", "XboxDeviceModule", "NullInputDeviceModule", "InputDeviceModuleBase", "InputDeviceModuleRegistry" })
            {
                string text = SourceText.ReadCode(SourceText.RelativePathOf(file + ".cs"));   // Phase14d-Step1: 旧パス（DS4Control/Services/）のまま、黙って省略されていた
                if (text == null) continue;

                Assert.False(Regex.IsMatch(text, @"\bControlService\b(?!DeviceOptions)"), file + " が ControlService を参照している");
            }
        }

        [Fact]
        public void TheOtherFiles_AndCallsTheSetupInTheOriginalOrder()
        {

            // Phase13-Step10 10-b: 利用者が 0 件だった CheckProfileOptions／TouchPadOn の転送は削除した（IProfileLoadTarget が別に持つ）

            // 接続時の順序: 設定の反映 → イベントの配線（旧: CheckProfileOptions → SetupInitialHookEvents）。接続の組み立ては DeviceConnector へ移った（E-6f1）
            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (connector == null) return;
            int options = connector.IndexOf("_deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);");
            int hooks = connector.IndexOf("_deviceConnectionSetup.WireInitialHooks(index, device);");
            Assert.True(options > 0 && hooks > options, "設定の反映（ApplyProfileOptions）は、イベントの配線（WireInitialHooks）より前に呼ぶ");
        }
    }
}
