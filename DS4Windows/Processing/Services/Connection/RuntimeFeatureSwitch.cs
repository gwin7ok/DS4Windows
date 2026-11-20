#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// <see cref="IRuntimeFeatureSwitch"/> の実装。本物のサービスへ委譲するだけ。
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
            ArgumentNullException.ThrowIfNull(osc);
            _osc = osc;
            ArgumentNullException.ThrowIfNull(udpMotion);
            _udpMotion = udpMotion;
            ArgumentNullException.ThrowIfNull(hidHide);
            _hidHide = hidHide;
            ArgumentNullException.ThrowIfNull(touchpadToggle);
            _touchpadToggle = touchpadToggle;
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(activeInputProbe);
            _activeInputProbe = activeInputProbe;
            ArgumentNullException.ThrowIfNull(rumble);
            _rumble = rumble;
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
            => _touchpadToggle.Slide(ind, _slots.CurrentStates[ind], _slots.TouchPads[ind], _slots.Controllers[ind] != null);

        public DS4Controls GetActiveInputControl(int ind)
            => _activeInputProbe.GetActive(_slots.CurrentStates[ind], _slots.TouchPads[ind], _slots.Controllers[ind] != null);

        // 旧 ControlService.setRumble と同じ（スロット上限の検査 → コントローラーがいれば、ブースト・反転つきの振動）
        public void SetRumble(byte heavyMotor, byte lightMotor, int deviceNum)
        {
            if (deviceNum < _controllerSlotLimit)
            {
                InputDeviceCore device = _slots.Controllers[deviceNum];
                if (device != null)
                    _rumble.SetDevRumble(device, heavyMotor, lightMotor, deviceNum);
            }
        }
    }
}
