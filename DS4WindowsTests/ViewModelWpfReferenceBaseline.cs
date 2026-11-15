namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V1（画面の部品［ViewModel］は WPF の型を参照しない。<c>ICommand</c> だけ例外）の、今ある違反の許可リスト。"参照元|参照先" の形（序数順）。
    /// <see cref="ViewModelWpfReferenceRatchetTests"/> が実数と一致することを検査する（多くても少なくても失敗）。
    /// 是正で減ったら、同じコミットでここからも消す。新しい違反は追加しない（是正する）。
    /// 更新の手順: 環境変数 DS4W_VMWPF_BASELINE_OUT にファイルのパスを入れてテストを実行すると、実測から作った内容をそのパスへ書き出す。
    /// </summary>
    public static class ViewModelWpfReferenceBaseline
    {
        public static readonly string[] Entries =
        {
            "DS4Windows.UI.ViewModels.AssignmentDescriber|System.Windows.Input.Key",
            "DS4Windows.UI.ViewModels.AssignmentDescriber|System.Windows.Input.KeyInterop",
            "DS4Windows.UI.ViewModels.CompositeDeviceModel|System.Windows.Application",
            "DS4Windows.UI.ViewModels.MacroStepItem|System.Windows.Application",
        };
    }
}
