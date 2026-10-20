using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Fix-MacroRepeatUnify W1（Phase8-Fix-MacroRepeatUnify-Plan.md §3.1。D1・D3）: Controls タブのマクロの「押している間繰り返す」は、
    /// プロファイル XML の KeyType に <c>RepeatMacro</c> と書く（SA と同じ値）。旧版が書いた <c>HoldMacro</c> は、読込だけ受け付けて RepeatMacro として読み、保存時に RepeatMacro へ置き換える。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8FixMacroRepeatUnifyPersistenceTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static string XmlWith(PersistenceGoldenContext ctx, string name, string keyTypeText)
        {
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), name);
            Assert.Contains("<Control />", xml);
            string control = "<Control><Macro><Square>350/256/310/256</Square></Macro><KeyType><Square>" + keyTypeText + "</Square></KeyType></Control>";
            return xml.Replace("<Control />", control);
        }

        private static string SavedKeyTypeOfSquare(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("/DS4Windows/Control/KeyType/Square")?.InnerText;
        }

        [Theory]
        [InlineData("RepeatMacro")]
        [InlineData("HoldMacro")]   // 旧版が書いた値。読込だけ受け付ける
        public void Load_ReadsTheRepeatWhileHeldNames_AsMacroAndRepeatMacro(string written)
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "RepeatIn_" + written, written), "RepeatLoad_" + written);

            DS4KeyType keyType = loaded.GetDS4CSetting(Slot, DS4Controls.Square).keyType;
            Assert.True(keyType.HasFlag(DS4KeyType.Macro));
            Assert.True(keyType.HasFlag(DS4KeyType.RepeatMacro));
        }

        [Theory]
        [InlineData("RepeatMacro")]
        [InlineData("HoldMacro")]
        public void Save_WritesRepeatMacro_NeverTheLegacyHoldMacro(string written)
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "RepeatIn2_" + written, written), "RepeatLoad2_" + written);

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "RepeatResave_" + written);

            Assert.Equal("RepeatMacro", SavedKeyTypeOfSquare(resaved));
        }

        [Fact]
        public void APlainMacro_StaysAPlainMacro_AndIsSavedAsMacro()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "PlainIn", "Macro"), "PlainLoad");

            DS4KeyType keyType = loaded.GetDS4CSetting(Slot, DS4Controls.Square).keyType;
            Assert.True(keyType.HasFlag(DS4KeyType.Macro));
            Assert.False(keyType.HasFlag(DS4KeyType.RepeatMacro));
            Assert.Equal("Macro", SavedKeyTypeOfSquare(PersistenceHarness.SaveProfile(ctx, loaded, "PlainResave")));
        }
    }
}
