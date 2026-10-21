using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W0・W4（Phase10-Step3-Plan.md §2.1・§3.4・§5）: <c>ProfileEditor</c> の、Controls の割り当ての編集の入口の構成（ソース走査）。
    /// W0 で、従来の構成（<c>CreateBindingWindow</c> の呼び出しが 6 か所）を固定し、W4 で、6 か所すべてが割り当てエディター
    /// （<c>ShowControlAssignmentEditor</c>）を呼ぶ形に更新した（<c>CreateBindingWindow</c> は 0 か所）。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3W0ProfileEditorCallSitesTests
    {
        private static readonly string[] CallerMethods =
        {
            "HoverConBtn_Click",
            "ShowControlBindingWindow",
            "TiltControlsButton_Click",
            "SwipeControlsButton_Click",
        };
        // Phase10-Step3b W2: Full Pull・Outer Bind の入口は、Controls タブの一覧の項目（LS／RS Outer、L2／R2 Full Pull）になった。
        // 専用のボタンとクリックのハンドラー（TriggerFullPullBtn_Click、StickOuterBindButton_Click）は、なくなった。一覧の入口は、ShowControlBindingWindow（ダブルクリック）

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
        public void CreateBindingWindow_IsNoLongerCalled()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            Assert.Empty(Regex.Matches(source, @"CreateBindingWindow\("));
        }

        [Fact]
        public void EachKnownEntryMethod_ExistsAndCallsTheAssignmentEditor()
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
                Assert.Contains("ShowControlAssignmentEditor(", body);
            }
        }

        [Fact]
        public void TheOnlyOtherCallers_AreTheSixEntryMethods()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            // 宣言 1 + 呼び出し 4（絵のボタン、一覧／コントローラーの入力［Full Pull・Outer Bind の項目を含む］、Tilt、Swipe）
            Assert.Equal(1 + CallerMethods.Length, Regex.Matches(source, @"ShowControlAssignmentEditor\(").Count);
        }

        [Fact]
        public void TheSavedHandler_DoesTheSameRefreshesAsBefore()
        {
            string source = ReadProfileEditor();
            if (source == null)
            {
                return;
            }

            // 閉じたあとの更新（従来は、各入口が、ダイアログのあとに行っていた）。いまは、保存（ControlSaved）の受け手 1 か所
            Assert.Contains("mpControl.UpdateMappingName();", source);
            Assert.Contains("UpdateHighlightLabel(mpControl);", source);
            Assert.Contains("profileSettingsVM.RefreshCustomsFlags();", source);
        }
    }
}
