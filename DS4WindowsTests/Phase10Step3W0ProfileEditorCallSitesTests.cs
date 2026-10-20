using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W0（Phase10-Step3-Plan.md §2.1・§5）: <c>ProfileEditor</c> が、<c>CreateBindingWindow</c> を呼ぶ箇所の構成の特性化（ソース走査）。
    /// 現状は 6 か所（Controls の絵のボタン、一覧／コントローラーの入力、Tilt、Swipe、Full Pull、Outer Bind）。W4 で、すべて割り当てエディターへ切り替える
    /// ときに、この検査を「0 か所」へ更新する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3W0ProfileEditorCallSitesTests
    {
        private static readonly string[] CallerMethods =
        {
            "HoverConBtn_Click",
            "ShowControlBindingWindow",
            "TiltControlsButton_Click",
            "SwipeControlsButton_Click",
            "TriggerFullPullBtn_Click",
            "StickOuterBindButton_Click",
        };

        private readonly ITestOutputHelper _output;

        public Phase10Step3W0ProfileEditorCallSitesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadProfileEditor()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("ProfileEditor.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        [Fact]
        public void CreateBindingWindow_IsCalledExactlySixTimes()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            Assert.Equal(6, Regex.Matches(source, @"CreateBindingWindow\(").Count);
        }

        [Fact]
        public void EachKnownEntryMethod_ExistsAndCallsCreateBindingWindow()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            foreach (string method in CallerMethods)
            {
                Match m = Regex.Match(source, @"(?:private|public|internal) \w+ " + method + @"\([^)]*\)\s*\{");
                Assert.True(m.Success, method + " が見つからない");

                // 次のメソッド宣言までを、このメソッドの本文とみなす
                int start = m.Index + m.Length;
                Match next = Regex.Match(source.Substring(start), @"\r?\n\s*(?:private|public|internal) \w+ \w+\(");
                string body = next.Success ? source.Substring(start, next.Index) : source.Substring(start);
                Assert.Contains("CreateBindingWindow(", body);
            }
        }

        [Fact]
        public void EveryCall_IsFollowedByTheSameRefreshes()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            // 閉じたあとの更新（割り当てエディターへ切り替えても、同じ更新を行うこと）
            Assert.True(Regex.Matches(source, @"mpControl\.UpdateMappingName\(\)").Count >= 6);
            Assert.True(Regex.Matches(source, @"profileSettingsVM\.RefreshCustomsFlags\(\)").Count >= 6);
        }
    }
}
