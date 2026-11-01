using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W4（Phase10-Step3-Plan.md §3.4）: <c>ProfileEditor</c> が、割り当てエディター（<c>AssignmentEditor</c> の Controls のモード）を、
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
            XDocument xaml = XamlTestHelper.Load("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null)
            {
                _output.WriteLine("DS4Windows/DS4Forms/ProfileEditor.xaml が見つからないため検査を省略した。");
                return;
            }

            // 属性の並び・空白・改行・「/>」の形は固定しない（XAML の整形で変わるため）
            XElement host = XamlTestHelper.FindByName(xaml, "controlEditorHost");
            Assert.Equal("Grid", host.Name.LocalName);
            Assert.Equal("1", XamlTestHelper.Attr(host, "Grid.Row"));
            Assert.Equal("0", XamlTestHelper.Attr(host, "Grid.Column"));
            Assert.Equal("Collapsed", XamlTestHelper.Attr(host, "Visibility"));

            // 左ペイン（sidebarTabControl）と、同じセル
            XElement sidebar = XamlTestHelper.FindByName(xaml, "sidebarTabControl");
            Assert.Equal("TabControl", sidebar.Name.LocalName);
            Assert.Equal("1", XamlTestHelper.Attr(sidebar, "Grid.Row"));
            Assert.Equal("0", XamlTestHelper.Attr(sidebar, "Grid.Column"));
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

            Assert.Contains("editor.Cancel += (sender2, args) => CloseLeftPaneEditor();", source);

            Match saved = Regex.Match(source, @"editor\.ControlSaved \+= \(sender2, args\) =>\s*\{(?<body>.*?)\n\s*\};", RegexOptions.Singleline);
            Assert.True(saved.Success, "ControlSaved の受け手が見つからない");
            Assert.Contains("CloseLeftPaneEditor();", saved.Groups["body"].Value);
            Assert.Contains("mpControl.UpdateMappingName();", saved.Groups["body"].Value);
            Assert.Contains("profileSettingsVM.RefreshCustomsFlags();", saved.Groups["body"].Value);

            Match close = Regex.Match(source, @"private void CloseLeftPaneEditor\(\)\s*\{(?<body>.*?)\n        \}", RegexOptions.Singleline);
            Assert.True(close.Success);
            Assert.Contains("controlEditorHost.Children.Remove(leftPaneEditor);", close.Groups["body"].Value);
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
            Assert.Contains("if (mpControl == null || leftPaneEditor != null) return;", source);

            // コントローラーの入力（タイマー）: 開いている間は、選択も、開くことも、しない
            Assert.Contains("if (index >= 0 && leftPaneEditor == null)", source);
            Assert.Contains("if (leftPaneEditor != null) return;", source);
        }

        [Fact]
        public void TheEditor_UnregistersItsDataContexts_OnControlModeSave()
        {
            string source = Read("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
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
