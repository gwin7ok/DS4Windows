using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IRuntimeFeatureSwitch"/> の実装。本物のサービスへ委譲するだけ（旧 <c>ControlService</c> の転送メンバーを、そのまま移したもの）。
    /// </summary>
    public class RuntimeFeatureSwitch : IRuntimeFeatureSwitch
    {
        private readonly IOscService _osc;
        private readonly IUdpMotionService _udpMotion;
        private readonly IHidHideService _hidHide;
        private readonly ITouchpadToggleService _touchpadToggle;
        private readonly IDeviceSlotState _slots;
        private readonly IActiveInputProbe _activeInputProbe;
        private readonly IDeviceRumbleService _rumble;
        private readonly int _controllerSlotLimit;

        public RuntimeFeatureSwitch(IOscService osc, IUdpMotionService udpMotion, IHidHideService hidHide,
            ITouchpadToggleService touchpadToggle, IDeviceSlotState slots,
            IActiveInputProbe activeInputProbe, IDeviceRumbleService rumble, IEnvironmentService environmentService)
        {
            _osc = osc ?? throw new ArgumentNullException(nameof(osc));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _hidHide = hidHide ?? throw new ArgumentNullException(nameof(hidHide));
            _touchpadToggle = touchpadToggle ?? throw new ArgumentNullException(nameof(touchpadToggle));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _activeInputProbe = activeInputProbe ?? throw new ArgumentNullException(nameof(activeInputProbe));
            _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
            _controllerSlotLimit = (environmentService ?? throw new ArgumentNullException(nameof(environmentService))).ControllerSlotLimit;
        }

        public void ChangeOscListenerStatus(bool state) => _osc.ChangeListenerStatus(state);

        public void ChangeOscSenderStatus(bool state) => _osc.ChangeSenderStatus(state);

        public void ChangeUdpStatus(bool state, bool openPort = true) => _udpMotion.ChangeStatus(state, openPort);

        public void ChangeMotionEventStatus(bool state) => _udpMotion.ChangeMotionEventStatus(state);

        public void CheckHidHidePresence(string exePath = "", string exeName = "Autoprofile Exe", bool addExe = true)
            => _hidHide.CheckPresence(exePath, exeName, addExe);

        // 旧: HidHideInstalled の確認のあと UpdateHidHideAttributes（確認は HidHideService.UpdateAttributes の中に 1 つにまとめてある）
        public void UpdateHidHiddenAttributes() => _hidHide.UpdateAttributes();

        public string TouchpadSlide(int ind)
            => _touchpadToggle.Slide(ind, _slots.CurrentStates[ind], _slots.TouchPads[ind], _slots.DS4Controllers[ind] != null);

        public DS4Controls GetActiveInputControl(int ind)
            => _activeInputProbe.GetActive(_slots.CurrentStates[ind], _slots.TouchPads[ind], _slots.DS4Controllers[ind] != null);

        // 旧 ControlService.setRumble と同じ（スロット上限の検査 → コントローラーがいれば、ブースト・反転つきの振動）
        public void SetRumble(byte heavyMotor, byte lightMotor, int deviceNum)
        {
            if (deviceNum < _controllerSlotLimit)
            {
                DS4Device device = _slots.DS4Controllers[deviceNum];
                if (device != null)
                    _rumble.SetDevRumble(device, heavyMotor, lightMotor, deviceNum);
            }
        }
    }
}
