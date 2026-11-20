using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 言語・アイコン・テーマの設定。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class AppearanceState
    {
        public string useLang { get; set; } = "";
        public bool downloadLang { get; set; } = true;
        public TrayIconChoice useIconChoice { get; set; }
        public AppThemeChoice useCurrentTheme { get; set; }
        public string fakeExeFileName { get; set; } = string.Empty;
    }
}
