using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W3（Phase10-Step3-Plan.md §3.5）: Controls のモードの割り当てエディターは、Macro タブの SA 専用の項目
    /// （Run on trigger release／Synchronized Run／Keep key state）を隠す（Controls に保存する場所がないため）。SA のモードでは、従来どおり出す。
    /// ウィンドウは WPF の STA 上でしか作れないため、ソースの構成で検査する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3W3ControlMacroTabGuardTests
    {
        private static readonly string[] SaOnlyCheckBoxes = { "runTriggerReleaseChk", "syncRunChk", "keepKeyStateChk" };

        private readonly ITestOutputHelper _output;

        public Phase10Step3W3ControlMacroTabGuardTests(ITestOutputHelper output)
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

        [Fact]
        public void TheThreeSaOnlyCheckBoxes_AreNamedInTheXaml_AndBoundToTheMacroViewModel()
        {
            string xaml = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml");
            if (xaml == null)
            {
                return;
            }

            Assert.Matches(@"x:Name=""runTriggerReleaseChk""[^>]*IsChecked=""\{Binding RunTriggerRelease\}""", xaml);
            Assert.Matches(@"x:Name=""syncRunChk""[^>]*IsChecked=""\{Binding SyncRun\}""", xaml);
            Assert.Matches(@"x:Name=""keepKeyStateChk""[^>]*IsChecked=""\{Binding KeepKeyState\}""", xaml);
        }

        [Fact]
        public void TheControlMode_CollapsesThem_AndOnlyTheControlMode()
        {
            string source = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            foreach (string name in SaOnlyCheckBoxes)
            {
                string statement = name + ".Visibility = Visibility.Collapsed";
                Assert.Single(Regex.Matches(source, Regex.Escape(statement)));

                // if (controlMode) { ... } の中にある
                int at = source.IndexOf(statement);
                int branch = source.LastIndexOf("if (controlMode)", at);
                Assert.True(branch >= 0, name + " が、controlMode の分岐の外にある");
                Assert.DoesNotContain("else", source.Substring(branch, at - branch));
            }
        }
    }
}
