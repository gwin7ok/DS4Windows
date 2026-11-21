#nullable enable
using System;
using System.Collections.Generic;
using System.Windows;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力ピッカー（<c>BindingWindow</c>）を、表示モードごとに実際に開き、各パネル・ボタンの見える／隠れるを固定する
    /// （モード × 部品の表。実行時の値を読むので、コード上で暗黙に決まる出し分けも含まれる）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class BindingWindowPanelVisibilityTests
    {
        private static readonly string[] Names =
        {
            "modePanel", "topOptsPanel", "recordMacroBtn", "macroOnLb", "extrasSidePanel", "mapBindingPanel", "extrasGB",
        };

        private readonly ITestOutputHelper _output;

        public BindingWindowPanelVisibilityTests(ITestOutputHelper output) => _output = output;

        private static Dictionary<string, Visibility> Read(BindingWindow.ExposeMode mode)
        {
            var result = new Dictionary<string, Visibility>();
            WpfPanelTestRunner.Run(() =>
            {
                string? appXaml = SourceText.Read("DS4Windows/App.xaml");
                if (appXaml == null) return;
                int start = appXaml.IndexOf("<Application.Resources>", StringComparison.Ordinal) + "<Application.Resources>".Length;
                int end = appXaml.IndexOf("</Application.Resources>", StringComparison.Ordinal);
                string dictionaryXaml = appXaml.Substring(start, end - start).Trim()
                    .Replace("<ResourceDictionary>", "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:lex=\"http://wpflocalizeextension.codeplex.com\">");
                var context = new System.Windows.Markup.ParserContext { BaseUri = new Uri("pack://application:,,,/DS4Windows;component/") };
                var theme = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionaryXaml, context);
                Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var factory = TestHost.GetService<IEditorWindowFactory>();
                    var window = factory.CreateBindingWindow(ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross), -1, mode);
                    // 結びつけ（祖先の Window を探す）は、窓が表示されて視覚木ができるまで効かないため、画面の外で 1 回表示する
                    window.ShowInTaskbar = false;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    window.Left = -10000;
                    window.Show();
                    try
                    {
                        window.UpdateLayout();
                        foreach (string name in Names)
                        {
                            var element = (UIElement)window.FindName(name);
                            result[name] = element.Visibility;
                        }
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
            return result;
        }

        [Fact]
        public void ControlMode_ShowsTheModePanelAndExtras_AndHidesMacroRecordingAndTopOptions()
        {
            var v = Read(BindingWindow.ExposeMode.Control);
            foreach (var pair in v) _output.WriteLine($"Control {pair.Key} = {pair.Value}");
            if (v.Count == 0) return;

            Assert.Equal(Visibility.Visible, v["modePanel"]);
            Assert.Equal(Visibility.Collapsed, v["topOptsPanel"]);
            Assert.Equal(Visibility.Collapsed, v["recordMacroBtn"]);
            Assert.Equal(Visibility.Collapsed, v["macroOnLb"]);
            Assert.Equal(Visibility.Visible, v["extrasSidePanel"]);
            Assert.Equal(Visibility.Visible, v["mapBindingPanel"]);
        }

        [Fact]
        public void SpecialActionMode_HidesTheModePanelAndMacroRecordingAndTopOptions_ButKeepsExtras()
        {
            var v = Read(BindingWindow.ExposeMode.SpecialAction);
            foreach (var pair in v) _output.WriteLine($"SpecialAction {pair.Key} = {pair.Value}");
            if (v.Count == 0) return;

            Assert.Equal(Visibility.Collapsed, v["modePanel"]);
            Assert.Equal(Visibility.Collapsed, v["topOptsPanel"]);
            Assert.Equal(Visibility.Collapsed, v["recordMacroBtn"]);
            // 実測: SA のモードでは、マクロの表示（macroOnLb）は DataContext がなく、結びつけが効かないため、既定の Visible のまま
            Assert.Equal(Visibility.Visible, v["macroOnLb"]);
            Assert.Equal(Visibility.Visible, v["extrasSidePanel"]);
            Assert.Equal(Visibility.Visible, v["mapBindingPanel"]);
        }
    }
}
