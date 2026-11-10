
using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase13-Step5: <see cref="IDeviceSlotState"/> の実装（DI の Singleton。依存なし）。
    /// 配列の長さは <see cref="ControllerLimits.MaxControllerCount"/>（旧 <c>ControlService.MAX_DS4_CONTROLLER_COUNT</c> と同じ）。
    /// 作業用・前回の <c>ControllerState</c> は、コンストラクターで作る（<c>CurrentStates</c> は <c>InputReportProcessor</c> が作る）。
    /// </summary>
    public class DeviceSlotState : IDeviceSlotState, DS4Windows.Core.Abstractions.IConfigurableDeviceLookup
    {
        public InputDeviceCore[] Controllers { get; } = new InputDeviceCore[ControllerLimits.MaxControllerCount];
        public ControllerState[] CurrentStates { get; } = new ControllerState[ControllerLimits.MaxControllerCount];
        public ControllerState[] PreviousStates { get; } = new ControllerState[ControllerLimits.MaxControllerCount];
        public ControllerState[] TempStates { get; } = new ControllerState[ControllerLimits.MaxControllerCount];
        public TouchpadMouse[] TouchPads { get; } = new TouchpadMouse[ControllerLimits.MaxControllerCount];
        public ControllerSlotManager SlotManager { get; } = new ControllerSlotManager();
        /// <summary>接続中のコントローラーの数。Phase13-Step5b: 旧 <c>ServiceLifecycle</c> の自動プロパティを、ここへ移した（<c>IDeviceSlotHost</c> の廃止で、<c>DeviceConnector</c> が直接読む）。</summary>
        public int ActiveControllers { get; set; }

        DS4Windows.Core.Abstractions.IConfigurableDevice DS4Windows.Core.Abstractions.IConfigurableDeviceLookup.GetConfigurableDevice(int deviceIndex) => GetController(deviceIndex);

        /// <summary>範囲外は null（旧 <c>ControlService.GetController</c> と同じ）。</summary>
        public InputDeviceCore GetController(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= Controllers.Length) return null;
            return Controllers[deviceIndex];
        }

        public DeviceSlotState()
        {
            for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
            {
                TempStates[i] = new ControllerState();
                PreviousStates[i] = new ControllerState();
            }
        }

        public ControllerState GetCurrentState(int ind) => CurrentStates[ind];

        public ControllerState GetTempState(int ind) => TempStates[ind];
    }
}
