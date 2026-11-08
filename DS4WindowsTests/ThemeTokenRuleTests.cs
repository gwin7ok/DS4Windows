using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// テーマの色の規則を XAML で検査する。
    /// 1) ブラシのキーは <c>…Brush</c>（または <c>Xxx.Yyy</c> の区切り）で、<c>…Color</c> で終わらない（Color 型は <c>…Color</c>）。
    /// 2) 両テーマの色（ブラシ）のキーは 1 対 1。ただし <c>Template.*</c> は Dark が移植した標準テンプレート専用で、Default は持たなくてよい。
    /// 3) 色の直書き（#RRGGBB）は、テーマの <c>SolidColorBrush</c>／<c>Color</c> の定義にだけ置く。画面・スタイルは名前で参照する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class ThemeTokenRuleTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly Regex HexLiteral = new Regex(@"#[0-9A-Fa-f]{6,8}\b", RegexOptions.Compiled);

        private const string DarkPath = "DS4Windows/UI/Themes/DarkTheme.xaml";
        private const string DefaultPath = "DS4Windows/UI/Themes/DefaultTheme.xaml";

        private static XElement Load(string relative)
        {
            string path = SourceFileLocator.Find(relative);
            return path == null ? null : XDocument.Load(path).Root;
        }

        private static IEnumerable<(string Type, string Key)> Definitions(XElement root, string typeName) =>
            root.Elements().Where(e => e.Name.LocalName == typeName)
                .Select(e => (typeName, (string)e.Attribute(Xaml + "Key"))).Where(x => x.Item2 != null);

        [Theory]
        [InlineData(DarkPath)]
        [InlineData(DefaultPath)]
        public void BrushKeys_DoNotEndWithColor(string relative)
        {
            XElement root = Load(relative);
            if (root == null) return;

            string[] bad = Definitions(root, "SolidColorBrush").Select(d => d.Key).Where(k => k.EndsWith("Color", StringComparison.Ordinal)).ToArray();
            Assert.True(bad.Length == 0, "Color で終わるブラシ: " + string.Join(", ", bad));

            string[] badColors = Definitions(root, "Color").Select(d => d.Key).Where(k => k.EndsWith("Brush", StringComparison.Ordinal)).ToArray();
            Assert.True(badColors.Length == 0, "Brush で終わる Color: " + string.Join(", ", badColors));
        }

        [Fact]
        public void BrushKeys_AreTheSameInBothThemes_ExceptTheDarkTemplatePalette()
        {
            XElement dark = Load(DarkPath);
            XElement light = Load(DefaultPath);
            if (dark == null || light == null) return;

            HashSet<string> Keys(XElement root) => new HashSet<string>(
                Definitions(root, "SolidColorBrush").Select(d => d.Key).Where(k => !k.StartsWith("Template.", StringComparison.Ordinal)));

            HashSet<string> d = Keys(dark);
            HashSet<string> l = Keys(light);
            Assert.True(d.SetEquals(l),
                "Dark のみ: " + string.Join(", ", d.Except(l)) + " / Default のみ: " + string.Join(", ", l.Except(d)));
        }

        [Fact]
        public void ColorLiterals_AppearOnlyInThemeColorDefinitions()
        {
            string themeRoot = SourceFileLocator.Find(DarkPath);
            if (themeRoot == null) return;
            string uiDir = Path.GetDirectoryName(Path.GetDirectoryName(themeRoot));

            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(uiDir, "*.xaml", SearchOption.AllDirectories))
            {
                XElement root = XDocument.Load(file).Root;
                bool isTheme = Path.GetDirectoryName(file).EndsWith("Themes", StringComparison.Ordinal);
                foreach (XElement element in root.DescendantsAndSelf())
                {
                    // テーマの直下の SolidColorBrush／Color の定義だけは、値を書いてよい
                    bool isDefinition = isTheme && element.Parent == root
                        && (element.Name.LocalName == "SolidColorBrush" || element.Name.LocalName == "Color");
                    if (isDefinition) continue;

                    foreach (XAttribute attribute in element.Attributes())
                    {
                        // バインディングの FallbackValue（実行時に値が来るまでの代替）は、画面の配色ではないので対象外
                        if (attribute.Value.Contains("FallbackValue")) continue;
                        if (HexLiteral.IsMatch(attribute.Value))
                            offenders.Add(Path.GetFileName(file) + ": " + element.Name.LocalName + "." + attribute.Name.LocalName + "=" + attribute.Value);
                    }
                }
            }

            Assert.True(offenders.Count == 0, "色の直書き:\n" + string.Join("\n", offenders));
        }

        /// <summary>
        /// テーマを実際に読み込めること。StaticResource の参照先がない（定義順が前後している、キーを改名し忘れた）と、
        /// 実行時にだけ XamlParseException になりビルドでは気づけないため、ここで確かめる。
        /// </summary>
        [Theory]
        [InlineData("DarkTheme")]
        [InlineData("DefaultTheme")]
        public void ThemeDictionary_LoadsAndDefinesTheTokensThatScreensUse(string theme)
        {
            System.Exception failure = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var uri = new Uri($"/DS4Windows;component/UI/Themes/{theme}.xaml", UriKind.Relative);
                    var dictionary = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(uri);
                    foreach (string key in new[] { "ForegroundBrush", "BackgroundBrush", "AccentBrush", "Readings.AlertFill", "Notification.Background", "KeyHighlight.Foreground", "Template.Document.Border" })
                    {
                        Assert.True(dictionary.Contains(key), key + " が " + theme + " にない");
                    }
                }
                catch (System.Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw new InvalidOperationException(theme + " を読み込めない: " + failure, failure);
        }

        /// <summary>
        /// 数値の表示・入力の寸法のスタイル（ReadingValueLabel*、NumberInput*）は、両テーマに同じ寸法で持つ。
        /// Dark だけ、または片方だけ違う幅になると、数値の見切れがテーマによって起きる。
        /// </summary>
        [Fact]
        public void NumberSizeStyles_HaveTheSameDimensionsInBothThemes()
        {
            XElement dark = Load(DarkPath);
            XElement light = Load(DefaultPath);
            if (dark == null || light == null) return;

            Dictionary<string, string> Sizes(XElement root) => root.Elements().Where(e => e.Name.LocalName == "Style")
                .Where(e => ((string)e.Attribute(Xaml + "Key") ?? "").StartsWith("ReadingValueLabel", StringComparison.Ordinal)
                         || ((string)e.Attribute(Xaml + "Key") ?? "").StartsWith("NumberInput", StringComparison.Ordinal))
                .ToDictionary(e => (string)e.Attribute(Xaml + "Key"),
                    e => string.Join(";", e.Elements().Where(c => c.Name.LocalName == "Setter")
                        .Select(c => (string)c.Attribute("Property") + "=" + (string)c.Attribute("Value"))));

            Dictionary<string, string> d = Sizes(dark);
            Dictionary<string, string> l = Sizes(light);
            Assert.NotEmpty(d);
            Assert.Equal(d.OrderBy(x => x.Key), l.OrderBy(x => x.Key));
        }

        /// <summary>
        /// xctk の UpDown の最小幅は、共有スタイル（NumberInput*）で指定する。幅の数字を各所に書くと、画面ごとにばらつく。
        /// BindingWindow は IntegerUpDown の暗黙のスタイルを持つため、対象外。
        /// </summary>
        [Fact]
        public void UpDownMinWidth_UsesTheSharedStyles()
        {
            string themeFile = SourceFileLocator.Find(DarkPath);
            if (themeFile == null) return;
            string uiDir = Path.GetDirectoryName(Path.GetDirectoryName(themeFile));

            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(uiDir, "*.xaml", SearchOption.TopDirectoryOnly))
            {
                if (file.EndsWith("BindingWindow.xaml", StringComparison.Ordinal)) continue;
                foreach (XElement e in XDocument.Load(file).Root.Descendants().Where(x => x.Name.LocalName.EndsWith("UpDown", StringComparison.Ordinal)))
                {
                    string width = (string)e.Attribute("MinWidth");
                    if (width is "50" or "60" or "80" or "100" && e.Attribute("Style") == null)
                        offenders.Add(Path.GetFileName(file) + ": " + e.Name.LocalName + " MinWidth=" + width);
                }
            }

            Assert.True(offenders.Count == 0, "共有スタイルを使っていない UpDown:\n" + string.Join("\n", offenders));
        }
    }
}
