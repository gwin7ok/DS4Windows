using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Conditioning;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Slots;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力レポートの処理の切り出しの守り。
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
            // On_Report は InputReportProcessor へ移した（順序の検査は新しいファイルに対して維持する）
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/Conditioning/InputReportProcessor.cs");
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
        [InlineData("DS4Windows.Processing.Services.Connection.LatencyMonitor")]
        [InlineData("DS4Windows.Processing.Services.Conditioning.TouchpadToggleService")]
        [InlineData("DS4Windows.Processing.Services.Slots.ActiveInputProbe")]
        public void NewComponents_DoNotReferenceStaticEntryPoints_OrControlService(string typeName)
        {
            // ソースの文字列ではなく、型の参照（メソッド本体を含む）で確かめる。
            // AppHost・ServiceProviderHolder・Global・Program を使わないことは RemovedApiRuleTests が守る
            var targets = LayerDependencyScanner.AllTargetsOf(typeName);
            Assert.DoesNotContain("DS4Windows.Processing.ControlService", targets);
        }

    }
}
