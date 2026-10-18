using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W3（Q3＝A, Q4＝A）: <c>AppHost.GetService</c> が暗黙にホストを作らないことの回帰ガード。
    /// - <c>GetService</c>（2 つ）の本体で <c>CreateHost</c> を呼ばない。
    /// - <c>CreationNote</c> に、呼び出し元スタックを組み立てない（プロパティと App.xaml.cs のログは維持）。
    /// - テストは <c>AppHost.GetService</c> を直接呼ばず、<c>TestHost</c> を通す。
    /// ソースが見つからない環境では、ソースの検査を省略する。
    /// </summary>
    public class Phase7Step10eW3AppHostGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10eW3AppHostGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void AppHost_GetService_DoesNotCreateTheHostImplicitly()
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/AppHost.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = StripComments(File.ReadAllText(path));
            foreach (string signature in new[] { "public static T GetService<T>()", "public static object GetService(Type serviceType)" })
            {
                int start = text.IndexOf(signature, System.StringComparison.Ordinal);
                Assert.True(start >= 0, "メソッドが見つからない: " + signature);
                int open = text.IndexOf('{', start);
                int depth = 0, end = open;
                for (int i = open; i < text.Length; i++)
                {
                    if (text[i] == '{') depth++;
                    else if (text[i] == '}' && --depth == 0) { end = i; break; }
                }

                string body = text.Substring(open, end - open + 1);
                Assert.DoesNotContain("CreateHost", body);
                Assert.DoesNotContain("CreationNote", body);
            }

            Assert.DoesNotContain("StackTrace", text);
            Assert.DoesNotContain("implicit CreateHost", text);
            // プロパティ自体は維持する（App.xaml.cs が起動ログに出す。copilot-instructions §2.3）
            Assert.Contains("public static string CreationNote", text);
        }

        [Fact]
        public void Tests_DoNotCallAppHostGetServiceDirectly()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            var offenders = new System.Collections.Generic.List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(root, "DS4WindowsTests"), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(@"\obj\") || file.Contains(@"\bin\")) continue;
                string name = Path.GetFileName(file);
                if (name == "TestHost.cs" || name == "Phase7Step10eW3AppHostGuardTests.cs") continue;

                string code = StripComments(File.ReadAllText(file));
                // 文字列の中の記述（ガードの期待値など）は対象外とするため、引用符で囲まれていない呼び出しだけを数える
                foreach (string line in code.Split('\n'))
                {
                    if (!Regex.IsMatch(line, @"(?<![\w])(?:DS4WinWPF\.)?AppHost\.GetService\s*[<(]")) continue;
                    if (line.Contains("\"")) continue;
                    offenders.Add(Path.GetFileName(file));
                    break;
                }
            }

            Assert.True(offenders.Count == 0, "AppHost.GetService を直接呼んでいるテスト: " + string.Join(", ", offenders.Distinct()));
        }

        [Fact]
        public void TestHost_CreatesTheSharedHostOnDemand()
        {
            Assert.NotNull(TestHost.Ensure());
            Assert.NotNull(TestHost.GetService<DS4Windows.DI.IEnvironmentService>());
            Assert.Same(DS4WinWPF.AppHost.Host, TestHost.Ensure());
        }
    }
}
