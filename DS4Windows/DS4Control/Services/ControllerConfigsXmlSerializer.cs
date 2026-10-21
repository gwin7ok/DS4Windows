using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step10c W2（Q1＝A）: コントローラー固有設定（<c>ControllerConfigs.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>createControllerConfigs</c>／<c>LoadControllerConfigsForDevice</c>／<c>SaveControllerConfigsForDevice</c>（移動のみ）。
    /// </summary>
    public class ControllerConfigsXmlSerializer
    {
        private readonly BackingStore store;

        public ControllerConfigsXmlSerializer(BackingStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool createControllerConfigs()
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
            catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + store.m_controllerConfigs, false); saved = false; }

            return saved;
        }

        public bool LoadControllerConfigsForDevice(DS4Device device)
        {
            bool loaded = false;

            if (device == null) return false;
            if (!File.Exists(store.m_controllerConfigs)) createControllerConfigs();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(store.m_controllerConfigs);

                XmlNode node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() + "\"]");
                if (node != null)
                {
                    // Phase9 Steering 削除 W2: 旧「車輪の較正値」（wheelCenterPoint／wheel90DegPointLeft／wheel90DegPointRight）の読み込みは削除した。古いファイルに要素が残っていても、無視される

                    device.optionsStore?.LoadSettings(xmlDoc, node);

                    loaded = true;
                }
            }
            catch
            {
                AppLogger.LogToGui("ControllerConfigs.xml can't be found.", false);
                loaded = false;
            }

            return loaded;
        }

        public bool SaveControllerConfigsForDevice(DS4Device device)
        {
            bool saved = true;

            if (device == null) return false;
            if (!File.Exists(store.m_controllerConfigs)) createControllerConfigs();

            try
            {
                //XmlNode node = null;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(store.m_controllerConfigs);

                XmlNode node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() + "\"]");
                XmlNode xmlControllersNode = xmlDoc.SelectSingleNode("/Controllers");
                if (node == null)
                {
                    XmlElement el = xmlDoc.CreateElement("Controller");
                    node = xmlControllersNode.AppendChild(el);
                }
                else
                {
                    node.RemoveAll();
                }

                XmlAttribute macAttr = xmlDoc.CreateAttribute("Mac");
                macAttr.Value = device.getMacAddress();
                node.Attributes.Append(macAttr);

                XmlAttribute contTypeAttr = xmlDoc.CreateAttribute("ControllerType");
                contTypeAttr.Value = device.DeviceType.ToString();
                node.Attributes.Append(contTypeAttr);

                // Phase9 Steering 削除 W2: 旧「車輪の較正値」の書き込みは削除した（Mac と ControllerType の属性、optionsStore の設定は、これまでどおり）
                device.optionsStore?.PersistSettings(xmlDoc, node);

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
                xmlDoc.AppendChild(xmlControllersNode);

                // Save XML to file
                xmlDoc.Save(store.m_controllerConfigs);
            }
            catch (UnauthorizedAccessException)
            {
                AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + store.m_controllerConfigs, false);
                saved = false;
            }

            return saved;
        }
    }
}
