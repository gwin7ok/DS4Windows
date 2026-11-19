#nullable enable
using System;
using System.Collections;
using System.Reflection;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力ピッカー（<c>BindingWindow</c>）が実行時に開けて、147 のボタンが対応表に載ること（XAML の <c>Command</c> の読み取りと、
    /// コントローラーの図のボタンへの <c>Command</c> の設定）の守り。ビルドが通っても、読み込み・対応表の作成の失敗は、開いたときにだけ出る。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class BindingWindowMarkupLoadTests
    {
        [Fact]
        public void BindingWindow_OpensAndRegistersEveryButton()
        {
            WpfPanelTestRunner.Run(() =>
            {
                string? appXaml = SourceText.Read("DS4Windows/App.xaml");
                if (appXaml == null) return;
                int start = appXaml.IndexOf("<Application.Resources>", StringComparison.Ordinal) + "<Application.Resources>".Length;
                int end = appXaml.IndexOf("</Application.Resources>", StringComparison.Ordinal);
                string dictionaryXaml = appXaml.Substring(start, end - start).Trim()
                    .Replace("<ResourceDictionary>", "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:DS4Windows\">");
                var context = new System.Windows.Markup.ParserContext { BaseUri = new Uri("pack://application:,,,/DS4Windows;component/") };
                var theme = (System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionaryXaml, context);
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var factory = TestHost.GetService<IEditorWindowFactory>();
                    var window = factory.CreateBindingWindow(ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross), -1, BindingWindow.ExposeMode.Control);

                    var field = typeof(BindingWindow).GetField("associatedBindings", BindingFlags.NonPublic | BindingFlags.Instance)!;
                    var map = (IDictionary)field.GetValue(window)!;

                    Assert.Equal(147, map.Count);
                    Assert.False(window.DefaultPicked);
                }
                finally
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }
    }
}
