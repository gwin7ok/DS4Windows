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
    /// 画面の文言のリソースの整合を守る。XAML には文字を直接書かず {lex:Loc キー} を使い、キーは英語のリソースに置く。
    /// 日本語のリソースにキーがなければ英語の値が使われる（.NET の既定の動作）ため、訳し忘れ自体は検査しない。
    /// 英語のままでよい語（キー名・単位・製品名・軸の略号）は、日本語側に書かない。
    /// </summary>
    public class LocalizationResourceTests
    {
        /// <summary>XAML に直接書いてよい表示文字（記号だけのもの）。</summary>
        private static readonly HashSet<string> XamlLiteralAllowList = new HashSet<string>(StringComparer.Ordinal)
        {
            "<"
        };

        private static Dictionary<string, string> Load(string relativePath)
        {
            XDocument doc = XDocument.Parse(SourceText.Read(relativePath));
            return doc.Root.Elements("data").ToDictionary(
                e => (string)e.Attribute("name"),
                e => (string)e.Element("value") ?? string.Empty,
                StringComparer.Ordinal);
        }

        public static IEnumerable<object[]> Dictionaries()
        {
            yield return new object[] { "DS4Windows/Translations/Strings.resx", "DS4Windows/Translations/Strings.ja.resx" };
            yield return new object[] { "DS4Windows/Properties/Resources.resx", "DS4Windows/Properties/Resources.ja.resx" };
        }

        [Theory]
        [MemberData(nameof(Dictionaries))]
        public void Placeholders_MatchBetweenEnglishAndJapanese(string enPath, string jaPath)
        {
            var en = Load(enPath);
            var ja = Load(jaPath);
            var token = new Regex(@"\{\d+\}|\*[a-z]+\*");

            var broken = ja
                .Where(p => en.ContainsKey(p.Key))
                .Where(p => !token.Matches(en[p.Key]).Select(m => m.Value).OrderBy(x => x)
                    .SequenceEqual(token.Matches(p.Value).Select(m => m.Value).OrderBy(x => x)))
                .Select(p => p.Key)
                .ToList();

            Assert.True(broken.Count == 0, "{0} や *number* の数・種類が違う: " + string.Join(", ", broken));
        }

        [Fact]
        public void XamlLocKeys_ExistInTheEnglishResources()
        {
            var keys = new HashSet<string>(Load("DS4Windows/Translations/Strings.resx").Keys, StringComparer.Ordinal);
            var resources = new HashSet<string>(Load("DS4Windows/Properties/Resources.resx").Keys, StringComparer.Ordinal);
            var loc = new Regex(@"lex:Loc\s+(?:Key=""|(?:(Strings|Resources):)?)([A-Za-z0-9_.]+)");

            var unknown = new List<string>();
            foreach (string file in XamlFiles())
            {
                string text = Regex.Replace(File.ReadAllText(file), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
                foreach (Match m in loc.Matches(text))
                {
                    var set = m.Groups[1].Value == "Resources" ? resources : keys;
                    if (!set.Contains(m.Groups[2].Value) && !keys.Contains(m.Groups[2].Value) && !resources.Contains(m.Groups[2].Value))
                    {
                        unknown.Add(Path.GetFileName(file) + ":" + m.Groups[2].Value);
                    }
                }
            }

            Assert.True(unknown.Count == 0, "リソースにないキー: " + string.Join(", ", unknown));
        }

        [Fact]
        public void Xaml_HasNoHardcodedDisplayText()
        {
            var attrs = new Regex(@"\s(Content|Header|Text|Title|ToolTip)=""([^""{][^""]*)""");
            var offenders = new List<string>();
            foreach (string file in XamlFiles())
            {
                foreach (Match m in attrs.Matches(Regex.Replace(File.ReadAllText(file), "<!--.*?-->", string.Empty, RegexOptions.Singleline)))
                {
                    string v = System.Net.WebUtility.HtmlDecode(m.Groups[2].Value);
                    if (Regex.IsMatch(v, "[A-Za-z]{2,}") && !XamlLiteralAllowList.Contains(v))
                    {
                        offenders.Add(Path.GetFileName(file) + ":" + m.Groups[1].Value + "=" + v);
                    }
                }
            }

            Assert.True(offenders.Count == 0, "XAML に直接書かれた文言（{lex:Loc キー} にする）: " + string.Join("; ", offenders));
        }

        private static IEnumerable<string> XamlFiles()
        {
            string root = Path.Combine(SourceFileLocator.RepoRoot(), "DS4Windows");
            return Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "Themes" + Path.DirectorySeparatorChar));
        }
    }
}
