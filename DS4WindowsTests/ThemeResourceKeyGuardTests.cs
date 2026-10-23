using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// テーマの ResourceDictionary（DarkTheme.xaml・DefaultTheme.xaml）に、同じキー（x:Key、または暗黙のスタイルの TargetType）が
    /// 2 回登録されていないことの回帰ガード。重複すると、起動時に ResourceDictionary.SetKeys の ArgumentException（Hashtable.Insert）で落ちる。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class ThemeResourceKeyGuardTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        private readonly ITestOutputHelper _output;

        public ThemeResourceKeyGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("DS4Windows/DS4Forms/Themes/DarkTheme.xaml")]
        [InlineData("DS4Windows/DS4Forms/Themes/DefaultTheme.xaml")]
        public void ThemeDictionary_HasNoDuplicateKeys(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return;
            }

            XElement root = XDocument.Load(path).Root;
            var keys = new List<string>();
            foreach (XElement element in root.Elements())
            {
                string key = (string)element.Attribute(Xaml + "Key");
                if (key == null)
                {
                    string targetType = (string)element.Attribute("TargetType");
                    if (targetType != null)
                    {
                        key = "implicit:" + targetType;
                    }
                }

                if (key != null)
                {
                    keys.Add(key);
                }
            }

            string[] duplicates = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.True(duplicates.Length == 0, "キーが重複している: " + string.Join(", ", duplicates));
        }

        [Fact]
        public void DarkTheme_DefinesEveryColorKeyThatTheLightThemeDefines()
        {
            string dark = SourceFileLocator.Find("DS4Windows/DS4Forms/Themes/DarkTheme.xaml");
            string light = SourceFileLocator.Find("DS4Windows/DS4Forms/Themes/DefaultTheme.xaml");
            if (dark == null || light == null)
            {
                _output.WriteLine("テーマの XAML が見つからないため検査を省略した。");
                return;
            }

            HashSet<string> BrushKeys(string path) => new HashSet<string>(
                XDocument.Load(path).Root.Elements()
                    .Where(e => e.Name.LocalName == "SolidColorBrush")
                    .Select(e => (string)e.Attribute(Xaml + "Key"))
                    .Where(k => k != null));

            // DynamicResource で参照するブラシは、どちらのテーマにも必要（片方に無いと、その画面は既定の色になる）
            string[] missingInDark = BrushKeys(light).Except(BrushKeys(dark)).ToArray();
            Assert.True(missingInDark.Length == 0, "ダークテーマに無いブラシ: " + string.Join(", ", missingInDark));
        }
    }
}
