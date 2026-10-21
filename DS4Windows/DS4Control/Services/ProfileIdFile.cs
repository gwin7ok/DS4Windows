using System;
using System.IO;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase10-Step4d W1（Phase10-Step4d-Plan.md §3.1）: プロファイルのファイルの固定 ID（<c>&lt;ProfileId&gt;</c>）の読み書き。状態を持たない。
    /// ID は GUID の <c>N</c> 形式（32 文字の 16 進数）で、ルート要素の直下に置く。名前の変更（ファイル名の変更）では変わらない。
    /// 古いバージョンは、この要素を未知の要素として読み飛ばす。<see cref="LegacyProfileXmlSerializer"/>（保存）と <see cref="ProfileIdentityService"/>（登録簿）が使う。
    /// </summary>
    internal sealed class ProfileIdFile
    {
        public const string ElementName = "ProfileId";

        public string NewId() => Guid.NewGuid().ToString("N");

        /// <summary>ID として妥当か（GUID の <c>N</c> 形式）。</summary>
        public bool IsValid(string id) => !string.IsNullOrEmpty(id) && Guid.TryParseExact(id, "N", out _);

        /// <summary>ファイルから ID を読む。ファイルがない・読めない・要素がない・形式が不正なときは null。</summary>
        public string ReadId(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, IgnoreComments = true, IgnoreWhitespace = true };
                using (XmlReader reader = XmlReader.Create(path, settings))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.Name == ElementName)
                        {
                            string id = reader.ReadElementContentAsString().Trim();
                            return IsValid(id) ? id : null;
                        }
                    }
                }
            }
            catch
            {
                // 壊れたファイル、読み取りの競合: ID なしとして扱う
            }

            return null;
        }

        /// <summary>保存先のファイルがすでに ID を持つならそれを、なければ新しい ID を返す（上書き保存は ID を保ち、新しい名前の保存は新しい ID を振る）。</summary>
        public string ReadIdOrNew(string path) => ReadId(path) ?? NewId();

        /// <summary>
        /// ファイルに ID を書く（すでにあれば置き換え、なければルート直下の先頭に足す）。ほかの内容は変えない。書けなければ偽。
        /// 呼び出し側が、同一 XML 排他ロック（<c>ProfileXmlStore.XmlIoLock</c>）を取る。
        /// </summary>
        public bool WriteId(string path, string id)
        {
            if (!IsValid(id)) return false;

            try
            {
                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.Load(path);
                XmlElement root = doc.DocumentElement;
                if (root == null) return false;

                XmlNode existing = root.SelectSingleNode(ElementName);
                if (existing != null)
                {
                    existing.InnerText = id;
                }
                else
                {
                    XmlElement element = doc.CreateElement(ElementName);
                    element.InnerText = id;
                    root.PrependChild(element);
                }

                doc.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
