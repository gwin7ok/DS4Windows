using System;
using DS4Windows.Core.Model;
using DS4Windows.Output.Devices;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary>
    /// <see cref="OutSlotDevice"/> を <see cref="IOutputSlotView"/> として UI に見せるアダプター。
    /// スロットごとに 1 つ作り、スロットの通知は作成時に一度だけ購読して、この面を送り主にして転送する。
    /// </summary>
    internal sealed class OutputSlotView : IOutputSlotView
    {
        private readonly OutSlotDevice _slot;

        internal OutputSlotView(OutSlotDevice slot)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            slot.PermanentTypeChanged += (s, e) => PermanentTypeChanged?.Invoke(this, e);
            slot.CurrentInputBoundChanged += (s, e) => InputBoundChanged?.Invoke(this, e);
        }

        public int Index => _slot.Index;

        public string CurrentTypeName => _slot.OutputDevice != null ? _slot.OutputDevice.GetDeviceType() : "Empty";

        public OutContType CurrentType
        {
            get => _slot.CurrentType;
            set => _slot.CurrentType = value;
        }

        public OutputSlotAttachedStatus AttachedStatus => _slot.CurrentAttachedStatus;

        public OutputSlotReserveStatus ReserveStatus
        {
            get => _slot.CurrentReserveStatus;
            set => _slot.CurrentReserveStatus = value;
        }

        public OutputSlotInputBound InputBound => _slot.CurrentInputBound;

        public OutContType PermanentType
        {
            get => _slot.PermanentType;
            set => _slot.PermanentType = value;
        }

        public bool SupportsXInputSlotNumber =>
            _slot.CurrentType == OutContType.X360
            && _slot.OutputDevice is Xbox360OutDevice x360
            && x360.Features.HasFlag(Xbox360OutDevice.X360Features.XInputSlotNum);

        public int XInputSlotNumber =>
            _slot.CurrentType == OutContType.X360 && _slot.OutputDevice is Xbox360OutDevice x360 ? x360.XinputSlotNum : -1;

        public int InputIndex => _slot.InputIndex;

        public string InputDisplayString => _slot.InputDisplayString;

        public event EventHandler PermanentTypeChanged;

        public event EventHandler InputBoundChanged;
    }
}
