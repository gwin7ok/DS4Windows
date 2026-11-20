namespace DS4Windows.UI.Services
{
    /// <summary>振動テストのボタンを押した結果。ボタンの文字を何にするかを画面へ伝える。</summary>
    public enum RumbleTestOutcome
    {
        /// <summary>何もしなかった（実機がない、スロットが範囲外）。ボタンの文字は変えない。</summary>
        None,

        /// <summary>振動を始めた。押したボタンを「停止」の文字にする。</summary>
        Started,

        /// <summary>振動を止めた。押したボタンを「テスト」の文字に戻す。</summary>
        Stopped,
    }
}
