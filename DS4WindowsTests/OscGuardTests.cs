using System.Collections.Generic;
using System.Linq;
using System;
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
    public class OscGuardTests
    {
        private readonly ITestOutputHelper _output;

        public OscGuardTests(ITestOutputHelper output)
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

        [Fact]
        public void OscService_AndTheContracts_DoNotUseTheSharpOscTypes()
        {
            // SharpOSC の型は、通信の実装（SharpOsc*）の中に閉じる。テストプロジェクトが SharpOSC を参照せずに済むための境界
            // Phase14d-Step3: ソースの文字列ではなく、型の参照（メソッド本体を含む）で確かめる
            // 空振りの防止: 通信の実装からは SharpOSC の型の参照が見えること
            Assert.Contains(LayerDependencyScanner.AllTargetsOf("DS4Windows.Output.Services.SharpOscMessageSender"), t => t.StartsWith("SharpOSC", StringComparison.Ordinal));
            foreach (string type in new[]
            {
                "DS4Windows.Output.Services.OscService", "DS4Windows.Processing.Abstractions.IOscService",
                "DS4Windows.Processing.Abstractions.IOscTransportFactory", "DS4Windows.Processing.Abstractions.IOscMessageSender", "DS4Windows.Processing.Abstractions.IOscListenerHandle",
            })
            {
                var sharpOsc = LayerDependencyScanner.AllTargetsOf(type).Where(t => t.StartsWith("SharpOSC", StringComparison.Ordinal)).ToList();
                Assert.True(sharpOsc.Count == 0, type + " が SharpOSC の型を参照している: " + string.Join(", ", sharpOsc));
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
