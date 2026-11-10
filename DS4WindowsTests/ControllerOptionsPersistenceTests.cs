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
    }
}
