namespace DS4Windows.Processing.Abstractions
{
    /// <summary>出力スロットの状態のうち、<see cref="IVirtualPadService.DescribeSlot"/> で文字列にして返す項目。</summary>
    public enum OutputSlotInfoKind
    {
        CurrentType,
        PermanentType,
        AttachedStatus,
        InputBound,
    }
}
