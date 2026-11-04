using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）→ Phase13-Step5: 入力レポートの処理と接続中の数への窓口。実体は <c>ControlService</c>。
    /// スロットごとの状態（接続中のデバイス、入力の状態、タッチパッド、スロット管理）は <see cref="IDeviceSlotState"/>（直接注入）へ移した。
    /// TODO(Phase13-Step5b): 残るのは <c>OnReport</c>（<c>ControlService.On_Report</c>）と <c>ActiveControllers</c>（<c>ServiceLifecycle</c> が所有する数）だけで、
    /// <c>DeviceConnector</c> が <c>Func</c> で遅延して受ける（<c>ControlService</c> → <c>ServiceLifecycle</c> → <c>DeviceConnector</c> → <c>ControlService</c> の循環を避けるため）。
    /// <c>On_Report</c> を <c>InputReportProcessor</c> へ移す Step5b で、この IF ごと撤廃する（<c>ActiveControllers</c> の持ち主もそこで決める）。
    /// </summary>
    public interface IDeviceSlotHost
    {
        /// <summary>接続中のコントローラーの数（旧 <c>ControlService.activeControllers</c>。LED 設定の変更の発火のたびに読む）。</summary>
        int ActiveControllers { get; }
        /// <summary>入力レポートが届いたときの処理（<c>ControlService.On_Report</c>）。デバイスの <c>Report</c> イベントの購読から呼ぶ。</summary>
        void OnReport(DS4Device device, EventArgs e, int ind);
    }
}
