using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="CodeHealthScanner"/> の数え方と <see cref="CodeHealthRatchet"/> の判定を、偽の入力で確かめる
    /// （本体のソースが変わっても、検査の道具そのものが壊れていないことを守る）。
    /// </summary>
    public class CodeHealthScannerTests
    {
        private static CodeHealthScanner.SourceFile File(string path, string text) => new CodeHealthScanner.SourceFile(path, text);

        [Fact]
        public void LayerOf_UsesTheFirstFolderOfTheRelativePath()
        {
            Assert.Equal("Core", CodeHealthScanner.LayerOf("Core/Model/DS4Color.cs"));
            Assert.Equal("Core", CodeHealthScanner.LayerOf(@"Core\Model\DS4Color.cs"));
            Assert.Equal(string.Empty, CodeHealthScanner.LayerOf("App.xaml.cs"));
        }

        [Fact]
        public void CountUpperLayerUsingFiles_CountsFilesOncePerLayerAndIgnoresOtherLayers()
        {
            var files = new[]
            {
                File("Core/A.cs", "using System;\r\nusing DS4Windows.Input;\r\nusing DS4Windows.UI.ViewModels;\r\nnamespace X {}"),
                File("Core/B.cs", "using System;\r\nusing DS4Windows.Core.Model;\r\nnamespace X {}"),
                File("Processing/C.cs", "using DS4Windows.Input;\r\nnamespace X {}"),
            };

            Assert.Equal(1, CodeHealthScanner.CountUpperLayerUsingFiles(files, "Core"));
            Assert.Equal(1, CodeHealthScanner.CountUpperLayerUsingFiles(files, "Processing"));
            Assert.Equal(0, CodeHealthScanner.CountUpperLayerUsingFiles(files, "UI"));
        }

        [Fact]
        public void CountWindowsUsingFiles_CountsPlainStaticAndAliasForms()
        {
            var files = new[]
            {
                File("Core/A.cs", "using System.Windows;\r\nnamespace X {}"),
                File("Core/B.cs", "using System.Windows.Input;\r\nnamespace X {}"),
                File("Core/C.cs", "using static System.Windows.Media.Colors;\r\nnamespace X {}"),
                File("Core/D.cs", "using Brush = System.Windows.Media.Brush;\r\nnamespace X {}"),
                File("Core/E.cs", "using System.Windows.Forms;\r\nnamespace X {}"),
                File("Core/F.cs", "using System.Text;\r\n// using System.Windows;\r\nnamespace X {}"),
                File("UI/G.cs", "using System.Windows;\r\nnamespace X {}"),
            };

            Assert.Equal(5, CodeHealthScanner.CountWindowsUsingFiles(files, "Core"));
            Assert.Equal(1, CodeHealthScanner.CountWindowsUsingFiles(files, "UI"));
        }

        [Fact]
        public void CountUnusedNonFormTimerAliasLines_CountsOnlyAliasesWithoutOtherUse()
        {
            var files = new[]
            {
                File("A.cs", "using NonFormTimer = System.Timers.Timer;\r\nnamespace X { class A { } }"),
                File("B.cs", "using NonFormTimer = System.Timers.Timer;\r\nnamespace X { class B { NonFormTimer t = new NonFormTimer(); } }"),
                File("C.cs", "using NonFormTimer = System.Timers.Timer;\r\n// NonFormTimer は、コメントの中の言及\r\nnamespace X { class C { } }"),
                File("D.cs", "namespace X { class D { } }"),
            };

            Assert.Equal(2, CodeHealthScanner.CountUnusedNonFormTimerAliasLines(files));
        }

        [Fact]
        public void Evaluate_PassesOnlyWhenTheActualCountEqualsTheBaseline()
        {
            Assert.Null(CodeHealthRatchet.Evaluate("Item", 3, 3));
            Assert.Contains("超えた", CodeHealthRatchet.Evaluate("Item", 4, 3));
            Assert.Contains("更新すること", CodeHealthRatchet.Evaluate("Item", 2, 3));
        }
    }
}
