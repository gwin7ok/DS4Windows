namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// Bluetooth の機器の電池の残量（0〜100%）を、Bluetooth アドレスで引く窓口。読み取りは時間がかかる（無線の通信）ため、
    /// 呼び出しは待たない: 最後に読めた値を返し、古ければ裏で読み直す。読めない間は false を返す（呼ぶ側は別の手段にもどる）。
    /// </summary>
    public interface IBluetoothBatteryReader
    {
        bool TryGetPercent(ulong bluetoothAddress, out int percent);

        /// <summary>
        /// 値がまだないとき、最初の読み取りが終わるまで（最長 <paramref name="maxWait"/>）待つ。接続直後の最初の 1 回だけ使う
        /// （接続時のログ・画面が、0% や不正確な値を出さないため）。入力のループからは、待たない方を使う。
        /// </summary>
        bool TryGetPercent(ulong bluetoothAddress, System.TimeSpan maxWait, out int percent);
    }
}
