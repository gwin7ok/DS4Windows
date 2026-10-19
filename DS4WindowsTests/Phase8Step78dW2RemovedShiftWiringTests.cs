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
    /// Phase8-Step7 7.8d-W2（Phase8-Step7-8d-Plan.md §4.1・§5）: 廃止された「シフト」機能の、読込・保存・実行への配線のテスト。
    /// 読込: <c>&lt;ShiftControl&gt;</c> の内容を解釈せずに保持する。保存: 保持した内容をそのまま書き出す（削除したら、空の要素）。
    /// 実行・設定: シフトの設定は、割り当てに反映されない。シフトのメンバー（<c>DS4ControlSettings</c> の欄、<c>Mapping</c> の処理）は存在しない。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78dW2RemovedShiftWiringTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private const string ShiftContent =
            "<ShiftControl><Button><Cross Trigger=\"12\">A</Cross></Button><Macro><Circle Trigger=\"1\">65/65</Circle></Macro>" +
            "<Extras><Square>1,2,0,0,0,0,0,0,0</Square></Extras></ShiftControl>";

        private static string DefaultXml(PersistenceGoldenContext ctx, string name)
            => PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), name);

        private static string WithShift(string xml)
        {
            Assert.Contains("<ShiftControl />", xml);
            return xml.Replace("<ShiftControl />", ShiftContent);
        }

        private static string ShiftNodeOf(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("/DS4Windows/ShiftControl")?.OuterXml;
        }

        [Fact]
        public void Load_KeepsTheShiftControlContent_WithoutInterpretingIt()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = WithShift(DefaultXml(ctx, "ShiftIn1"));

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "ShiftLoad1");

            RemovedShiftSettings kept = loaded.removedShiftSettings[Slot];
            Assert.True(kept.HasContent);
            Assert.Equal(ShiftContent, kept.OuterXml);

            // 割り当てには反映されない（Cross は、通常の割り当てがないため、既定のまま）
            DS4ControlSettings cross = loaded.GetDS4CSetting(Slot, DS4Controls.Cross);
            Assert.Equal(DS4ControlSettings.ActionType.Default, cross.actionType);
            Assert.Equal(DS4ControlSettings.ActionType.Default, loaded.GetDS4CSetting(Slot, DS4Controls.Circle).actionType);
            Assert.Null(loaded.GetDS4CSetting(Slot, DS4Controls.Square).extras);
        }

        [Fact]
        public void Load_WithoutContent_KeepsNothing()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, DefaultXml(ctx, "ShiftIn2"), "ShiftLoad2");

            Assert.False(loaded.removedShiftSettings[Slot].HasContent);
        }

        [Fact]
        public void Save_WritesTheKeptContentUnchanged()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithShift(DefaultXml(ctx, "ShiftIn3")), "ShiftLoad3");

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "ShiftResave3");

            Assert.Equal(ShiftContent, ShiftNodeOf(resaved));
        }

        [Fact]
        public void Save_AfterTheUserDeletedIt_WritesAnEmptyElement()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithShift(DefaultXml(ctx, "ShiftIn4")), "ShiftLoad4");

            loaded.removedShiftSettings[Slot].Clear();
            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "ShiftResave4");

            Assert.Equal("<ShiftControl />", ShiftNodeOf(resaved));
        }

        [Fact]
        public void Save_OfAProfileWithoutContent_StillWritesTheEmptyElement_LikeBefore()
        {
            using var ctx = new PersistenceGoldenContext();

            string xml = DefaultXml(ctx, "ShiftIn5");

            Assert.Equal("<ShiftControl />", ShiftNodeOf(xml));
        }

        [Fact]
        public void ResetProfile_ClearsTheKeptContent()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithShift(DefaultXml(ctx, "ShiftIn6")), "ShiftLoad6");
            Assert.True(loaded.removedShiftSettings[Slot].HasContent);

            loaded.ResetProfile(Slot);

            Assert.False(loaded.removedShiftSettings[Slot].HasContent);
        }

        [Fact]
        public void EditingOtherSettings_AndSaving_KeepsTheShiftContent()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, WithShift(DefaultXml(ctx, "ShiftIn7")), "ShiftLoad7");

            DS4ControlSettings dcs = loaded.GetDS4CSetting(Slot, DS4Controls.L1);
            dcs.actionType = DS4ControlSettings.ActionType.Key;
            dcs.action.actionKey = 65;
            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "ShiftResave7");

            Assert.Equal(ShiftContent, ShiftNodeOf(resaved));
            Assert.Contains("<L1>65</L1>", resaved);
        }

        [Fact]
        public void TheShiftMembers_AreGone_FromTheSettingsAndTheStoreApi()
        {
            string[] names = typeof(DS4ControlSettings)
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .Where(n => n.IndexOf("shift", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            Assert.Empty(names);
        }

        [Fact]
        public void TheSources_NoLongerMentionTheShiftModifier()
        {
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            string settings = SourceFileLocator.Find("DS4Windows/DS4Control/DS4ControlSettings.cs");
            string misc = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileSections/ProfileMiscLoadSection.cs");
            string bindingWindow = SourceFileLocator.Find("DS4Windows/DS4Forms/BindingWindow.xaml");
            if (mapping == null || settings == null || misc == null || bindingWindow == null) return;

            string mappingText = File.ReadAllText(mapping);
            Assert.DoesNotContain("ShiftTrigger(", mappingText);
            Assert.DoesNotContain("shiftTriggerMapping", mappingText);
            Assert.DoesNotContain(".shiftAction", mappingText);
            Assert.DoesNotContain("shiftExtras", mappingText);
            Assert.DoesNotContain("shiftActionType", File.ReadAllText(settings));
            Assert.DoesNotContain("ShiftModifier\")", File.ReadAllText(misc));
            Assert.DoesNotContain("shiftBindRadio", File.ReadAllText(bindingWindow));
            Assert.DoesNotContain("shiftTriggerCombo", File.ReadAllText(bindingWindow));
        }
    }
}
