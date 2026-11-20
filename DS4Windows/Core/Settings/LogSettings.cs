using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// ログの保存の設定。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class LogSettings
    {
        public int logMaxArchiveFiles { get; set; } = BackingStore.DEFAULT_LOG_MAX_ARCHIVE_FILES;
        public string logMinLevel { get; set; } = BackingStore.DEFAULT_LOG_MIN_LEVEL;
    }
}
