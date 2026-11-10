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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;

namespace DS4Windows.Core.Settings
{

    public abstract class ControllerOptionsStore
    {
        protected InputDeviceType deviceType;
        public InputDeviceType DeviceType { get => deviceType; }

        public ControllerOptionsStore(InputDeviceType deviceType)
        {
            this.deviceType = deviceType;
        }

        public virtual void PersistSettings(XmlDocument xmlDoc, XmlNode node)
        {
        }

        public virtual void LoadSettings(XmlDocument xmlDoc, XmlNode node)
        {
        }

        // 機種ごとの設定の DTO を XML の断片にして、親の節の下へ置く（同名の節があれば置き換える）。保存形式は機種に関わらず同じ
        protected static void PersistDto<TDto>(XmlDocument xmlDoc, XmlNode node, string elementName, TDto dto) where TDto : class
        {
            string xmlText;
            XmlSerializer serializer = new XmlSerializer(typeof(TDto));

            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = false,
                        OmitXmlDeclaration = true, // 宣言なしの部分的な XML
                    });

                // xmlns:xsi と xmlns:xsd は出力しない
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                xmlText = strWriter.ToString();
            }

            XmlNode fragment = xmlDoc.CreateDocumentFragment();
            fragment.InnerXml = xmlText;

            XmlNode existing = node.SelectSingleNode(elementName);
            if (existing != null)
            {
                node.RemoveChild(existing);
            }

            node.AppendChild(fragment);
        }

        // 親の節の下の該当の節を DTO として読み、適用する。節がない場合と、XML が DTO の形でない場合（InvalidOperationException）は、何もしない
        protected static void LoadDto<TDto>(XmlNode node, string elementName, Action<TDto> apply) where TDto : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TDto));
            XmlNode baseNode = node.SelectSingleNode(elementName);
            if (baseNode == null)
                return;

            try
            {
                using var stringReader = new StringReader(baseNode.OuterXml);
                using var xmlReader = XmlReader.Create(stringReader);
                TDto dto = serializer.Deserialize(xmlReader) as TDto;
                apply(dto);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
