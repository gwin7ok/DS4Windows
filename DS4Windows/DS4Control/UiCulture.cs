using System.Globalization;
using System.Threading;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10d W4: 旧 <c>Global.SetCulture</c>。表示言語（UI カルチャ）を切り替える、状態を持たない静的ヘルパー（内容は移動のみ）。
    /// </summary>
    public static class UiCulture
    {
        public static void Set(string culture)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(culture);
            }
            catch { /* Skip setting culture that we cannot set */ }
        }
    }
}
