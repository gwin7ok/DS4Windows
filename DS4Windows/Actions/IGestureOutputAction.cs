namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（Phase8-Step7-7-Plan.md §3.2）: ジェスチャー（タップ・長押し・ダブルタップ）を受ける出力。
    /// 入力アクション（<c>MultiTapInputAction</c>）が <see cref="TriggerState.Gesture"/> を返したフレームに、エンジンが呼ぶ。
    /// 通常の出力（成立のエッジで <c>Execute</c>、離したとき <c>Stop</c>）とは独立で、ガード（Fresh-press、実行中のブロック）や <c>BeingTriggered</c> を見ない。
    /// <c>RequestEndFrame</c> は効かない（旧経路の MultiAction も、フレームを打ち切らない）。
    /// </summary>
    public interface IGestureOutputAction : IOutputAction
    {
        /// <param name="gesture"><see cref="TriggerState.Tap"/>、<see cref="TriggerState.Hold"/>、<see cref="TriggerState.DoubleTap"/> のいずれか。</param>
        void OnGesture(IOutputContext ctx, int gesture);
    }
}
