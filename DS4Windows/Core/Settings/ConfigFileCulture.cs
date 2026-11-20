#nullable enable

using System.Globalization;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 旧 <c>Global.configFileDecimalCulture</c>。設定ファイルの小数の読み書きは、常に en-US の書式
    /// （小数点がドット、コンマではない）にする。不変の静的な値。
    /// </summary>
    public static class ConfigFileCulture
    {
        public static readonly CultureInfo DecimalCulture = new CultureInfo("en-US");
    }
}
