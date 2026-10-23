using System.IO;
using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-2 W2（R2-MacroToggleRepeat-Plan.md §3.1）: SA のマクロの Execute Mode は、Actions.xml の独立した要素 <c>MacroExecuteMode</c>
    /// （<c>Held</c>／<c>Toggle</c>。<c>Once</c> は書かない）へ保存し、Extras には書かない。読込は、要素があればそれを使い、
    /// なければ（古いファイル）Extras の <c>Repeat</c> を <c>Held</c> として読む（保存し直すと新しい形になる）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class R2W2SpecialActionExecuteModePersistenceTests
    {
        private static BackingStore NewStoreFor(PersistenceGoldenContext ctx)
        {
            BackingStore store = PersistenceHarness.NewStore();
            store.m_Actions = Path.Combine(ctx.AppData, "Actions.xml");
            return store;
        }

        private static string Save(PersistenceGoldenContext ctx, BackingStore store, string extras, MacroExecuteMode mode)
        {
            new DS4Windows.Services.SpecialActionsXmlSerializer(store).SaveActionNew("m", "Cross", 1, "350/256", false, extras: extras, macroExecuteMode: mode);
            return File.ReadAllText(store.m_Actions);
        }

        private static XmlNode ActionNode(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("/Actions/Action[@Name='m']");
        }

        private static string ActionsXml(string extras, string modeElement) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions><Action Name=\"m\"><Trigger>Cross</Trigger><Type>Macro</Type><Details>350/256</Details>"
            + (extras == null ? string.Empty : "<Extras>" + extras + "</Extras>") + modeElement + "</Action></Actions>";

        private static DS4KeyType LoadedKeyType(PersistenceGoldenContext ctx, string xml)
        {
            BackingStore store = PersistenceHarness.LoadActions(ctx, xml);
            return store.actions.Find(item => item.name == "m").keyType;
        }

        [Theory]
        [InlineData(MacroExecuteMode.Held, "Held", DS4KeyType.RepeatMacro)]
        [InlineData(MacroExecuteMode.Toggle, "Toggle", DS4KeyType.RepeatLatchMacro)]
        public void Save_WritesTheModeElement_AndNotTheExtras(MacroExecuteMode mode, string expectedText, DS4KeyType expectedFlag)
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = NewStoreFor(ctx);

            string xml = Save(ctx, store, "Scan Code", mode);

            XmlNode node = ActionNode(xml);
            Assert.Equal(expectedText, node.SelectSingleNode("MacroExecuteMode")?.InnerText);
            Assert.Equal("Scan Code", node.SelectSingleNode("Extras")?.InnerText);
            Assert.True(store.actions.Find(item => item.name == "m").keyType.HasFlag(expectedFlag));
            Assert.True(LoadedKeyType(ctx, xml).HasFlag(expectedFlag));
        }

        [Fact]
        public void Once_WritesNoModeElement()
        {
            using var ctx = new PersistenceGoldenContext();

            string xml = Save(ctx, NewStoreFor(ctx), "Scan Code", MacroExecuteMode.Once);

            Assert.Null(ActionNode(xml).SelectSingleNode("MacroExecuteMode"));
            DS4KeyType keyType = LoadedKeyType(ctx, xml);
            Assert.False(keyType.HasFlag(DS4KeyType.RepeatMacro));
            Assert.False(keyType.HasFlag(DS4KeyType.RepeatLatchMacro));
        }

        [Fact]
        public void ALegacyFile_WithRepeatInExtras_IsReadAsHeld_AndResavedAsTheModeElement()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.LoadActions(ctx, ActionsXml("Scan Code/Repeat", string.Empty));
            SpecialAction loaded = store.actions.Find(item => item.name == "m");
            Assert.True(loaded.keyType.HasFlag(DS4KeyType.RepeatMacro));
            Assert.True(loaded.keyType.HasFlag(DS4KeyType.ScanCode));

            Assert.True(new DS4Windows.Services.SpecialActionsXmlSerializer(store).SaveActions());

            XmlNode node = ActionNode(File.ReadAllText(store.m_Actions));
            Assert.Equal("Held", node.SelectSingleNode("MacroExecuteMode")?.InnerText);
            Assert.Equal("Scan Code", node.SelectSingleNode("Extras")?.InnerText);
        }

        [Fact]
        public void TheElement_TakesPriorityOver_TheLegacyRepeatInExtras()
        {
            using var ctx = new PersistenceGoldenContext();

            DS4KeyType toggle = LoadedKeyType(ctx, ActionsXml("Repeat", "<MacroExecuteMode>Toggle</MacroExecuteMode>"));
            Assert.True(toggle.HasFlag(DS4KeyType.RepeatLatchMacro));
            Assert.False(toggle.HasFlag(DS4KeyType.RepeatMacro));

            DS4KeyType once = LoadedKeyType(ctx, ActionsXml("Repeat", "<MacroExecuteMode>Once</MacroExecuteMode>"));
            Assert.False(once.HasFlag(DS4KeyType.RepeatMacro));
            Assert.False(once.HasFlag(DS4KeyType.RepeatLatchMacro));
        }

        [Fact]
        public void AnUnknownValue_FallsBackToTheLegacyRepeat()
        {
            using var ctx = new PersistenceGoldenContext();

            DS4KeyType keyType = LoadedKeyType(ctx, ActionsXml("Repeat", "<MacroExecuteMode>Bogus</MacroExecuteMode>"));

            Assert.True(keyType.HasFlag(DS4KeyType.RepeatMacro));
        }

        [Theory]
        [InlineData("Repeat", "")]
        [InlineData("Scan Code/Repeat/Sync", "Scan Code/Sync")]
        [InlineData("Scan Code", "Scan Code")]
        [InlineData("RepeatX", "RepeatX")]   // 完全一致のトークンだけを取り除く
        [InlineData("", "")]
        public void RemoveLegacyRepeatToken_RemovesOnlyTheExactToken(string extras, string expected)
        {
            string actual = DS4WinWPF.DS4Control.DTOXml.ActionsDTO.RemoveLegacyRepeatToken(extras);

            Assert.Equal(expected, actual);
        }
    }
}
