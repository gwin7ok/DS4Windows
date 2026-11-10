using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase13-Step6 6-d: <see cref="IProfileLoadTarget"/> の実装（DI の Singleton。状態なし）。
    /// プロファイル読込の鎖が <c>ControlService</c> に求めていた 8 つのメンバーを、既に独立したサービス（<see cref="IDeviceSlotState"/>、
    /// <see cref="IVirtualPadService"/>、<see cref="IDeviceConnectionSetup"/>、<see cref="ITouchpadToggleService"/>、<see cref="StickMotionState"/>）へ
    /// 直接つなぐ。<c>ControlService</c>、<c>ProfileRepository</c>、<c>ProfileApplicationService</c> のいずれにも依存しない（循環しない。6-a のプローブで確認）。
    /// <see cref="PreLoadReset"/> の本体は、旧 <c>ControlService.PreLoadReset</c> をそのまま移したもの。
    /// </summary>
    public sealed class ProfileLoadTarget : IProfileLoadTarget
    {
        private readonly IDeviceSlotState _slots;
        private readonly IVirtualPadService _virtualPad;
        private readonly IDeviceConnectionSetup _connectionSetup;
        private readonly ITouchpadToggleService _touchpadToggle;
        private readonly StickMotionState _stickMotion;

        public ProfileLoadTarget(
            IDeviceSlotState slots,
            IVirtualPadService virtualPad,
            IDeviceConnectionSetup connectionSetup,
            ITouchpadToggleService touchpadToggle,
            StickMotionState stickMotion)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _connectionSetup = connectionSetup ?? throw new ArgumentNullException(nameof(connectionSetup));
            _touchpadToggle = touchpadToggle ?? throw new ArgumentNullException(nameof(touchpadToggle));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
        }

        // 旧 control.DS4Controllers[ind] と同じ（範囲外は例外）。IDeviceStateAccessor.GetController（範囲外は null）にしない: 鎖の PostLoadSnippet などは範囲の検査をしないため、挙動を保つ
        public IConfigurableDevice GetController(int ind) => _slots.DS4Controllers[ind];

        public ITouchPadGyroToggles GetTouchPad(int ind) => _slots.TouchPads[ind];

        public bool HasOutputDevice(int ind) => _virtualPad.HasOutputDevice(ind);

        // IConfigurableDevice を実装するのは DS4Device だけ（この層は具象を知ってよい）
        public void PluginOutDev(int index, IConfigurableDevice device) => _virtualPad.PluginOutDev(index, (DS4Device)device);

        public void UnplugOutDev(int index, IConfigurableDevice device, bool immediate = false, bool force = false)
            => _virtualPad.UnplugOutDev(index, (DS4Device)device, immediate, force);

        public void CheckProfileOptions(int ind, IConfigurableDevice device, bool startUp = false)
            => _connectionSetup.ApplyProfileOptions(ind, (DS4Device)device, _slots.TouchPads[ind], startUp);

        public void StartTPOff(int deviceID) => _touchpadToggle.StartTouchpadOff(deviceID);

        public void PreLoadReset(int ind)
        {
            // Skip running for test profile with no mapping data
            if (ind >= ControllerLimits.TestProfileIndex)
            {
                return;
            }

            // Reset current flick stick progress from previous profile
            _stickMotion.FlickMappingData[ind].Reset();

            // Reset delta accel processors for sticks
            _stickMotion.DeltaAccelProcessors[ind].LSProcessor.Reset();
            _stickMotion.DeltaAccelProcessors[ind].RSProcessor.Reset();

            // Reset some elements of current Mouse instance
            _slots.TouchPads[ind]?.Reset();
        }
    }
}
