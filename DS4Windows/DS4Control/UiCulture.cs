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
                CultureInfo ci = CultureInfo.GetCultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                // Resources.Culture が null のままだと、ログを書くバックグラウンドスレッドが OS の言語（日本語など）で文言を引いてしまう。
                // 空文字（English UI）はインバリアントカルチャ＝既定の英語になる。
                DS4WinWPF.Properties.Resources.Culture = ci;
            }
            catch { /* Skip setting culture that we cannot set */ }
        }
    }
}
