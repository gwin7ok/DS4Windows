namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2）: そのフレームで束縛が<b>評価された</b>ときだけ、成立の有無によらず、毎フレーム呼ばれる出力。
    /// アナログの値（パッドの軸・トリガー）の出力は、押していなくても値を読んで出す必要がある。ただし、旧経路が dcs を処理しなかったフレーム
    /// （2 段トリガーの選ばれなかった段、スティックがマウスなど）は、出力しない。エンジンの <c>ApplyFrameOutputs</c> が、評価されたフレームだけ呼ぶ。
    /// <see cref="IFrameOutputAction"/>（評価の有無によらず、毎フレーム呼ばれる）とは別。
    /// </summary>
    public interface IEvaluatedFrameOutputAction : IOutputAction
    {
        /// <param name="established">このフレームで束縛が成立したか（参考。値の出力は成立に依らない）。</param>
        void ApplyEvaluatedFrame(IOutputContext ctx, bool established);
    }
}
