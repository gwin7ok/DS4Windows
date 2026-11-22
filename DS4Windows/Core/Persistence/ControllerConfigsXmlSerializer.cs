using System;
using System.IO;
using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>
    /// コントローラー固有設定（<c>ControllerConfigs.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>CreateControllerConfigs</c>／<c>LoadControllerConfigsForDevice</c>／<c>SaveControllerConfigsForDevice</c>（移動のみ）。
    /// </summary>
    public class ControllerConfigsXmlSerializer
    {
        private readonly BackingStore store;

        public ControllerConfigsXmlSerializer(BackingStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            this.store = store;
        }

        public bool CreateControllerConfigs()
        {
            bool saved = true;
            XmlDocument configXdoc = new XmlDocument();
            XmlNode Node;

            Node = configXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateWhitespace("\r\n");
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateNode(XmlNodeType.Element, "Controllers", "");
            configXdoc.AppendChild(Node);

            try { configXdoc.Save(store.m_controllerConfigs); }
            catch (UnauthorizedAccessException) { AppLogger.LogUser(AppLogLevel.Warn, "Log_Xml_UnauthorizedSave", store.m_controllerConfigs); saved = false; }

            return saved;
        }

        public bool LoadControllerConfigsForDevice(IConfigurableDevice device)
        {
            bool loaded = false;

            if (device == null) return false;
            if (!File.Exists(store.m_controllerConfigs)) CreateControllerConfigs();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(store.m_controllerConfigs);

                XmlNode? node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.MacAddress + "\"]");
                if (node != null)
                {
                    // 旧「車輪の較正値」（wheelCenterPoint／wheel90DegPointLeft／wheel90DegPointRight）の読み込みは削除した。古いファイルに要素が残っていても、無視される

                    device.OptionsStore?.LoadSettings(xmlDoc, node);

                    loaded = true;
                }
            }
            catch
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Xml_ControllerConfigsNotFound");
                loaded = false;
            }

            return loaded;
        }

        public bool SaveControllerConfigsForDevice(IConfigurableDevice device)
        {
            bool saved = true;

            if (device == null) return false;
            if (!File.Exists(store.m_controllerConfigs)) CreateControllerConfigs();

            try
            {
                //XmlNode node = null;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(store.m_controllerConfigs);

                XmlNode? node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.MacAddress + "\"]");
                XmlNode? xmlControllersNode = xmlDoc.SelectSingleNode("/Controllers");
                if (node == null)
                {
                    XmlElement el = xmlDoc.CreateElement("Controller");
                    node = xmlControllersNode.Required().AppendChild(el);
                }
                else
                {
                    node.RemoveAll();
                }

                XmlAttribute macAttr = xmlDoc.CreateAttribute("Mac");
                macAttr.Value = device.MacAddress;
                node.Required().Attributes!.Append(macAttr);   // 要素ノードは、必ず属性のコレクションを持つ

                XmlAttribute contTypeAttr = xmlDoc.CreateAttribute("ControllerType");
                contTypeAttr.Value = device.DeviceType.ToString();
                node.Required().Attributes!.Append(contTypeAttr);   // 同上

                device.OptionsStore?.PersistSettings(xmlDoc, node.Required());

                // Remove old elements
                xmlDoc.RemoveAll();

                XmlNode Node;
                Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateWhitespace("\r\n");
                xmlDoc.AppendChild(Node);

                // Write old Controllers node back in
                xmlDoc.AppendChild(xmlControllersNode.Required());

                // Save XML to file
                xmlDoc.Save(store.m_controllerConfigs);
            }
            catch (UnauthorizedAccessException)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Xml_UnauthorizedSave", store.m_controllerConfigs);
                saved = false;
            }

            return saved;
        }
    }
}
