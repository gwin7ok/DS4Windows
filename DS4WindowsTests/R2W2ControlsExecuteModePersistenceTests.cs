using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-2 W2（R2-MacroToggleRepeat-Plan.md §3.1）: Controls のマクロの Execute Mode は、プロファイルの独立したノード <c>Control/MacroExecuteMode</c>
    /// （<c>Held</c>／<c>Toggle</c>。<c>Once</c> は書かない）へ保存し、読込では、あれば KeyType の旧形式（<c>RepeatMacro</c>／<c>HoldMacro</c>）より優先する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class R2W2ControlsExecuteModePersistenceTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static string XmlWith(PersistenceGoldenContext ctx, string name, string keyTypeText, string modeNode, string macroNode = "<Macro><Square>350/256/310/256</Square></Macro>")
        {
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), name);
            Assert.Contains("<Control />", xml);
            string control = "<Control>" + macroNode + "<KeyType><Square>" + keyTypeText + "</Square></KeyType>" + modeNode + "</Control>";
            return xml.Replace("<Control />", control);
        }

        private static XmlDocument Doc(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        private static DS4KeyType Load(PersistenceGoldenContext ctx, string xml, string name)
        {
            return PersistenceHarness.LoadProfile(ctx, xml, name).GetDS4CSetting(Slot, DS4Controls.Square).keyType;
        }

        [Fact]
        public void Toggle_RoundTrips_AsTheMacroKeyTypeAndAToggleNode()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = XmlWith(ctx, "ToggleIn", "Macro", "<MacroExecuteMode><Square>Toggle</Square></MacroExecuteMode>");

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "ToggleLoad");
            DS4KeyType keyType = loaded.GetDS4CSetting(Slot, DS4Controls.Square).keyType;
            Assert.True(keyType.HasFlag(DS4KeyType.Macro));
            Assert.True(keyType.HasFlag(DS4KeyType.RepeatLatchMacro));
            Assert.False(keyType.HasFlag(DS4KeyType.RepeatMacro));

            XmlDocument resaved = Doc(PersistenceHarness.SaveProfile(ctx, loaded, "ToggleResave"));
            Assert.Equal("Macro", resaved.SelectSingleNode("/DS4Windows/Control/KeyType/Square")?.InnerText);
            Assert.Equal("Toggle", resaved.SelectSingleNode("/DS4Windows/Control/MacroExecuteMode/Square")?.InnerText);
        }

        [Fact]
        public void HeldNode_IsReadAsRepeatMacro()
        {
            using var ctx = new PersistenceGoldenContext();

            DS4KeyType keyType = Load(ctx, XmlWith(ctx, "HeldIn", "Macro", "<MacroExecuteMode><Square>Held</Square></MacroExecuteMode>"), "HeldLoad");

            Assert.True(keyType.HasFlag(DS4KeyType.RepeatMacro));
            Assert.False(keyType.HasFlag(DS4KeyType.RepeatLatchMacro));
        }

        [Fact]
        public void TheNode_TakesPriorityOver_TheLegacyKeyTypeName()
        {
            using var ctx = new PersistenceGoldenContext();

            DS4KeyType toggle = Load(ctx, XmlWith(ctx, "PriToggleIn", "RepeatMacro", "<MacroExecuteMode><Square>Toggle</Square></MacroExecuteMode>"), "PriToggleLoad");
            Assert.True(toggle.HasFlag(DS4KeyType.RepeatLatchMacro));
            Assert.False(toggle.HasFlag(DS4KeyType.RepeatMacro));

            DS4KeyType once = Load(ctx, XmlWith(ctx, "PriOnceIn", "RepeatMacro", "<MacroExecuteMode><Square>Once</Square></MacroExecuteMode>"), "PriOnceLoad");
            Assert.False(once.HasFlag(DS4KeyType.RepeatLatchMacro));
            Assert.False(once.HasFlag(DS4KeyType.RepeatMacro));
            Assert.True(once.HasFlag(DS4KeyType.Macro));
        }

        [Theory]
        [InlineData("<MacroExecuteMode><Square>Bogus</Square></MacroExecuteMode>")]
        [InlineData("<MacroExecuteMode><Square></Square></MacroExecuteMode>")]
        public void AnUnknownOrEmptyValue_FallsBackToTheLegacyKeyTypeName(string modeNode)
        {
            using var ctx = new PersistenceGoldenContext();

            DS4KeyType keyType = Load(ctx, XmlWith(ctx, "BadIn" + modeNode.GetHashCode(), "RepeatMacro", modeNode), "BadLoad" + modeNode.GetHashCode());

            Assert.True(keyType.HasFlag(DS4KeyType.RepeatMacro));
        }

        [Fact]
        public void TheNode_IsIgnored_ForAButtonThatIsNotAMacro()
        {
            using var ctx = new PersistenceGoldenContext();
            // Square は Macro の割り当てではない（Macro ノードなし）。KeyType も Macro ではない
            string xml = XmlWith(ctx, "NoMacroIn", "ScanCode", "<MacroExecuteMode><Square>Toggle</Square></MacroExecuteMode>", macroNode: string.Empty);

            DS4KeyType keyType = Load(ctx, xml, "NoMacroLoad");

            Assert.False(keyType.HasFlag(DS4KeyType.RepeatLatchMacro));
        }

        [Fact]
        public void APlainMacro_WritesNoNode()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "NoNodeIn", "Macro", string.Empty), "NoNodeLoad");

            XmlDocument resaved = Doc(PersistenceHarness.SaveProfile(ctx, loaded, "NoNodeResave"));

            Assert.Null(resaved.SelectSingleNode("/DS4Windows/Control/MacroExecuteMode"));
        }
    }
}
