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
    /// 画面の文言が、英語・日本語の両方のリソースで揃っていることを守る。
    /// 新しい文言を足すときは、Strings.resx と Strings.ja.resx（または Resources の対）へ同じキーで書き、
    /// XAML には文字を直接書かず {lex:Loc キー} を使う。
    /// 日本語でも英語のまま残してよい語（キー名・単位・製品名・軸の略号）は、下の許可リストへ足す。
    /// </summary>
    public class LocalizationResourceTests
    {
        private static readonly HashSet<string> KeptEnglishKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "HidNinja",
            "XInputChecker",
            "HidHide",
            "Copycat",
            "ID",
            "MainWin.OneEuroBeta",
            "MainWin.OneEuroMinCutoff",
            "Appdata",
            "OK",
            "About_ViGEmBus",
            "About_FakerInput",
            "About_CRC32",
            "Ui_1Filter",
            "About_HNotifyIcon",
            "BindingWindow_Esc",
            "BindingWindow_BkSpace",
            "BindingWindow_Tab",
            "BindingWindow_Caps",
            "BindingWindow_Enter",
            "BindingWindow_LShift",
            "BindingWindow_RShift",
            "BindingWindow_LCtrl",
            "BindingWindow_LWin",
            "BindingWindow_LAlt",
            "BindingWindow_RAlt",
            "BindingWindow_RWin",
            "BindingWindow_RCtrl",
            "BindingWindow_Prt",
            "BindingWindow_Scl",
            "BindingWindow_Brk",
            "BindingWindow_Ins",
            "BindingWindow_Home",
            "BindingWindow_PgUp",
            "BindingWindow_Del",
            "BindingWindow_End",
            "BindingWindow_PgDown",
            "BindingWindow_NumLk",
            "ControllerReadingsControl_LXI",
            "ControllerReadingsControl_LXO",
            "ControllerReadingsControl_LYI",
            "ControllerReadingsControl_LYO",
            "ControllerReadingsControl_RXI",
            "ControllerReadingsControl_RXO",
            "ControllerReadingsControl_RYI",
            "ControllerReadingsControl_RYO",
            "ControllerReadingsControl_TX",
            "ControllerReadingsControl_TY",
            "GeneralSettingsPanel_Max1Ms",
            "GeneralSettingsPanel_1000Hz1Ms",
            "GeneralSettingsPanel_500Hz2Ms",
            "GeneralSettingsPanel_333Hz3Ms",
            "GeneralSettingsPanel_250Hz4Ms",
            "GeneralSettingsPanel_200Hz5Ms",
            "GeneralSettingsPanel_166Hz6Ms",
            "GeneralSettingsPanel_142Hz7Ms",
            "GeneralSettingsPanel_125Hz8Ms",
            "GeneralSettingsPanel_111Hz9Ms",
            "GeneralSettingsPanel_100Hz10Ms",
            "GeneralSettingsPanel_90Hz11Ms",
            "GeneralSettingsPanel_83Hz12Ms",
            "GeneralSettingsPanel_76Hz13Ms",
            "GeneralSettingsPanel_71Hz14Ms",
            "GeneralSettingsPanel_66Hz15Ms",
            "GeneralSettingsPanel_62Hz16Ms",
            "MainWindow_DS4Windows",
            "Ui_Ms",
            "OutputSettingsPanel_Xbox360",
            "OutputSettingsPanel_DualShock4",
            "PluginOutDevWindow_DS4",
            "ProfileEditor_DPad",
            "ProfileEditor_WASD",
            "StickSettingsPanel_LS",
            "StickSettingsPanel_RS",
            "StickSideSettingsView_Units",
            "TouchpadSettingsPanel_PS"
        };

        /// <summary>ファイル参照のリソース（アイコン）は、訳す対象ではない。</summary>
        private static readonly HashSet<string> NonTextKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "DS4", "DS4W", "DS4W___White", "Name1", "Bitmap1", "Icon1"
        };

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
        public void EveryEnglishKey_HasAJapaneseValue(string enPath, string jaPath)
        {
            var en = Load(enPath);
            var ja = Load(jaPath);

            var missing = en.Keys.Where(k => !NonTextKeys.Contains(k) && !ja.ContainsKey(k)).ToList();

            Assert.True(missing.Count == 0, "日本語のキーがない: " + string.Join(", ", missing));
        }

        [Theory]
        [MemberData(nameof(Dictionaries))]
        public void JapaneseValues_AreJapanese_UnlessListedAsKeptEnglish(string enPath, string jaPath)
        {
            var en = Load(enPath);
            var ja = Load(jaPath);
            var japanese = new Regex(@"[぀-ヿ㐀-鿿＀-￯]");

            var untranslated = ja
                .Where(p => en.ContainsKey(p.Key) && !NonTextKeys.Contains(p.Key) && !KeptEnglishKeys.Contains(p.Key))
                .Where(p => Regex.IsMatch(p.Value, "[A-Za-z]{2,}") && !japanese.IsMatch(p.Value))
                .Select(p => p.Key)
                .ToList();

            Assert.True(untranslated.Count == 0, "日本語の値が英語のまま: " + string.Join(", ", untranslated));
        }

        [Fact]
        public void KeptEnglishKeys_AreStillEnglish()
        {
            var en = Load("DS4Windows/Translations/Strings.resx");
            var ja = Load("DS4Windows/Translations/Strings.ja.resx");

            var stale = KeptEnglishKeys.Where(k => ja.TryGetValue(k, out var v) && en.TryGetValue(k, out var e) && v != e).ToList();

            Assert.True(stale.Count == 0, "許可リストにあるが訳してある（リストから外す）: " + string.Join(", ", stale));
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
