using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W1（Phase10-Step3-Plan.md §3.2）: 出力ピッカー（<c>BindingWindow</c>）の表示モードの整理を固定するソース走査ガード。
    /// <c>SpecialAction</c>（SA の Press Key 用。旧 <c>ForPressToggle</c>）と <c>Control</c>（Controls の割り当てエディター用）を追加した。
    /// <c>Control</c> は、Default／Unbound（<c>modePanel</c>）と Extras（<c>extrasSidePanel</c>）を出し、Record A Macro・Toggle・Scan Code を出さない。
    /// ウィンドウは WPF の STA 上でしか作れないため、ソースの構成で検査する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3W1BindingWindowModeGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step3W1BindingWindowModeGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relative)
        {
            string path = SourceFileLocator.Find(relative);
            if (path == null)
            {
                _output.WriteLine(relative + " が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        /// <summary>if (expose == ExposeMode.XXX) の { } の本文（波括弧の対応で切り出す）</summary>
        private static string BranchBody(string source, string mode)
        {
            Match m = Regex.Match(source, @"if \(expose == ExposeMode\." + mode + @"\)\s*\{");
            Assert.True(m.Success, mode + " の分岐が見つからない");

            int depth = 1;
            int i = m.Index + m.Length;
            int start = i;
            while (i < source.Length && depth > 0)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}') depth--;
                i++;
            }

            return source.Substring(start, i - start - 1);
        }

        [Fact]
        public void TheModes_AreFull_SpecialAction_Control_Keyboard()
        {
            string source = Read("DS4Windows/DS4Forms/BindingWindow.xaml.cs");
            if (source == null)
            {
                return;
            }

            Match m = Regex.Match(source, @"public enum ExposeMode : uint\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
            Assert.True(m.Success);
            string body = Regex.Replace(m.Groups["body"].Value, @"//[^\r\n]*", "");
            Assert.Matches(@"Full\s*,\s*SpecialAction\s*,\s*Control\s*,\s*Keyboard\s*,", body);
            // コメント（「旧 ForPressToggle」の説明）は除いて、旧名の使用が残っていないことを検査する
            Assert.DoesNotContain("ForPressToggle", Regex.Replace(source, @"//[^\r\n]*", ""));
        }

        [Fact]
        public void TheAssignmentEditor_OpensThePickerInTheSpecialActionMode()
        {
            string source = Read("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            Assert.Contains("BindingWindow.ExposeMode.SpecialAction", source);
            Assert.DoesNotContain("ForPressToggle", source);
        }

        [Fact]
        public void TheControlMode_HidesRecordToggleAndScanCode_ButKeepsDefaultUnboundAndExtras()
        {
            string source = Read("DS4Windows/DS4Forms/BindingWindow.xaml.cs");
            if (source == null)
            {
                return;
            }

            string body = BranchBody(source, "Control");
            Assert.Contains("recordMacroBtn.Visibility = Visibility.Collapsed", body);
            Assert.Contains("topOptsPanel.Visibility = Visibility.Collapsed", body);
            Assert.DoesNotContain("modePanel.Visibility = Visibility.Collapsed", body);
            Assert.DoesNotContain("extrasSidePanel.Visibility = Visibility.Collapsed", body);
        }

        // Phase10-Step6 I5c（ユーザー承認）: SA のモードも Extras を出す（SA に保存先ができたため）。Default／Unbound・Record A Macro・Toggle／Scan Code は、引き続き隠す。
        // 以前は「Extras も隠す」検査だった（TheSpecialActionMode_StillHidesDefaultUnboundExtrasAndRecord）
        [Fact]
        public void TheSpecialActionMode_ShowsExtras_AndStillHidesDefaultUnboundAndRecord()
        {
            string source = Read("DS4Windows/DS4Forms/BindingWindow.xaml.cs");
            if (source == null)
            {
                return;
            }

            string rest = BranchBody(source, "SpecialAction");
            Assert.Contains("modePanel.Visibility = Visibility.Collapsed", rest);
            Assert.Contains("recordMacroBtn.Visibility = Visibility.Collapsed", rest);
            Assert.DoesNotContain("extrasSidePanel.Visibility = Visibility.Collapsed", rest);
            Assert.Contains("topOptsPanel.Visibility = Visibility.Collapsed", rest);
            Assert.Matches(@"else if \(expose == ExposeMode\.SpecialAction\)\s*\{[^}]*extrasGB\.DataContext = bind;", source);
        }

        [Fact]
        public void TheControlMode_BindsTheExtrasAndDefaultUnboundPanelsToTheBinding()
        {
            string source = Read("DS4Windows/DS4Forms/BindingWindow.xaml.cs");
            if (source == null)
            {
                return;
            }

            Match m = Regex.Match(source, @"if \(expose == ExposeMode\.Full \|\| expose == ExposeMode\.Control\)\s*\{(?<body>[^}]*)\}");
            Assert.True(m.Success);
            Assert.Contains("extrasGB.DataContext = bind", m.Groups["body"].Value);
            Assert.Contains("modePanel.DataContext = bind", m.Groups["body"].Value);
        }

        [Fact]
        public void ProfileEditor_NoLongerOpensTheFullModePicker_AfterW4()
        {
            // W4 で、ProfileEditor の呼び出しは、すべて割り当てエディターへ切り替えた（BindingWindow を直接開かない）
            string source = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            Assert.DoesNotContain("ExposeMode.Control", source);
            Assert.DoesNotContain("CreateBindingWindow(", source);
        }
    }
}
