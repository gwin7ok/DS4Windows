/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels;
using static DS4Windows.Mouse;

namespace DS4WinWPF.DS4Control.DTOXml
{

    public class DS4ControlLightbarMacroAssignmentSerializer : DS4ControlAssignmentSerializerBase, IXmlSerializable
    {
        private Dictionary<DS4Controls, string> customMapMacro
            = new Dictionary<DS4Controls, string>();
        [XmlIgnore]
        public Dictionary<DS4Controls, string> CustomMapMacro => customMapMacro;

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            XmlDocument tempDoc = new XmlDocument();
            string tempXml = reader.ReadOuterXml();
            XmlReader tempXmlReader = XmlReader.Create(new StringReader(tempXml));
            tempDoc.Load(tempXmlReader);
            XmlNode parentNode = tempDoc.SelectSingleNode("LightbarMacro");
            if (parentNode != null)
            {
                foreach (XmlNode item in parentNode.ChildNodes)
                {
                    if (item.InnerText != string.Empty &&
                        Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        if (item.Attributes["Trigger"] != null)
                        {
                            int.TryParse(item.Attributes["Trigger"].Value, out int shiftT);
                            shiftTriggers.TryAdd(currentControl, shiftT);
                        }

                        customMapMacro.Add(Global.getDS4ControlsByName(item.Name), item.InnerText);
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (KeyValuePair<DS4Controls, string> pair in customMapMacro)
            {
                writer.WriteStartElement(pair.Key.ToString());
                if (shiftTriggers.TryGetValue(pair.Key, out int shiftTrigger) &&
                    shiftTrigger > 0)
                {
                    writer.WriteAttributeString("Trigger", shiftTrigger.ToString());
                }

                writer.WriteValue(pair.Value.ToString());
                writer.WriteEndElement();
            }
        }
    }
}