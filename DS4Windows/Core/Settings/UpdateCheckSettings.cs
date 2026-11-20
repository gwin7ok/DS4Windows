using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 更新の確認の設定。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class UpdateCheckSettings
    {
        public DateTime lastChecked { get; set; }
        public string lastVersionChecked { get; set; } = string.Empty;
        public ulong lastVersionCheckedNum { get; set; }
        // New explicit startup update-check persistence fields
        public bool checkUpdateStartup { get; set; } = true;
        // 既定値の出所を DEFAULT_CHECK_EVERY_VALUE／_UNIT に一本化した（値は従来と同じ）
        public int checkEveryValue { get; set; } = BackingStore.DEFAULT_CHECK_EVERY_VALUE; // default 1 day
        public int checkEveryUnit { get; set; } = BackingStore.DEFAULT_CHECK_EVERY_UNIT; // 0=hours,1=days
    }
}
