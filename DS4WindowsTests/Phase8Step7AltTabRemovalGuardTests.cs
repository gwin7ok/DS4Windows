using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7-3a（Phase8-Step7-3a-AltTab-Removal-Plan.md。機能廃止［ユーザー承認済み］）: Alt-Tab のマクロの特別扱い（<c>AltTabSwapping</c> ほか）と、
    /// 記録画面の「Cycle Programs」プリセットが、ソースに残っていないこと。Alt-Tab の並びのマクロが、通常のマクロとして手順どおりに再生されること。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step7AltTabRemovalGuardTests
    {
        private static string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            return path == null ? null : File.ReadAllText(path);
        }

        [Fact]
        public void TheAltTabSpecialCase_IsGoneFromMapping()
        {
            string text = Read("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            foreach (string token in new[] { "AltTabSwapping", "altTabDone", "altTabNow", "oldAltTabNow", "164/9/9/164", "18/9/9/18", "macro[0] == 164", "macroLst[0] == 164", "macroArr[0] == 164" })
                Assert.DoesNotContain(token, text);
        }

        [Fact]
        public void TheCyclePrograms_Preset_IsGone()
        {
            foreach (string file in new[] { "DS4Windows/DS4Forms/RecordBox.xaml", "DS4Windows/DS4Forms/RecordBox.xaml.cs", "DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs" })
            {
                string text = Read(file);
                if (text == null) return;

                Assert.DoesNotContain("CycleProg", text);
                Assert.DoesNotContain("cycleProg", text);
                Assert.DoesNotContain("Cycle Programs", text);
            }

            // 他のプリセット（ファイルから読み込む）は残る
            string xaml = Read("DS4Windows/DS4Forms/RecordBox.xaml");
            if (xaml != null) Assert.Contains("loadPresetFromFileMenuItem", xaml);
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
