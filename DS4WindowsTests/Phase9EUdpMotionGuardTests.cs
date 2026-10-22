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
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: UDP モーションの切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、<c>ControlService</c> が <see cref="IUdpMotionHost"/> として解決できること。
    /// (3) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class Phase9EUdpMotionGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EUdpMotionGuardTests(ITestOutputHelper output)
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
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-3 で UdpMotionService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public void ChangeUDPStatus(bool state, bool openPort = true)", text);
            Assert.Contains("_udpMotion.ChangeStatus(state, openPort);", text);
            Assert.Contains("public void ChangeMotionEventStatus(bool state)", text);
            Assert.Contains("_udpMotion.ChangeMotionEventStatus(state);", text);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/UdpMotionService.cs")]
        [InlineData("DS4Windows/DS4Control/Services/UdpMotionServerAdapter.cs")]
        [InlineData("DS4Windows/DS4Control/Services/UdpMotionServerFactory.cs")]
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
        public void NewComponents_AreRegistered_AndTheHostIsTheControlService()
        {
            Assert.IsType<UdpMotionService>(TestHost.GetService<IUdpMotionService>());
            Assert.IsType<UdpMotionServerFactory>(TestHost.GetService<IUdpMotionServerFactory>());
            Assert.Same(TestHost.GetService<IUdpMotionService>(), TestHost.GetService<IUdpMotionService>());
            Assert.Same(TestHost.GetService<ControlService>(), TestHost.GetService<IUdpMotionHost>());
            Assert.Same(TestHost.GetService<IUdpMotionHost>(), TestHost.GetService<System.Func<IUdpMotionHost>>()());
        }
    }
}
