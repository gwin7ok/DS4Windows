namespace DS4WindowsTests
{
    /// <summary>
    /// コンストラクターの引数が 15 を超える型の許可リスト（"型の完全名|引数の数"。序数順）。
    /// <see cref="ConstructorArityRuleTests"/> が実数と一致することを検査する（増えても減っても失敗）。
    /// 是正で減ったら、同じコミットでここも更新する。新しい型は追加しない（責務の分割や束ねで是正する）。
    /// 更新の手順: 環境変数 DS4W_ARITY_BASELINE_OUT にファイルのパスを入れてテストを実行すると、実測から作ったこのファイルの内容をそのパスへ書き出す。
    /// </summary>
    public static class ConstructorArityBaseline
    {
        public static readonly string[] Entries =
        {
            "DS4Windows.Processing.Actions.ActionBindingBuilder|16",
            "DS4Windows.Processing.ControlService|16",
            "DS4Windows.Processing.Mapping|25",
            "DS4Windows.Processing.Services.DeviceConnector|18",
            "DS4Windows.UI.MainWindow|21",
            "DS4Windows.UI.Services.EditorWindowFactory|18",
            "DS4Windows.UI.Services.MainWindowFactory|20",
            "DS4Windows.UI.Services.ViewModelFactory|19",
        };
    }
}
