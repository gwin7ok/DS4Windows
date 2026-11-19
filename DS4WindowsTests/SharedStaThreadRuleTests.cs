using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 画面部品（<c>{lex:Loc}</c> を使う XAML）を作るテストが、自前の STA スレッドを作らず、共有の STA スレッド
    /// （<see cref="WpfPanelTestRunner"/>）で動くことを守る。
    /// WPFLocalizeExtension は、作った部品を弱い参照で静的に覚えていて、パネルの読み込みのたびに全部の部品の Dispatcher へ
    /// <c>Invoke</c> して待つ。別のテストが自前のスレッドで作った部品が（終わったスレッドのまま）残っていると、
    /// 共有のスレッドが永久に止まる。止まるかどうかはガベージコレクションの時機しだいで、実行のたびに変わる。
    /// </summary>
    public class SharedStaThreadRuleTests
    {
        // 画面部品を作らないため、自前のスレッドでよいテスト
        private static readonly string[] AllowedFiles =
        {
            "WpfPanelTestRunner.cs",
            "WpfUiDispatcherCollectionSyncTests.cs",
            "SharedStaThreadRuleTests.cs",
        };

        [Fact]
        public void TestsDoNotCreateTheirOwnStaThreads()
        {
            string root = SourceFileLocator.RepoRoot();
            if (root == null) return;

            // 検査の文字列は、このファイル自身に当たらないよう、つないで作る
            string needle = "SetApartmentState(ApartmentState." + "STA)";
            string testsRoot = Path.Combine(root, "DS4WindowsTests");
            string[] offenders = Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsBuildOutput(testsRoot, f))
                .Where(f => !AllowedFiles.Contains(Path.GetFileName(f)))
                .Where(f => File.ReadAllText(f).Contains(needle))
                .Select(f => Path.GetRelativePath(testsRoot, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            Assert.True(offenders.Length == 0,
                "自前の STA スレッドを作っているテスト（WpfPanelTestRunner.Run を使うこと）: " + string.Join(", ", offenders));
        }

        private static bool IsBuildOutput(string testsRoot, string file)
        {
            string first = Path.GetRelativePath(testsRoot, file).Split(Path.DirectorySeparatorChar)[0];
            return first == "bin" || first == "obj";
        }
    }
}
