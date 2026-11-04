using System;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Control;

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

        public event Action<string, bool> DebugMessage { add { } remove { } }

        public OutputSlotManager OutputSlotManager { get; }

        public OutputDevice[] OutputDevices => _inner?.OutputDevices ?? Array.Empty<OutputDevice>();

        public bool HasViGEmClient => _inner?.HasViGEmClient ?? false;

        public void StartViGEm() => _inner?.StartViGEm();

        public void StopViGEm() => _inner?.StopViGEm();

        public void AssignInitialDevices() => _inner?.AssignInitialDevices();

        public void EstablishOutFeedback(int index, OutContType contType, OutputDevice outDevice, DS4Device device)
            => _inner?.EstablishOutFeedback(index, contType, outDevice, device);

        public void RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx)
            => _inner?.RemoveOutFeedback(contType, outDevice, inIdx);

        public void AttachNewUnboundOutDev(OutContType contType) => _inner?.AttachNewUnboundOutDev(contType);

        public void AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType)
            => _inner?.AttachUnboundOutDev(slotDevice, contType);

        public void DetachUnboundOutDev(OutSlotDevice slotDevice) => _inner?.DetachUnboundOutDev(slotDevice);

        public void PluginOutDev(int index, DS4Device device) => _inner?.PluginOutDev(index, device);

        public void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false)
            => _inner?.UnplugOutDev(index, device, immediate, force);

        public void QueueOnEventThread(Action action) => _inner?.QueueOnEventThread(action);
    }
}
