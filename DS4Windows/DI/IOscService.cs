using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: OSC（外部ツールとのボタン・スティックのやり取り）。
    /// 以前は <c>ControlService</c> の OSC 系のメンバー（受信の解釈、リスナー／送信側、入力への重ね合わせ、変化の送信、<c>oscState</c>）だった。
    /// <c>ControlService</c> の公開メソッド（<c>ChangeOSCListenerStatus</c> など）は、薄い委譲として残す（呼び出し側は 9-F まで変えない）。
    /// </summary>
    public interface IOscService
    {
        /// <summary>受信側（リスナー）が動作中か（旧: <c>oscListener != null</c>）。</summary>
        bool HasListener { get; }

        /// <summary>送信側が作られているか（旧: <c>oscSender != null</c>）。</summary>
        bool HasSender { get; }

        /// <summary>受信側を開始／停止する（旧 <c>ChangeOSCListenerStatus</c>）。停止は、動作中のときだけ呼ぶこと。</summary>
        void ChangeListenerStatus(bool state);

        /// <summary>送信側を開始／停止する（旧 <c>ChangeOSCSenderStatus</c>）。</summary>
        void ChangeSenderStatus(bool state);

        /// <summary>コントローラーの接続（plugged）／切断の通知を送る。呼び出し側が <c>UseOscSender</c> を確かめてから呼ぶ。</summary>
        void SendPlug(int index, bool plugged);

        /// <summary>そのデバイスの OSC 側の入力状態を初期化する（旧: <c>oscState[i] = new DS4State()</c>）。</summary>
        void ResetDeviceState(int index);

        /// <summary>受信した OSC メッセージ（アドレス、引数）を解釈し、そのデバイスの OSC 側の入力状態を更新する。</summary>
        void HandleMessage(string address, IList<object> arguments);

        /// <summary>マッピングの前: バッテリーとボタン等の変化を送信し、OSC 側の入力を入力 <paramref name="cState"/> に重ねる。</summary>
        void PreMappingStep(int ind, DS4State cState, DS4State tempMapState);

        /// <summary>マッピングの後: OSC 側の入力をマッピング結果 <paramref name="tempMapState"/> に重ねる。</summary>
        void PostMappingStep(int ind, DS4State tempMapState);
    }
}
