namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W1: 押している間も毎フレーム呼ばれる出力アクション（Phase8-Step2-Design.md §14-3）。
    /// 現行の Macro は、成立中の 2 フレーム目以降に <c>EndMacro</c> を呼ぶ（Alt-Tab のマクロのときだけ効果がある）。この呼び出しを失わないための任意の契約。
    /// </summary>
    public interface IHeldOutputAction : IOutputAction
    {
        void WhileHeld(IOutputContext ctx);
    }
}
