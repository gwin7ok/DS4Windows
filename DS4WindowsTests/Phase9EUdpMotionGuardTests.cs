using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: UDP モーションの切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、<c>IDeviceSlotState</c> を直接注入で受けること（Phase13-Step5 で <c>IUdpMotionHost</c> を廃止）。
    /// (3) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class Phase9EUdpMotionGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EUdpMotionGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"\b_udpServer\b")]
        [InlineData(@"\budpEuroPair\w+")]
        [InlineData(@"\budpOutBuffers\b")]
        [InlineData(@"\budpChangeStatus\b")]
        [InlineData(@"\bchangingUDPPort\b")]
        [InlineData(@"\bPrepareDevUDPMotion\b")]
        [InlineData(@"\bResetUdpSmoothingFilters\b")]
        [InlineData(@"\bChangeUdpSmoothingAttrs\b")]
        [InlineData(@"\bTestQueueBus\b")]
        [InlineData(@"\bUseUDPPort\b")]
        [InlineData(@"\bnew UdpServer\(")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-3 で UdpMotionService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase13-Step10 10-c: 転送は削除した。利用者は IRuntimeFeatureSwitch（RuntimeFeatureSwitch）を使う
            Assert.DoesNotContain("public void ChangeUDPStatus(", text);
            Assert.DoesNotContain("public void ChangeMotionEventStatus(", text);
            string sw = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (sw == null) return;
            Assert.Contains("_udpMotion.ChangeStatus(state, openPort)", sw);
            Assert.Contains("_udpMotion.ChangeMotionEventStatus(state)", sw);
        }

        [Theory]
        [InlineData("DS4Windows/Output/Services/UdpMotionService.cs")]
        [InlineData("DS4Windows/Output/Services/UdpMotionServerAdapter.cs")]
        [InlineData("DS4Windows/Output/Services/UdpMotionServerFactory.cs")]
        public void NewComponents_DoNotReferenceStaticEntryPoints(string relativePath)
        {
            string text = SourceText.ReadCode(relativePath);
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), relativePath + " が Global を参照している");
            Assert.False(Regex.IsMatch(text, @"\bProgram\.rootHub\b"), relativePath + " が Program.rootHub を参照している");
        }

        [Fact]
        public void NewComponents_AreRegistered_AndTheUdpHostInterfaceIsGone()
        {
            Assert.IsType<UdpMotionService>(TestHost.GetService<IUdpMotionService>());
            Assert.IsType<UdpMotionServerFactory>(TestHost.GetService<IUdpMotionServerFactory>());
            Assert.Same(TestHost.GetService<IUdpMotionService>(), TestHost.GetService<IUdpMotionService>());
            Assert.Null(typeof(ControlService).Assembly.GetType("DS4Windows.DI.IUdpMotionHost"));
            Assert.DoesNotContain(typeof(ControlService).GetInterfaces(), i => i.Name == "IUdpMotionHost");
        }
    }
}
