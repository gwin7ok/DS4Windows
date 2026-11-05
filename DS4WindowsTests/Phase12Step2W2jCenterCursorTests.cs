using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;
using DS4Windows.UI.ViewModels;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2j（R-4）: FocusWindow の「前面化のあと、ウィンドウの中央へマウスポインターを移動する」。
    /// 保存（Actions.xml の <c>CenterCursor</c>、Controls の <c>CenterCursor</c>）、読込、古い版との互換、編集、実行への受け渡し、説明文。
    /// 中央への移動そのもの（<c>WindowFocuser</c>）は <c>Phase12Step2W2b2WindowFocuserTests</c>。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase12Step2W2jCenterCursorTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static BackingStore NewStoreFor(PersistenceGoldenContext ctx)
        {
            BackingStore store = PersistenceHarness.NewStore();
            store.m_Actions = Path.Combine(ctx.AppData, "Actions.xml");
            return store;
        }

        private static string Save(BackingStore store, bool centerCursor)
        {
            new SpecialActionsXmlSerializer(store, TestActions.RuntimeReset).SaveActionNew("f", "Cross", 11, "game.exe$", false, 0, "*Test", centerCursor: centerCursor);
            return File.ReadAllText(store.m_Actions);
        }

        private static XmlNode ActionNode(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("/Actions/Action[@Name='f']");
        }

        [Fact]
        public void SpecialAction_WritesCenterCursorOnlyWhenOn_AndLoadsItBack()
        {
            using var ctx = new PersistenceGoldenContext();
            string on = Save(NewStoreFor(ctx), true);

            Assert.Equal("true", ActionNode(on).SelectSingleNode("CenterCursor")?.InnerText);
            Assert.True(PersistenceHarness.LoadActions(ctx, on).actions.Find(item => item.name == "f").centerCursor);

            using var ctx2 = new PersistenceGoldenContext();
            string off = Save(NewStoreFor(ctx2), false);

            Assert.Null(ActionNode(off).SelectSingleNode("CenterCursor"));
            Assert.False(PersistenceHarness.LoadActions(ctx2, off).actions.Find(item => item.name == "f").centerCursor);
        }

        [Fact]
        public void SpecialAction_ResaveKeepsCenterCursor()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.LoadActions(ctx, Save(NewStoreFor(ctx), true));

            Assert.True(new SpecialActionsXmlSerializer(store, TestActions.RuntimeReset).SaveActions());

            Assert.Equal("true", ActionNode(File.ReadAllText(store.m_Actions)).SelectSingleNode("CenterCursor")?.InnerText);
        }

        [Fact]
        public void AnActionsXmlWithoutCenterCursor_LoadsAsOff_WhichIsHowExistingAssignmentsKeepWorking()
        {
            using var ctx = new PersistenceGoldenContext();
            const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>"
                + "<Action Name=\"f\"><Trigger>Cross</Trigger><Type>FocusWindow</Type><Details>game.exe$</Details></Action></Actions>";

            SpecialAction loaded = PersistenceHarness.LoadActions(ctx, xml).actions.Find(item => item.name == "f");

            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, loaded.typeID);
            Assert.False(loaded.centerCursor);
        }

        [Fact]
        public void ControlsItem_RoundTripsCenterCursorThroughTheProfileXml()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "game.exe$", CenterCursor = true };

            string xml = PersistenceHarness.SaveProfile(ctx, store, "P12Center");
            Assert.Contains("CenterCursor", xml);
            ControlSpecialItem loaded = PersistenceHarness.LoadProfile(ctx, xml, "P12CenterLoad").GetDS4CSetting(Slot, DS4Controls.Cross).special;
            Assert.True(loaded.CenterCursor);

            dcs.special.CenterCursor = false;
            Assert.DoesNotContain("CenterCursor", PersistenceHarness.SaveProfile(ctx, store, "P12CenterOff"));
        }

        [Fact]
        public void ItemMapper_CarriesCenterCursorBothWays_AndCloneKeepsIt()
        {
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "game.exe$", CenterCursor = true };

            SpecialAction sa = SpecialActionItemMapper.ToSpecialAction("Cross", new List<DS4Controls> { DS4Controls.Cross }, item);
            Assert.True(sa.centerCursor);
            Assert.True(SpecialActionItemMapper.ToItem(sa).CenterCursor);
            Assert.True(item.Clone().CenterCursor);
        }

        private sealed class RecordingFocuser : IWindowFocuser
        {
            public readonly List<bool> CenterCursorFlags = new List<bool>();

            public WindowFocusResult Focus(WindowTargetMatcher target, bool centerCursor = false)
            {
                CenterCursorFlags.Add(centerCursor);
                return WindowFocusResult.Focused;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FocusWindowAction_PassesCenterCursorToTheFocuser(bool centerCursor)
        {
            var sa = new SpecialAction("n", "Cross", "FocusWindow", "game.exe$") { centerCursor = centerCursor };
            var focuser = new RecordingFocuser();

            new FocusWindowAction(sa, focuser, work => work()).Execute(null);

            Assert.Equal(new[] { centerCursor }, focuser.CenterCursorFlags);
        }

        [Fact]
        public void ViewModel_SavesAndLoadsCenterCursor_ForSpecialActionsAndControls()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = new FocusWindowViewModel(repo) { WindowPath = "x", CenterCursor = true };

            vm.SaveAction(new SpecialAction("N", "Cross", "FocusWindow", "x"), edit: false);
            Assert.True(repo.SavedCenterCursor);
            Assert.True(vm.ToControlItem(SpecialAction.ActionTypeId.FocusWindow).CenterCursor);

            var loaded = new FocusWindowViewModel(repo);
            loaded.LoadAction(new SpecialAction("N", "Cross", "FocusWindow", "x") { centerCursor = true });
            Assert.True(loaded.CenterCursor);

            var fromItem = new FocusWindowViewModel(repo);
            fromItem.LoadControlItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "x", CenterCursor = true });
            Assert.True(fromItem.CenterCursor);

            var off = new FocusWindowViewModel(repo) { WindowPath = "x" };
            off.SaveAction(new SpecialAction("N", "Cross", "FocusWindow", "x"), edit: false);
            Assert.False(repo.SavedCenterCursor);
        }

        [Fact]
        public void Describer_MentionsCenterCursorOnlyWhenOn()
        {
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "game.exe$", CenterCursor = true };
            Assert.Equal("Focus Window: game.exe$ (center cursor)", AssignmentDescriber.ForSpecialItem(item));

            item.CenterCursor = false;
            Assert.Equal("Focus Window: game.exe$", AssignmentDescriber.ForSpecialItem(item));

            var sa = new SpecialAction("n", "Cross", "FocusWindow", "game.exe$") { centerCursor = true };
            Assert.Equal("Focus Window: game.exe$ (center cursor)", AssignmentDescriber.ForSpecialAction(sa, c => c.ToString()));
        }
    }
}
