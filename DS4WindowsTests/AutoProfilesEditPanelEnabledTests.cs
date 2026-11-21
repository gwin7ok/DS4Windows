#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 自動プロファイルの編集欄（editControlsPanel）は、項目を選んでいる間だけ操作できる。
    /// 判断は XAML の DataTrigger が行う（code-behind は DataContext を入れ替えるだけ）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AutoProfilesEditPanelEnabledTests
    {
        private sealed class Item
        {
            public string Filename => "game.exe";
        }

        [Fact]
        public void EditPanel_IsEnabledOnlyWhileAnItemIsSelected()
        {
            bool? initial = null, selected = null, cleared = null;
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
                    var view = new AutoProfiles { DataContext = new object() };
                    var window = new Window { Content = view, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000 };
                    window.Show();
                    try
                    {
                        view.UpdateLayout();
                        var panel = (StackPanel)view.FindName("editControlsPanel");
                        initial = panel.IsEnabled;
                        panel.DataContext = new Item();
                        view.UpdateLayout();
                        selected = panel.IsEnabled;
                        panel.DataContext = null;
                        view.UpdateLayout();
                        cleared = panel.IsEnabled;
                    }
                    finally { window.Close(); }
                }
                finally { Application.Current.Resources.MergedDictionaries.Remove(theme); }
            });

            if (initial == null) return;   // ソースが見つからない環境では検査しない
            Assert.False(initial);
            Assert.True(selected);
            Assert.False(cleared);
        }
    }
}
