using System;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>コントローラーが接続されスロットに入ったことの通知（旧: <c>ControlService.HotplugController</c>）。</summary>
    public sealed class ControllerSlotChangedEventArgs : EventArgs
    {
        public ControllerSlotChangedEventArgs(IControllerSlotView view, int slotIndex)
        {
            View = view;
            SlotIndex = slotIndex;
        }

        public IControllerSlotView View { get; }

        public int SlotIndex { get; }
    }
}
