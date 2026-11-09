using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>ProfileEditor</c> の XAML が、実行時に読み込めること（<c>InitializeComponent</c> が例外を出さないこと）の守り。
    /// 名前を付けた部品の型と、コンパイル済みの接続の割り当てが食い違うと、ビルドは通っても、画面を開いた時にだけ
    /// <see cref="InvalidCastException"/> で落ちる。コンストラクターは通さず（依存の組み立てを避けるため）、XAML の読み込みだけを行う。
    /// </summary>
    // 画面のテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class ProfileEditorMarkupLoadTests
    {
        [Fact]
        public void ProfileEditorMarkup_LoadsWithTheTheme()
        {
            WpfPanelTestRunner.Run(() =>
            {
                // アプリ全体のリソース（App.xaml の Application.Resources。テーマと共有スタイル）を、そのまま足す
                string appXaml = SourceText.Read("DS4Windows/App.xaml");
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
                    // コンストラクターは、最初に必須の引数を検査し、そのあとで InitializeComponent を呼ぶ。
                    // 検査を通すだけの実物を渡し、XAML の読み込み（InitializeComponent）のあとの引数の検査で落ちるところまで進める。
                    // 読み込みに失敗すれば、その例外（InvalidCastException など）が先に出る。
                    var ex = Record.Exception(() => new DS4Windows.UI.ProfileEditor(
                        0,
                        TestHost.GetService<DS4Windows.Core.Abstractions.IAppSettingsService>(),
                        TestHost.GetService<DS4Windows.Core.Abstractions.IProfileSettingsService>(),
                        TestHost.GetService<DS4Windows.UI.Abstractions.IEditorWindowFactory>(),
                        null, null, null, null,
                        TestHost.GetService<DS4Windows.Core.Abstractions.IEnvironmentService>(),
                        TestHost.GetService<DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch>(),
                        null));
                    if (!(ex is ArgumentNullException)) throw new InvalidOperationException(ex?.ToString(), ex);
                }
                finally
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }

        private static readonly HashSet<string> EventAttributes = new HashSet<string>
        {
            "Click", "MouseDoubleClick", "SelectionChanged", "Loaded", "Unloaded", "Checked", "Unchecked", "TextChanged", "ValueChanged",
            "MouseRightButtonUp", "MouseLeftButtonUp", "MouseLeftButtonDown", "MouseEnter", "MouseLeave", "Closed", "Opened", "Closing",
            "PreviewMouseDown", "PreviewMouseUp", "PreviewKeyDown", "KeyDown", "KeyUp", "LostFocus", "GotFocus", "DragEnter", "Drop",
        };

        /// <summary>
        /// リソース（<c>….Resources</c>）の中の、スタイル・テンプレート以外の部品に、名前（x:Name）やイベントの属性を付けない。
        /// リソースの中身は読み込みが遅れるため、コンパイル時の接続番号と読み込み時の順序がずれ、ビルドもテストも通るのに、
        /// 画面を開いた時にだけ <see cref="InvalidCastException"/> で落ちる（ContextMenu の MenuItem で起きた）。
        /// </summary>
        [Fact]
        public void NamedOrEventfulElements_AreNotPlacedInsideResources()
        {
            string root = SourceFileLocator.RepoRoot();
            if (root == null) return;

            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.xaml", SearchOption.AllDirectories)
                         .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                  && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                XDocument doc = XDocument.Load(file);
                foreach (XElement resources in doc.Descendants().Where(e => e.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal)))
                {
                    foreach (XElement item in resources.Elements().SelectMany(e => e.DescendantsAndSelf()))
                    {
                        string kind = item.Name.LocalName;
                        // スタイル・テンプレートの中は、別の仕組み（IStyleConnector）で接続されるため、対象外
                        if (item.AncestorsAndSelf().Any(a => a != resources && (a.Name.LocalName == "Style" || a.Name.LocalName.EndsWith("Template", StringComparison.Ordinal)))) continue;
                        if (item.Attribute(x + "Name") != null || item.Attributes().Any(a => EventAttributes.Contains(a.Name.LocalName) && a.Value.Length > 0 && !a.Value.StartsWith("{", StringComparison.Ordinal)))
                        {
                            offenders.Add(Path.GetFileName(file) + ": <" + kind + ">");
                        }
                    }
                }
            }

            Assert.True(offenders.Count == 0, "リソースの中に名前・イベントを持つ部品がある: " + string.Join(", ", offenders.Distinct()));
        }
    }
}
