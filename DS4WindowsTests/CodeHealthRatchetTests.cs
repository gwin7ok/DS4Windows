using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 本体のソースの健全さを、数字で守る検査。今ある数は <see cref="CodeHealthBaseline"/> に凍結し、
    /// 「超えない」と「実数と一致する」の両方を検査する（減らしたら、同じコミットで基準も更新する）。
    /// 項目を足すときは、<see cref="CodeHealthBaseline"/> に定数を足し、ここに同名の検査を足す。
    /// 環境変数 <c>DS4W_CODEHEALTH_BASELINE_OUT</c> に書き出し先のパスを入れて実行すると、実測値を「名前=値」の行で書き出す。
    /// </summary>
    public class CodeHealthRatchetTests
    {
        private const string BaselineOutEnv = "DS4W_CODEHEALTH_BASELINE_OUT";

        [Fact]
        public void CoreDoesNotUseUpperLayerNamespaces()
        {
            int actual = CodeHealthScanner.CountUpperLayerUsingFiles(ProductionSources(), "Core");
            Check(nameof(CodeHealthBaseline.CoreUpperLayerUsingFiles), actual, CodeHealthBaseline.CoreUpperLayerUsingFiles);
        }

        [Fact]
        public void CoreDoesNotUseWpfNamespaces()
        {
            int actual = CodeHealthScanner.CountWindowsUsingFiles(ProductionSources(), "Core");
            Check(nameof(CodeHealthBaseline.CoreWindowsUsingFiles), actual, CodeHealthBaseline.CoreWindowsUsingFiles);
        }

        [Fact]
        public void NonFormTimerAliasIsDeclaredOnlyWhereItIsUsed()
        {
            int actual = CodeHealthScanner.CountUnusedNonFormTimerAliasLines(ProductionSources());
            Check(nameof(CodeHealthBaseline.UnusedNonFormTimerAliasLines), actual, CodeHealthBaseline.UnusedNonFormTimerAliasLines);
        }

        [Fact]
        public void NullableIsEnabledInEveryFileOfTheEnabledFolders()
        {
            var missing = CodeHealthScanner.FilesMissingNullableDirective(ProductionSources(), CodeHealthBaseline.NullableEnabledFolders);
            Assert.True(missing.Count == 0, "#nullable enable がないファイル: " + string.Join(", ", missing.Take(20)));
        }

        [Fact]
        public void NullForgivingOperatorAlwaysHasAReason()
        {
            int actual = CodeHealthScanner.CountNullForgivingWithoutReason(ProductionSources());
            Check(nameof(CodeHealthBaseline.NullForgivingWithoutReason), actual, CodeHealthBaseline.NullForgivingWithoutReason);
        }

        [Fact]
        public void NullableWarningsAreNotSuppressedByPragma()
        {
            int actual = CodeHealthScanner.CountNullableWarningPragmas(ProductionSources());
            Check(nameof(CodeHealthBaseline.NullableWarningPragmas), actual, CodeHealthBaseline.NullableWarningPragmas);
        }

        private static void Check(string name, int actual, int baseline)
        {
            string failure = CodeHealthRatchet.Evaluate(name, actual, baseline);
            if (failure == null) return;

            string outPath = Environment.GetEnvironmentVariable(BaselineOutEnv);
            if (!string.IsNullOrEmpty(outPath))
            {
                File.AppendAllText(outPath, $"{name}={actual}{Environment.NewLine}", new UTF8Encoding(true));
            }

            Assert.True(false, failure);
        }

        private static IReadOnlyList<CodeHealthScanner.SourceFile> ProductionSources()
        {
            string root = SourceFileLocator.RepoRoot();
            if (root == null) return new CodeHealthScanner.SourceFile[0];

            string productionRoot = Path.Combine(root, "DS4Windows");
            return SourceText.ProductionFiles()
                .Select(f => new CodeHealthScanner.SourceFile(Path.GetRelativePath(productionRoot, f), File.ReadAllText(f)))
                .ToList();
        }
    }
}
