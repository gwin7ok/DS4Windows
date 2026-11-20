namespace DS4Windows.Processing.Macro
{
    /// <summary>
    /// マクロのマウスポインター移動の、座標の基準。
    /// 値は、整数へ詰めるときのモード番号（0、1）と一致する。2 は将来の「現在のポインターからの相対」の予約で、今回は実装しない。
    /// </summary>
    public enum MacroPointerMoveMode
    {
        /// <summary>画面（仮想スクリーン）の座標。物理ピクセル。</summary>
        Absolute = 0,

        /// <summary>そのとき前面にあるウィンドウの外枠（タイトルバーを含む）の左上を原点とする座標。</summary>
        WindowRelative = 1,
    }
}
