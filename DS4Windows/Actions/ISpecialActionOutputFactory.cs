namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step4 W2（Phase10-Step4-Plan.md §3.3）: 特殊アクション（<see cref="SpecialAction"/>）の種別ごとの、出力アクションと実行方針の作り方。
    /// <see cref="ActionBindingBuilder"/>（SA タブ）と、Controls タブの束縛を作る側が、共通の組み立て（<see cref="AssignmentBindingBuilder"/>）を通して同じ部品を使う。入力アクションは、組み立ての側が作る。
    /// </summary>
    public interface ISpecialActionOutputFactory
    {
        /// <summary>
        /// Phase10-Step6 I3: 定義（設定の静的な内容。<see cref="AssignmentDefinition"/>）から、出力と方針を作る。方針は、種別と由来（<see cref="AssignmentDefinition.Origin"/>）で決める。
        /// 種別が未対応のとき、種別に必要な窓口が未接続のとき（テスト）は null（束縛を作らない）。
        /// Phase10-Step6 I4c: 互換の入口 <c>Create(device, sa, index, forControls)</c> は削除した（<c>forControls</c> は由来に置き換わった）。
        /// </summary>
        /// <param name="source">
        /// 実行時の状態の入れ物（出力アクションとエンジンが受け取る <see cref="SpecialAction"/>）。SA 由来は、プロファイルの実物。Controls 由来は、
        /// <see cref="SpecialActionItemMapper.ToSpecialAction"/> などが作る合成のもの。
        /// </param>
        SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index);
    }
}
