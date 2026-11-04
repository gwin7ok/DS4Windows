using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase13-Step5: <see cref="IDeviceSlotState"/> の実装（DI の Singleton。依存なし）。
    /// 配列の長さは <see cref="ControllerLimits.MaxControllerCount"/>（旧 <c>ControlService.MAX_DS4_CONTROLLER_COUNT</c> と同じ）。
    /// 各 <c>DS4State</c> の初期化（<c>new DS4State()</c>）は、これまでどおり <c>ControlService</c> のコンストラクターが行う（初期化の位置を変えない）。
    /// </summary>
    public class DeviceSlotState : IDeviceSlotState
    {
        public DS4Device[] DS4Controllers { get; } = new DS4Device[ControllerLimits.MaxControllerCount];
        public DS4State[] CurrentStates { get; } = new DS4State[ControllerLimits.MaxControllerCount];
        public DS4State[] PreviousStates { get; } = new DS4State[ControllerLimits.MaxControllerCount];
        public DS4State[] TempStates { get; } = new DS4State[ControllerLimits.MaxControllerCount];
        public Mouse[] TouchPads { get; } = new Mouse[ControllerLimits.MaxControllerCount];
        public ControllerSlotManager SlotManager { get; } = new ControllerSlotManager();

        /// <summary>範囲外は null（旧 <c>ControlService.GetController</c> と同じ）。</summary>
        public DS4Device GetController(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= DS4Controllers.Length) return null;
            return DS4Controllers[deviceIndex];
        }

        public DS4State GetCurrentState(int ind) => CurrentStates[ind];

        public DS4State GetTempState(int ind) => TempStates[ind];
    }
}
