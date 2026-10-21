namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step4 W2（Phase10-Step4-Plan.md §3.3）: 特殊アクション（<see cref="SpecialAction"/>）の種別ごとの、出力アクションと実行方針の作り方。
    /// <see cref="ActionBindingBuilder"/>（SA タブ）と、Controls タブの束縛を作る側（W3 以降）が、同じ部品を使う。入力アクションは、呼び出し側が作る。
    /// </summary>
    public interface ISpecialActionOutputFactory
    {
        /// <summary>
        /// 出力と方針を作る。種別が未対応のとき、種別に必要な窓口が未接続のとき（テスト）は null（束縛を作らず、旧経路が処理する）。
        /// </summary>
        /// <param name="forControls">
        /// Controls 用（Phase10-Step4 W4）。方針を、Controls の既存の束縛と同じ形にする: トリガーのボタンを抑制せず・出力も消さず、割り当てたボタンの通常の出力を毎フレーム消す。
        /// SA（既定の偽）は、種別ごとの方針のまま。
        /// </param>
        SpecialActionOutputs Create(int device, SpecialAction sa, int index, bool forControls = false);
    }
}
