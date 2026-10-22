using System.IO;
using System.Text.RegularExpressions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6（Phase9-E6-Plan.md）: ライフサイクルの切り出しの守り（段階ごとに検査を足していく）。
    /// E-6a: 接続時のプロファイルの選択と適用が <c>ConnectionProfileSelector</c> へ移り、<c>ControlService</c> は委譲だけを持つこと。
    /// E-6b: 設定の反映とイベントの配線が <c>DeviceConnectionSetup</c> へ移り、<c>ControlService</c> は薄い委譲だけを持つこと。
    /// </summary>
    public class Phase9E6ConnectionGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9E6ConnectionGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\bFIRST CONNECTION detected\b")]
        [InlineData(@"\bRECONNECTION detected\b")]
        [InlineData(@"\.ApplyToSlot\(")]
        [InlineData(@"\.PublishSlot\(")]
        [InlineData(@"\bMarkConnected\(")]
        public void ControlService_NoLongerContains_TheMovedProfileSelection(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6a で ConnectionProfileSelector へ移した）");
        }

        [Fact]
        public void ControlService_DelegatesTheSelection_BeforeThePluginStep()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
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

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/ConnectionProfileSelector.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"(?<!ProfileChangeSource\.)\bControlService\b"), "ConnectionProfileSelector が ControlService を参照している");
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "ConnectionProfileSelector が Global を参照している");
        }
    
        [Theory]
        [InlineData(@"\bCheckLauchProfileOption\b")]
        [InlineData(@"\bSetupInitialHookEvents\b")]
        [InlineData(@"\bPrepareTriggerEffect\(")]
        [InlineData(@"\bResetTrackAccel\(")]
        [InlineData(@"\bTouchButtonDown\b")]
        public void ControlService_NoLongerContains_TheMovedSetup(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6b で DeviceConnectionSetup へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_AndCallsTheSetupInTheOriginalOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public void CheckProfileOptions(int ind, DS4Device device, bool startUp = false)", text);
            Assert.Contains("_deviceConnectionSetup.ApplyProfileOptions(ind, device, touchPad[ind], startUp);", text);
            Assert.Contains("public void TouchPadOn(int ind, DS4Device device)", text);
            Assert.Contains("_deviceConnectionSetup.AttachTouchpad(ind, device, touchPad);", text);

            // 接続時の順序: 設定の反映 → イベントの配線（旧: CheckProfileOptions → SetupInitialHookEvents）
            int options = text.IndexOf("CheckProfileOptions(index, device);");
            int hooks = text.IndexOf("_deviceConnectionSetup.WireInitialHooks(index, device);");
            Assert.True(options > 0 && hooks > options, "設定の反映（CheckProfileOptions）は、イベントの配線（WireInitialHooks）より前に呼ぶ");
        }

        [Fact]
        public void DeviceConnectionSetup_IsRegistered_KeepsTheTouchpadLookupAtEventTime_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<DeviceConnectionSetup>(TestHost.GetService<IDeviceConnectionSetup>());
            Assert.Same(TestHost.GetService<IDeviceConnectionSetup>(), TestHost.GetService<IDeviceConnectionSetup>());

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/DeviceConnectionSetup.cs");
            if (text == null) return;

            // 旧 TouchPadOn と同じく、発火のたびに配列からマウスを引き直す
            Assert.Contains("touchPads[ind].populatePriorButtonStates()", text);
            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"(?<!ProfileChangeSource\.)\bControlService\b"), "DeviceConnectionSetup が ControlService を参照している");
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "DeviceConnectionSetup が Global を参照している");
        }
    }
}
