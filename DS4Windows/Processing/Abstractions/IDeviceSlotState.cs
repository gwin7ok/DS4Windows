#nullable enable

using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// スロットごとの状態（接続中のデバイス、入力の状態、タッチパッド、スロット管理）の持ち主。
    /// 旧: <c>ControlService</c> が配列を所有し、<c>IDeviceSlotHost</c>（Step5b で削除）／<c>IUdpMotionHost</c>／<c>IDeviceStateAccessor</c> の 3 つの窓口で公開していた（利用者は <c>Func</c> で遅延して受けていた）。
    /// 実装は依存を持たないため、利用者は直接注入で受ける。
    /// 配列の参照は再代入されない（要素の書き込みだけ）。ホットパス（<c>On_Report</c>）は、コンストラクターで一度だけ参照を保持して読む。
    /// </summary>
    public interface IDeviceSlotState : IDeviceStateAccessor
    {
        /// <summary>接続中のデバイス（スロットごと。未接続は null）。</summary>
        InputDeviceCore[] Controllers { get; }
        /// <summary>そのスロットのデバイスの現在の入力。</summary>
        ControllerState[] CurrentStates { get; }
        /// <summary>前回の入力。</summary>
        ControllerState[] PreviousStates { get; }
        /// <summary>UDP へ送る前の加工に使う作業用の入力。</summary>
        ControllerState[] TempStates { get; }
        /// <summary>タッチパッド。</summary>
        TouchpadMouse[] TouchPads { get; }
        /// <summary>スロット管理。</summary>
        ControllerSlotManager SlotManager { get; }
        /// <summary>接続中のコントローラーの数。書くのは <c>ServiceLifecycle</c>（<c>IServiceLifecycle.ActiveControllers</c> はこれへ転送する）。</summary>
        int ActiveControllers { get; set; }
        /// <summary>そのスロットのデバイスの現在の入力（範囲外は例外。旧 <c>ControlService.GetCurrentState</c>）。</summary>
        ControllerState GetCurrentState(int ind);
        /// <summary>そのスロットの作業用の入力（範囲外は例外。旧 <c>ControlService.GetTempState</c>）。</summary>
        ControllerState GetTempState(int ind);
    }
}
