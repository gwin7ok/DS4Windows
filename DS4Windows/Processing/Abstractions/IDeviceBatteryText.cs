namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step6 6-b: スロットのコントローラーの電池残量の文言（旧 <c>ControlService.GetDS4Battery</c>）。
    /// 充電中は "%+"、充電中で 100% 以上は「満充電」、コントローラーがいなければ「N/A」。
    /// </summary>
    public interface IDeviceBatteryText
    {
        /// <summary>電池残量の文言を返す（範囲外・未接続は「N/A」）。</summary>
        string GetBatteryText(int index);
    }
}
