using System.Threading.Tasks;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>機器の GATT のバッテリーサービス（0x180F）から、残量（0〜100%）を 1 回読む。読めなければ null。実装は <c>GattBatteryFetcher</c>（WinRT）。</summary>
    public interface IGattBatteryFetcher
    {
        Task<int?> FetchAsync(ulong bluetoothAddress);
    }
}
