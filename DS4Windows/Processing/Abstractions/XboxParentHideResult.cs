namespace DS4Windows.Processing.Abstractions
{
    /// <summary><see cref="IXboxParentHider.Apply"/> の結果。</summary>
    public enum XboxParentHideResult
    {
        /// <summary>隠すように、HidHide の非表示の一覧へ足した。</summary>
        Hidden,

        /// <summary>すでに一覧にあった（利用者が足した分、または前回の分）。何も変えていない。</summary>
        AlreadyHidden,

        /// <summary>足してあった分を、一覧から外した。</summary>
        Restored,

        /// <summary>戻す対象がなかった（自分が足した分ではない）。何も変えていない。</summary>
        NothingToRestore,

        /// <summary>HidHide が導入されていない。</summary>
        HidHideNotInstalled,

        /// <summary>HidHide の制御デバイスを開けない（権限がない場合がある）。</summary>
        NoAccess,

        /// <summary>HidHide のアプリケーションの許可リストが反転している。DS4Windows は、利用者の設定を触らない。</summary>
        InverseWhitelist,

        /// <summary>HidHide の隠す機能の全体スイッチが切れている。DS4Windows は、切り替えない。</summary>
        CloakOff,

        /// <summary>HidHide への書き込みに失敗した。</summary>
        WriteFailed,
    }
}
