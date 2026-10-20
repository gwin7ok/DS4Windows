using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step8 8-0（Phase8-Step8-Plan.md §5.1。Q2＝C、Q9＝A）: 廃止された「ジャイロスワイプ」機能の設定を、<b>解釈せずに、そのまま保持する</b>入れ物（プロファイルごと）。
    /// 保持するのは 2 つ: (1) 設定の <c>&lt;GyroSwipeSettings&gt;</c> ノード（既定値と異なるときだけ）、(2) <c>&lt;Control&gt;</c> 内の
    /// <c>GyroSwipeLeft／Right／Up／Down</c> の割り当ての項目（<c>Button</c>・<c>Macro</c>・<c>Key</c>・<c>Extras</c>・<c>KeyType</c> の各グループの子）。
    /// ユーザーが編集画面で「残す」を選んだとき、保存しても内容がファイルに残る。「削除」を選ぶと、内容を捨てる（<see cref="Clear"/>）。
    /// 設定値としては使わず、実行にも関わらない。7.8d の <see cref="RemovedShiftSettings"/> と同じ進め方。
    /// </summary>
    public sealed class RemovedGyroSwipeSettings
    {
        // 廃止前の既定値（保存で毎回書いていた値。GyroDirectionalSwipeInfo の既定と、BackingStore.SaTriggerCondString(true) ＝ "and"）
        private static readonly string[] DefaultNames = { "DeadZoneX", "DeadZoneY", "Triggers", "TriggerCond", "TriggerTurns", "XAxis", "DelayTime" };
        private static readonly string[] DefaultValues = { "80", "80", "-1", "and", "True", "Yaw", "0" };

        private static readonly string[] SwipeControlNames = { "GyroSwipeLeft", "GyroSwipeRight", "GyroSwipeUp", "GyroSwipeDown" };

        private string _settingsOuterXml;
        private readonly List<(string Parent, string OuterXml)> _entries = new List<(string, string)>();

        /// <summary>保持している内容があるか（既定と異なる設定、または割り当ての項目が 1 つ以上）。</summary>
        public bool HasContent => _settingsOuterXml != null || _entries.Count > 0;

        /// <summary>保持している <c>&lt;GyroSwipeSettings&gt;</c> ノードの XML（既定と異なるときだけ。なければ null）。</summary>
        public string SettingsOuterXml => _settingsOuterXml;

        /// <summary>保持している割り当ての項目の数。</summary>
        public int EntryCount => _entries.Count;

        /// <summary><c>&lt;Control&gt;</c> 内の項目の名前が、廃止されたジャイロスワイプの割り当てか。</summary>
        public static bool IsSwipeControlName(string name)
        {
            return Array.IndexOf(SwipeControlNames, name) >= 0;
        }

        /// <summary>読込の始めに呼ぶ。以前の内容を、すべて捨てる（読み込むたびに、取り込み直すため）。</summary>
        public void BeginCapture()
        {
            Clear();
        }

        /// <summary>
        /// 読み込んだ <c>&lt;GyroSwipeSettings&gt;</c> ノードを取り込む。ノードがない、または全項目が既定値（欠けた項目は既定とみなす）なら、保持しない。
        /// 以前の設定は、常に置き換わる。
        /// </summary>
        /// <returns>保持する設定があるか。</returns>
        public bool CaptureSettings(XmlNode swipeSettings)
        {
            if (swipeSettings == null || IsAllDefault(swipeSettings))
            {
                _settingsOuterXml = null;
                return false;
            }

            _settingsOuterXml = swipeSettings.OuterXml;
            return true;
        }

        /// <summary>
        /// 読み込んだ <c>&lt;Control&gt;</c> 内の、ジャイロスワイプの割り当ての項目（例: <c>&lt;Button&gt;</c> の子の <c>&lt;GyroSwipeLeft&gt;A&lt;/GyroSwipeLeft&gt;</c>）を取り込む。
        /// </summary>
        /// <param name="parentName">項目の親のグループの名前（<c>Button</c>、<c>Macro</c>、<c>Key</c>、<c>Extras</c>、<c>KeyType</c>）。</param>
        public void CaptureControlEntry(string parentName, XmlNode entry)
        {
            if (string.IsNullOrEmpty(parentName)) throw new ArgumentException("parentName is required", nameof(parentName));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            _entries.Add((parentName, entry.OuterXml));
        }

        /// <summary>保持している内容を、すべて捨てる（編集画面で「削除」を選んだとき、読込の始め）。</summary>
        public void Clear()
        {
            _settingsOuterXml = null;
            _entries.Clear();
        }

        /// <summary>
        /// プロファイルの保存で、<c>&lt;GyroSwipeSettings&gt;</c> を <paramref name="root"/> に書き出す。保持した設定があれば、そのまま。なければ、従来と同じ既定値の要素
        /// （廃止前も、既定値のときも毎回書き出していたため、形式は変わらない）。
        /// </summary>
        public void AppendSettingsTo(XmlDocument document, XmlElement root)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (root == null) throw new ArgumentNullException(nameof(root));

            if (_settingsOuterXml != null)
            {
                var holder = new XmlDocument();
                holder.LoadXml(_settingsOuterXml);
                root.AppendChild(document.ImportNode(holder.DocumentElement, true));
                return;
            }

            XmlElement element = document.CreateElement("GyroSwipeSettings");
            for (int i = 0; i < DefaultNames.Length; i++)
            {
                XmlNode child = document.CreateNode(XmlNodeType.Element, DefaultNames[i], null);
                child.InnerText = DefaultValues[i];
                element.AppendChild(child);
            }

            root.AppendChild(element);
        }

        /// <summary>
        /// プロファイルの保存で、保持した割り当ての項目を、<paramref name="control"/>（<c>&lt;Control&gt;</c>）の該当するグループへ戻す。
        /// グループがなければ作る。保持した項目がなければ、何もしない。項目は、元の属性・内容のまま。
        /// </summary>
        public void AppendControlsTo(XmlDocument document, XmlElement control)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (control == null) throw new ArgumentNullException(nameof(control));

            foreach ((string parent, string outerXml) in _entries)
            {
                XmlNode group = control.SelectSingleNode(parent);
                if (group == null)
                {
                    group = document.CreateElement(parent);
                    control.AppendChild(group);
                }

                var holder = new XmlDocument();
                holder.LoadXml(outerXml);
                group.AppendChild(document.ImportNode(holder.DocumentElement, true));
            }
        }

        private static bool IsAllDefault(XmlNode swipeSettings)
        {
            foreach (XmlNode child in swipeSettings.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element) continue;

                int index = Array.IndexOf(DefaultNames, child.Name);
                if (index < 0) return false;   // 知らない項目がある。そのまま残す

                if (!SameValue(child.InnerText, DefaultValues[index])) return false;
            }

            return true;
        }

        // 数値は数として、それ以外は大文字小文字を区別せずに比べる（"True"／"true"、"and"／"AND" を同じとみなす）
        private static bool SameValue(string text, string defaultText)
        {
            string trimmed = (text ?? string.Empty).Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int a)
                && int.TryParse(defaultText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
            {
                return a == b;
            }

            return string.Equals(trimmed, defaultText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
