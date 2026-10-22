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
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: 仮想パッド出力の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 過渡期の逆依存（<c>OutputSlotService</c> → <c>ControlService</c>）が解消されていること。
    /// (3) 新しい部品が DI の Singleton として登録され、<c>ControlService</c> が <see cref="IVirtualPadHost"/> として解決できること。
    /// (4) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class Phase9EVirtualPadGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EVirtualPadGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\boutputslotMan\b")]
        [InlineData(@"\bvigemTestClient\b")]
        [InlineData(@"\btempThread\b")]
        [InlineData(@"(?<!_virtualPad\.)\bStartViGEm\(")]   // Start からの呼び出し（_virtualPad.StartViGEm()）は残る
        [InlineData(@"\bEstablishOutDevice\(")]
        [InlineData(@"\bnew OutputSlotManager\(")]
        [InlineData(@"(?<!_virtualPad\.)\bAssignInitialDevices\b")]   // Start からの呼び出し（_virtualPad.AssignInitialDevices()）は残る
        [InlineData(@"\bAttachNewUnboundOutDev\b")]
        [InlineData(@"\bAttachUnboundOutDev\b")]
        [InlineData(@"\bDetachUnboundOutDev\b")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-4 で VirtualPadService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public void PluginOutDev(int index, DS4Device device)", text);
            Assert.Contains("_virtualPad.PluginOutDev(index, device);", text);
            Assert.Contains("public void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false)", text);
            Assert.Contains("_virtualPad.UnplugOutDev(index, device, immediate, force);", text);
            Assert.Contains("public OutputDevice[] outputDevices => _virtualPad.OutputDevices;", text);
        }

        [Fact]
        public void OutputSlotService_NoLongerDependsOnControlService()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/OutputSlotService.cs");
            if (text == null) return;

            Assert.DoesNotContain("ControlService", text);
            Assert.DoesNotContain("EventDispatcher", text);
            Assert.Contains("Func<IVirtualPadService>", text);
            Assert.Contains("virtualPad.QueueOnEventThread(", text);
        }

        [Fact]
        public void VirtualPadService_DoesNotReferenceStaticEntryPoints_OrControlService()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/VirtualPadService.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.DoesNotContain("ControlService", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "VirtualPadService が Global を参照している");
            Assert.False(Regex.IsMatch(text, @"\bProgram\.rootHub\b"), "VirtualPadService が Program.rootHub を参照している");
        }

        [Fact]
        public void NewComponents_AreRegistered_AndTheHostIsTheControlService()
        {
            Assert.IsType<VirtualPadService>(TestHost.GetService<IVirtualPadService>());
            Assert.Same(TestHost.GetService<IVirtualPadService>(), TestHost.GetService<IVirtualPadService>());
            Assert.Same(TestHost.GetService<ControlService>(), TestHost.GetService<IVirtualPadHost>());
            Assert.Same(TestHost.GetService<IVirtualPadService>(), TestHost.GetService<System.Func<IVirtualPadService>>()());
            Assert.Same(TestHost.GetService<IVirtualPadHost>(), TestHost.GetService<System.Func<IVirtualPadHost>>()());
        }
    }
}
