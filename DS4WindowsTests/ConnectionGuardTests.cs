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

        [Theory]
        [InlineData(@"\bFIRST CONNECTION detected\b")]
        [InlineData(@"\bRECONNECTION detected\b")]
        [InlineData(@"\.ApplyToSlot\(")]
        [InlineData(@"\.PublishSlot\(")]
        [InlineData(@"\bMarkConnected\(")]
        public void ControlService_NoLongerContains_TheMovedProfileSelection(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6a で ConnectionProfileSelector へ移した）");
        }

        [Fact]
        public void ControlService_DelegatesTheSelection_BeforeThePluginStep()
        {
            // Phase9 9-E E-6f1: 接続の組み立ては DeviceConnector へ移った
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (text == null) return;

            int select = text.IndexOf("_connectionProfileSelector.ApplyOnConnect(");
            int plugin = text.IndexOf("PluginOutDev(index, device);", select < 0 ? 0 : select);
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
    
        [Theory]
        [InlineData(@"\bCheckLauchProfileOption\b")]
        [InlineData(@"\bSetupInitialHookEvents\b")]
        [InlineData(@"\bPrepareTriggerEffect\(")]
        [InlineData(@"\bResetTrackAccel\(")]
        [InlineData(@"\bTouchButtonDown\b")]
        public void ControlService_NoLongerContains_TheMovedSetup(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6b で DeviceConnectionSetup へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_AndCallsTheSetupInTheOriginalOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase13-Step10 10-b: 利用者が 0 件だった CheckProfileOptions／TouchPadOn の転送は削除した（IProfileLoadTarget が別に持つ）
            Assert.DoesNotContain("public void CheckProfileOptions(", text);
            Assert.DoesNotContain("public void TouchPadOn(", text);

            // 接続時の順序: 設定の反映 → イベントの配線（旧: CheckProfileOptions → SetupInitialHookEvents）。接続の組み立ては DeviceConnector へ移った（E-6f1）
            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (connector == null) return;
            int options = connector.IndexOf("_deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);");
            int hooks = connector.IndexOf("_deviceConnectionSetup.WireInitialHooks(index, device);");
            Assert.True(options > 0 && hooks > options, "設定の反映（ApplyProfileOptions）は、イベントの配線（WireInitialHooks）より前に呼ぶ");
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
    
        [Theory]
        [InlineData(@"\bInputDeviceType\.")]
        [InlineData(@"\bDS4DeviceOpts\b")]
        [InlineData(@"\bDualSenseOpts\b")]
        [InlineData(@"\bSwitchProDeviceOpts\b")]
        [InlineData(@"\bDS3DeviceOpts\b")]
        [InlineData(@"\bJoinGyroProv\b")]
        [InlineData(@"\bNativeOptionsStore\b")]
        public void ControlService_NoLongerBranchesOnTheDeviceType(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Joy-Con の結合の手順（Start／HotPlug の中）も、E-6d で JoyConLinker へ移したため、例外はない
            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6c で IInputDeviceModule へ移した）");
        }

        [Fact]
        public void ControlService_DelegatesToTheDeviceModules()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.Contains("_deviceModules.Resolve(device.DeviceType).OnDeviceInit(device, deviceOptions);", text);
            Assert.Contains("_deviceModules.Resolve(metaInfo.inputDevType).IsEnabled(deviceOptions);", text);

            // 接続の組み立て（E-6f1 で DeviceConnector へ移った）が呼ぶもの
            string connector = SourceText.ReadCode("DS4Windows/Processing/Services/DeviceConnector.cs");
            if (connector == null) return;
            Assert.Contains("_deviceModules.Resolve(device.DeviceType).HookSettings(device, () => _slots.ActiveControllers);", connector);
            Assert.Contains("_deviceModules.Resolve(device.DeviceType).GetKnownExtraButtons(device)", connector);
        }

        [Fact]
        public void TheDeviceModules_AreRegistered_AllFiveOfThem_AndHaveNoStaticEntryPoints()
        {
            var modules = new System.Collections.Generic.List<IInputDeviceModule>(
                (System.Collections.Generic.IEnumerable<IInputDeviceModule>)TestHost.GetService(typeof(System.Collections.Generic.IEnumerable<IInputDeviceModule>)));
            Assert.Equal(5, modules.Count);
            Assert.IsType<InputDeviceModuleRegistry>(TestHost.GetService<IInputDeviceModuleRegistry>());

            foreach (string file in new[] { "Ds4DeviceModule", "DualSenseDeviceModule", "SwitchProDeviceModule", "JoyConDeviceModule", "Ds3DeviceModule", "NullInputDeviceModule", "InputDeviceModuleBase", "InputDeviceModuleRegistry" })
            {
                string text = SourceText.ReadCode(SourceText.RelativePathOf(file + ".cs"));   // Phase14d-Step1: 旧パス（DS4Control/Services/）のまま、黙って省略されていた
                if (text == null) continue;

                Assert.False(Regex.IsMatch(text, @"\bControlService\b(?!DeviceOptions)"), file + " が ControlService を参照している");
            }
        }

        [Theory]
        [InlineData(@"\bJoyConDevice\b")]
        [InlineData(@"\.JointDevice\s*=[^=]")]
        [InlineData(@"\.JointState\s*=[^=]")]   // 代入だけ。On_Report の「cState = device.JointState」（読み取り）は残る
        public void ControlService_NoLongerContains_TheJoyConLinking(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6d で JoyConLinker へ移した）");
        }

        [Fact]
        public void ControlService_CallsTheLinker_AfterThePreparation_AndBeforeTheSlotAssignment()
        {
            // Phase9 9-E E-6f2: Start／HotPlug の本体は ServiceLifecycle へ移った
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
            if (text == null) return;

            // Start: 準備（BeginPrepareConnectedInputController）→ 結合（LinkOnStart）→ スロットへの割り当て（DS4Controllers[i] = device）
            int begin = text.IndexOf("_deviceConnector.BeginPrepare(device, showlog: true);");
            int linkStart = text.IndexOf("_joyConLinker.LinkOnStart(", begin < 0 ? 0 : begin);
            int assign = text.IndexOf("slots.DS4Controllers[i] = device;", linkStart < 0 ? 0 : linkStart);
            Assert.True(begin > 0 && linkStart > begin && assign > linkStart, "Start の順序（準備 → 結合 → 割り当て）が変わっている");

            // HotPlug: 準備 → 結合（LinkOnHotPlug）→ 割り当て（DS4Controllers[Index] = device）
            int beginHot = text.IndexOf("_deviceConnector.BeginPrepare(device);");
            int linkHot = text.IndexOf("_joyConLinker.LinkOnHotPlug(", beginHot < 0 ? 0 : beginHot);
            int assignHot = text.IndexOf("slots.DS4Controllers[Index] = device;", linkHot < 0 ? 0 : linkHot);
            Assert.True(beginHot > 0 && linkHot > beginHot && assignHot > linkHot, "HotPlug の順序（準備 → 結合 → 割り当て）が変わっている");

            // HotPlug の保留の探索は、ループの前
            int find = text.IndexOf("_joyConLinker.FindPendingForHotPlug(");
            Assert.True(find > 0 && find < beginHot, "HotPlug の保留の探索は、デバイスのループより前に行う");
        }

        [Fact]
        public void JoyConLinker_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<JoyConLinker>(TestHost.GetService<IJoyConLinker>());

            foreach (string file in new[] { "JoyConLinker", "JoinableDeviceAdapter" })
            {
                string text = SourceText.ReadCode(SourceText.RelativePathOf(file + ".cs"));   // Phase14d-Step1: 旧パス（DS4Control/Services/）のまま、黙って省略されていた
                if (text == null) continue;

                Assert.False(Regex.IsMatch(text, @"\bControlService\b"), file + " が ControlService を参照している");
            }
        }
    }
}
