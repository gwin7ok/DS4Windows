using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// デバイスの接続時に行う、出力側の準備（OSC への接続の通知、仮想パッドの割り当て、UDP のモーション配信の準備）。
    /// 接続の手順そのもの（<c>DeviceConnector.PrepareConnected</c>）が、決まった位置でこの契約を呼ぶ。
    /// </summary>
    public interface IConnectedDeviceOutputs
    {
        /// <summary>OSC の送信が有効なら、スロット <paramref name="index"/> の接続を通知する。</summary>
        void NotifyPlugged(int index);

        /// <summary>
        /// スロット <paramref name="index"/> に仮想パッドを割り当てる。主のデバイスなら新しく差し込み、結合デバイスの相手側なら相手の仮想パッドを共有する。
        /// 割り当てない設定（DInput のみ、未同期）なら、DInput のみの印と種類の記録を更新する。
        /// </summary>
        void PlugVirtualPad(int index, DS4Device device);

        /// <summary>UDP のモーション配信が動いていて、スロットが範囲内なら、デバイスの配信の準備をする。</summary>
        void PrepareMotion(int index, DS4Device device);
    }
}
