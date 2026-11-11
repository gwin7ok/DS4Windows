namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// Bluetooth の機器の電池の残量（0〜100%）を、Bluetooth アドレスで引く窓口。読み取りは時間がかかる（無線の通信）ため、
    /// 呼び出しは待たない: 最後に読めた値を返し、古ければ裏で読み直す。読めない間は false を返す（呼ぶ側は別の手段にもどる）。
    /// </summary>
    public interface IBluetoothBatteryReader
    {
        bool TryGetPercent(ulong bluetoothAddress, out int percent);
    }
}
