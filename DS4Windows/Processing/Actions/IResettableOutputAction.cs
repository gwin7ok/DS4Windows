namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e（Phase8-Step7-8e-Plan.md §3.2。Q6＝B）: 束縛の一覧が差し替わる（プロファイルの適用）か、デバイスが切断されるときに、
    /// 押している間だけ有効にした効果（振動、ライトバーの強制色、感度の一時変更）を元に戻すための任意の契約。
    /// 通常の <see cref="IOutputAction.Stop"/> は、押していたボタンを離したときにだけ呼ばれる。押したままの適用・切断では呼ばれず、効果が残ってしまう
    /// （旧経路の不具合。承認済みの修正）。エンジンは、差し替え前（切断では登録を消す前）の束縛の出力について、この契約を実装するものの
    /// <see cref="OnReset"/> を呼ぶ。有効にしていなければ、何もしない。ほかの出力（Key など）は実装せず、押したままの適用の動きは変わらない。
    /// </summary>
    public interface IResettableOutputAction : IOutputAction
    {
        /// <param name="ctx">デバイスの文脈（<see cref="IOutputContext.Device"/> だけが有効。フレームの出力は null）。</param>
        void OnReset(IOutputContext ctx);
    }
}
