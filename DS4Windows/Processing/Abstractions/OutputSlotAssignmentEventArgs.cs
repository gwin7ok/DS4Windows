using System;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>出力スロットへの仮想コントローラーの割り当て・解除の通知。</summary>
    public sealed class OutputSlotAssignmentEventArgs : EventArgs
    {
        public OutputSlotAssignmentEventArgs(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        /// <summary>対象のスロット番号（0 始まり）。</summary>
        public int SlotIndex { get; }
    }
}
