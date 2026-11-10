using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step7 7-d: テスト用の <see cref="IVirtualPadService"/>。旧 <c>OutputSlotService</c> のテストが渡していた
    /// <c>Func&lt;OutputSlotManager&gt;</c>／<c>Func&lt;IVirtualPadService&gt;</c>（ControlService なしなら null）の置き換え。
    /// <see cref="OutputSlotManager"/> はテストが渡したものを返し、それ以外の操作は <c>inner</c> があれば転送、なければ何もしない。
    /// </summary>
    internal sealed class FakeVirtualPadService : IVirtualPadService
    {
        private readonly IVirtualPadService _inner;

        public FakeVirtualPadService(OutputSlotManager manager, IVirtualPadService inner = null)
        {
            OutputSlotManager = manager;
            _inner = inner;
        }


        public OutputSlotManager OutputSlotManager { get; }

        public bool HasOutputDevice(int index) => _inner?.HasOutputDevice(index) ?? false;

        public void SendReport(int index, DS4State state) => _inner?.SendReport(index, state);

        public bool TryShareOutputDevice(int fromIndex, int toIndex, OutContType contType, InputDeviceCore device)
            => _inner?.TryShareOutputDevice(fromIndex, toIndex, contType, device) ?? false;

        public void ReleaseSharedOutputDevice(int index) => _inner?.ReleaseSharedOutputDevice(index);

        public bool IsOutputQueueRunning => _inner?.IsOutputQueueRunning ?? false;

        public void StopOutputSlots(bool immediate) => _inner?.StopOutputSlots(immediate);

        public void ShutDownOutputSlots() => _inner?.ShutDownOutputSlots();

        public event EventHandler<int> BackendFailure
        {
            add { if (_inner != null) _inner.BackendFailure += value; }
            remove { if (_inner != null) _inner.BackendFailure -= value; }
        }

        public string DescribeSlot(int slot, OutputSlotInfoKind kind) => _inner?.DescribeSlot(slot, kind);

        public bool HasViGEmClient => _inner?.HasViGEmClient ?? false;

        public void StartViGEm() => _inner?.StartViGEm();

        public void StopViGEm() => _inner?.StopViGEm();

        public void AssignInitialDevices() => _inner?.AssignInitialDevices();

        public void EstablishOutFeedback(int index, OutContType contType, OutputDevice outDevice, InputDeviceCore device)
            => _inner?.EstablishOutFeedback(index, contType, outDevice, device);

        public void RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx)
            => _inner?.RemoveOutFeedback(contType, outDevice, inIdx);

        public void AttachNewUnboundOutDev(OutContType contType) => _inner?.AttachNewUnboundOutDev(contType);

        public void AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType)
            => _inner?.AttachUnboundOutDev(slotDevice, contType);

        public void DetachUnboundOutDev(OutSlotDevice slotDevice) => _inner?.DetachUnboundOutDev(slotDevice);

        public void PluginOutDev(int index, InputDeviceCore device) => _inner?.PluginOutDev(index, device);

        public void UnplugOutDev(int index, InputDeviceCore device, bool immediate = false, bool force = false)
            => _inner?.UnplugOutDev(index, device, immediate, force);

        public void QueueOnEventThread(Action action) => _inner?.QueueOnEventThread(action);
    }
}
