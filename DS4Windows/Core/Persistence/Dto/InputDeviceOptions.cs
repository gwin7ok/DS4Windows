using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

/// <summary>
/// アプリ設定（AppSettings.xml）の <c>DeviceOptions</c> 節: 機種の系統ごとの設定。
/// 系統の並び（<see cref="InputDeviceFamily"/> の値の順）と、各系統の節の名前・設定項目は、<see cref="DeviceOptionsBase"/> の定義が持つ。
/// 系統を足しても、ここは変えない。保存形式は従来のまま（節の下に Enabled と、機種固有の項目の要素）。
/// </summary>
public class InputDeviceOptions : IXmlSerializable
{
    /// <summary>節の値を持つ入れ物（既定値で始まる）。</summary>
    public ControlServiceDeviceOptions Options { get; } = new ControlServiceDeviceOptions();

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        foreach (InputDeviceFamily family in Enum.GetValues(typeof(InputDeviceFamily)))
        {
            DeviceOptionsBase options = Options[family];
            writer.WriteStartElement(options.SectionName);
            foreach (ControllerSetting setting in options.Settings)
            {
                writer.WriteElementString(setting.XmlName, setting.Read());
            }

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// 節を読む。ない節・ない要素・解析できない真偽値は既定値のまま。解析できない列挙型の値は、設定全体の読み込みの失敗にする（従来の XML シリアライザーと同じ）。
    /// </summary>
    public void ReadXml(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        XmlDocument doc = new XmlDocument();
        XmlNode node = doc.ReadNode(reader);
        foreach (InputDeviceFamily family in Enum.GetValues(typeof(InputDeviceFamily)))
        {
            DeviceOptionsBase options = Options[family];
            XmlNode section = node.SelectSingleNode(options.SectionName);
            if (section == null)
            {
                continue;
            }

            foreach (ControllerSetting setting in options.Settings)
            {
                string text = section.SelectSingleNode(setting.XmlName)?.InnerText;
                Action apply = setting.ParseOrKeep(text);
                if (apply != null)
                {
                    apply();
                }
                else if (text != null && setting.IsEnum)
                {
                    throw new InvalidOperationException(setting.XmlName + " の値を解析できない: " + text);
                }
            }
        }
    }
}
