using System.IO;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I5c（Phase10-Step6-Plan.md §4）: SA のモードでも、Controls のモードと同じ出力ピッカー（<c>BindingWindow</c>）で Extras を編集する画面の接続の検査
    /// （WPF の画面は単体テストで開けないため、ソースの接続を確かめる）。2 つのモードの違いは、Default／Unbound（modePanel）の表示だけ。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step6I5cSaExtrasEditorGuardTests
    {
        private static string Read(string relative)
        {
            string path = SourceFileLocator.Find(relative);
            return path == null ? null : File.ReadAllText(path);
        }

        [Fact]
        public void BindingWindow_SaMode_ShowsTheExtrasPanel_AndOnlyHidesTheDefaultAndUnboundPanel()
        {
            string text = Read("DS4Windows/DS4Forms/BindingWindow.xaml.cs");
            if (text == null) return;

            int start = text.IndexOf("else if (expose == ExposeMode.SpecialAction)", System.StringComparison.Ordinal);
            int end = text.IndexOf("ChangeForCurrentAction();", start, System.StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            string saBlock = text.Substring(start, end - start);

            Assert.Contains("modePanel.Visibility = Visibility.Collapsed;", saBlock);
            Assert.DoesNotContain("extrasSidePanel", saBlock.Replace("（extrasSidePanel）", string.Empty));   // コメントの語は除く
            Assert.DoesNotContain("ColumnDefinitions[2]", saBlock);
        }

        [Fact]
        public void AssignmentEditor_PassesTheSaExtrasToThePicker_AndSavesThemAfterTheAction()
        {
            string text = Read("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
            if (text == null) return;

            Assert.Contains("saExtras = specialAction.outputExtras;", text);
            Assert.Contains("settings.extras = saExtras;", text);
            Assert.Contains("saExtras = settings.IsExtrasEmpty(settings.extras) ? null : settings.extras;", text);

            int save = text.IndexOf("typeEditors.Save(typeId, tempAct, editMode);", System.StringComparison.Ordinal);
            int extras = text.IndexOf("specialActionRepository.SaveActionExtras(tempAct.name, saExtras);", System.StringComparison.Ordinal);
            Assert.True(save >= 0 && extras > save, "Extras は、SA の保存のあとに書く（SA の保存は SA を作り直すため）");
        }
    }
}
