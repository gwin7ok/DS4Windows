using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 2026-09-26（ユーザー決定、Phase6-Step7b の実機確認中に追加）: マクロ記録画面（RecordBox）の
    /// 4th/5th Mouse Button（mouseButtonsPanel）と Add Rumble/Change Lightbar Color（extraConPanel）は、
    /// 記録中だけ、同じ条件で表示する。以前は extraConPanel だけ Record Delays にチェックがあるときに限っていた。
    /// その制限の再混入を検出するソース走査ガード。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class RecordBoxExtraButtonsVisibilityGuardTests
    {
        private readonly ITestOutputHelper _output;

        public RecordBoxExtraButtonsVisibilityGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RecordBtn_ShowsAllFourButtonsWhileRecording_RegardlessOfRecordDelays()
        {
            string path = SourceFileLocator.Find("DS4Windows/UI/RecordBox.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("RecordBox.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            string body = SourceText.Method(text, "private void RecordBtn_Click(");

            Assert.Contains("mouseButtonsPanel.Visibility = Visibility.Visible;", body);
            Assert.Contains("extraConPanel.Visibility = Visibility.Visible;", body);
            // 表示の判定に Record Delays を使わない
            Assert.DoesNotContain("RecordDelays", body);
        }
    }
}
