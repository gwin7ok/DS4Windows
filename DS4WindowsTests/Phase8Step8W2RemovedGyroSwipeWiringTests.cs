using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step8 8-0-W2（Phase8-Step8-Plan.md §5.1・§5.6。Q2＝C、Q9＝A）: 廃止された「ジャイロスワイプ」機能の、読込・保存・実行への配線のテスト。
    /// 読込: 設定（既定値と異なるとき）、<c>&lt;Control&gt;</c> 内の割り当ての項目、ジャイロの出力モード <c>DirectionalSwipe</c> を、解釈せずに保持する。
    /// 保存: 保持した内容をそのまま書き出す（削除したら、従来と同じ既定値の設定の要素だけ）。実行・設定: ジャイロスワイプの割り当て・設定・モードは存在しない。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step8W2RemovedGyroSwipeWiringTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private const string DefaultSettings =
            "<GyroSwipeSettings><DeadZoneX>80</DeadZoneX><DeadZoneY>80</DeadZoneY><Triggers>-1</Triggers><TriggerCond>and</TriggerCond><TriggerTurns>True</TriggerTurns><XAxis>Yaw</XAxis><DelayTime>0</DelayTime></GyroSwipeSettings>";

        private const string ChangedSettings =
            "<GyroSwipeSettings><DeadZoneX>120</DeadZoneX><DeadZoneY>80</DeadZoneY><Triggers>4,5</Triggers><TriggerCond>or</TriggerCond><TriggerTurns>False</TriggerTurns><XAxis>Roll</XAxis><DelayTime>30</DelayTime></GyroSwipeSettings>";

        private const string Controls =
            "<Control><Button><Cross>B</Cross><GyroSwipeLeft>A</GyroSwipeLeft></Button><Macro><GyroSwipeRight>65/65</GyroSwipeRight></Macro>" +
            "<Key><GyroSwipeUp>65</GyroSwipeUp></Key><Extras><GyroSwipeDown>1,2,0,0,0,0,0,0,0</GyroSwipeDown></Extras>" +
            "<KeyType><GyroSwipeUp>Toggle</GyroSwipeUp></KeyType></Control>";

        private static string DefaultXml(PersistenceGoldenContext ctx, string name)
            => PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), name);

        private static string Regex(string input, string pattern, string replacement)
        {
            var rx = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.Matches(rx, input);
            return rx.Replace(input, replacement.Replace("$", "$$"), 1);
        }

        private static string WithControls(string xml)
        {
            Assert.Contains("<Control />", xml);
            return xml.Replace("<Control />", Controls);
        }

        private static string WithDirectionalSwipeMode(string xml)
        {
            Assert.Contains("<GyroOutputMode>Controls</GyroOutputMode>", xml);
            return xml.Replace("<GyroOutputMode>Controls</GyroOutputMode>", "<GyroOutputMode>DirectionalSwipe</GyroOutputMode>");
        }

        private static XmlDocument Doc(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        private static string SettingsNodeOf(string xml)
        {
            // 空白を含まない形に整える（保存の結果は、インデントつき）
            XmlNode node = Doc(xml).SelectSingleNode("/DS4Windows/GyroSwipeSettings");
            var compact = new XmlDocument();
            compact.LoadXml(node.OuterXml);
            return compact.OuterXml.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("    <", "<").Replace("  <", "<");
        }

        // ---- 設定 ----

        [Fact]
        public void Load_WithDefaultSettings_KeepsNothing_AndSaveWritesTheLegacyDefaultElement()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, DefaultXml(ctx, "SwipeIn1"), "SwipeLoad1");

            Assert.False(loaded.removedGyroSwipeSettings[Slot].HasContent);

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave1");
            Assert.Equal(DefaultSettings, SettingsNodeOf(resaved));   // 廃止前も、既定値のときは、この形で毎回書いていた
        }

        [Fact]
        public void Load_KeepsChangedSettings_AndSaveWritesThemUnchanged()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = Regex(DefaultXml(ctx, "SwipeIn2"), "<GyroSwipeSettings>.*?</GyroSwipeSettings>", ChangedSettings);

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "SwipeLoad2");

            Assert.True(loaded.removedGyroSwipeSettings[Slot].HasContent);
            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave2");
            Assert.Equal(ChangedSettings, SettingsNodeOf(resaved));
        }

        // ---- 割り当て ----

        [Fact]
        public void Load_KeepsTheSwipeAssignments_WithoutInterpretingThem_AndOthersStillLoad()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithControls(DefaultXml(ctx, "SwipeIn3")), "SwipeLoad3");

            RemovedGyroSwipeSettings kept = loaded.removedGyroSwipeSettings[Slot];
            Assert.True(kept.HasContent);
            Assert.Equal(5, kept.EntryCount);   // GyroSwipe の項目だけ（Button・Macro・Key・Extras・KeyType の各 1 つ。Cross は含めない）
        }

        [Fact]
        public void Save_PutsTheSwipeAssignmentsBackIntoTheirGroups_AndKeepsTheOthers()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithControls(DefaultXml(ctx, "SwipeIn4")), "SwipeLoad4");

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave4");

            XmlDocument doc = Doc(resaved);
            Assert.Equal("B Button", doc.SelectSingleNode("/DS4Windows/Control/Button/Cross").InnerText);                 // 通常の割り当ては、そのまま読み書きされる
            Assert.Equal("A", doc.SelectSingleNode("/DS4Windows/Control/Button/GyroSwipeLeft").InnerText);
            Assert.Equal("65/65", doc.SelectSingleNode("/DS4Windows/Control/Macro/GyroSwipeRight").InnerText);
            Assert.Equal("65", doc.SelectSingleNode("/DS4Windows/Control/Key/GyroSwipeUp").InnerText);
            Assert.Equal("1,2,0,0,0,0,0,0,0", doc.SelectSingleNode("/DS4Windows/Control/Extras/GyroSwipeDown").InnerText);
            Assert.Equal("Toggle", doc.SelectSingleNode("/DS4Windows/Control/KeyType/GyroSwipeUp").InnerText);
        }

        [Fact]
        public void Save_AfterTheUserDeletedIt_HasNoSwipeAssignments()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithControls(DefaultXml(ctx, "SwipeIn5")), "SwipeLoad5");

            loaded.removedGyroSwipeSettings[Slot].Clear();
            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave5");

            Assert.DoesNotContain("GyroSwipeLeft", resaved);
            Assert.DoesNotContain("GyroSwipeUp", resaved);
            Assert.Equal("B Button", Doc(resaved).SelectSingleNode("/DS4Windows/Control/Button/Cross").InnerText);
        }

        [Fact]
        public void TheSwipeAssignments_DoNotAffectTheControlSettings()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithControls(DefaultXml(ctx, "SwipeIn6")), "SwipeLoad6");

            Assert.Equal(DS4ControlSettings.ActionType.Button, loaded.GetDS4CSetting(Slot, DS4Controls.Cross).actionType);
            Assert.Equal(DS4ControlSettings.ActionType.Default, loaded.GetDS4CSetting(Slot, DS4Controls.Capture).actionType);
        }

        // ---- 出力モード ----

        [Fact]
        public void Load_OfTheRemovedMode_UsesNone_AndKeepsTheModeText()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithDirectionalSwipeMode(DefaultXml(ctx, "SwipeIn7")), "SwipeLoad7");

            Assert.Equal(GyroOutMode.None, loaded.gyroOutMode[Slot]);
            Assert.True(loaded.removedGyroSwipeSettings[Slot].HadDirectionalSwipeMode);
            Assert.True(loaded.removedGyroSwipeSettings[Slot].HasContent);
        }

        [Fact]
        public void Save_WritesTheRemovedModeAsItWas_WhileKept_AndTheCurrentModeAfterDeleting()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithDirectionalSwipeMode(DefaultXml(ctx, "SwipeIn8")), "SwipeLoad8");

            string kept = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave8a");
            Assert.Equal("DirectionalSwipe", Doc(kept).SelectSingleNode("/DS4Windows/GyroOutputMode").InnerText);

            loaded.removedGyroSwipeSettings[Slot].Clear();
            string deleted = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave8b");
            Assert.Equal("None", Doc(deleted).SelectSingleNode("/DS4Windows/GyroOutputMode").InnerText);
        }

        [Fact]
        public void ChoosingAnotherMode_DropsTheKeptRemovedMode()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithDirectionalSwipeMode(DefaultXml(ctx, "SwipeIn9")), "SwipeLoad9");

            loaded.gyroOutMode[Slot] = GyroOutMode.Mouse;
            loaded.removedGyroSwipeSettings[Slot].ForgetDirectionalSwipeMode();   // 編集画面で選び直したとき（ProfileSettingsViewModel.GyroOutModeIndex）
            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SwipeResave9");

            Assert.Equal("Mouse", Doc(resaved).SelectSingleNode("/DS4Windows/GyroOutputMode").InnerText);
        }

        [Fact]
        public void ResetProfile_ClearsEverythingKept()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithDirectionalSwipeMode(WithControls(DefaultXml(ctx, "SwipeIn10"))), "SwipeLoad10");
            Assert.True(loaded.removedGyroSwipeSettings[Slot].HasContent);

            loaded.ResetProfile(Slot);

            Assert.False(loaded.removedGyroSwipeSettings[Slot].HasContent);
            Assert.Equal(0, loaded.removedGyroSwipeSettings[Slot].EntryCount);
        }

        // ---- 存在しないこと ----

        [Fact]
        public void TheSwipeMembers_AreGone()
        {
            Assert.DoesNotContain(Enum.GetNames(typeof(DS4Controls)), n => n.StartsWith("GyroSwipe", StringComparison.Ordinal));
            Assert.DoesNotContain("DirectionalSwipe", Enum.GetNames(typeof(GyroOutMode)));
            Assert.Null(typeof(BackingStore).GetField("gyroSwipeInfo", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(ControlSettingsGroup).GetField("GyroSwipeLeft"));
            Assert.Null(typeof(DS4Windows.DI.IProfileSettingsService).GetProperty("GyroSwipeInf"));
            Assert.Null(typeof(DS4Windows.DI.IProfileSettingsService).GetMethod("GetGyroSwipeInfo"));
            Assert.Null(typeof(Mouse).GetField("gyroSwipe"));
        }

        [Fact]
        public void TheSources_NoLongerMentionTheRemovedFeature_OutsideTheKeptHolder()
        {
            string repo = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (repo == null) return;
            string product = Path.GetDirectoryName(Path.GetDirectoryName(repo));   // DS4Windows/

            var allowed = new[]
            {
                "RemovedGyroSwipeSettings.cs", "RemovedGyroSwipeChoice.cs", "RemovedGyroSwipeOutcome.cs", "IRemovedGyroSwipePrompt.cs", "RemovedGyroSwipeNotice.cs",
                "BackingStore.cs", "ProfileGyroMouseLoadSection.cs", "ProfileGyroControlsLoadSection.cs", "ProfileMotionSaveSection.cs", "ProfileButtonControlLoadSection.cs",
                "ProfileButtonSaveSection.cs", "ProfileSettingsViewModel.cs", "IProfileSettingsService.cs", "ProfileSettingsService.cs",
                "Strings.Designer.cs", "MessageBoxRemovedGyroSwipePrompt.cs", "ProfileEditor.xaml.cs", "Resources.Designer.cs",
            };

            foreach (string file in Directory.EnumerateFiles(product, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) continue;
                if (allowed.Contains(Path.GetFileName(file))) continue;

                string text = File.ReadAllText(file);
                Assert.False(text.Contains("GyroSwipe") || text.Contains("gyroSwipe") || text.Contains("SixDirectionalSwipe"),
                    $"{file} がジャイロスワイプに言及している");
            }
        }
    }
}
