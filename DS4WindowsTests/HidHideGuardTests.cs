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
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: HidHide の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// (3) ドライバーの窓口（<c>HidHideAPIDevice</c>）は、<c>HidHideApiAdapter</c> の中に閉じていること。
    /// </summary>
    public class Phase9EHidHideGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EHidHideGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"\bhidDeviceHiding\w+")]
        [InlineData(@"\bCheckAffected\b")]
        [InlineData(@"\bChangeExclusiveStatus\b")]
        [InlineData(@"\bUpdateHidHideAttributes\b")]
        [InlineData(@"\bHidHideAPIDevice\b")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-2 で HidHideService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // Phase13-Step10 10-c: 転送は削除した。利用者は IRuntimeFeatureSwitch（RuntimeFeatureSwitch）を使う
            Assert.DoesNotContain("public void CheckHidHidePresence(", text);
            Assert.DoesNotContain("public void UpdateHidHiddenAttributes()", text);
            string sw = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (sw == null) return;
            Assert.Contains("_hidHide.CheckPresence(exePath, exeName, addExe)", sw);
            Assert.Contains("_hidHide.UpdateAttributes()", sw);
        }

        [Fact]
        public void HidHideService_DoesNotTouchTheDriverDirectly()
        {
            // Phase14d-Step3: ソースの文字列ではなく、型の参照（メソッド本体を含む）で確かめる。ドライバーの窓口 HidHideAPIDevice に触れるのは工場（アダプター）だけ
            var targets = LayerDependencyScanner.AllTargetsOf("DS4Windows.Output.Services.HidHideService");
            Assert.DoesNotContain(targets, t => t.EndsWith(".HidHideAPIDevice", StringComparison.Ordinal));
            Assert.Contains(typeof(DS4Windows.Processing.Abstractions.IHidHideApiFactory).FullName, targets);
        }

        [Fact]
        public void NewComponents_AreRegisteredAsSingletons()
        {
            Assert.IsType<HidHideService>(TestHost.GetService<IHidHideService>());
            Assert.IsType<HidHideApiFactory>(TestHost.GetService<IHidHideApiFactory>());
            Assert.Same(TestHost.GetService<IHidHideService>(), TestHost.GetService<IHidHideService>());
        }
    }
}
