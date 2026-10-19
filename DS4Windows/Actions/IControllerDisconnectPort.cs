namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6b（Phase8-Step7-6-Plan.md Q2＝A）: DisconnectBT の出力が、実機のコントローラー（<c>DS4Device</c>）を操作するための窓口。
    /// 出力アクションが <c>DS4Device</c> を直接持たないようにし、単体テストでは記録用の偽の実装に差し替える。
    /// </summary>
    public interface IControllerDisconnectPort
    {
        /// <summary>旧 <c>MapCustomAction</c> の DisconnectBT の判定と同じ: 同期済み・非充電で、Bluetooth なら <see cref="DisconnectTargetKind.Bluetooth"/>、SONYWA なら <see cref="DisconnectTargetKind.Dongle"/>。それ以外（コントローラーがいない場合を含む）は <see cref="DisconnectTargetKind.None"/>。</summary>
        DisconnectTargetKind GetTargetKind(int deviceIndex);

        /// <summary>
        /// Bluetooth を切断する（<c>DS4Device.DisconnectBT()</c>）。戻り値は、切断の要求が受け付けられたか（Phase8-Step7-6-Plan.md Q3＝B: 失敗なら再試行する）。
        /// 注意: <c>DS4Device.DisconnectBT()</c> は、Windows の API の結果を使わず、MAC アドレスがあれば true を返す（既存の挙動。ほかの呼び出し元が依存するため変えない）。
        /// そのため、実際に失敗として観測できるのは、コントローラーがいない・MAC アドレスがない・例外のときに限られる。
        /// </summary>
        bool DisconnectBluetooth(int deviceIndex);

        /// <summary>ドングル接続（SONYWA）で同期済みか（旧経路の、離したときの切断待ちの条件）。</summary>
        bool IsDongleSynced(int deviceIndex);

        /// <summary>コントローラーが無操作か（<c>DS4Device.isDS4Idle()</c>）。コントローラーがいなければ false。</summary>
        bool IsIdle(int deviceIndex);

        /// <summary>ドングル接続を切断する（<c>DS4Device.DisconnectDongle()</c>）。コントローラーがいなければ false。</summary>
        bool DisconnectDongle(int deviceIndex);
    }
}
