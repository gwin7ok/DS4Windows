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
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: 入力レポートの処理の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 呼び出し側のための薄い委譲メソッドが残っていること。
    /// (3) 新しい部品が DI の Singleton として登録されること。
    /// (4) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）や <c>ControlService</c> を参照しないこと。
    /// </summary>
    public class Phase9EInputReportGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EInputReportGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\bLagFlashWarning\b")]
        [InlineData(@"\binWarnMonitor\b")]
        [InlineData(@"\btempStrings\b")]
        [InlineData(@"\btouchreleased\b")]
        [InlineData(@"\btouchslid\b")]
        [InlineData(@"\bbool\[\] lag\b")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-5 で部品へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public DS4Controls GetActiveInputControl(int ind)", text);
            Assert.Contains("_activeInputProbe.GetActive(", text);
            Assert.Contains("public void StartTPOff(int deviceID)", text);
            Assert.Contains("_touchpadToggle.StartTouchpadOff(deviceID);", text);
            Assert.Contains("public string TouchpadSlide(int ind)", text);
            Assert.Contains("_touchpadToggle.Slide(", text);
            Assert.Contains("_touchpadToggle.CheckForToggle(", text);
            Assert.Contains("_latencyMonitor.OnReport(device, ind);", text);
        }

        [Fact]
        public void OnReport_KeepsTheOriginalOrder_OfTheCallsToTheNewParts()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            int monitor = text.IndexOf("_latencyMonitor.OnReport(device, ind);");
            int toggle = text.IndexOf("_touchpadToggle.CheckForToggle(ind, cState, pState);");
            int mapCustom = text.IndexOf("_mapping.MapCustom(ind, cState, tempMapState");
            Assert.True(monitor > 0 && toggle > monitor && mapCustom > toggle, "On_Report の呼び出しの順序（監視 → タッチ切り替え → マッピング）が変わっている");
        }

        [Fact]
        public void NewComponents_AreRegistered_AsSingletons()
        {
            Assert.Same(TestHost.GetService<ILatencyMonitor>(), TestHost.GetService<ILatencyMonitor>());
            Assert.Same(TestHost.GetService<ITouchpadToggleService>(), TestHost.GetService<ITouchpadToggleService>());
            Assert.Same(TestHost.GetService<IActiveInputProbe>(), TestHost.GetService<IActiveInputProbe>());
            Assert.IsType<LatencyMonitor>(TestHost.GetService<ILatencyMonitor>());
            Assert.IsType<TouchpadToggleService>(TestHost.GetService<ITouchpadToggleService>());
            Assert.IsType<ActiveInputProbe>(TestHost.GetService<IActiveInputProbe>());
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/LatencyMonitor.cs")]
        [InlineData("DS4Windows/DS4Control/Services/TouchpadToggleService.cs")]
        [InlineData("DS4Windows/DS4Control/Services/ActiveInputProbe.cs")]
        public void NewComponents_DoNotReferenceStaticEntryPoints_OrControlService(string relativePath)
        {
            string text = ReadWithoutComments(relativePath);
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.DoesNotContain("ControlService", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), relativePath + " が Global を参照している");
            Assert.False(Regex.IsMatch(text, @"\bProgram\.rootHub\b"), relativePath + " が Program.rootHub を参照している");
        }
    }
}
