namespace DS4Windows.Core.Model
{
    /// <summary>出力スロットの予約の種類（動的か、恒久か）。層をまたいで共有する値の型のため、機器の型から出して置いている。</summary>
    public enum OutputSlotReserveStatus : uint
    {
        Dynamic = 0,
        Permanent = 1,
    }
}
