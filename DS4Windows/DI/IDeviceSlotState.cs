using DS4Windows.Services;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase13-Step5（Phase13-Step5-Plan.md）: スロットごとの状態（接続中のデバイス、入力の状態、タッチパッド、スロット管理）の持ち主。
    /// 旧: <c>ControlService</c> が配列を所有し、<c>IDeviceSlotHost</c>／<c>IUdpMotionHost</c>／<c>IDeviceStateAccessor</c> の 3 つの窓口で公開していた（利用者は <c>Func</c> で遅延して受けていた）。
    /// 実装は依存を持たないため、利用者は直接注入で受ける。
    /// 配列の参照は再代入されない（要素の書き込みだけ）。ホットパス（<c>On_Report</c>）は、コンストラクターで一度だけ参照を保持して読む。
    /// </summary>
    public interface IDeviceSlotState : IDeviceStateAccessor
    {
        /// <summary>接続中のデバイス（スロットごと。未接続は null）。</summary>
        DS4Device[] DS4Controllers { get; }
        /// <summary>そのスロットのデバイスの現在の入力（旧 <c>ControlService.CurrentState</c>）。</summary>
        DS4State[] CurrentStates { get; }
        /// <summary>前回の入力（旧 <c>ControlService.PreviousState</c>）。</summary>
        DS4State[] PreviousStates { get; }
        /// <summary>UDP へ送る前の加工に使う作業用の入力（旧 <c>ControlService.TempState</c>）。</summary>
        DS4State[] TempStates { get; }
        /// <summary>タッチパッド（旧 <c>ControlService.touchPad</c>）。</summary>
        Mouse[] TouchPads { get; }
        /// <summary>スロット管理（旧 <c>ControlService.slotManager</c>）。</summary>
        ControllerSlotManager SlotManager { get; }
        /// <summary>そのスロットのデバイスの現在の入力（範囲外は例外。旧 <c>ControlService.GetCurrentState</c>）。</summary>
        DS4State GetCurrentState(int ind);
        /// <summary>そのスロットの作業用の入力（範囲外は例外。旧 <c>ControlService.GetTempState</c>）。</summary>
        DS4State GetTempState(int ind);
    }
}
