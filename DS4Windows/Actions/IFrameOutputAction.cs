namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W1: 毎フレームの出力を持つ出力アクション（Button の Press／Toggle。Phase8-Step2-Design.md §5）。
    /// エンジンの <c>ApplyFrameOutputs</c> が、通常の割り当ての後（現行の Stage3 の位置）に、束縛ごとに呼ぶ。
    /// </summary>
    public interface IFrameOutputAction : IOutputAction
    {
        /// <param name="active">出力を ON にするか（トリガーが成立している間 true。Toggle は実装が内部で反転の状態を持つ）。</param>
        /// <param name="risingEdge">このフレームで成立した（前のフレームは未成立）か。</param>
        void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge);
    }
}
