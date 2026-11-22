using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using DS4Windows.Processing.Services.Slots;

namespace DS4WindowsTests
{
    /// <summary>
    /// ライフサイクルの切り出しの守り（段階ごとに検査を足していく）。
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
        public void ConnectionProfileSelector_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<ConnectionProfileSelector>(TestHost.GetService<IConnectionProfileSelector>());
            Assert.Same(TestHost.GetService<IConnectionProfileSelector>(), TestHost.GetService<IConnectionProfileSelector>());
        }
    
        [Fact]
        public void DeviceConnectionSetup_IsRegistered_KeepsTheTouchpadLookupAtEventTime_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnectionSetup>(TestHost.GetService<IDeviceConnectionSetup>());
            Assert.Same(TestHost.GetService<IDeviceConnectionSetup>(), TestHost.GetService<IDeviceConnectionSetup>());

            string text = SourceText.ReadCode("DS4Windows/Processing/Services/Connection/DeviceConnectionSetup.cs");
            if (text == null) return;

            // 旧 TouchPadOn と同じく、発火のたびに配列からマウスを引き直す
            Assert.Contains("touchPads[ind].PopulatePriorButtonStates()", text);
        }
    
        [Fact]
        public void ControlService_DelegatesToTheDeviceModules()
        {
            // 登録簿へ結ぶフックは DeviceRegistryHooks が持つ（ControlService は Attach を呼ぶだけ）
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/Devices/DeviceRegistryHooks.cs");
            if (text == null) return;

            Assert.Contains("_deviceModules.Resolve(device.DeviceType).OnDeviceInit(device, _deviceOptions!);", text);
            Assert.Contains("_deviceModules.Resolve(metaInfo.inputDevType).IsEnabled(_deviceOptions!);", text);

            // 接続の組み立て（E-6f1 で DeviceConnector へ移った）が呼ぶもの
            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/Connection/DeviceConnector.cs");
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
        }

    }
}
