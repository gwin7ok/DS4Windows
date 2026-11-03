using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6（S9 の追加要望）: Controls タブのコントローラー図のマウスオーバーは、ボタン一覧の選択の切り替えを 500ms 遅らせる。
    /// マウスが離れたら取り消す。コントローラーの入力による選択（実機）は即時のまま。
    /// </summary>
    public class Phase11W6HoverSelectDelayTests
    {
        private static string Code()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (path == null) return null;

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return Regex.Replace(text, @"\s+", " ");
        }

        [Fact]
        public void TheDelay_Is500ms()
        {
            string code = Code();
            if (code == null) return;

            Assert.Contains("private const int HoverSelectDelayMs = 500;", code);
        }

        [Fact]
        public void MouseEnter_DelaysTheSelection_AndMouseLeaveCancelsIt()
        {
            string code = Code();
            if (code == null) return;

            int enter = code.IndexOf("private void ContBtn_MouseEnter(", System.StringComparison.Ordinal);
            int leave = code.IndexOf("private void ContBtn_MouseLeave(", System.StringComparison.Ordinal);
            Assert.True(enter >= 0 && leave > enter);

            string enterBody = code.Substring(enter, leave - enter);
            Assert.Contains("InputControlHighlight(control, selectNow: false);", enterBody);
            Assert.Contains("StartHoverSelect(control);", enterBody);

            string leaveBody = code.Substring(leave, 120);
            Assert.Contains("CancelHoverSelect();", leaveBody);
        }

        [Fact]
        public void TheControllerInputHighlight_StaysImmediate()
        {
            string code = Code();
            if (code == null) return;

            Assert.Contains("InputControlHighlight(tempBtn);", code);
        }
    }
}
