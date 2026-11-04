namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step7 7.8d: <see cref="RemovedShiftNotice.Apply"/> の結果。
    /// </summary>
    public enum RemovedShiftOutcome
    {
        /// <summary>保持した内容がなく、警告を出さなかった。</summary>
        NoNotice,

        /// <summary>警告を出し、ユーザーが削除を選んだ（保持した内容を捨てた）。</summary>
        Deleted,

        /// <summary>警告を出し、ユーザーが残すを選んだ（内容はそのまま）。</summary>
        Kept,
    }
}
