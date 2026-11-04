namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.5a-W2（Q2＝A）: 出力アクションが、立ち上がりの実行の直前に「実行してよいか」を答えるための口（任意）。
    /// 偽のときは、エンジンは、その束縛について、<c>BeingTriggered</c>・<c>IsExecuting</c> を変えず、<c>Execute</c> も呼ばずに素通しする（次のフレームで、また確かめる）。
    /// Profile の連鎖の防止（一時プロファイルの間、復帰の合図が Profile なら、別の切替を始めない）に使う。
    /// </summary>
    public interface IGatedOutputAction : IOutputAction
    {
        bool CanExecute(IOutputContext ctx);
    }
}
