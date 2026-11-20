#nullable enable

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// スロットのコントローラーの電池残量の文言。
    /// 充電中は "%+"、充電中で 100% 以上は「満充電」、コントローラーがいなければ「N/A」。
    /// </summary>
    public interface IDeviceBatteryText
    {
        /// <summary>電池残量の文言を返す（範囲外・未接続は「N/A」）。</summary>
        string GetBatteryText(int index);
    }
}
