using DS4Windows.Core.Model;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IControllerReadingsSource"/> の実装。機器の読み取りの待機（<c>ReadWaitEv</c>）を取って、入力レポートの処理と競合しないようにコピーする。
    /// </summary>
    public sealed class ControllerReadingsSource : IControllerReadingsSource
    {
        private readonly IDeviceSlotState _slotState;
        private readonly IMapping _mapping;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IInputReportProcessor _inputReport;

        public ControllerReadingsSource(IDeviceSlotState slotState, IMapping mapping,
            IProfileSettingsService profileSettings, IInputReportProcessor inputReport)
        {
            _slotState = slotState ?? throw new ArgumentNullException(nameof(slotState));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _inputReport = inputReport ?? throw new ArgumentNullException(nameof(inputReport));
        }

        public bool IsMeasuringProcessingDelay
        {
            get => _inputReport.IsMeasuringProcessingDelay;
            set => _inputReport.IsMeasuringProcessingDelay = value;
        }

        public bool TryRead(int slot, int profileSlot, DS4State input, DS4State mapped, out ControllerReadingInfo info)
        {
            info = default;
            InputDeviceCore[] controllers = _slotState.DS4Controllers;
            InputDeviceCore device = controllers != null && slot >= 0 && slot < controllers.Length ? controllers[slot] : null;
            if (device == null || !device.IsAlive())
                return false;

            DS4State currentState = _slotState.GetCurrentState(slot);
            DS4State tempState = _slotState.GetTempState(slot);
            long calibrationCount = device.SixAxis.CntCalibrating;

            // 待機が長引いても入力レポートの処理を止めないよう、最大 10ms で諦める
            if (!device.ReadWaitEv.Wait(10))
                return false;

            try
            {
                device.ReadWaitEv.Reset();

                currentState.CopyTo(input);
                tempState.CopyTo(mapped);

                if (slot != profileSlot)
                    _mapping.SetCurveAndDeadzone(profileSlot, input, mapped, _profileSettings);
            }
            finally
            {
                device.ReadWaitEv.Set();
            }

            info = new ControllerReadingInfo(calibrationCount, device.Latency, device.getWarnInterval(),
                _inputReport.GetProcessingDelay(slot));
            return true;
        }

        public DS4State GetCurrentState(int slot) => _slotState.GetCurrentState(slot);
    }
}
