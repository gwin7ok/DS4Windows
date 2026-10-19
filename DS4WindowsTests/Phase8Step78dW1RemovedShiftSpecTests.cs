using System;
using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8d-W1（Phase8-Step7-8d-Plan.md §4）: 廃止された「シフト」機能の設定の、保持（<see cref="RemovedShiftSettings"/>）と、編集画面の警告の判定
    /// （<see cref="RemovedShiftNotice"/>）の仕様のテスト。画面・読込・保存への配線は W2・W3。
    /// </summary>
    public class Phase8Step78dW1RemovedShiftSpecTests
    {
        private const string WithContent =
            "<ShiftControl><Button><Cross Trigger=\"12\">A</Cross></Button><Macro><Circle Trigger=\"1\">65/65</Circle></Macro></ShiftControl>";

        private static XmlNode Node(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        private sealed class FakePrompt : IRemovedShiftPrompt
        {
            private readonly RemovedShiftChoice _choice;
            public int Asked;
            public string LastProfile;

            public FakePrompt(RemovedShiftChoice choice) { _choice = choice; }

            public RemovedShiftChoice Ask(string profileName)
            {
                Asked++;
                LastProfile = profileName;
                return _choice;
            }
        }

        // ---- RemovedShiftSettings ----

        [Fact]
        public void Capture_KeepsTheNodeAsItIs_WhenItHasItems()
        {
            var settings = new RemovedShiftSettings();

            Assert.True(settings.Capture(Node(WithContent)));

            Assert.True(settings.HasContent);
            Assert.Equal(WithContent, settings.OuterXml);
        }

        [Theory]
        [InlineData("<ShiftControl />")]
        [InlineData("<ShiftControl></ShiftControl>")]
        [InlineData("<ShiftControl><Button /></ShiftControl>")]   // 項目がない（グループだけ）。保持しない
        public void Capture_KeepsNothing_WhenThereAreNoItems(string xml)
        {
            var settings = new RemovedShiftSettings();

            Assert.False(settings.Capture(Node(xml)));

            Assert.False(settings.HasContent);
            Assert.Null(settings.OuterXml);
        }

        [Fact]
        public void Capture_Null_ClearsThePreviousContent()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));

            Assert.False(settings.Capture(null));

            Assert.False(settings.HasContent);
        }

        [Fact]
        public void Capture_ReplacesThePreviousContent()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));
            const string other = "<ShiftControl><Key><Square Trigger=\"3\">66</Square></Key></ShiftControl>";

            settings.Capture(Node(other));

            Assert.Equal(other, settings.OuterXml);
        }

        [Fact]
        public void Clear_DropsTheContent()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));

            settings.Clear();

            Assert.False(settings.HasContent);
            Assert.Null(settings.OuterXml);
        }

        [Fact]
        public void AppendTo_WritesTheKeptContentUnchanged_IncludingTheAttributes()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));
            var document = new XmlDocument();
            XmlElement root = document.CreateElement("DS4Windows");
            document.AppendChild(root);

            settings.AppendTo(document, root);

            Assert.Single(root.ChildNodes);
            Assert.Equal(WithContent, root.FirstChild.OuterXml);
            Assert.Equal("12", root.SelectSingleNode("ShiftControl/Button/Cross").Attributes["Trigger"].Value);
        }

        [Fact]
        public void AppendTo_WritesAnEmptyElement_WhenNothingIsKept()
        {
            var settings = new RemovedShiftSettings();
            var document = new XmlDocument();
            XmlElement root = document.CreateElement("DS4Windows");
            document.AppendChild(root);

            settings.AppendTo(document, root);

            Assert.Equal("<ShiftControl />", root.FirstChild.OuterXml);   // 従来の保存と同じ形（シフトの設定がないときも、空の要素を書き出していた）
        }

        [Fact]
        public void AppendTo_AfterClear_WritesAnEmptyElement()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));
            settings.Clear();
            var document = new XmlDocument();
            XmlElement root = document.CreateElement("DS4Windows");
            document.AppendChild(root);

            settings.AppendTo(document, root);

            Assert.Equal("<ShiftControl />", root.FirstChild.OuterXml);
        }

        [Fact]
        public void AppendTo_ValidatesItsArguments()
        {
            var settings = new RemovedShiftSettings();
            var document = new XmlDocument();
            XmlElement root = document.CreateElement("DS4Windows");

            Assert.Throws<ArgumentNullException>(() => settings.AppendTo(null, root));
            Assert.Throws<ArgumentNullException>(() => settings.AppendTo(document, null));
        }

        // ---- RemovedShiftNotice（編集画面の警告の判定）----

        [Fact]
        public void NoContent_NoWarning_AndThePromptIsNotAsked()
        {
            var prompt = new FakePrompt(RemovedShiftChoice.Delete);

            Assert.Equal(RemovedShiftOutcome.NoNotice, RemovedShiftNotice.Apply(new RemovedShiftSettings(), "p", prompt));
            Assert.Equal(RemovedShiftOutcome.NoNotice, RemovedShiftNotice.Apply(null, "p", prompt));
            Assert.Equal(0, prompt.Asked);
        }

        [Fact]
        public void Delete_DropsTheContent_AndThenNothingIsAskedAgain()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));
            var prompt = new FakePrompt(RemovedShiftChoice.Delete);

            Assert.Equal(RemovedShiftOutcome.Deleted, RemovedShiftNotice.Apply(settings, "MyProfile", prompt));

            Assert.False(settings.HasContent);
            Assert.Equal("MyProfile", prompt.LastProfile);
            Assert.Equal(RemovedShiftOutcome.NoNotice, RemovedShiftNotice.Apply(settings, "MyProfile", prompt));
            Assert.Equal(1, prompt.Asked);
        }

        [Fact]
        public void Keep_LeavesTheContent_AndTheWarningComesBackEveryTime()
        {
            var settings = new RemovedShiftSettings();
            settings.Capture(Node(WithContent));
            var prompt = new FakePrompt(RemovedShiftChoice.Keep);

            for (int i = 1; i <= 3; i++)
            {
                Assert.Equal(RemovedShiftOutcome.Kept, RemovedShiftNotice.Apply(settings, "MyProfile", prompt));
                Assert.True(settings.HasContent);
                Assert.Equal(WithContent, settings.OuterXml);
                Assert.Equal(i, prompt.Asked);   // 選択を記憶せず、開くたびに警告
            }
        }

        [Fact]
        public void AMissingPrompt_ThrowsOnlyWhenAWarningIsNeeded()
        {
            var settings = new RemovedShiftSettings();
            Assert.Equal(RemovedShiftOutcome.NoNotice, RemovedShiftNotice.Apply(settings, "p", null));

            settings.Capture(Node(WithContent));
            Assert.Throws<ArgumentNullException>(() => RemovedShiftNotice.Apply(settings, "p", null));
        }
    }
}
