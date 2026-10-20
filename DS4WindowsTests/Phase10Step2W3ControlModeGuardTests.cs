using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3・§7 Q2）: 共通の編集画面の Controls のモードは、Step4 まで、ユーザーの操作からは開かない
    /// （呼び出し元がない）。また、名前の欄は、読み取り専用の指定（<c>IsReadOnly</c>）を ViewModel に結ぶ。再混入・早すぎる接続を検出するソース走査ガード。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step2W3ControlModeGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step2W3ControlModeGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheNameField_BindsItsReadOnlyFlag_ToTheViewModel()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/SpecialActionEditor.xaml");
            if (path == null)
            {
                _output.WriteLine("SpecialActionEditor.xaml が見つからないため検査を省略した。");
                return;
            }

            Assert.Contains("IsReadOnly=\"{Binding ActionNameReadOnly}\"", File.ReadAllText(path));
        }

        [Fact]
        public void TheControlModeEditor_IsOpenedOnlyFromTheProfileEditor()
        {
            string profileEditor = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (profileEditor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string projectDir = Path.GetDirectoryName(Path.GetDirectoryName(profileEditor));
            string[] callers = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .Where(f => File.ReadAllText(f).Contains("CreateControlActionEditor("))
                .Select(f => Path.GetFileName(f))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            // 宣言（インターフェース）と実装（窓口）と、呼び出し元の ProfileEditor だけ（Phase10-Step3 W4 で、ProfileEditor に接続した）
            Assert.Equal(new[] { "EditorWindowFactory.cs", "IEditorWindowFactory.cs", "ProfileEditor.xaml.cs" }, callers);
        }
    }
}
