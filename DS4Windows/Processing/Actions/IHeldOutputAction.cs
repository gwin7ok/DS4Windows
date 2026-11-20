namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// 押している間も毎フレーム呼ばれる出力アクション。
    /// 現行の Macro は、成立中の 2 フレーム目以降に <c>EndMacro</c> を呼ぶ（Alt-Tab のマクロのときだけ効果がある）。この呼び出しを失わないための任意の契約。
    /// </summary>
    public interface IHeldOutputAction : IOutputAction
    {
        void WhileHeld(IOutputContext ctx);
    }
}
