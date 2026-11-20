namespace DS4Windows.Core.Model
{
    /// <summary>コントローラーを他のアプリから見えなくしている状態（排他の状態）。層をまたいで共有する値の型のため、機器の型から出して置いている。</summary>
    public enum DeviceExclusiveStatus : byte
    {
        Shared = 0,
        Exclusive = 1,
        HidGuardAffected = 2,
        HidHideAffected = 3,
    }
}
