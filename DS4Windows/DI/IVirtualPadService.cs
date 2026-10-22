using System;

namespace DS4Windows.DI
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
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>仮想パッドのスロット管理（旧: <c>ControlService.OutputslotMan</c>）。</summary>
        OutputSlotManager OutputSlotManager { get; }

        /// <summary>入力デバイスのスロットごとの、割り当て中の出力デバイス（旧: <c>ControlService.outputDevices</c>。同じ配列を返す）。</summary>
        OutputDevice[] OutputDevices { get; }

        /// <summary>ViGEm のクライアントが生成できているか（旧: <c>vigemTestClient != null</c>）。</summary>
        bool HasViGEmClient { get; }

        /// <summary>ViGEm のクライアントを生成する（対応する ViGEmBus が動いているとき。別スレッドで生成し、完了まで待つ）。</summary>
        void StartViGEm();

        /// <summary>ViGEm のクライアントを破棄する。</summary>
        void StopViGEm();

        /// <summary>恒久スロットに、仮想パッドを割り当てる。</summary>
        void AssignInitialDevices();

        /// <summary>仮想パッドからの振動・ライトバーの要求を、デバイスへ戻す処理を付ける。</summary>
        void EstablishOutFeedback(int index, OutContType contType, OutputDevice outDevice, DS4Device device);

        /// <summary>仮想パッドからの振動・ライトバーの要求を戻す処理を外す。</summary>
        void RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx);

        void AttachNewUnboundOutDev(OutContType contType);

        void AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType);

        void DetachUnboundOutDev(OutSlotDevice slotDevice);

        /// <summary>デバイスに、設定の種類（Xbox 360／DS4）の仮想パッドを割り当てる。</summary>
        void PluginOutDev(int index, DS4Device device);

        /// <summary>デバイスから仮想パッドを外す。</summary>
        void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false);

        /// <summary>イベントスレッドで、非同期に実行する（<c>OutputSlotService</c> が、画面からの抜き差しをイベントスレッドへ渡すのに使う）。</summary>
        void QueueOnEventThread(Action action);
    }
}
