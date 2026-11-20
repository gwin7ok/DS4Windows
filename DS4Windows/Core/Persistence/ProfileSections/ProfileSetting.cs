using System;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// プロファイルのルート直下の要素 1 つの定義: 要素名、保存する文字列の作り方、読み込んだ値の適用。
    /// 保存と読み込みは、定義の並び（<see cref="ProfileGeneralSettings"/> など）を順に処理する。項目を足すときは、並びへ 1 行足す。
    /// 保存の順序は並びの順（XML の要素の順序は、黄金テストが固定している）。
    /// 読み込みの動き: 要素がないときは何もしない（<see cref="MarksMissing"/> が真なら、欠けた設定として印を付ける）。
    /// 要素があって解析に失敗したときは何もしない（既定値のまま）。
    /// </summary>
    internal sealed class ProfileSetting
    {
        private readonly Func<ProfileSaveContext, string> read;
        private readonly Func<ProfileLoadContext, string, bool>? apply;

        private ProfileSetting(string xmlName, Func<ProfileSaveContext, string> read, Func<ProfileLoadContext, string, bool>? apply, bool marksMissing)
        {
            XmlName = xmlName;
            this.read = read;
            this.apply = apply;
            MarksMissing = marksMissing;
        }

        public string XmlName { get; }

        /// <summary>要素がないとき、読み込みの最後に欠けた設定を追記して保存し直す印を付けるか。</summary>
        public bool MarksMissing { get; }

        /// <summary>読み込みの定義を持つか（保存だけの定義は、読み込みを節に手書きで残している）。</summary>
        public bool CanLoad => apply != null;

        public static ProfileSetting Bool(string xmlName, Func<ProfileSaveContext, bool> get, Action<ProfileLoadContext, bool> set, bool marksMissing = true)
            => new ProfileSetting(xmlName, c => get(c).ToString(),
                (c, text) => { if (!bool.TryParse(text, out bool v)) return false; set(c, v); return true; }, marksMissing);

        public static ProfileSetting Int(string xmlName, Func<ProfileSaveContext, int> get, Action<ProfileLoadContext, int> set, bool marksMissing = true)
            => new ProfileSetting(xmlName, c => get(c).ToString(),
                (c, text) => { if (!int.TryParse(text, out int v)) return false; set(c, v); return true; }, marksMissing);

        public static ProfileSetting Byte(string xmlName, Func<ProfileSaveContext, byte> get, Action<ProfileLoadContext, byte> set, bool marksMissing = true)
            => new ProfileSetting(xmlName, c => get(c).ToString(),
                (c, text) => { if (!byte.TryParse(text, out byte v)) return false; set(c, v); return true; }, marksMissing);

        /// <summary>保存だけの定義（色・モードなど、読み込みが手書きの項目）。保存の順序を、並びの中で保つために置く。</summary>
        public static ProfileSetting SaveOnly(string xmlName, Func<ProfileSaveContext, string> get)
            => new ProfileSetting(xmlName, get, null, false);

        /// <summary>保存だけの定義。保存する文字列を、ストア・スロット・保存の文脈から作る。</summary>
        public static ProfileSetting Text(string xmlName, Func<BackingStore, int, ProfileSaveContext, string> get)
            => new ProfileSetting(xmlName, c => get(c.Store, c.Device, c), null, false);

        /// <summary>子の要素を持つ要素の定義（保存だけ）。子は並びの順に、この要素の下へ保存する。</summary>
        public static ProfileSetting Group(string xmlName, params ProfileSetting[] children)
            => new ProfileSetting(xmlName, null!, null, false) { children = children }; // 子を持つ定義は文字列を作らず、Save が子を保存する

        private ProfileSetting[]? children;

        public void Save(ProfileSaveContext context) => Save(context, context.RootElement);

        private void Save(ProfileSaveContext context, XmlNode parent)
        {
            if (children != null)
            {
                XmlElement group = context.Xdoc.CreateElement(XmlName);
                foreach (ProfileSetting child in children) child.Save(context, group);
                parent.AppendChild(group);
                return;
            }

            XmlNode node = context.Xdoc.CreateNode(XmlNodeType.Element, XmlName, null);
            node.InnerText = read(context);
            parent.AppendChild(node);
        }

        /// <summary>読み込む。欠けた設定として印を付けるべきときは true を返す。</summary>
        public bool Load(ProfileLoadContext context)
        {
            if (apply == null) return false;
            XmlNode? node = context.Xdoc.SelectSingleNode("/" + context.RootName + "/" + XmlName);
            if (node == null) return MarksMissing;
            apply(context, node.InnerText);
            return false;
        }
    }
}
