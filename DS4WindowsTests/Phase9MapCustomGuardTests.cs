using System.IO;
using System.Text.RegularExpressions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 MapCustom の分割（Phase9-MapCustom-Plan.md）の守り（段階ごとに検査を足していく）。
    /// M1: 合成入力の確定（旧 <c>Mapping.Commit</c>）が <c>SyntheticInputCommitter</c> へ移り、<c>Mapping</c> は薄い委譲だけを持つこと。
    /// M2: 2 段階トリガーの状態機械（旧 <c>Mapping.ProcessTwoStageTrigger</c>）が <c>TwoStageTriggerProcessor</c> へ移り、<c>MapCustom</c> は呼び出しだけを持つこと。
    /// </summary>
    public class Phase9MapCustomGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9MapCustomGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\bPerformMouseButtonEvent\(")]
        [InlineData(@"\bPerformMouseButtonEventAlt\(")]
        [InlineData(@"\bPerformMouseWheelEvent\(")]
        [InlineData(@"\bSaveToPrevious\(")]
        [InlineData(@"\bpressagain\b")]
        [InlineData(@"\boldnow\b")]
        [InlineData(@"\bMOUSEEVENTF_LEFTDOWN\b")]
        public void Mapping_NoLongerContains_TheMovedCommit(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"Mapping.cs に '{pattern}' が残っている（Phase9 MapCustom の分割 M1 で SyntheticInputCommitter へ移した）");
        }

        [Fact]
        public void Mapping_KeepsTheThinCommit_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            Assert.Contains("public void Commit(int device, DS4Windows.DI.IProfileSettingsService settings)", text);
            Assert.Contains("_syntheticCommitter.Commit(device, settings);", text);
        }

        [Fact]
        public void TheCommitter_IsRegistered_AndKeepsTheOriginalOrder_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<SyntheticInputCommitter>(TestHost.GetService<ISyntheticInputCommitter>());
            Assert.Same(TestHost.GetService<ISyntheticInputCommitter>(), TestHost.GetService<ISyntheticInputCommitter>());

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/SyntheticInputCommitter.cs");
            if (text == null) return;

            // 集計 → 出力 → 全体の保存 → ロックの解放 → デバイスの保存 → Sync（ガード: ロックの範囲と、Sync が最後）
            string[] order =
            {
                "_syntheticInput.SyncStateLock.EnterWriteLock();",
                "_syntheticInput.GlobalState.currentClicks.leftCount +=",
                "_virtualKbm.PerformMouseButtonEvent(",
                "_virtualKbm.PerformMouseWheelEvent(",
                "_syntheticInput.GlobalState.SaveToPrevious(false);",
                "_syntheticInput.SyncStateLock.ExitWriteLock();",
                "state.SaveToPrevious(true);",
                "_virtualKbm.Sync();",
            };
            int last = -1;
            foreach (string step in order)
            {
                int index = text.IndexOf(step);
                Assert.True(index > last, $"合成入力の確定の順序が変わっている（'{step}'）");
                last = index;
            }

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "SyntheticInputCommitter が Global を参照している");
        }
    }

        [Theory]
        [InlineData(@"\bTwoStageTriggerMode\.(Normal|ExclusiveButtons|HairTrigger|HipFire|HipFireExclusiveButtons)\b")]
        [InlineData(@"\bhipFireMS\b")]
        [InlineData(@"\.StartProcessing\(")]
        [InlineData(@"\bEngageButtonsMode\b")]
        public void Mapping_NoLongerContains_TheTwoStageStateMachine(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"Mapping.cs に '{pattern}' が残っている（Phase9 MapCustom の分割 M2 で TwoStageTriggerProcessor へ移した）");
        }

        [Fact]
        public void MapCustom_CallsTheProcessor_ForL2AndR2_InTheOriginalOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            int l2 = text.IndexOf("_twoStageTrigger.Process(device, cState.L2, cState.L2Raw,");
            int r2 = text.IndexOf("_twoStageTrigger.Process(device, cState.R2, cState.R2Raw,");
            int buttons = text.IndexOf("controlSetGroup.ControlButtons.GetEnumerator()");
            Assert.True(l2 > 0 && r2 > l2 && buttons > r2, "MapCustom の順序（L2 の 2 段階 → R2 の 2 段階 → 通常ボタン）が変わっている");
        }

        [Fact]
        public void TheTwoStageProcessor_IsRegistered_AndHasNoStaticEntryPoints()
        {
            Assert.IsType<TwoStageTriggerProcessor>(TestHost.GetService<ITwoStageTriggerProcessor>());

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/TwoStageTriggerProcessor.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "TwoStageTriggerProcessor が Global を参照している");
        }
    }
}
