using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6（P11-7 の実機確認で発見）: メインウィンドウ最下部の 1 行メッセージ欄（<c>lastMsgLb</c>）を、すべてのタブで表示する。
    /// 旧: <c>MainTabCon_SelectionChanged</c> が、タブの番号（<c>SelectedIndex == 4</c>）で欄を隠していた。本来は Log タブ
    /// （内容が一覧と同じ）を隠す意図と思われるが、Output Slots タブが途中に入って番号がずれ、実際には <b>Settings</b> タブで隠れていた。
    /// そのため、Settings タブで出る警告（カスタム Steam フォルダーが見つからない、など）が、他のタブへ移るまで見えなかった。
    /// 利用者の決定（2026-11-02）: すべてのタブで表示する。隠す処理は持たない。
    /// ソースの走査で担保する（画面は WPF の STA 上でしか作れない）。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase11W5StatusMessageVisibilityTests
    {
        private const string MainWindowXaml = "DS4Windows/UI/MainWindow.xaml";
        private const string MainWindowCode = "DS4Windows/UI/MainWindow.xaml.cs";

        private readonly ITestOutputHelper _output;

        public Phase11W5StatusMessageVisibilityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheStatusLabel_IsOutsideTheTabControl_AndHasNoVisibilitySetting()
        {
            var xaml = XamlTestHelper.Load(MainWindowXaml);
            if (xaml == null)
            {
                _output.WriteLine(MainWindowXaml + " が見つからないため検査を省略した。");
                return;
            }

            var label = XamlTestHelper.FindByName(xaml, "lastMsgLb");
            var tabControl = XamlTestHelper.FindByName(xaml, "mainTabCon");

            // タブの外（最下部の行）にある。どのタブを選んでも、同じ場所に残る
            Assert.DoesNotContain(tabControl, label.Ancestors());
            // 欄自身が表示を決めない（既定の Visible）
            Assert.Null(XamlTestHelper.Attr(label, "Visibility"));
        }

        [Fact]
        public void SwitchingTabs_DoesNotHideTheStatusLabel()
        {
            var xaml = XamlTestHelper.Load(MainWindowXaml);
            string code = SourceText.ReadNormalized(MainWindowCode);
            if (xaml == null || code == null) return;

            // タブの切り替えで欄を隠す処理（タブの番号に依存する SelectionChanged）を持たない
            var tabControl = XamlTestHelper.FindByName(xaml, "mainTabCon");
            Assert.Null(XamlTestHelper.Attr(tabControl, "SelectionChanged"));
            Assert.DoesNotContain("MainTabCon_SelectionChanged", code);
            Assert.DoesNotContain("lastMsgLb.Visibility", code);
        }
    }
}
