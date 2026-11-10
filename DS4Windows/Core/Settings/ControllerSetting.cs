using System;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 機種ごとのコントローラー設定の 1 項目の定義: XML の要素名と、値の読み書き。
    /// 保存と読み込み（<see cref="ControllerOptionsStore"/>）は、この定義の並びを順に処理する。機種の設定項目を足すときは、設定クラスに定義を 1 つ足すだけでよい（保存用の DTO は不要）。
    /// 値の文字列は従来の保存形式のまま: 真偽値は True／False、列挙型は名前。
    /// </summary>
    public sealed class ControllerSetting
    {
        private readonly Func<string> read;
        private readonly Func<string, Action> parse;

        private ControllerSetting(string xmlName, bool isEnum, Func<string> read, Func<string, Action> parse)
        {
            XmlName = xmlName;
            IsEnum = isEnum;
            this.read = read;
            this.parse = parse;
        }

        /// <summary>XML の要素名。</summary>
        public string XmlName { get; }

        /// <summary>列挙型の項目か（アプリ設定の読み込みでは、列挙型の値が解析できないと全体の読み込みが失敗する）。</summary>
        public bool IsEnum { get; }

        /// <summary>現在の値を保存用の文字列にする。</summary>
        public string Read() => read();

        /// <summary>
        /// 保存された文字列（要素がないときは null）を解析し、適用の動作を返す。列挙型で解析できない値のときは null を返す（読み込み全体を取りやめる）。
        /// 従来の保存の読み込みと同じく、要素がないときと真偽値の解析に失敗したときは、その型の既定値（false、列挙型は 0）を適用する。
        /// </summary>
        public Action Parse(string text) => parse(text);

        /// <summary>
        /// 保存された文字列を、解析できたときだけ適用する動作にする（要素がない、または解析できないときは null。現在の値を保つ）。
        /// アプリ設定（機種の系統ごとの設定）の読み込みで使う。列挙型は、解析できないとき null を返す（呼び出し側が失敗にする）。
        /// </summary>
        public Action ParseOrKeep(string text)
        {
            if (text == null)
            {
                return null;
            }

            if (IsEnum)
            {
                return parse(text);
            }

            return bool.TryParse(text, out _) ? parse(text) : null;
        }

        public static ControllerSetting Bool<TOptions>(string xmlName, TOptions options, Func<TOptions, bool> get, Action<TOptions, bool> set)
        {
            return new ControllerSetting(xmlName, false,
                () => get(options).ToString(),
                text =>
                {
                    bool value = false;
                    if (text != null)
                    {
                        bool.TryParse(text, out value);
                    }

                    return () => set(options, value);
                });
        }

        public static ControllerSetting Enum<TOptions, TEnum>(string xmlName, TOptions options, Func<TOptions, TEnum> get, Action<TOptions, TEnum> set)
            where TEnum : struct, Enum
        {
            return new ControllerSetting(xmlName, true,
                () => get(options).ToString(),
                text =>
                {
                    TEnum value = default;
                    if (text != null && !(System.Enum.TryParse(text.Trim(), out value) && System.Enum.IsDefined(typeof(TEnum), value)))
                    {
                        return null;
                    }

                    return () => set(options, value);
                });
        }
    }
}
