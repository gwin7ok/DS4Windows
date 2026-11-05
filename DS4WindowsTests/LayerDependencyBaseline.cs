namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 層の依存の規則（L1・L3〜L6）に今ある違反の許可リスト。"規則|参照元の型|参照先の型" の形（序数順）。
    /// <see cref="LayerDependencyRuleTests"/> が実数と一致することを検査する（多くても少なくても失敗。規約 §3.1 のラチェットの一致検査と同じ運用）。
    /// 是正で減ったら、同じ Step でここからも消す。新しい違反は追加しない（是正する）。
    /// 更新の手順: 環境変数 DS4W_LAYER_BASELINE_OUT にファイルのパスを入れてテストを実行すると、実測から作った本ファイルの内容をそのパスへ書き出す。
    /// </summary>
    public static class LayerDependencyBaseline
    {
        public static readonly string[] Entries =
        {
            "L1|DS4Windows.Core.Persistence.SpecialActionRepository|DS4Windows.Processing.Actions.IManagedActionManager",
            "L1|DS4Windows.Core.Persistence.SpecialActionRepository|DS4Windows.Processing.SyntheticInputState",
            "L1|DS4Windows.Core.Persistence.SpecialActionsXmlSerializer|DS4Windows.Processing.Actions.IManagedActionManager",
            "L1|DS4Windows.Core.Utilities.Changelog|DS4Windows.App",
            "L6|DS4Windows.UI.ImageLocationPaths|DS4Windows.App",
            "L6|DS4Windows.UI.LanguagePackControl|DS4Windows.App",
            "L6|DS4Windows.UI.MainWindow|DS4Windows.App",
            "L6|DS4Windows.UI.ViewModels.SettingsViewModel|DS4Windows.StartupMethods",
            "L6|DS4Windows.UI.WelcomeDialog|DS4Windows.App",
        };
    }
}
