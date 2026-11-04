namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I4a（旧 <c>ControlJudgement</c> の置き換え）: Controls タブの束縛（<see cref="ControlButtonInputAction"/>）の出力の種類。
    /// 押下の判定は、出力の種類から決まる（決める箇所は <see cref="ControlOutputNatureTable"/> の 1 か所）。
    /// </summary>
    public enum ControlOutputNature
    {
        /// <summary>
        /// デジタルな出力（キー、マクロ、SA 専用の種別、Extras、パッドのボタン、マウスのボタン・デジタルのホイール）。
        /// SA タブのトリガーと同じ判定（<see cref="ITriggerInput.IsActive"/>。Phase10-Step4c W4）。
        /// </summary>
        Digital,

        /// <summary>
        /// アナログな出力（パッドの軸・トリガー、マウスの移動、アナログのホイール、Default の軸の素通し）。
        /// 従来の Controls の判定（<see cref="IControlTriggerInput.IsControlActive"/>。現行の <c>GetBoolActionMapping</c>）。
        /// </summary>
        Analog,
    }
}
