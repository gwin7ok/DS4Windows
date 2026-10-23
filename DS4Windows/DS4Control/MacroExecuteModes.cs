using System;

namespace DS4Windows
{
    /// <summary>
    /// R-2: <see cref="MacroExecuteMode"/> と、実行時の表現（<see cref="DS4KeyType"/> の <c>RepeatMacro</c>／<c>RepeatLatchMacro</c>）・保存する文字列の変換。
    /// </summary>
    public static class MacroExecuteModes
    {
        /// <summary><paramref name="keyType"/> のビットから、モードを求める（<c>RepeatLatchMacro</c> を優先。どちらもなければ <see cref="MacroExecuteMode.Once"/>）。</summary>
        public static MacroExecuteMode FromKeyType(DS4KeyType keyType)
        {
            if (keyType.HasFlag(DS4KeyType.RepeatLatchMacro)) return MacroExecuteMode.Toggle;
            if (keyType.HasFlag(DS4KeyType.RepeatMacro)) return MacroExecuteMode.Held;
            return MacroExecuteMode.Once;
        }

        /// <summary><paramref name="keyType"/> の <c>RepeatMacro</c>／<c>RepeatLatchMacro</c> のビットだけを、<paramref name="mode"/> にそろえて返す（ほかのビットは保つ）。</summary>
        public static DS4KeyType ApplyTo(DS4KeyType keyType, MacroExecuteMode mode)
        {
            keyType &= ~(DS4KeyType.RepeatMacro | DS4KeyType.RepeatLatchMacro);
            if (mode == MacroExecuteMode.Held) keyType |= DS4KeyType.RepeatMacro;
            else if (mode == MacroExecuteMode.Toggle) keyType |= DS4KeyType.RepeatLatchMacro;
            return keyType;
        }

        /// <summary>保存する文字列（<c>Held</c>／<c>Toggle</c>）。<see cref="MacroExecuteMode.Once"/> は空（要素を書かない）。</summary>
        public static string ToXmlValue(MacroExecuteMode mode)
        {
            return mode == MacroExecuteMode.Once ? string.Empty : mode.ToString();
        }

        /// <summary>
        /// 保存された文字列（前後の空白を除き、大文字小文字を区別しない <c>Once</c>／<c>Held</c>／<c>Toggle</c>）を読む。
        /// 空・不明な値は false（呼び出し側は、古い形式［SA の extras の <c>Repeat</c>、Controls の <c>KeyType</c> の <c>RepeatMacro</c>／<c>HoldMacro</c>］の読込に任せる）。
        /// </summary>
        public static bool TryParse(string text, out MacroExecuteMode mode)
        {
            mode = MacroExecuteMode.Once;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Enum.TryParse(text.Trim(), true, out mode) && Enum.IsDefined(typeof(MacroExecuteMode), mode);
        }
    }
}
