using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: 仮想パッド（Xbox 360／DS4。ViGEmBus 経由）の出力。
    /// 以前は <c>ControlService</c> の仮想パッド系のメンバー（ViGEm クライアントの生成・破棄、仮想パッドの作成、振動・ライトバーの戻り、デバイスへの割り当て・取り外し、
    /// 入力に紐づかない仮想パッドの抜き差し、スロット管理 <c>OutputSlotManager</c>、出力デバイスの配列）だった。
    /// <c>ControlService.PluginOutDev</c>／<c>UnplugOutDev</c>／<c>OutputslotMan</c>／<c>outputDevices</c> は、窓口として残す（呼び出し側は 9-F まで変えない）。
    /// <c>OutputSlotService</c> は、<c>ControlService</c> ではなく、この部品を呼ぶ（過渡期の逆依存の解消）。
    /// </summary>
    public interface IVirtualPadService
    {
        /// <summary>仮想パッドのスロット管理（旧: <c>ControlService.OutputslotMan</c>）。</summary>
        OutputSlotManager OutputSlotManager { get; }

        /// <summary>入力デバイスのスロット <paramref name="index"/> に、出力デバイス（仮想パッド）が割り当てられているか。</summary>
        bool HasOutputDevice(int index);

        /// <summary>スロット <paramref name="index"/> の出力デバイスへ、入力の状態を送る（割り当てがなければ何もしない）。入力レポートごとに呼ばれる。</summary>
        void SendReport(int index, DS4State state);

        /// <summary>
        /// 結合デバイスの相手側（<paramref name="toIndex"/>）が、<paramref name="fromIndex"/> に割り当て済みの出力デバイスを共有する。
        /// 割り当てがなければ何もせず false。あれば、戻りの処理（振動・ライトバー）を付けてから共有し、true。
        /// </summary>
        bool TryShareOutputDevice(int fromIndex, int toIndex, OutContType contType, InputDeviceCore device);

        /// <summary>共有している出力デバイスから、スロット <paramref name="index"/> を外す（戻りの処理を外し、割り当てを空にする）。割り当てがなければ何もしない。</summary>
        void ReleaseSharedOutputDevice(int index);

        /// <summary>出力デバイスの抜き差しのキューが動いているか（<see cref="StopOutputSlots"/> の前に空になるのを待つのに使う）。</summary>
        bool IsOutputQueueRunning { get; }

        /// <summary>出力スロットの管理を止める（キューを待ってから、デバイスを取り外す）。</summary>
        void StopOutputSlots(bool immediate);

        /// <summary>出力スロットの管理を終了する（アプリの終了時）。</summary>
        void ShutDownOutputSlots();

        /// <summary>仮想パッドのドライバーへの接続に失敗した（引数はエラーコード）。購読側がサービスを止める。</summary>
        event EventHandler<int> BackendFailure;

        /// <summary>出力スロットの状態を文字列で返す（IPC の問い合わせ用）。</summary>
        string DescribeSlot(int slot, OutputSlotInfoKind kind);

        /// <summary>ViGEm のクライアントが生成できているか（旧: <c>vigemTestClient != null</c>）。</summary>
        bool HasViGEmClient { get; }

        /// <summary>ViGEm のクライアントを生成する（対応する ViGEmBus が動いているとき。別スレッドで生成し、完了まで待つ）。</summary>
        void StartViGEm();

        /// <summary>ViGEm のクライアントを破棄する。</summary>
        void StopViGEm();

        /// <summary>恒久スロットに、仮想パッドを割り当てる。</summary>
        void AssignInitialDevices();

        /// <summary>仮想パッドからの振動・ライトバーの要求を、デバイスへ戻す処理を付ける。</summary>
        void EstablishOutFeedback(int index, OutContType contType, OutputDevice outDevice, InputDeviceCore device);

        /// <summary>仮想パッドからの振動・ライトバーの要求を戻す処理を外す。</summary>
        void RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx);

        void AttachNewUnboundOutDev(OutContType contType);

        void AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType);

        void DetachUnboundOutDev(OutSlotDevice slotDevice);

        /// <summary>デバイスに、設定の種類（Xbox 360／DS4）の仮想パッドを割り当てる。</summary>
        void PluginOutDev(int index, InputDeviceCore device);

        /// <summary>デバイスから仮想パッドを外す。</summary>
        void UnplugOutDev(int index, InputDeviceCore device, bool immediate = false, bool force = false);

        /// <summary>イベントスレッドで、非同期に実行する（<c>OutputSlotService</c> が、画面からの抜き差しをイベントスレッドへ渡すのに使う）。</summary>
        void QueueOnEventThread(Action action);
    }
}
