using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W4（Phase10-Step3-Plan.md §3.4）: <c>ProfileEditor</c> が、割り当てエディター（<c>SpecialActionEditor</c> の Controls のモード）を、
    /// 左ペイン全体に重ねて表示する仕組みを固定するソース走査ガード。表示場所（ホスト）、幅の固定、保存・キャンセルでの後始末、
    /// 二重に開かない判定（コントローラーの入力［タイマー］経由を含む）。ウィンドウは WPF の STA 上でしか作れないため、ソースの構成で検査する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3W4ControlEditorHostGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step3W4ControlEditorHostGuardTests(ITestOutputHelper output)
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
        public void TheHost_SitsInTheSameCellAsTheLeftPane_AndStartsCollapsed()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null)
            {
                return;
            }

            Match host = Regex.Match(xaml, @"<Grid x:Name=""controlEditorHost""[^>]*/>");
            Assert.True(host.Success, "controlEditorHost が見つからない");
            Assert.Contains("Grid.Row=\"1\"", host.Value);
            Assert.Contains("Grid.Column=\"0\"", host.Value);
            Assert.Contains("Visibility=\"Collapsed\"", host.Value);

            // 左ペイン（sidebarTabControl）と、同じセル
            Match sidebar = Regex.Match(xaml, @"<TabControl x:Name=""sidebarTabControl""[^>]*>");
            Assert.True(sidebar.Success);
            Assert.Contains("Grid.Row=\"1\"", sidebar.Value);
            Assert.Contains("Grid.Column=\"0\"", sidebar.Value);
        }

        [Fact]
        public void ShowControlAssignmentEditor_SwapsTheLeftPaneForTheEditor_AndKeepsTheColumnWidth()
        {
            string source = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            Assert.Contains("editorWindowFactory.CreateControlActionEditor(deviceNum, mpControl.Setting, profList, targetDevice)", source);
            Assert.Contains("controlEditorHost.Width = sidebarTabControl.ActualWidth;", source);
            Assert.Contains("sidebarTabControl.Visibility = Visibility.Collapsed;", source);
            Assert.Contains("controlEditorHost.Children.Add(editor);", source);
        }

        [Fact]
        public void SaveAndCancel_BothCloseTheEditor_AndOnlySaveRefreshes()
        {
            string source = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            Assert.Contains("editor.Cancel += (sender2, args) => CloseControlEditor();", source);

            Match saved = Regex.Match(source, @"editor\.ControlSaved \+= \(sender2, args\) =>\s*\{(?<body>.*?)\n\s*\};", RegexOptions.Singleline);
            Assert.True(saved.Success, "ControlSaved の受け手が見つからない");
            Assert.Contains("CloseControlEditor();", saved.Groups["body"].Value);
            Assert.Contains("mpControl.UpdateMappingName();", saved.Groups["body"].Value);
            Assert.Contains("profileSettingsVM.RefreshCustomsFlags();", saved.Groups["body"].Value);

            Match close = Regex.Match(source, @"private void CloseControlEditor\(\)\s*\{(?<body>.*?)\n        \}", RegexOptions.Singleline);
            Assert.True(close.Success);
            Assert.Contains("controlEditorHost.Children.Remove(controlEditor);", close.Groups["body"].Value);
            Assert.Contains("sidebarTabControl.Visibility = Visibility.Visible;", close.Groups["body"].Value);
            Assert.Contains("controlEditorHost.Visibility = Visibility.Collapsed;", close.Groups["body"].Value);
        }

        [Fact]
        public void WhileTheEditorIsOpen_NothingElseOpensIt()
        {
            string source = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            // 入口の判定（二重に開かない）
            Assert.Contains("if (mpControl == null || controlEditor != null) return;", source);

            // コントローラーの入力（タイマー）: 開いている間は、選択も、開くことも、しない
            Assert.Contains("if (index >= 0 && controlEditor == null)", source);
            Assert.Contains("if (controlEditor != null) return;", source);
        }

        [Fact]
        public void TheEditor_UnregistersItsDataContexts_OnControlModeSave()
        {
            string source = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            int unregister = source.IndexOf("UnregisterDataContext();\r\n            ControlSaved?.Invoke");
            if (unregister < 0) unregister = source.IndexOf("UnregisterDataContext();\n            ControlSaved?.Invoke");
            Assert.True(unregister >= 0, "Controls のモードの保存で、ControlSaved の前に UnregisterDataContext を呼んでいない");
        }
    }
}
