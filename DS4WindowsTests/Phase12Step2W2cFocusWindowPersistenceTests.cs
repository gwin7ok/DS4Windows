using System.Collections.Generic;
using System.IO;
using System.Xml;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2c（R-4）: 新しい種別 <see cref="SpecialAction.ActionTypeId.FocusWindow"/>（ウィンドウのフォーカス）の保存と読込。
    /// SA は Actions.xml（Details＝プログラムのパス、WindowTitle＝ウィンドウタイトル、Delay）、Controls はプロファイル XML の
    /// <c>WindowPath</c>／<c>WindowTitle</c>。古いバージョンは、未知の種別として読み飛ばす（既存の寛容な読込）ことも確かめる。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class Phase12Step2W2cFocusWindowPersistenceTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static BackingStore NewStoreFor(PersistenceGoldenContext ctx)
        {
            BackingStore store = PersistenceHarness.NewStore();
            store.m_Actions = Path.Combine(ctx.AppData, "Actions.xml");
            return store;
        }

        private static string Save(BackingStore store, string path, string title, double delay = 0)
        {
            new DS4Windows.Core.Persistence.SpecialActionsXmlSerializer(store, TestActions.RuntimeReset).SaveActionNew("f", "Cross", 11, path, false, delay, title);
            return File.ReadAllText(store.m_Actions);
        }

        private static XmlNode ActionNode(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("/Actions/Action[@Name='f']");
        }

        [Fact]
        public void SpecialAction_Save_WritesPathTitleAndDelay_AndLoadsThemBack()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = NewStoreFor(ctx);

            string xml = Save(store, "^C:\\Games\\game.exe", "*Test Window", 0.5);

            XmlNode node = ActionNode(xml);
            Assert.Equal("FocusWindow", node.SelectSingleNode("Type")?.InnerText);
            Assert.Equal("^C:\\Games\\game.exe", node.SelectSingleNode("Details")?.InnerText);
            Assert.Equal("*Test Window", node.SelectSingleNode("WindowTitle")?.InnerText);
            Assert.Equal("0.5", node.SelectSingleNode("Delay")?.InnerText);
            Assert.Null(node.SelectSingleNode("Extras"));
            Assert.Null(node.SelectSingleNode("Arguements"));

            SpecialAction loaded = PersistenceHarness.LoadActions(ctx, xml).actions.Find(item => item.name == "f");
            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, loaded.typeID);
            Assert.Equal("^C:\\Games\\game.exe", loaded.details);
            Assert.Equal("*Test Window", loaded.extra);
            Assert.Equal(0.5, loaded.delayTime);
        }

        [Fact]
        public void SpecialAction_TitleIsNotReadAsUntriggerButtons()
        {
            // Program 以外の種別では、extras は「トリガー解除」のボタン名として読まれる。FocusWindow のタイトルは、これに混ざってはならない
            var action = new SpecialAction("f", "Cross", "FocusWindow", "game.exe$", 0, "Cross/Circle");

            Assert.Equal("Cross/Circle", action.extra);
            Assert.Null(action.ucontrols);
            Assert.Empty(action.uTrigger);
            Assert.False(action.automaticUntrigger);
        }

        [Fact]
        public void SpecialAction_EmptyTitle_WritesNoTitleElement_AndTitleOnlyTargetIsAllowed()
        {
            using var ctx = new PersistenceGoldenContext();

            string pathOnly = Save(NewStoreFor(ctx), "c:\\games\\game.exe", string.Empty);
            Assert.Null(ActionNode(pathOnly).SelectSingleNode("WindowTitle"));
            Assert.Equal(string.Empty, PersistenceHarness.LoadActions(ctx, pathOnly).actions.Find(item => item.name == "f").extra ?? string.Empty);

            using var ctx2 = new PersistenceGoldenContext();
            string titleOnly = Save(NewStoreFor(ctx2), string.Empty, "*notes");
            SpecialAction loaded = PersistenceHarness.LoadActions(ctx2, titleOnly).actions.Find(item => item.name == "f");
            Assert.Equal(string.Empty, loaded.details ?? string.Empty);
            Assert.Equal("*notes", loaded.extra);
        }

        [Fact]
        public void SpecialAction_ResaveKeepsTheSameXml()
        {
            using var ctx = new PersistenceGoldenContext();
            string first = Save(NewStoreFor(ctx), "game.exe$", "Window$", 1);
            BackingStore store = PersistenceHarness.LoadActions(ctx, first);

            Assert.True(new DS4Windows.Core.Persistence.SpecialActionsXmlSerializer(store, TestActions.RuntimeReset).SaveActions());

            XmlNode node = ActionNode(File.ReadAllText(store.m_Actions));
            Assert.Equal("game.exe$", node.SelectSingleNode("Details")?.InnerText);
            Assert.Equal("Window$", node.SelectSingleNode("WindowTitle")?.InnerText);
            Assert.Equal("1", node.SelectSingleNode("Delay")?.InnerText);
        }

        [Fact]
        public void AnUnknownSpecialActionType_IsSkippedOnLoad_WhichIsHowOlderVersionsTreatFocusWindow()
        {
            using var ctx = new PersistenceGoldenContext();
            const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>"
                + "<Action Name=\"future\"><Trigger>Cross</Trigger><Type>SomeFutureType</Type><Details>x</Details></Action>"
                + "<Action Name=\"keep\"><Trigger>Circle</Trigger><Type>DisconnectBT</Type><Details>0</Details></Action></Actions>";

            BackingStore store = PersistenceHarness.LoadActions(ctx, xml);

            Assert.DoesNotContain(store.actions, item => item.name == "future");
            Assert.Contains(store.actions, item => item.name == "keep");
        }

        [Fact]
        public void ControlsItem_RoundTripsThroughTheProfileXml()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.FocusWindow,
                WindowPath = "^c:\\games",
                WindowTitle = "*Test <Window> & more",
                HoldSeconds = 0.25,
            };

            string xml = PersistenceHarness.SaveProfile(ctx, store, "P12Focus");
            ControlSpecialItem loaded = PersistenceHarness.LoadProfile(ctx, xml, "P12FocusLoad").GetDS4CSetting(Slot, DS4Controls.Cross).special;

            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, loaded.Type);
            Assert.Equal("^c:\\games", loaded.WindowPath);
            Assert.Equal("*Test <Window> & more", loaded.WindowTitle);
            Assert.Equal(0.25, loaded.HoldSeconds);
        }

        [Fact]
        public void ControlsItem_WithAnUnknownType_IsSkipped_AsOlderVersionsDoWithFocusWindow()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "P12Unknown");
            Assert.Contains("<Control />", xml);
            xml = xml.Replace("<Control />", "<Control><Special><Cross Type=\"SomeFutureType\"><WindowPath>x</WindowPath></Cross></Special></Control>");

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "P12UnknownLoad");

            Assert.NotEqual(DS4ControlSettings.ActionType.Special, loaded.GetDS4CSetting(Slot, DS4Controls.Cross).actionType);
        }

        [Fact]
        public void ItemMapper_RoundTripsBetweenControlsItemAndSpecialAction()
        {
            var item = new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.FocusWindow,
                WindowPath = "game.exe$",
                WindowTitle = "^Test",
                HoldSeconds = 0.75,
            };

            SpecialAction sa = SpecialActionItemMapper.ToSpecialAction("Cross", new List<DS4Controls> { DS4Controls.Cross }, item);
            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, sa.typeID);
            Assert.Equal("game.exe$", sa.details);
            Assert.Equal("^Test", sa.extra);
            Assert.Equal(0.75, sa.delayTime);

            ControlSpecialItem back = SpecialActionItemMapper.ToItem(sa);
            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, back.Type);
            Assert.Equal("game.exe$", back.WindowPath);
            Assert.Equal("^Test", back.WindowTitle);
            Assert.Equal(0.75, back.HoldSeconds);

            ControlSpecialItem clone = item.Clone();
            Assert.Equal("game.exe$", clone.WindowPath);
            Assert.Equal("^Test", clone.WindowTitle);
        }
    }
}
