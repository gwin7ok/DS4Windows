using System;
using System.Xml;

namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step7 7.8d（Phase8-Step7-8d-Plan.md §4.1）: 廃止された「シフト」機能の設定（プロファイル設定ファイルの <c>&lt;ShiftControl&gt;</c> ノード）を、
    /// <b>解釈せずに、そのまま保持する</b>入れ物（プロファイルごと）。設定値としては使わず、実行にも関わらない。
    /// ユーザーが編集画面で「残す」を選んだとき、プロファイルを保存しても、設定ファイルに内容が残る（<see cref="AppendTo"/>）。「削除」を選ぶと、内容を捨てる（<see cref="Clear"/>）。
    /// </summary>
    public sealed class RemovedShiftSettings
    {
        private string _outerXml;

        /// <summary>保持している内容があるか（<c>&lt;ShiftControl&gt;</c> に、項目［孫の要素］が 1 つ以上ある）。</summary>
        public bool HasContent => _outerXml != null;

        /// <summary>保持している <c>&lt;ShiftControl&gt;</c> ノードの XML（なければ null）。</summary>
        public string OuterXml => _outerXml;

        /// <summary>
        /// 読み込んだ <c>&lt;ShiftControl&gt;</c> ノードから、内容を取り込む。項目がなければ（null、または空）、保持を空にする。
        /// 以前の内容は、常に置き換わる。
        /// </summary>
        /// <returns>保持する内容があるか。</returns>
        public bool Capture(XmlNode shiftControl)
        {
            if (shiftControl == null || shiftControl.SelectNodes("*/*").Count == 0)
            {
                _outerXml = null;
                return false;
            }

            _outerXml = shiftControl.OuterXml;
            return true;
        }

        /// <summary>保持している内容を捨てる（編集画面で「削除」を選んだとき）。</summary>
        public void Clear()
        {
            _outerXml = null;
        }

        /// <summary>
        /// プロファイルの保存で、<c>&lt;ShiftControl&gt;</c> を <paramref name="root"/> に書き出す。保持した内容があれば、そのまま。なければ、空の要素
        /// （従来、シフトの設定がないときも、空の <c>&lt;ShiftControl /&gt;</c> が書き出されていたため、形式は変わらない）。
        /// </summary>
        public void AppendTo(XmlDocument document, XmlElement root)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (root == null) throw new ArgumentNullException(nameof(root));

            if (_outerXml == null)
            {
                root.AppendChild(document.CreateElement("ShiftControl"));
                return;
            }

            var holder = new XmlDocument();
            holder.LoadXml(_outerXml);
            root.AppendChild(document.ImportNode(holder.DocumentElement, true));
        }
    }
}
