using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: OSC の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、SharpOSC の型は通信の実装（<c>SharpOsc*</c>）の中に閉じていること。
    /// (3) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class Phase9EOscGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EOscGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"\boscState\b")]
        [InlineData(@"\boscCallback\b")]
        [InlineData(@"\boscListener\b")]
        [InlineData(@"\boscSender\b")]
        [InlineData(@"\bSendOsc\(")]
        [InlineData(@"\bOSCPreMappingStep\b")]
        [InlineData(@"\bOSCPostMappingStep\b")]
        [InlineData(@"\bCompareAndSendChangesToOSC\b")]
        [InlineData(@"\bCreateOSCCallback\b")]
        [InlineData(@"\bMapMonitoringOscMessageToCommand\b")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-1 で OscService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase13-Step10 10-c: 転送は削除した。利用者は IRuntimeFeatureSwitch（RuntimeFeatureSwitch）を使う
            Assert.DoesNotContain("public void ChangeOSCListenerStatus(", text);
            Assert.DoesNotContain("public void ChangeOSCSenderStatus(", text);
            string sw = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (sw == null) return;
            Assert.Contains("_osc.ChangeListenerStatus(state)", sw);
            Assert.Contains("_osc.ChangeSenderStatus(state)", sw);
        }

        [Theory]
        [InlineData("DS4Windows/Output/Services/OscService.cs")]
        [InlineData("DS4Windows/Output/Services/SharpOscTransportFactory.cs")]
        [InlineData("DS4Windows/Output/Services/SharpOscListenerHandle.cs")]
        [InlineData("DS4Windows/Output/Services/SharpOscMessageSender.cs")]
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
        public void OscService_AndTheContracts_DoNotUseTheSharpOscTypes()
        {
            // SharpOSC の型は、通信の実装（SharpOsc*）の中に閉じる。テストプロジェクトが SharpOSC を参照せずに済むための境界
            foreach (string file in new[]
            {
                "DS4Windows/Output/Services/OscService.cs", "DS4Windows/Processing/Abstractions/IOscService.cs",
                "DS4Windows/Processing/Abstractions/IOscTransportFactory.cs", "DS4Windows/Processing/Abstractions/IOscMessageSender.cs", "DS4Windows/Processing/Abstractions/IOscListenerHandle.cs",
            })
            {
                string text = SourceText.ReadCode(file);
                if (text == null) continue;

                Assert.DoesNotContain("using SharpOSC", text);
                Assert.DoesNotContain("UDPSender", text);
                Assert.DoesNotContain("UDPListener", text);
                Assert.DoesNotContain("OscMessage(", text);
            }
        }

        [Fact]
        public void NewComponents_AreRegisteredAsSingletons()
        {
            Assert.IsType<OscService>(TestHost.GetService<IOscService>());
            Assert.IsType<SharpOscTransportFactory>(TestHost.GetService<IOscTransportFactory>());
            Assert.Same(TestHost.GetService<IOscService>(), TestHost.GetService<IOscService>());
        }
    }
}
