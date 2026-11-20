using System;
using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// プロファイル 1 回分の読込で、節をまたいで共有する状態。
    /// 節ごとの局所変数（<c>Item</c> など）は、各節が自前で持つ。
    /// <see cref="LightbarSettings"/>／<see cref="LightInfo"/> は、<c>store.ResetProfile</c> の前に取得した参照のまま使う。
    /// </summary>
    internal sealed class ProfileLoadContext
    {
        public ProfileLoadContext(BackingStore store, XmlDocument xdoc, string rootName, int device, IProfileLoadTarget control,
            LightbarSettingInfo lightbarSettings, LightbarDS4WinInfo lightInfo)
        {
            ArgumentNullException.ThrowIfNull(store);
            Store = store;
            ArgumentNullException.ThrowIfNull(xdoc);
            Xdoc = xdoc;
            ArgumentNullException.ThrowIfNull(rootName);
            RootName = rootName;
            Device = device;
            Control = control;
            LightbarSettings = lightbarSettings;
            LightInfo = lightInfo;
        }

        public BackingStore Store { get; }
        public XmlDocument Xdoc { get; }
        public string RootName { get; }
        public int Device { get; }
        public IProfileLoadTarget Control { get; }
        public LightbarSettingInfo LightbarSettings { get; }
        public LightbarDS4WinInfo LightInfo { get; }

        public DS4Windows.Core.Abstractions.IProfileSettingsService ProfileSettings { get; set; } = null!;   // 生成の直後に、LegacyProfileXmlSerializer の初期化子が必ず設定する


        /// <summary><c>UseTPforControls</c> 要素の有無。雑多の節が書き、<c>TouchpadOutputMode</c> を読む節が読む。</summary>
        public bool TpForControlsPresent { get; set; }

        public DS4Windows.Core.Abstractions.IEnvironmentService Environment { get; set; } = null!;   // 同上

        /// <summary>読込の引数 <c>xinputChange</c>（起動前の読込では、出力デバイスの状態を確認しない）。</summary>
        public bool XinputChange { get; set; }

        /// <summary>読込の前に有効だった出力コントローラーの種別（<c>ResetProfile</c> の前に取得）。</summary>
        public OutContType OldContType { get; set; }

        /// <summary>出力デバイスの状態の確認の結果。出力コントローラーの節が書き、読込の最後の <c>PostLoadSnippet</c> が使う。</summary>
        public bool XinputPlug { get; set; }

        public bool XinputStatus { get; set; }

        // ---- 要素の読み取りの補助 ----
        // 要素が「ない」ときは何もしない（既定値のまま）。要素が「ある」ときの扱いは、型ごとに下のとおり。
        // 節の読み込みを、要素ごとの try/catch ではなく、この補助で書く（変異ゴールデン ProfileLoadMutationGoldenTests が動きを固定している）。

        /// <summary>
        /// 整数の要素を読む。要素がない、または解析に失敗したとき（空の要素を含む）は何もしない（既定値のまま）。
        /// 解析に成功した値が範囲の外のときは、範囲へ丸めて代入する。
        /// </summary>
        public void ReadInt(XmlNode source, string xpath, int min, int max, Action<int> assign)
        {
            XmlNode? node = source.SelectSingleNode(xpath);
            if (node == null) return;
            if (int.TryParse(node.InnerText, out int value)) assign(Math.Min(Math.Max(value, min), max));
        }

        /// <summary><see cref="ReadInt"/> の実数版。</summary>
        public void ReadDouble(XmlNode source, string xpath, double min, double max, Action<double> assign)
        {
            XmlNode? node = source.SelectSingleNode(xpath);
            if (node == null) return;
            if (double.TryParse(node.InnerText, out double value)) assign(Math.Min(Math.Max(value, min), max));
        }

        /// <summary>真偽値の要素を読む。要素がない、または解析に失敗したときは何もしない。</summary>
        public void ReadBool(XmlNode source, string xpath, Action<bool> assign)
        {
            XmlNode? node = source.SelectSingleNode(xpath);
            if (node != null && bool.TryParse(node.InnerText ?? string.Empty, out bool value)) assign(value);
        }

        /// <summary>列挙型の要素を読む。要素がない、または解析に失敗したときは何もしない。</summary>
        public void ReadEnum<T>(XmlNode source, string xpath, Action<T> assign) where T : struct
        {
            XmlNode? node = source.SelectSingleNode(xpath);
            if (node != null && Enum.TryParse(node.InnerText ?? string.Empty, out T value)) assign(value);
        }

        /// <summary>要素が欠けていた、または読めなかったとき true（読込の最後で、欠けた設定を追記して保存し直す判断に使う）。</summary>
        public bool MissingSetting { get; set; }
    }
}
