#nullable enable
using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DS4Windows.Core.Persistence.Dto
{
    /// <summary>
    /// DTO を、設定ファイルの形式（UTF-8 の宣言、インデント、先頭のコメントと空行 2 行、名前空間の宣言なし）の文字列へ直す。
    /// 設定ファイルを書く各シリアライザーが共有する（ファイルへの書き込みと例外の扱いは、呼び出し側が決める）。
    /// </summary>
    public static class XmlDtoText
    {
        public static string Serialize(XmlSerializer serializer, object dto, string headerComment, bool writeStartDocument = false)
        {
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                if (writeStartDocument) xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(headerComment);
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // xmlns:xsi と xmlns:xsd は出力しない
                serializer.Serialize(xmlWriter, dto, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                return strWriter.ToString();
            }
        }
    }
}
