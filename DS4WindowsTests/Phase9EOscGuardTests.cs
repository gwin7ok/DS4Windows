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
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-1 で OscService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public void ChangeOSCListenerStatus(bool state)", text);
            Assert.Contains("public void ChangeOSCSenderStatus(bool state)", text);
            Assert.Contains("_oscService.ChangeListenerStatus(state);", text);
            Assert.Contains("_oscService.ChangeSenderStatus(state);", text);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/OscService.cs")]
        [InlineData("DS4Windows/DS4Control/Services/SharpOscTransportFactory.cs")]
        [InlineData("DS4Windows/DS4Control/Services/SharpOscListenerHandle.cs")]
        [InlineData("DS4Windows/DS4Control/Services/SharpOscMessageSender.cs")]
        public void NewComponents_DoNotReferenceStaticEntryPoints(string relativePath)
        {
            string text = ReadWithoutComments(relativePath);
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
                "DS4Windows/DS4Control/Services/OscService.cs", "DS4Windows/DI/IOscService.cs",
                "DS4Windows/DI/IOscTransportFactory.cs", "DS4Windows/DI/IOscMessageSender.cs", "DS4Windows/DI/IOscListenerHandle.cs",
            })
            {
                string text = ReadWithoutComments(file);
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
