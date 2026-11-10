using System.Linq;
using System.Xml;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 機種ごとのコントローラー設定（<see cref="ControllerOptionsStore"/> の派生）の XML の書き出しと読み込み。
    /// 書き出しは親の節の下へ機種の節を 1 つだけ置き（2 回目は置き換える）、読み込みは書き出した値を戻す。節がないときは何もしない。
    /// </summary>
    public class ControllerOptionsPersistenceTests
    {
        private static XmlNode NewParent(out XmlDocument doc)
        {
            doc = new XmlDocument();
            return doc.AppendChild(doc.CreateElement("Root"));
        }

        [Fact]
        public void SwitchPro_RoundTripsTheHomeLed_AndKeepsOneSection()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            var options = new SwitchProControllerOptions(InputDeviceType.SwitchPro) { EnableHomeLED = false };

            options.PersistSettings(doc, parent);
            options.PersistSettings(doc, parent); // 2 回目は置き換える

            Assert.Single(parent.SelectNodes(SwitchProControllerOptions.XML_ELEMENT_NAME).Cast<XmlNode>());

            var loaded = new SwitchProControllerOptions(InputDeviceType.SwitchPro);
            Assert.True(loaded.EnableHomeLED);
            loaded.LoadSettings(doc, parent);
            Assert.False(loaded.EnableHomeLED);
        }

        [Fact]
        public void JoyCon_RoundTripsTheHomeLed()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            new JoyConControllerOptions(InputDeviceType.JoyConL) { EnableHomeLED = false }.PersistSettings(doc, parent);

            var loaded = new JoyConControllerOptions(InputDeviceType.JoyConL);
            loaded.LoadSettings(doc, parent);
            Assert.False(loaded.EnableHomeLED);
        }

        [Fact]
        public void DS4_RoundTripsTheCopyCatFlag()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            new DS4ControllerOptions(InputDeviceType.DS4) { IsCopyCat = true }.PersistSettings(doc, parent);

            var loaded = new DS4ControllerOptions(InputDeviceType.DS4);
            Assert.False(loaded.IsCopyCat);
            loaded.LoadSettings(doc, parent);
            Assert.True(loaded.IsCopyCat);
        }

        [Fact]
        public void LoadingWithoutTheSection_ChangesNothing()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            var options = new SwitchProControllerOptions(InputDeviceType.SwitchPro);
            options.LoadSettings(doc, parent);
            Assert.True(options.EnableHomeLED);
        }

        [Fact]
        public void DualSense_PersistsUnderItsOwnSectionName()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            new DualSenseControllerOptions(InputDeviceType.DualSense).PersistSettings(doc, parent);
            Assert.NotNull(parent.SelectSingleNode(DualSenseControllerOptions.XML_ELEMENT_NAME));
            Assert.Null(parent.SelectSingleNode(DS4ControllerOptions.XML_ELEMENT_NAME));
        }

        [Fact]
        public void DualSense_WritesTheLegacyXmlShape_AndRoundTripsTheModes()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            var options = new DualSenseControllerOptions(InputDeviceType.DualSense)
            {
                LedMode = DualSenseControllerOptions.LEDBarMode.BatteryPercentage,
                MuteLedMode = DualSenseControllerOptions.MuteLEDMode.Pulse,
            };
            options.PersistSettings(doc, parent);

            // 従来の保存形式（宣言・名前空間なし、要素の順は LEDBarMode、MuteLEDMode）
            Assert.Equal("<DualSenseSupportSettings><LEDBarMode>BatteryPercentage</LEDBarMode><MuteLEDMode>Pulse</MuteLEDMode></DualSenseSupportSettings>",
                parent.FirstChild.OuterXml);

            var loaded = new DualSenseControllerOptions(InputDeviceType.DualSense);
            loaded.LoadSettings(doc, parent);
            Assert.Equal(DualSenseControllerOptions.LEDBarMode.BatteryPercentage, loaded.LedMode);
            Assert.Equal(DualSenseControllerOptions.MuteLEDMode.Pulse, loaded.MuteLedMode);
        }

        [Fact]
        public void SwitchPro_WritesTheLegacyXmlShape()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            new SwitchProControllerOptions(InputDeviceType.SwitchPro) { EnableHomeLED = false }.PersistSettings(doc, parent);
            Assert.Equal("<SwitchProSupportSettings><EnableHomeLED>False</EnableHomeLED></SwitchProSupportSettings>", parent.FirstChild.OuterXml);
        }

        [Fact]
        public void DualSense_WithAnUnknownEnumName_AppliesNothing()
        {
            XmlNode parent = NewParent(out XmlDocument doc);
            parent.InnerXml = "<DualSenseSupportSettings><LEDBarMode>On</LEDBarMode><MuteLEDMode>Bogus</MuteLEDMode></DualSenseSupportSettings>";
            var options = new DualSenseControllerOptions(InputDeviceType.DualSense);
            options.LoadSettings(doc, parent);
            Assert.Equal(DualSenseControllerOptions.LEDBarMode.MultipleControllers, options.LedMode);
        }
    }
}
