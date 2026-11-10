namespace DS4Windows.Processing.Abstractions
{
    /// <summary>仮想パッドのバックエンドの失敗（ドライバーの接続の失敗など）の通知を受けて、サービスを止める。</summary>
    public interface IBackendFailureHandler
    {
        /// <summary>失敗の通知を購読する。1 回呼ぶ（<c>ControlService</c> のコンストラクターが呼ぶ）。</summary>
        void Attach();
    }
}
