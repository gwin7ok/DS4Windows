using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: 入力レポートの処理の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 呼び出し側のための薄い委譲メソッドが残っていること。
    /// (3) 新しい部品が DI の Singleton として登録されること。
    /// (4) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）や <c>ControlService</c> を参照しないこと。
    /// </summary>
    public class InputReportGuardTests
    {
        private readonly ITestOutputHelper _output;

        public InputReportGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void OnReport_KeepsTheOriginalOrder_OfTheCallsToTheNewParts()
        {
            // Phase13-Step5b: On_Report は InputReportProcessor へ移した（順序の検査は新しいファイルに対して維持する）
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/InputReportProcessor.cs");
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
        [InlineData("DS4Windows.Processing.Services.LatencyMonitor")]
        [InlineData("DS4Windows.Processing.Services.TouchpadToggleService")]
        [InlineData("DS4Windows.Processing.Services.ActiveInputProbe")]
        public void NewComponents_DoNotReferenceStaticEntryPoints_OrControlService(string typeName)
        {
            // Phase14d-Step3: ソースの文字列ではなく、型の参照（メソッド本体を含む）で確かめる。
            // AppHost・ServiceProviderHolder・Global・Program を使わないことは RemovedApiRuleTests が守る
            var targets = LayerDependencyScanner.AllTargetsOf(typeName);
            Assert.DoesNotContain("DS4Windows.Processing.ControlService", targets);
        }

        [Fact]
        public void TheOtherFiles_ForTheCallers()
        {

            // Phase13-Step10 10-d: GetActiveInputControl の転送も削除した。利用者は IRuntimeFeatureSwitch を使う
            string rfs = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (rfs != null) Assert.Contains("_activeInputProbe.GetActive(", rfs);
            // Phase13-Step10 10-b: 利用者が 0 件だった StartTPOff／CheckForTouchToggle の転送は削除した（ITouchpadToggleService／IProfileLoadTarget が持つ）
            // Phase13-Step10 10-c: TouchpadSlide の転送も削除した。利用者は IRuntimeFeatureSwitch を使う
            string sw = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (sw != null) Assert.Contains("_touchpadToggle.Slide(", sw);
            // Phase13-Step5b: _latencyMonitor.OnReport は On_Report と一緒に InputReportProcessor へ移した
            string processor = SourceText.ReadCode("DS4Windows/Processing/Services/InputReportProcessor.cs");
            Assert.Contains("_latencyMonitor.OnReport(device, ind);", processor);
        }
    }
}
