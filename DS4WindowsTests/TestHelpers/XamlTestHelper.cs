using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// XAML のレイアウトを検査するテストのための共通ヘルパー（Phase11 W1）。
    /// XAML を XML として読み、要素・属性の構造で検査する。空白・改行・属性の順序・インデント・「/&gt;」の前の空白は、
    /// XAML の整形（フォーマッター）で変わるため、文字列照合（正規表現や <c>IndexOf</c>）には使わない。
    /// ソースファイルが見つからない環境では <see cref="Load"/> が null を返す（呼び出し側は検査を省略して合格とする）。
    /// </summary>
    internal static class XamlTestHelper
    {
        /// <summary>XAML の言語名前空間（<c>x:Name</c>、<c>x:Key</c> の <c>x</c>）。</summary>
        public static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static readonly Regex StaticResourceReference = new Regex(@"\{StaticResource\s+(?<key>[^\s}]+)\s*\}", RegexOptions.Compiled);

        /// <param name="relativePath">リポジトリのルートからの相対パス（例: DS4Windows/UI/ProfileEditor.xaml）。</param>
        public static XDocument Load(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            return path == null ? null : XDocument.Load(path);
        }

        public static XDocument Parse(string xaml)
        {
            return XDocument.Parse(xaml);
        }

        /// <summary><c>x:Name</c>（なければ <c>Name</c>）。どちらもなければ null。</summary>
        public static string NameOf(XElement element)
        {
            return (string)element.Attribute(XamlNs + "Name") ?? (string)element.Attribute("Name");
        }

        /// <summary><c>x:Key</c>。なければ null。</summary>
        public static string KeyOf(XElement element)
        {
            return (string)element.Attribute(XamlNs + "Key");
        }

        /// <summary>名前空間のない属性（<c>Header</c>、<c>Grid.Row</c> など）の値。なければ null。</summary>
        public static string Attr(XElement element, string attributeName)
        {
            return (string)element.Attribute(attributeName);
        }

        /// <summary>指定の名前の要素を 1 つだけ返す。見つからない、または複数あるときは失敗する。</summary>
        public static XElement FindByName(XDocument document, string name)
        {
            XElement[] found = document.Descendants().Where(e => NameOf(e) == name).ToArray();
            Assert.True(found.Length == 1, name + " が" + (found.Length == 0 ? "見つからない" : "複数ある（" + found.Length + "）"));
            return found[0];
        }

        public static bool HasName(XElement scope, string name)
        {
            return scope.DescendantsAndSelf().Any(e => NameOf(e) == name);
        }

        /// <summary>直下の子要素のうち、指定の要素名のもの（<c>TabControl.Resources</c> のようなプロパティ要素は含まない）。</summary>
        public static IEnumerable<XElement> ChildElements(XElement parent, string localName)
        {
            return parent.Elements().Where(e => e.Name.LocalName == localName);
        }

        /// <summary>直下のプロパティ要素（例: <c>Grid.RowDefinitions</c>）。</summary>
        public static XElement PropertyElement(XElement owner, string propertyName)
        {
            return owner.Elements().SingleOrDefault(e => e.Name.LocalName == owner.Name.LocalName + "." + propertyName);
        }

        /// <summary>
        /// タブの識別名。<c>x:Name</c> があればそれ、なければ <c>Header</c>。
        /// <c>Header="{lex:Loc Controls}"</c> の形式は、キー（<c>Controls</c>）だけを返す。
        /// </summary>
        public static string Describe(XElement tabItem)
        {
            string name = NameOf(tabItem);
            if (name != null)
            {
                return name;
            }

            string header = Attr(tabItem, "Header") ?? string.Empty;
            Match loc = Regex.Match(header, @"^\{lex:Loc\s+(?<key>\w+)\}$");
            return loc.Success ? loc.Groups["key"].Value : header;
        }

        /// <summary>要素とその子孫のすべての属性値。</summary>
        public static IEnumerable<string> AllAttributeValues(XElement scope)
        {
            return scope.DescendantsAndSelf().SelectMany(e => e.Attributes()).Select(a => a.Value);
        }

        /// <summary>要素とその子孫の属性値が参照している <c>{StaticResource Key}</c> のキー（重複なし、序数順）。</summary>
        public static string[] StaticResourceKeys(XElement scope)
        {
            return AllAttributeValues(scope)
                .SelectMany(v => StaticResourceReference.Matches(v).Cast<Match>().Select(m => m.Groups["key"].Value))
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>要素自身の <c>&lt;要素名.Resources&gt;</c> に定義されたリソースの <c>x:Key</c>。</summary>
        public static string[] OwnResourceKeys(XElement owner)
        {
            XElement resources = PropertyElement(owner, "Resources");
            return resources == null
                ? new string[0]
                : resources.Elements().Select(KeyOf).Where(k => k != null).ToArray();
        }
    }
}
