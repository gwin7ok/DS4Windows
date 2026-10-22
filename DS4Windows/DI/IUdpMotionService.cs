using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: UDP のモーションデータの送出（Cemuhook 互換サーバー）。
    /// 以前は <c>ControlService</c> の UDP 系のメンバー（サーバーの作成・開始・停止、モーションの送出のハンドラー、スムージングのフィルター、出力バッファー）だった。
    /// <c>ControlService.ChangeUDPStatus</c>／<c>ChangeMotionEventStatus</c> は、薄い委譲として残す（呼び出し側は 9-F まで変えない）。
    /// </summary>
    public interface IUdpMotionService
    {
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>モーションを送れるスロット数（旧: <c>UdpServer.NUMBER_SLOTS</c>）。</summary>
        int SlotCount { get; }

        /// <summary>サーバーが作られているか（旧: <c>_udpServer != null</c>）。</summary>
        bool HasServer { get; }

        /// <summary>サーバーの作成・停止の処理中か（旧: <c>udpChangeStatus</c>）。<c>Start</c> が、作成の完了を待つのに使う。</summary>
        bool IsChangingStatus { get; }

        /// <summary>
        /// サーバーを作る（<paramref name="state"/> が true。<paramref name="openPort"/> が true なら、待ち受けも始める）／止める。
        /// 作成・停止は、イベントスレッドで非同期に行う（旧実装と同じ）。
        /// </summary>
        void ChangeStatus(bool state, bool openPort = true);

        /// <summary>サーバーが作られているとき、設定のポート・アドレスで待ち受けを始める。作られていなければ何もしない。</summary>
        void StartListeningIfCreated();

        /// <summary>接続中のデバイスすべてに、モーションの送出のハンドラーを付ける（true）／外す（false）。</summary>
        void ChangeMotionEventStatus(bool state);

        /// <summary>そのデバイスに、モーションの送出のハンドラーを付ける（設定でスムージングが有効なら、加速度とジャイロにフィルターをかけて送る）。</summary>
        void PrepareDeviceMotion(DS4Windows.DS4Device device, int index);

        /// <summary>そのスロットのスムージングのフィルターを作り直す。</summary>
        void ResetSmoothingFilters(int idx);
    }
}
