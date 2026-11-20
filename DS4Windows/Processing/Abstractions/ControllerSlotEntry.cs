namespace DS4Windows.Processing.Abstractions
{
    /// <summary>接続中のコントローラーの一覧の 1 行。<see cref="SlotIndex"/> は入力スロットの番号、一覧での位置は並びの順。</summary>
    public readonly struct ControllerSlotEntry
    {
        public ControllerSlotEntry(int slotIndex, IControllerSlotView view)
        {
            SlotIndex = slotIndex;
            View = view;
        }

        public int SlotIndex { get; }

        public IControllerSlotView View { get; }
    }
}
