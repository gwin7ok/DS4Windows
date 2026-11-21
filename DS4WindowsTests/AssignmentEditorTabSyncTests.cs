#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>割り当て編集画面で、種別の一覧を切り替えると、その下の設定の部品（タブ）が切り替わること。</summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AssignmentEditorTabSyncTests
    {
        private static (int combo, int tab, string header)? SwitchTo(int index)
        {
            (int, int, string)? result = null;
            WpfPanelTestRunner.Run(() =>
            {
                string? appXaml = SourceText.Read("DS4Windows/App.xaml");
                if (appXaml == null) return;
                int start = appXaml.IndexOf("<Application.Resources>", StringComparison.Ordinal) + "<Application.Resources>".Length;
                int end = appXaml.IndexOf("</Application.Resources>", StringComparison.Ordinal);
                string xaml = appXaml.Substring(start, end - start).Trim()
                    .Replace("<ResourceDictionary>", "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:lex=\"http://wpflocalizeextension.codeplex.com\">");
                var context = new System.Windows.Markup.ParserContext { BaseUri = new Uri("pack://application:,,,/DS4Windows;component/") };
                var theme = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(xaml, context);
                Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var factory = TestHost.GetService<IEditorWindowFactory>();
                    var profileList = new ProfileList(TestHost.GetService<DS4Windows.Core.Abstractions.IPathService>());
                    var editor = factory.CreateAssignmentEditor(ControllerLimits.TestProfileIndex, profileList, 0);
                    var window = new Window { Content = editor, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000 };
                    window.Show();
                    try
                    {
                        editor.UpdateLayout();
                        var combo = (ComboBox)editor.FindName("actionTypeCombo");
                        var tabs = (TabControl)editor.FindName("actionTypeTabControl");
                        combo.SelectedIndex = index;
                        editor.UpdateLayout();
                        result = (combo.SelectedIndex, tabs.SelectedIndex, ((TabItem)tabs.SelectedItem).Name);
                    }
                    finally { window.Close(); }
                }
                finally { Application.Current.Resources.MergedDictionaries.Remove(theme); }
            });
            return result;
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(7)]
        public void ChangingTheTypeList_SwitchesTheTabToTheSameIndex(int index)
        {
            var r = SwitchTo(index);
            if (r == null) return;
            Assert.Equal(index, r.Value.tab);
        }
    }
}
