using DS4Windows.Processing.Touchpad;
using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IMacroRecordingState"/> の実装。記録中のフラグは入力レポートの処理（<see cref="IInputReportProcessor"/>）が持つ。
    /// </summary>
    public sealed class MacroRecordingState : IMacroRecordingState
    {
        private readonly IDeviceSlotState _slotState;
        private readonly IMapping _mapping;
        private readonly IInputReportProcessor _inputReport;

        public MacroRecordingState(IDeviceSlotState slotState, IMapping mapping, IInputReportProcessor inputReport)
        {
            _slotState = slotState ?? throw new ArgumentNullException(nameof(slotState));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _inputReport = inputReport ?? throw new ArgumentNullException(nameof(inputReport));
        }

        public bool IsRecording
        {
            get => _inputReport.RecordingMacro;
            set => _inputReport.RecordingMacro = value;
        }

        public bool CanReadInput => Device != null;

        public bool IsPressed(DS4Controls control)
        {
            InputDeviceCore device = Device;
            if (device == null)
                return false;

            ControllerState state = device.getCurrentStateRef();
            TouchpadMouse touchPad = _slotState.TouchPads[0];
            return _mapping.GetBoolMappingExternal(0, control, state, null, touchPad);
        }

        private InputDeviceCore Device
        {
            get
            {
                InputDeviceCore[] controllers = _slotState.Controllers;
                return controllers != null && controllers.Length > 0 ? controllers[0] : null;
            }
        }
    }
}
