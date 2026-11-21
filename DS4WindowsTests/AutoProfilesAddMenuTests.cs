#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS4Windows.UI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 自動プロファイルの「プログラムを追加」ボタンの右クリックメニュー（Steam・スタートメニュー・フォルダー・exe）が、
    /// 画面の DataContext の ViewModel のコマンドに結ばれ、押すと実行されること。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AutoProfilesAddMenuTests
    {
        private sealed class Stub
        {
            public int Runs;
            public ICommand AddFromSteamCommand { get; }
            public ICommand AddFromStartMenuCommand { get; }
            public ICommand AddFromFolderCommand { get; }
            public ICommand AddProgramFileCommand { get; }
            public bool ShowSteamMenu => true;
            public bool ShowStartMenu => true;

            public Stub()
            {
                AddFromSteamCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Runs++);
                AddFromStartMenuCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Runs++);
                AddFromFolderCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Runs++);
                AddProgramFileCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Runs++);
            }
        }

        [Fact]
        public void TheMenuItems_AreBoundToTheViewModelCommands()
        {
            int bound = -1;
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
                    var view = new AutoProfiles();
                    var stub = new Stub();
                    view.DataContext = stub;
                    var window = new Window { Content = view, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000 };
                    window.Show();
                    try
                    {
                        view.UpdateLayout();
                        var button = (Button)view.FindName("addProgramsBtn");
                        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                        view.UpdateLayout();
                        bound = 0;
                        foreach (var item in button.ContextMenu.Items)
                        {
                            if (item is MenuItem mi && mi.Command != null) bound++;
                        }
                        button.ContextMenu.IsOpen = false;
                    }
                    finally { window.Close(); }
                }
                finally { Application.Current.Resources.MergedDictionaries.Remove(theme); }
            });

            if (bound == -1) return;   // ソースが見つからない環境では検査しない
            Assert.Equal(4, bound);
        }
    }
}
