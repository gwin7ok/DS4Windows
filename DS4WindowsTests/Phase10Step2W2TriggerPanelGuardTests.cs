using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2）: SA の編集画面のトリガーの一覧は、データ駆動（<c>TriggerOptions</c>）。
    /// 名前つきのチェックボックス 49＋49 個と、その <c>Click</c> のコードビハインド、チェックボックスの照合の再混入を検出するソース走査ガード。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step2W2TriggerPanelGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step2W2TriggerPanelGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        [Fact]
        public void TheXaml_BuildsTheTriggerRowsFromTheViewModel_NotFromNamedCheckBoxes()
        {
            string xaml = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml");
            if (xaml == null) return;

            Assert.Contains("ItemsSource=\"{Binding TriggerOptions}\"", xaml);
            Assert.DoesNotContain("TrigCk", xaml);                        // 名前つきのチェックボックス（crossTrigCk、unloadCrossTrigCk ほか）
            Assert.DoesNotContain("ControlTriggerCheckBox_Click", xaml);
            Assert.DoesNotContain("ControlUnloadTriggerCheckBox_Click", xaml);
            Assert.DoesNotContain("unloadTriggersListView", xaml);        // 実際は非表示だった、解除トリガーの一覧
        }

        [Fact]
        public void TheCodeBehind_NoLongerMatchesOrClicksTheCheckBoxes()
        {
            string code = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            if (code == null) return;

            Assert.DoesNotContain("triggerBoxes", code);                  // triggerBoxes／unloadTriggerBoxes
            Assert.DoesNotContain("TrigCk", code);
            Assert.DoesNotContain("ControlTriggerCheckBox_Click", code);
            Assert.DoesNotContain("ControlUnloadTriggerCheckBox_Click", code);
            Assert.DoesNotContain("unloadTriggersListView", code);
        }
    }
}
