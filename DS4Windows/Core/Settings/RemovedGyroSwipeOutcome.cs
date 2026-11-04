namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step8 8-0: <see cref="RemovedGyroSwipeNotice.Apply"/> の結果。
    /// </summary>
    public enum RemovedGyroSwipeOutcome
    {
        /// <summary>保持した内容がなく、警告を出さなかった。</summary>
        NoNotice,

        /// <summary>警告を出し、ユーザーが削除を選んだ（保持した内容を捨てた）。</summary>
        Deleted,

        /// <summary>警告を出し、ユーザーが残すを選んだ（内容はそのまま）。</summary>
        Kept,
    }
}
