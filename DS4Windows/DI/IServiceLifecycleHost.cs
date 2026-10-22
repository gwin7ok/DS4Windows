using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9.2）: サービスの開始・停止が、<c>ControlService</c> に頼る 2 つの機能。
    /// 実体は <c>ControlService</c>（イベントスレッドと、キーボード／マウス出力の初期化を所有している）。<c>ServiceLifecycle</c> が <c>Func</c> で遅延して受ける（循環を避ける）。
    /// </summary>
    public interface IServiceLifecycleHost
    {
        /// <summary>アクションを、イベントスレッドで同期的に実行する（旧 <c>eventDispatcher.Invoke</c>。デバイスの列挙に使う）。</summary>
        void InvokeOnEventThread(Action action);

        /// <summary>キーボード／マウス出力のハンドラーを決めて接続し、マッピングを初期化する（旧 <c>InitOutputKBMHandler</c>）。</summary>
        void InitializeOutputKbmHandler();
    }
}
