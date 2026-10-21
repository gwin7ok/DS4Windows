using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4 W1（Phase10-Step4-Plan.md §3.1・§3.2・§6）: Controls の割り当ての、SA 専用の種別と項目（型のある項目 <see cref="ControlSpecialItem"/>）の、
    /// プロファイル XML（<c>&lt;Control&gt;&lt;Special&gt;</c>）への保存・読み込み（往復、既定の値は書かない、文化に依らない、壊れた要素の扱い、優先順位）と、
    /// <see cref="DS4ControlSettings"/> のリセット・編集用のコピーでの扱い。画面・実行は、まだ関係しない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W1ControlSpecialItemTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static string XmlWith(PersistenceGoldenContext ctx, string name, string controlSection)
        {
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), name);
            Assert.Contains("<Control />", xml);
            return xml.Replace("<Control />", controlSection);
        }

        private static XmlDocument Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        private static void SetSpecial(BackingStore store, DS4Controls control, ControlSpecialItem item)
        {
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, control);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = item;
        }

        private static ControlSpecialItem RoundTrip(PersistenceGoldenContext ctx, ControlSpecialItem item, string name, DS4Controls control = DS4Controls.Cross)
        {
            BackingStore store = PersistenceHarness.NewStore();
            SetSpecial(store, control, item);
            string xml = PersistenceHarness.SaveProfile(ctx, store, name);
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, name + "Load");
            DS4ControlSettings dcs = loaded.GetDS4CSetting(Slot, control);
            Assert.Equal(DS4ControlSettings.ActionType.Special, dcs.actionType);
            return dcs.special;
        }

        // ---- 往復 ----

        [Fact]
        public void Program_RoundTrips_EveryField()
        {
            using var ctx = new PersistenceGoldenContext();
            var item = new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.Program,
                ProgramPath = "C:\\Program Files\\Tool\\tool.exe",
                ProgramArguments = "-a \"b c\" /x",
                HideWindow = true,
                HoldSeconds = 0.75,
            };

            ControlSpecialItem loaded = RoundTrip(ctx, item, "S4W1Program");

            Assert.Equal(SpecialAction.ActionTypeId.Program, loaded.Type);
            Assert.Equal(item.ProgramPath, loaded.ProgramPath);
            Assert.Equal(item.ProgramArguments, loaded.ProgramArguments);
            Assert.True(loaded.HideWindow);
            Assert.Equal(0.75, loaded.HoldSeconds);
        }

        [Fact]
        public void Profile_BatteryCheck_MultiAction_AndTheOthers_RoundTrip()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            SetSpecial(store, DS4Controls.Circle, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "Game A", AutomaticUntrigger = true });
            SetSpecial(store, DS4Controls.Square, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT, HoldSeconds = 1.5 });
            SetSpecial(store, DS4Controls.Triangle, new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.BatteryCheck,
                EmptyColor = new DS4Color(255, 0, 0),
                FullColor = new DS4Color(0, 255, 10),
            });
            SetSpecial(store, DS4Controls.L1, new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.MultiAction,
                TapMacro = new List<int> { 350, 256 },
                HoldMacro = new List<int> { 360 },
                DoubleTapMacro = new List<int> { 70, 350, 70 },
                HoldSeconds = 0.3,
            });
            SetSpecial(store, DS4Controls.R1, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate });
            SetSpecial(store, DS4Controls.L2, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.GyroCalibrate });

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, PersistenceHarness.SaveProfile(ctx, store, "S4W1Many"), "S4W1ManyLoad");

            ControlSpecialItem profile = loaded.GetDS4CSetting(Slot, DS4Controls.Circle).special;
            Assert.Equal("Game A", profile.ProfileName);
            Assert.True(profile.AutomaticUntrigger);

            Assert.Equal(1.5, loaded.GetDS4CSetting(Slot, DS4Controls.Square).special.HoldSeconds);

            ControlSpecialItem battery = loaded.GetDS4CSetting(Slot, DS4Controls.Triangle).special;
            Assert.Equal(new DS4Color(255, 0, 0), battery.EmptyColor);
            Assert.Equal(new DS4Color(0, 255, 10), battery.FullColor);

            ControlSpecialItem multi = loaded.GetDS4CSetting(Slot, DS4Controls.L1).special;
            Assert.Equal(new[] { 350, 256 }, multi.TapMacro);
            Assert.Equal(new[] { 360 }, multi.HoldMacro);
            Assert.Equal(new[] { 70, 350, 70 }, multi.DoubleTapMacro);
            Assert.Equal(0.3, multi.HoldSeconds);

            Assert.Equal(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, loaded.GetDS4CSetting(Slot, DS4Controls.R1).special.Type);
            Assert.Equal(SpecialAction.ActionTypeId.GyroCalibrate, loaded.GetDS4CSetting(Slot, DS4Controls.L2).special.Type);
        }

        [Fact]
        public void MacroOptions_RoundTrip_OnAMacroAssignment_AndTheMacroStepsStayInTheMacroSection()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, DS4Controls.R2);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Macro;
            dcs.action.actionMacro = new[] { 350, 256 };
            dcs.keyType = DS4KeyType.Macro;
            dcs.special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Macro, RunOnRelease = true, Synchronized = true, KeepKeyState = true };

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W1MacroOpts");
            XmlDocument doc = Parse(xml);
            Assert.Equal("350/256", doc.SelectSingleNode("/DS4Windows/Control/Macro/R2").InnerText);
            Assert.Equal("Macro", doc.SelectSingleNode("/DS4Windows/Control/Special/R2").Attributes["Type"].Value);

            DS4ControlSettings loaded = PersistenceHarness.LoadProfile(ctx, xml, "S4W1MacroOptsLoad").GetDS4CSetting(Slot, DS4Controls.R2);
            Assert.Equal(DS4ControlSettings.ActionType.Macro, loaded.actionType);
            Assert.Equal(new[] { 350, 256 }, loaded.action.actionMacro);
            Assert.True(loaded.special.RunOnRelease);
            Assert.True(loaded.special.Synchronized);
            Assert.True(loaded.special.KeepKeyState);
        }

        [Fact]
        public void AMacroWithoutOptions_WritesNoSpecialNode()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, DS4Controls.R2);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Macro;
            dcs.action.actionMacro = new[] { 350, 256 };
            dcs.keyType = DS4KeyType.Macro;
            dcs.special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Macro };

            XmlDocument doc = Parse(PersistenceHarness.SaveProfile(ctx, store, "S4W1MacroNoOpts"));

            Assert.Null(doc.SelectSingleNode("/DS4Windows/Control/Special"));
        }

        // ---- 書き出しの形 ----

        [Fact]
        public void OnlyNonDefaultFields_AreWritten_AndSpecialComesAfterKeyType()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            SetSpecial(store, DS4Controls.Cross, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = "a.exe" });
            DS4ControlSettings key = store.GetDS4CSetting(Slot, DS4Controls.Circle);
            key.Reset();
            key.actionType = DS4ControlSettings.ActionType.Key;
            key.action.actionKey = 65;
            key.keyType = DS4KeyType.Toggle;

            XmlDocument doc = Parse(PersistenceHarness.SaveProfile(ctx, store, "S4W1Shape"));

            XmlNode control = doc.SelectSingleNode("/DS4Windows/Control");
            Assert.Equal(new[] { "Key", "KeyType", "Special" }, control.ChildNodes.Cast<XmlNode>().Select(n => n.Name).ToArray());
            XmlNode cross = doc.SelectSingleNode("/DS4Windows/Control/Special/Cross");
            Assert.Equal("Program", cross.Attributes["Type"].Value);
            Assert.Equal(new[] { "ProgramPath" }, cross.ChildNodes.Cast<XmlNode>().Select(n => n.Name).ToArray());
        }

        [Fact]
        public void AProfileWithoutSpecialAssignments_HasNoSpecialNode()
        {
            using var ctx = new PersistenceGoldenContext();

            XmlDocument doc = Parse(PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "S4W1None"));

            Assert.Null(doc.SelectSingleNode("/DS4Windows/Control/Special"));
        }

        // ---- 境界値、文化 ----

        [Fact]
        public void Boundaries_ZeroSeconds_LargeSeconds_LongPath_AndSpecialCharacters_RoundTrip()
        {
            using var ctx = new PersistenceGoldenContext();
            string longPath = "C:\\" + new string('a', 4000) + ".exe";
            string special = "<tag a=\"1\" & 'b'>\u3042\u3044\u3046</tag>";

            ControlSpecialItem zero = RoundTrip(ctx, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT, HoldSeconds = 0 }, "S4W1Zero");
            ControlSpecialItem large = RoundTrip(ctx, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT, HoldSeconds = 86400.5 }, "S4W1Large");
            ControlSpecialItem path = RoundTrip(ctx, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = longPath }, "S4W1Long");
            ControlSpecialItem chars = RoundTrip(ctx, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = special, ProgramArguments = special }, "S4W1Chars");

            Assert.Equal(0, zero.HoldSeconds);
            Assert.Equal(86400.5, large.HoldSeconds);
            Assert.Equal(longPath, path.ProgramPath);
            Assert.Equal(special, chars.ProgramPath);
            Assert.Equal(special, chars.ProgramArguments);
        }

        [Fact]
        public void TheDecimalSeparator_DoesNotDependOnTheCulture()
        {
            using var ctx = new PersistenceGoldenContext();
            CultureInfo saved = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                BackingStore store = PersistenceHarness.NewStore();
                SetSpecial(store, DS4Controls.Cross, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT, HoldSeconds = 1.25 });
                string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W1De");

                Assert.Contains("<HoldSeconds>1.25</HoldSeconds>", xml);
                Assert.Equal(1.25, PersistenceHarness.LoadProfile(ctx, xml, "S4W1DeLoad").GetDS4CSetting(Slot, DS4Controls.Cross).special.HoldSeconds);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        // ---- 壊れた要素、優先順位 ----

        [Fact]
        public void BrokenElements_AreIgnoredOrDefaulted_WithoutBreakingTheOtherAssignments()
        {
            using var ctx = new PersistenceGoldenContext();
            const string section =
                "<Control>" +
                "<Button><L1>A</L1></Button>" +
                "<Special>" +
                "<Cross Type=\"Nope\"><ProgramPath>x</ProgramPath></Cross>" +                                  // 未知の種別: 読み飛ばす
                "<Circle Type=\"Key\"><ProgramPath>x</ProgramPath></Circle>" +                                 // Controls で使えない種別: 読み飛ばす
                "<NotAButton Type=\"Program\"><ProgramPath>x</ProgramPath></NotAButton>" +                     // 不明なボタン名: 読み飛ばす
                "<Square><ProgramPath>x</ProgramPath></Square>" +                                              // 種別なし: 読み飛ばす
                "<Triangle Type=\"DisconnectBT\"><HoldSeconds>abc</HoldSeconds></Triangle>" +                  // 壊れた秒数: 既定（0）
                "<R1 Type=\"DisconnectBT\"><HoldSeconds>-5</HoldSeconds></R1>" +                               // 負の秒数: 既定（0）
                "<R2 Type=\"DisconnectBT\"><HoldSeconds>NaN</HoldSeconds></R2>" +                              // NaN: 既定（0）
                "<L2 Type=\"BatteryCheck\"><EmptyColor>1,2</EmptyColor><FullColor>1,2,300</FullColor></L2>" +  // 壊れた色: 既定
                "<R3 Type=\"MultiAction\"><TapMacro>350/x/70</TapMacro><HoldMacro>1/2</HoldMacro></R3>" +      // 壊れたマクロ: 空。壊れていないものは取り込む
                "<L3 Type=\"Program\"><Unknown>1</Unknown><ProgramPath>ok.exe</ProgramPath></L3>" +            // 未知の子要素は無視。ほかの項目は取り込む
                "</Special>" +
                "</Control>";

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "S4W1Broken", section), "S4W1BrokenLoad");

            foreach (DS4Controls ignored in new[] { DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square })
            {
                Assert.Equal(DS4ControlSettings.ActionType.Default, loaded.GetDS4CSetting(Slot, ignored).actionType);
                Assert.Null(loaded.GetDS4CSetting(Slot, ignored).special);
            }

            Assert.Equal(X360Controls.A, loaded.GetDS4CSetting(Slot, DS4Controls.L1).action.actionBtn);   // ほかの割り当ては、壊れない

            foreach (DS4Controls zero in new[] { DS4Controls.Triangle, DS4Controls.R1, DS4Controls.R2 })
            {
                Assert.Equal(0, loaded.GetDS4CSetting(Slot, zero).special.HoldSeconds);
            }

            ControlSpecialItem battery = loaded.GetDS4CSetting(Slot, DS4Controls.L2).special;
            Assert.Equal(default(DS4Color), battery.EmptyColor);
            Assert.Equal(default(DS4Color), battery.FullColor);

            ControlSpecialItem multi = loaded.GetDS4CSetting(Slot, DS4Controls.R3).special;
            Assert.Empty(multi.TapMacro);
            Assert.Equal(new[] { 1, 2 }, multi.HoldMacro);

            Assert.Equal("ok.exe", loaded.GetDS4CSetting(Slot, DS4Controls.L3).special.ProgramPath);
        }

        [Fact]
        public void ASpecialAssignment_WinsOverAButtonOfTheSameControl()
        {
            using var ctx = new PersistenceGoldenContext();
            const string section =
                "<Control><Button><Cross>A</Cross></Button>" +
                "<Special><Cross Type=\"Program\"><ProgramPath>x.exe</ProgramPath></Cross></Special></Control>";

            DS4ControlSettings cross = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "S4W1Wins", section), "S4W1WinsLoad").GetDS4CSetting(Slot, DS4Controls.Cross);

            Assert.Equal(DS4ControlSettings.ActionType.Special, cross.actionType);
            Assert.Equal("x.exe", cross.special.ProgramPath);
        }

        [Fact]
        public void MacroOptions_AreIgnored_WhenTheControlIsNotAMacro()
        {
            using var ctx = new PersistenceGoldenContext();
            const string section =
                "<Control><Key><Cross>65</Cross></Key>" +
                "<Special><Cross Type=\"Macro\"><RunOnRelease>true</RunOnRelease></Cross></Special></Control>";

            DS4ControlSettings cross = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "S4W1NotMacro", section), "S4W1NotMacroLoad").GetDS4CSetting(Slot, DS4Controls.Cross);

            Assert.Equal(DS4ControlSettings.ActionType.Key, cross.actionType);
            Assert.Null(cross.special);
        }

        // ---- DS4ControlSettings、編集用のコピー ----

        [Fact]
        public void Reset_ClearsTheSpecialItem()
        {
            var dcs = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Special,
                special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program },
            };

            dcs.Reset();

            Assert.Null(dcs.special);
            Assert.Equal(DS4ControlSettings.ActionType.Default, dcs.actionType);
        }

        [Fact]
        public void Clone_IsIndependent_IncludingTheMacroLists()
        {
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.MultiAction, TapMacro = new List<int> { 1, 2 }, ProgramPath = "a" };

            ControlSpecialItem copy = item.Clone();
            copy.TapMacro.Add(3);
            copy.ProgramPath = "b";

            Assert.Equal(new[] { 1, 2 }, item.TapMacro);
            Assert.Equal("a", item.ProgramPath);
        }

        [Fact]
        public void TheEditCopy_IsIndependent_AndWritingAnotherTypeDropsTheSpecialItem()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            var original = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Special,
                special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = "a.exe" },
            };
            var target = new ControlSettingsEditTarget(original, profile);

            Assert.NotSame(original.special, target.Working.special);
            Assert.Equal("a.exe", target.Working.special.ProgramPath);

            // 画面が、まだ Special を作らない（Step4 の後続の Wave）。Default を書くと、SA 専用の項目も、捨てる
            profile.OutputKBMMapping = new SendInputMapping();
            var repo = new RecordingSpecialActionRepository();
            var pressKey = new PressKeyViewModel(repo, profile, TestRepositoryFactory.CreateOutputSlotService());
            var macro = new MacroViewModel(repo);
            target.WriteFrom(10, pressKey, macro);   // 添字 10＝Default（Controls のモードの項目）
            Assert.Null(target.Working.special);
            Assert.NotNull(original.special);   // Save（Apply）するまで、元は変わらない

            target.Apply();
            Assert.Null(original.special);
            Assert.Equal(DS4ControlSettings.ActionType.Default, original.actionType);
        }
    }
}
