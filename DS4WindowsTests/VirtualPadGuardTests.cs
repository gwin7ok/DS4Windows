using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: 仮想パッド出力の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 過渡期の逆依存（<c>OutputSlotService</c> → <c>ControlService</c>）が解消されていること。
    /// (3) 新しい部品が DI の Singleton として登録され、<c>ControlService</c> が <see cref="IVirtualPadHost"/> として解決できること。
    /// (4) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class VirtualPadGuardTests
    {
        private readonly ITestOutputHelper _output;

        public VirtualPadGuardTests(ITestOutputHelper output)
        {
            _output = output;
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
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-4 で VirtualPadService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase13-Step10 10-b: 利用者が 0 件だった PluginOutDev／UnplugOutDev の転送は削除した（IVirtualPadService／IProfileLoadTarget が持つ）
            Assert.DoesNotContain("public void PluginOutDev(", text);
            Assert.DoesNotContain("public void UnplugOutDev(", text);
            // 同様に、利用者が 0 件になった outputDevices の転送も削除した（IVirtualPadService.OutputDevices を使う）
            Assert.DoesNotContain("outputDevices", text);
        }

        [Fact]
        public void NewComponents_AreRegistered_AndTheHostIsTheControlService()
        {
            Assert.IsType<VirtualPadService>(TestHost.GetService<IVirtualPadService>());
            Assert.Same(TestHost.GetService<IVirtualPadService>(), TestHost.GetService<IVirtualPadService>());
            Assert.IsType<EventThreadDispatcher>(TestHost.GetService<IEventThreadDispatcher>());
        }
    }
}
