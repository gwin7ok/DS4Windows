using System.Linq;
using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4 W0（Phase10-Step4-Plan.md §3.2・§5）: プロファイル XML（<c>Profile.xml</c>）の <c>&lt;Control&gt;</c> の、現在の保存・読み込みの形式の特性化。
    /// Step4 の W1 で、SA 専用の割り当ての節（<c>&lt;Special&gt;</c>）を足す。そのとき、既存の節（Button／Macro／Key／Extras／KeyType）の形式・要素の順序が、
    /// 変わらないことを、この検査で守る。また、将来の節（未知の要素）を、現在は読み飛ばして、保存し直すと失う、という互換の事実を固定する。製品コードは変えない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W0ControlProfileXmlFormatTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private const string ControlSection =
            "<Control>" +
            "<Button><Cross>A</Cross><L1>Unbound</L1></Button>" +
            "<Macro><Square>350/256</Square></Macro>" +
            "<Key><Circle>65</Circle></Key>" +
            "<Extras><Triangle>1,2,0,0,0,0,0,0,0</Triangle></Extras>" +
            "<KeyType><Square>Macro</Square><Circle>Toggle</Circle><L1>Unbound</L1></KeyType>" +
            "</Control>";

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

        [Fact]
        public void Load_ReadsEachKindOfAssignment()
        {
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "Step4W0In1", ControlSection), "Step4W0Load1");

            DS4ControlSettings cross = loaded.GetDS4CSetting(Slot, DS4Controls.Cross);
            Assert.Equal(DS4ControlSettings.ActionType.Button, cross.actionType);
            Assert.Equal(X360Controls.A, cross.action.actionBtn);

            DS4ControlSettings l1 = loaded.GetDS4CSetting(Slot, DS4Controls.L1);
            Assert.Equal(DS4ControlSettings.ActionType.Button, l1.actionType);
            Assert.Equal(X360Controls.Unbound, l1.action.actionBtn);

            DS4ControlSettings square = loaded.GetDS4CSetting(Slot, DS4Controls.Square);
            Assert.Equal(DS4ControlSettings.ActionType.Macro, square.actionType);
            Assert.Equal(new[] { 350, 256 }, square.action.actionMacro);
            Assert.True(square.keyType.HasFlag(DS4KeyType.Macro));

            DS4ControlSettings circle = loaded.GetDS4CSetting(Slot, DS4Controls.Circle);
            Assert.Equal(DS4ControlSettings.ActionType.Key, circle.actionType);
            Assert.Equal(65, circle.action.actionKey);
            Assert.True(circle.keyType.HasFlag(DS4KeyType.Toggle));

            DS4ControlSettings triangle = loaded.GetDS4CSetting(Slot, DS4Controls.Triangle);
            Assert.Equal(DS4ControlSettings.ActionType.Default, triangle.actionType);
            Assert.Equal("1,2,0,0,0,0,0,0,0", triangle.extras);
        }

        [Fact]
        public void Save_WritesTheSectionsInTheFixedOrder_WithTheSameValues()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "Step4W0In2", ControlSection), "Step4W0Load2");

            XmlDocument doc = Parse(PersistenceHarness.SaveProfile(ctx, loaded, "Step4W0Resave2"));
            XmlNode control = doc.SelectSingleNode("/DS4Windows/Control");

            Assert.NotNull(control);
            Assert.Equal(new[] { "Button", "Macro", "Key", "Extras", "KeyType" }, control.ChildNodes.Cast<XmlNode>().Select(n => n.Name).ToArray());
            Assert.Equal(X360ControlTables.getX360ControlString(X360Controls.A), doc.SelectSingleNode("/DS4Windows/Control/Button/Cross").InnerText);
            Assert.Equal("350/256", doc.SelectSingleNode("/DS4Windows/Control/Macro/Square").InnerText);
            Assert.Equal("65", doc.SelectSingleNode("/DS4Windows/Control/Key/Circle").InnerText);
            Assert.Equal("1,2,0,0,0,0,0,0,0", doc.SelectSingleNode("/DS4Windows/Control/Extras/Triangle").InnerText);
            Assert.Equal("Macro", doc.SelectSingleNode("/DS4Windows/Control/KeyType/Square").InnerText);
            Assert.Equal("Toggle", doc.SelectSingleNode("/DS4Windows/Control/KeyType/Circle").InnerText);
            Assert.Equal("Unbound", doc.SelectSingleNode("/DS4Windows/Control/KeyType/L1").InnerText);
        }

        [Fact]
        public void ADefaultProfile_HasAnEmptyControlSection()
        {
            using var ctx = new PersistenceGoldenContext();

            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "Step4W0Empty");

            Assert.Contains("<Control />", xml);
        }

        [Fact]
        public void AnUnknownSection_InControl_IsIgnoredOnLoad_AndLostOnResave()
        {
            // 互換の事実（W1 で <Special> を読むようになったため、未知の節の例は <Future> にした。古い版のアプリが <Special> を、同じように、読み飛ばし、保存し直すと失うことは、ユーザーが許容済み）
            const string withUnknown =
                "<Control><Button><Cross>A</Cross></Button><Future><Square Type=\"Program\">C:\\x.exe</Square></Future></Control>";
            using var ctx = new PersistenceGoldenContext();

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, XmlWith(ctx, "Step4W0In4", withUnknown), "Step4W0Load4");

            Assert.Equal(X360Controls.A, loaded.GetDS4CSetting(Slot, DS4Controls.Cross).action.actionBtn);
            Assert.Equal(DS4ControlSettings.ActionType.Default, loaded.GetDS4CSetting(Slot, DS4Controls.Square).actionType);

            XmlDocument resaved = Parse(PersistenceHarness.SaveProfile(ctx, loaded, "Step4W0Resave4"));
            Assert.Null(resaved.SelectSingleNode("/DS4Windows/Control/Future"));
            Assert.NotNull(resaved.SelectSingleNode("/DS4Windows/Control/Button/Cross"));
        }
    }
}
