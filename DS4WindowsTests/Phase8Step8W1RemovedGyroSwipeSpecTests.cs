using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step8 8-0-W1（Phase8-Step8-Plan.md §5.1。Q2＝C、Q9＝A）: 廃止された「ジャイロスワイプ」機能の設定の、保持（<see cref="RemovedGyroSwipeSettings"/>）と、
    /// 編集画面の警告の判定（<see cref="RemovedGyroSwipeNotice"/>）の仕様のテスト。読込・保存・画面への配線は W2・W3。7.8d の Shift の W1 と対応する。
    /// </summary>
    public class Phase8Step8W1RemovedGyroSwipeSpecTests
    {
        private const string Defaults =
            "<GyroSwipeSettings><DeadZoneX>80</DeadZoneX><DeadZoneY>80</DeadZoneY><Triggers>-1</Triggers><TriggerCond>and</TriggerCond><TriggerTurns>True</TriggerTurns><XAxis>Yaw</XAxis><DelayTime>0</DelayTime></GyroSwipeSettings>";

        private const string Changed =
            "<GyroSwipeSettings><DeadZoneX>120</DeadZoneX><DeadZoneY>80</DeadZoneY><Triggers>4,5</Triggers><TriggerCond>or</TriggerCond><TriggerTurns>False</TriggerTurns><XAxis>Roll</XAxis><DelayTime>30</DelayTime></GyroSwipeSettings>";

        private static XmlNode Node(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        private sealed class FakePrompt : IRemovedGyroSwipePrompt
        {
            private readonly RemovedGyroSwipeChoice _choice;
            public int Asked;
            public string LastProfile;

            public FakePrompt(RemovedGyroSwipeChoice choice) { _choice = choice; }

            public RemovedGyroSwipeChoice Ask(string profileName)
            {
                Asked++;
                LastProfile = profileName;
                return _choice;
            }
        }

        // ---- 設定 ----

        [Fact]
        public void CaptureSettings_KeepsNothing_WhenEverythingIsTheDefault()
        {
            var settings = new RemovedGyroSwipeSettings();

            Assert.False(settings.CaptureSettings(Node(Defaults)));
            Assert.False(settings.HasContent);
            Assert.Null(settings.SettingsOuterXml);
        }

        [Theory]
        [InlineData("<GyroSwipeSettings />")]   // 項目なし＝すべて既定
        [InlineData("<GyroSwipeSettings><DeadZoneX>080</DeadZoneX><TriggerTurns>true</TriggerTurns><TriggerCond>AND</TriggerCond><XAxis>yaw</XAxis></GyroSwipeSettings>")]   // 書き方の違い（数・大文字小文字）は同じ値
        public void CaptureSettings_TreatsMissingAndEquivalentValuesAsDefault(string xml)
        {
            var settings = new RemovedGyroSwipeSettings();

            Assert.False(settings.CaptureSettings(Node(xml)));
            Assert.False(settings.HasContent);
        }

        [Fact]
        public void CaptureSettings_Null_KeepsNothing()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureSettings(Node(Changed));

            Assert.False(settings.CaptureSettings(null));
            Assert.False(settings.HasContent);
        }

        [Theory]
        [InlineData("<GyroSwipeSettings><DeadZoneX>81</DeadZoneX></GyroSwipeSettings>")]
        [InlineData("<GyroSwipeSettings><Triggers>3</Triggers></GyroSwipeSettings>")]
        [InlineData("<GyroSwipeSettings><TriggerCond>or</TriggerCond></GyroSwipeSettings>")]
        [InlineData("<GyroSwipeSettings><XAxis>Roll</XAxis></GyroSwipeSettings>")]
        [InlineData("<GyroSwipeSettings><DelayTime>1</DelayTime></GyroSwipeSettings>")]
        [InlineData("<GyroSwipeSettings><Unknown>x</Unknown></GyroSwipeSettings>")]   // 知らない項目は、そのまま残す
        public void CaptureSettings_KeepsTheNodeAsItIs_WhenAnyValueDiffers(string xml)
        {
            var settings = new RemovedGyroSwipeSettings();

            Assert.True(settings.CaptureSettings(Node(xml)));
            Assert.True(settings.HasContent);
            Assert.Equal(xml, settings.SettingsOuterXml);
        }

        // ---- 割り当ての項目 ----

        [Fact]
        public void SwipeControlNames_AreTheFourDirections()
        {
            foreach (string name in new[] { "GyroSwipeLeft", "GyroSwipeRight", "GyroSwipeUp", "GyroSwipeDown" })
                Assert.True(RemovedGyroSwipeSettings.IsSwipeControlName(name));

            Assert.False(RemovedGyroSwipeSettings.IsSwipeControlName("SwipeLeft"));   // タッチパッドのスワイプは廃止しない
            Assert.False(RemovedGyroSwipeSettings.IsSwipeControlName("GyroXPos"));
            Assert.False(RemovedGyroSwipeSettings.IsSwipeControlName(null));
        }

        [Fact]
        public void CaptureControlEntry_MakesHasContentTrue()
        {
            var settings = new RemovedGyroSwipeSettings();

            settings.CaptureControlEntry("Button", Node("<GyroSwipeLeft>A</GyroSwipeLeft>"));

            Assert.True(settings.HasContent);
            Assert.Equal(1, settings.EntryCount);
            Assert.Null(settings.SettingsOuterXml);
        }

        [Fact]
        public void BeginCapture_DropsThePreviousContent()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureSettings(Node(Changed));
            settings.CaptureControlEntry("Key", Node("<GyroSwipeUp>65</GyroSwipeUp>"));

            settings.BeginCapture();

            Assert.False(settings.HasContent);
            Assert.Equal(0, settings.EntryCount);
        }

        // ---- 出力モード ----

        [Theory]
        [InlineData("DirectionalSwipe", true)]
        [InlineData("directionalswipe", true)]
        [InlineData(" DirectionalSwipe ", true)]
        [InlineData("Controls", false)]
        [InlineData("Passthru", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void CaptureGyroOutputMode_KeepsOnlyTheRemovedMode(string text, bool expected)
        {
            var settings = new RemovedGyroSwipeSettings();

            Assert.Equal(expected, settings.CaptureGyroOutputMode(text));
            Assert.Equal(expected, settings.HadDirectionalSwipeMode);
            Assert.Equal(expected, settings.HasContent);
        }

        [Fact]
        public void GyroOutputModeTextForSave_WritesTheRemovedModeAsItWas_WhileKept()
        {
            var settings = new RemovedGyroSwipeSettings();
            Assert.Equal("Controls", settings.GyroOutputModeTextForSave("Controls"));

            settings.CaptureGyroOutputMode("DirectionalSwipe");
            Assert.Equal("DirectionalSwipe", settings.GyroOutputModeTextForSave("None"));

            settings.Clear();
            Assert.Equal("None", settings.GyroOutputModeTextForSave("None"));   // 「削除」で捨てたあとは、現在のモード
        }

        [Fact]
        public void CaptureGyroOutputMode_IsReplacedByTheNextLoad()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureGyroOutputMode("DirectionalSwipe");

            settings.BeginCapture();
            settings.CaptureGyroOutputMode("Mouse");

            Assert.False(settings.HasContent);
        }

        // ---- 書き出し ----

        [Fact]
        public void AppendSettingsTo_WritesTheLegacyDefaultElement_WhenNothingIsKept()
        {
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("DS4Windows");
            doc.AppendChild(root);

            new RemovedGyroSwipeSettings().AppendSettingsTo(doc, root);

            Assert.Equal(Defaults, root.FirstChild.OuterXml);   // 廃止前に毎回書いていた形のまま
        }

        [Fact]
        public void AppendSettingsTo_WritesTheKeptNodeUnchanged()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureSettings(Node(Changed));
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("DS4Windows");
            doc.AppendChild(root);

            settings.AppendSettingsTo(doc, root);

            Assert.Equal(Changed, root.FirstChild.OuterXml);
        }

        [Fact]
        public void AppendControlsTo_PutsTheEntriesBackIntoTheirGroups_CreatingMissingGroups()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureControlEntry("Button", Node("<GyroSwipeLeft>A</GyroSwipeLeft>"));
            settings.CaptureControlEntry("Key", Node("<GyroSwipeUp>65</GyroSwipeUp>"));
            settings.CaptureControlEntry("KeyType", Node("<GyroSwipeUp>Toggle</GyroSwipeUp>"));
            settings.CaptureControlEntry("Extras", Node("<GyroSwipeDown>1,2,3,4,5,6,7,8,9</GyroSwipeDown>"));

            var doc = new XmlDocument();
            doc.LoadXml("<DS4Windows><Control><Button><Cross>B</Cross></Button></Control></DS4Windows>");
            XmlElement control = (XmlElement)doc.SelectSingleNode("/DS4Windows/Control");

            settings.AppendControlsTo(doc, control);

            Assert.Equal("<Cross>B</Cross><GyroSwipeLeft>A</GyroSwipeLeft>", doc.SelectSingleNode("/DS4Windows/Control/Button").InnerXml);   // 既にあるグループへは、末尾に足す
            Assert.Equal("65", doc.SelectSingleNode("/DS4Windows/Control/Key/GyroSwipeUp").InnerText);                                       // なければ作る
            Assert.Equal("Toggle", doc.SelectSingleNode("/DS4Windows/Control/KeyType/GyroSwipeUp").InnerText);
            Assert.Equal("1,2,3,4,5,6,7,8,9", doc.SelectSingleNode("/DS4Windows/Control/Extras/GyroSwipeDown").InnerText);
        }

        [Fact]
        public void AppendControlsTo_WithNothingKept_ChangesNothing()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<DS4Windows><Control><Button><Cross>B</Cross></Button></Control></DS4Windows>");
            XmlElement control = (XmlElement)doc.SelectSingleNode("/DS4Windows/Control");

            new RemovedGyroSwipeSettings().AppendControlsTo(doc, control);

            Assert.Equal("<Button><Cross>B</Cross></Button>", control.InnerXml);
        }

        [Fact]
        public void Append_ValidatesItsArguments()
        {
            var settings = new RemovedGyroSwipeSettings();
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("x");

            Assert.Throws<System.ArgumentNullException>(() => settings.AppendSettingsTo(null, root));
            Assert.Throws<System.ArgumentNullException>(() => settings.AppendSettingsTo(doc, null));
            Assert.Throws<System.ArgumentNullException>(() => settings.AppendControlsTo(null, root));
            Assert.Throws<System.ArgumentNullException>(() => settings.AppendControlsTo(doc, null));
            Assert.Throws<System.ArgumentNullException>(() => settings.CaptureControlEntry("Button", null));
            Assert.Throws<System.ArgumentException>(() => settings.CaptureControlEntry("", Node("<GyroSwipeLeft>A</GyroSwipeLeft>")));
        }

        // ---- 警告の判定 ----

        [Fact]
        public void NoContent_NoWarning_AndThePromptIsNotAsked()
        {
            var prompt = new FakePrompt(RemovedGyroSwipeChoice.Delete);

            Assert.Equal(RemovedGyroSwipeOutcome.NoNotice, RemovedGyroSwipeNotice.Apply(new RemovedGyroSwipeSettings(), "p", prompt));
            Assert.Equal(RemovedGyroSwipeOutcome.NoNotice, RemovedGyroSwipeNotice.Apply(null, "p", prompt));
            Assert.Equal(0, prompt.Asked);
        }

        [Fact]
        public void Delete_DropsBothSettingsAndEntries_AndThenNothingIsAskedAgain()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureSettings(Node(Changed));
            settings.CaptureControlEntry("Button", Node("<GyroSwipeLeft>A</GyroSwipeLeft>"));
            var prompt = new FakePrompt(RemovedGyroSwipeChoice.Delete);

            Assert.Equal(RemovedGyroSwipeOutcome.Deleted, RemovedGyroSwipeNotice.Apply(settings, "MyProfile", prompt));

            Assert.False(settings.HasContent);
            Assert.Equal("MyProfile", prompt.LastProfile);
            Assert.Equal(RemovedGyroSwipeOutcome.NoNotice, RemovedGyroSwipeNotice.Apply(settings, "MyProfile", prompt));
            Assert.Equal(1, prompt.Asked);
        }

        [Fact]
        public void Keep_LeavesTheContent_AndTheWarningComesBackEveryTime()
        {
            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureControlEntry("Macro", Node("<GyroSwipeRight>65/65</GyroSwipeRight>"));
            var prompt = new FakePrompt(RemovedGyroSwipeChoice.Keep);

            Assert.Equal(RemovedGyroSwipeOutcome.Kept, RemovedGyroSwipeNotice.Apply(settings, "p", prompt));
            Assert.Equal(RemovedGyroSwipeOutcome.Kept, RemovedGyroSwipeNotice.Apply(settings, "p", prompt));

            Assert.True(settings.HasContent);
            Assert.Equal(2, prompt.Asked);
        }

        [Fact]
        public void AMissingPrompt_ThrowsOnlyWhenAWarningIsNeeded()
        {
            Assert.Equal(RemovedGyroSwipeOutcome.NoNotice, RemovedGyroSwipeNotice.Apply(new RemovedGyroSwipeSettings(), "p", null));

            var settings = new RemovedGyroSwipeSettings();
            settings.CaptureSettings(Node(Changed));
            Assert.Throws<System.ArgumentNullException>(() => RemovedGyroSwipeNotice.Apply(settings, "p", null));
        }
    }
}
