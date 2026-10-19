namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（Phase8-Step7-7-Plan.md Q4＝A）: MultiAction が、マクロを**文字列**（例 <c>65/65</c>）で直接指定して実行するための窓口。
    /// <c>IMacroPlayer</c> は <c>SpecialAction</c> を受けるため、文字列のマクロには使えない。<c>IMapping</c> のメンバーは増やさず（10 メンバーのまま）、
    /// 実装（<c>DefaultMultiActionMacroPlayer</c>）が <c>Mapping</c> の同名のメソッドへ委譲する。
    /// </summary>
    public interface IMultiActionMacroPlayer
    {
        /// <summary>マクロの文字列を、旧 <c>MapCustomAction</c> の MultiAction と同じ呼び出し（<c>PlayMacro(…, 文字列, null, null, DS4Controls.None, DS4KeyType.None)</c>）で実行する。空・範囲外のデバイスは何もしない。</summary>
        void PlayMacroString(int deviceIndex, string macro);
    }
}
