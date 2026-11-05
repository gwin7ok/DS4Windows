using System.Collections.Generic;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI
{
    /// <summary>
    /// Phase7-Step10b W2（Q1＝A）: トレイアイコンの選択ごとの資源パスの表（旧: <c>Global.iconChoiceResources</c>。宣言は移動のみ）。
    /// UI（<c>MainWindow</c>、<c>TrayIconViewModel</c>）が、<c>IAppearanceSettingsService.UseIconChoice</c> からパスを引く。
    /// Phase14b-Step13（決定 D13）: 旧 TODO（パスの解決を画面側へ寄せる）を実施した。<c>IAppearanceSettingsService.GetIconResourcePath</c> は外した。
    /// </summary>
    public static class TrayIconResources
    {
        public static Dictionary<TrayIconChoice, string> iconChoiceResources = new Dictionary<TrayIconChoice, string>
        {
            [TrayIconChoice.Default] = $"{DS4Windows.UI.ResourcePaths.ResourcesPrefix}/DS4W.ico",
            [TrayIconChoice.Colored] = $"{DS4Windows.UI.ResourcePaths.ResourcesPrefix}/DS4W.ico",
            [TrayIconChoice.White] = $"{DS4Windows.UI.ResourcePaths.ResourcesPrefix}/DS4W - White.ico",
            [TrayIconChoice.Black] = $"{DS4Windows.UI.ResourcePaths.ResourcesPrefix}/DS4W - Black.ico",
            [TrayIconChoice.Battery] = $"{DS4Windows.UI.ResourcePaths.ResourcesPrefix}/DS4W.ico"
        };
    }
}
