#nullable enable

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// ジャイロの有効化トリガーの判定の結果。<see cref="Active"/> は（トグルを反映した）成立、
    /// <see cref="ReverseRatchet"/> は「成立している間だけ出力する」（反転）か。出力するのは、両者が等しいとき。
    /// </summary>
    public readonly struct GyroTriggerResult
    {
        public GyroTriggerResult(bool active, bool reverseRatchet)
        {
            Active = active;
            ReverseRatchet = reverseRatchet;
        }

        public bool Active { get; }

        public bool ReverseRatchet { get; }

        /// <summary>出力してよいか（反転なしなら成立していないとき、反転ありなら成立しているとき）。</summary>
        public bool ShouldOutput => ReverseRatchet == Active;
    }
}
