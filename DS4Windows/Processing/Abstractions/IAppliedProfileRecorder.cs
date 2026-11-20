namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// プロファイルを適用したとき（<c>SelectedProfileChanged</c> は適用のたびに発火する）、そのスロットのコントローラーに、適用したプロファイルを記憶する。
    /// 再接続（同じコントローラー）のとき、切断前のプロファイルを適用するため。
    /// </summary>
    public interface IAppliedProfileRecorder
    {
        /// <summary>適用の通知を購読する。1 回呼ぶ（<c>ControlService</c> のコンストラクターが呼ぶ）。</summary>
        void Attach();
    }
}
