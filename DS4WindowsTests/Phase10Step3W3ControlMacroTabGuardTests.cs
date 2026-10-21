using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W3（Phase10-Step3-Plan.md §3.5）→ Phase10-Step4 W3 で更新: Macro タブの SA 専用の項目
    /// （Run on trigger release／Synchronized Run／Keep key state）は、Step3 W3 では Controls のモードで隠していた（保存する場所がないため）。
    /// Step4 W3 で、Controls にも保存できるようになったため、どちらのモードでも出す（隠すコードがないことを検査する）。
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
            string xaml = Read("DS4Windows/DS4Forms/AssignmentEditor.xaml");
            if (xaml == null)
            {
                return;
            }

            Assert.Matches(@"x:Name=""runTriggerReleaseChk""[^>]*IsChecked=""\{Binding RunTriggerRelease\}""", xaml);
            Assert.Matches(@"x:Name=""syncRunChk""[^>]*IsChecked=""\{Binding SyncRun\}""", xaml);
            Assert.Matches(@"x:Name=""keepKeyStateChk""[^>]*IsChecked=""\{Binding KeepKeyState\}""", xaml);
        }

        [Fact]
        public void NoMode_CollapsesThem_SinceControlsCanSaveThemNow()
        {
            // Phase10-Step4 W3: Controls にも保存できるようになったため、Controls のモードでも隠さない（Step3 W3 までは、保存する場所がなく、隠していた）
            string source = Read("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            foreach (string name in SaOnlyCheckBoxes)
            {
                Assert.DoesNotContain(name + ".Visibility", source);
            }
        }
    }
}
