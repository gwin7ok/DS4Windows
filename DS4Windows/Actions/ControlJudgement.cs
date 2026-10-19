namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8c（Phase8-Step7-8c-Plan.md §3.2）: Controls タブの束縛（<see cref="ControlButtonInputAction"/>）の、押下の判定の種類。
    /// </summary>
    public enum ControlJudgement
    {
        /// <summary>Controls タブの Key／Button の判定（<see cref="IControlTriggerInput.IsControlActive"/>。現行の <c>GetBoolActionMapping</c>）。</summary>
        Assignment,

        /// <summary>SA タブのトリガーと同じ判定（<see cref="ITriggerInput.IsActive"/>）。Controls タブの Macro は、SA の Macro の仕様に合わせる（ユーザー指示）。</summary>
        Trigger,
    }
}
