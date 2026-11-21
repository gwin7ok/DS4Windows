using System;
using System.Diagnostics;
using System.Threading;
using Xunit;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// Alt-Tab のマクロの特別扱い（<c>AltTabSwapping</c> ほか）と、
    /// 記録画面の「Cycle Programs」プリセットが、ソースに残っていないこと。Alt-Tab の並びのマクロが、通常のマクロとして手順どおりに再生されること。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AltTabRemovalGuardTests
    {

        [Fact]
        public void TheCyclePrograms_Preset_IsGone()
        {
            foreach (string file in new[] { "DS4Windows/UI/RecordBox.xaml", "DS4Windows/UI/RecordBox.xaml.cs", "DS4Windows/UI/ViewModels/RecordBoxViewModel.cs" })
            {
                string text = SourceText.Read(file);
                if (text == null) return;

                Assert.DoesNotContain("CycleProg", text);
                Assert.DoesNotContain("cycleProg", text);
                Assert.DoesNotContain("Cycle Programs", text);
            }

            // 「Load Preset」のボタンは、メニューを出さず、ファイルの選択を直接表示する（保存のボタンと同じ）
            string xaml = SourceText.Read("DS4Windows/UI/RecordBox.xaml");
            string code = SourceText.Read("DS4Windows/UI/RecordBox.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.DoesNotContain("ContextMenu", xaml);
            Assert.DoesNotContain("loadPresetBtn.ContextMenu", code);
            Assert.DoesNotContain("LoadPresetFromFileMenuItem", xaml + code);
            int handler = code.IndexOf("private void LoadPreset(", StringComparison.Ordinal);
            Assert.True(handler >= 0, "LoadPreset が見つからない");
            Assert.Contains("PickAndLoadPreset", code.Substring(handler, Math.Min(900, code.Length - handler)));
        }

        [Fact]
        public void AMacroThatStartsWithTheAltTabSequence_IsPlayedStepByStep_LikeAnyOtherMacro()
        {
            using var t = new SpecialActionTestScope();
            var sa = new SpecialAction("p8s73a_alttab_" + Guid.NewGuid().ToString("N"), "L1", "Macro", "18/9/9/18", 0, string.Empty);

            t.Mapping.PlayMacroDirect(t.Device, sa);

            var stopwatch = Stopwatch.StartNew();
            while (t.Scope.Recorder.KeyReleases.Count < 2 && stopwatch.ElapsedMilliseconds < 3000)
                Thread.Sleep(10);

            uint Native(uint key) => t.Scope.KbmMapping.macroKeyTranslate ? t.Scope.KbmMapping.GetRealEventKey(key) : key;

            // 手順どおり: Alt 押す → Tab 押す → Tab 離す → Alt 離す
            Assert.Equal(new[] { Native(18), Native(9) }, t.Scope.Recorder.KeyPresses.ToArray());
            Assert.Equal(new[] { Native(9), Native(18) }, t.Scope.Recorder.KeyReleases.ToArray());
        }
    }
}
