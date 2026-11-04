using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W1: <see cref="XamlTestHelper"/> が、XAML の書式（空白・改行・属性の順序・インデント・「/&gt;」の前の空白）に依存せず、
    /// 構造だけで読めること。XAML の整形を適用しても、これを使うテストが壊れないことの根拠。
    /// </summary>
    public class XamlTestHelperTests
    {
        private const string Header = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
            + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" "
            + "xmlns:lex=\"http://wpflocalizeextension.codeplex.com\"";

        // 同じ構造を、書式だけ変えた 2 通り（コンパクト／整形し直し：属性を折り返し、順序を入れ替え、インデントを変更）
        private static readonly string Compact =
            "<UserControl " + Header + ">"
            + "<UserControl.Resources><Thickness x:Key=\"rootMargin\">1</Thickness></UserControl.Resources>"
            + "<TabControl x:Name=\"pane\"><TabControl.Resources><Thickness x:Key=\"unrelated\">1</Thickness></TabControl.Resources>"
            + "<TabItem Header=\"{lex:Loc Controls}\"/>"
            + "<TabItem x:Name=\"second\" Header=\"Second\"><TabItem.Resources><Thickness x:Key=\"spaceMargin\">2</Thickness></TabItem.Resources>"
            + "<Button Margin=\"{StaticResource spaceMargin}\" Click=\"Foo_Click\"/><TextBox Margin=\"{StaticResource rootMargin}\"/></TabItem>"
            + "</TabControl></UserControl>";

        private static readonly string Reformatted =
            "<UserControl\n    " + Header + "\n    >\n"
            + "\t\t<UserControl.Resources>\n\t\t\t<Thickness\n\t\t\t\tx:Key=\"rootMargin\"  >1</Thickness>\n\t\t</UserControl.Resources>\n"
            + "        <TabControl\n            x:Name=\"pane\"\n        >\n"
            + "            <TabControl.Resources>\n                <Thickness x:Key=\"unrelated\">1</Thickness>\n            </TabControl.Resources>\n"
            + "            <TabItem   Header=\"{lex:Loc Controls}\"   />\n"
            + "            <TabItem\n                Header=\"Second\"\n                x:Name=\"second\">\n"
            + "                <TabItem.Resources>\n                    <Thickness x:Key=\"spaceMargin\">2</Thickness>\n                </TabItem.Resources>\n"
            + "                <Button\n                    Click=\"Foo_Click\"\n                    Margin=\"{StaticResource spaceMargin}\" />\n"
            + "                <TextBox Margin=\"{StaticResource rootMargin}\" />\n            </TabItem>\n        </TabControl>\n</UserControl>";

        public static TheoryData<string> Variants => new TheoryData<string> { Compact, Reformatted };

        [Theory]
        [MemberData(nameof(Variants))]
        public void TabsAreListedByNameOrHeaderKey_InDocumentOrder(string xaml)
        {
            XDocument doc = XamlTestHelper.Parse(xaml);
            XElement pane = XamlTestHelper.FindByName(doc, "pane");

            string[] tabs = XamlTestHelper.ChildElements(pane, "TabItem").Select(XamlTestHelper.Describe).ToArray();

            Assert.Equal(new[] { "Controls", "second" }, tabs);
        }

        [Theory]
        [MemberData(nameof(Variants))]
        public void StaticResourceKeys_AreCollectedFromAttributeValues_DistinctAndSorted(string xaml)
        {
            XDocument doc = XamlTestHelper.Parse(xaml);

            Assert.Equal(new[] { "rootMargin", "spaceMargin" }, XamlTestHelper.StaticResourceKeys(XamlTestHelper.FindByName(doc, "second")));
        }

        [Theory]
        [MemberData(nameof(Variants))]
        public void OwnResourceKeys_ReadOnlyTheOwnersOwnResources(string xaml)
        {
            XDocument doc = XamlTestHelper.Parse(xaml);

            Assert.Equal(new[] { "spaceMargin" }, XamlTestHelper.OwnResourceKeys(XamlTestHelper.FindByName(doc, "second")));
            Assert.Equal(new[] { "unrelated" }, XamlTestHelper.OwnResourceKeys(XamlTestHelper.FindByName(doc, "pane")));
            Assert.Equal(new[] { "rootMargin" }, XamlTestHelper.OwnResourceKeys(doc.Root));
        }

        [Theory]
        [MemberData(nameof(Variants))]
        public void AttributeValues_FindAHandlerNameWhereverItIsWritten(string xaml)
        {
            XDocument doc = XamlTestHelper.Parse(xaml);

            Assert.Contains("Foo_Click", XamlTestHelper.AllAttributeValues(doc.Root));
            Assert.DoesNotContain("Bar_Click", XamlTestHelper.AllAttributeValues(doc.Root));
        }

        [Fact]
        public void FindByName_FailsWhenTheNameIsMissing()
        {
            XDocument doc = XamlTestHelper.Parse(Compact);

            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => XamlTestHelper.FindByName(doc, "missing"));
        }

        [Fact]
        public void TheRealProfileEditorXaml_CanBeLoadedAsXml()
        {
            XDocument doc = XamlTestHelper.Load("DS4Windows/UI/ProfileEditor.xaml");
            if (doc == null)
            {
                return;
            }

            Assert.Equal("UserControl", doc.Root.Name.LocalName);
            Assert.NotNull(XamlTestHelper.FindByName(doc, "sidebarTabControl"));
        }
    }
}
