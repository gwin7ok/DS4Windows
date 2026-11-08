namespace DS4Windows.Core.Model
{
    /// <summary>出力スロットが入力の機器に結び付いているか。層をまたいで共有する値の型のため、機器の型から出して置いている。</summary>
    public enum OutputSlotInputBound : uint
    {
        Unbound = 0,
        Bound = 1,
    }
}
