using System.Globalization;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10d W2（Q6＝A）: 旧 <c>Global.configFileDecimalCulture</c>。設定ファイルの小数の読み書きは、常に en-US の書式
    /// （小数点がドット、コンマではない）にする。不変の静的な値。
    /// </summary>
    public static class ConfigFileCulture
    {
        public static readonly CultureInfo DecimalCulture = new CultureInfo("en-US");
    }
}
