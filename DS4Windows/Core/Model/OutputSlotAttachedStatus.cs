namespace DS4Windows.Core.Model
{
    /// <summary>出力スロットに仮想パッドが接続されているか。層をまたいで共有する値の型のため、機器の型から出して置いている。</summary>
    public enum OutputSlotAttachedStatus : uint
    {
        UnAttached = 0,
        Attached = 1,
    }
}
