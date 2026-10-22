using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）: デバイスの切断（Removal）、接続状態の変化（SyncChange）、シリアルの変化（SerialChange）の処理。
    /// 以前は <c>ControlService.On_DS4Removal</c>／<c>On_SyncChange</c>／<c>On_SerialChange</c> だった。
    /// 切断の後始末（仮想パッドの取り外し → 合成入力のコミット → 切断の通知 → スロットの解放 → 状態の初期化）の順序は変えない（ガードレール: 切断時クリーンアップ）。
    /// <c>ControlService</c> の同名の protected メソッドは、イベントの購読先として、薄い委譲で残す。
    /// </summary>
    public interface IDeviceDisconnectHandler
    {
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>デバイスが切断された、またはタイムアウトしたとき（旧 <c>On_DS4Removal</c>）。</summary>
        void OnRemoval(object sender, EventArgs e);

        /// <summary>デバイスの同期状態が変わったとき（旧 <c>On_SyncChange</c>）。同期が外れたら仮想パッドを外し、同期したらつなぐ。</summary>
        void OnSyncChange(object sender, EventArgs e);

        /// <summary>デバイスのシリアル（MAC）が変わったとき（旧 <c>On_SerialChange</c>）。</summary>
        void OnSerialChange(object sender, EventArgs e);
    }
}
