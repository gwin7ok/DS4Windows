namespace DS4Windows.DI
{
    /// <summary>
    /// Phase7-Step10d W3（Q4＝A）: 第 1 層（<c>DS4Devices</c>／<c>DS4Device</c>／<c>DS4SixAxis</c>／<c>HidDevices</c>）が実行時に読む設定値の、
    /// 読み取り専用の窓口。旧: <c>Global.UseMoonlight／UseAdvancedMoonlight／DeviceOptions／DebouncingMs／UseDs3PitchRollSim</c> を第 1 層が直接参照していた
    /// （下位層から上位層への依存）。値は設定画面で変わるため、使うたびに読む。
    /// </summary>
    public interface IDeviceLayerSettings
    {
        /// <summary>Moonlight（仮想コントローラーの検出の除外の緩和）。</summary>
        bool UseMoonlight { get; }

        /// <summary>Advanced Moonlight（新しいコントローラーの検出を 5 秒間抑える）。</summary>
        bool UseAdvancedMoonlight { get; }

        /// <summary>HID の列挙などで詳細なログを出すか。</summary>
        bool VerboseLogMessages { get; }

        /// <summary>入力のデバウンス時間（ミリ秒）。</summary>
        int GetDebouncingMs(int slot);

        /// <summary>DS3 の加速度から pitch／roll を疑似計算するか。</summary>
        bool UseDs3PitchRollSim { get; }
    }
}
