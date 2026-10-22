using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）: スロットごとの状態（接続中のデバイス、入力の状態、タッチパッド）への窓口。
    /// 実体は <c>ControlService</c>（これらの配列を所有している）。切断の処理 <c>DeviceDisconnectHandler</c> が、<c>Func</c> で遅延して受ける（循環を避ける）。
    /// 配列の参照は再代入されない（要素の書き込みだけ）。E-6f1 で、接続の組み立て（<c>DeviceConnector</c>）も使うようになった。
    /// </summary>
    public interface IDeviceSlotHost
    {
        DS4Device[] DS4Controllers { get; }

        DS4State[] CurrentStates { get; }

        DS4State[] PreviousStates { get; }

        Mouse[] TouchPads { get; }

        ControllerSlotManager SlotManager { get; }

        /// <summary>接続中のコントローラーの数（旧 <c>ControlService.activeControllers</c>。LED 設定の変更の発火のたびに読む）。</summary>
        int ActiveControllers { get; }

        /// <summary>入力レポートが届いたときの処理（<c>ControlService.On_Report</c>）。デバイスの <c>Report</c> イベントの購読から呼ぶ。</summary>
        void OnReport(DS4Device device, EventArgs e, int ind);
    }
}
