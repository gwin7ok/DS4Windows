namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md Q2＝A）: Controls タブの割り当て用の押下の判定を持つ入力。
    /// <see cref="ITriggerInput.IsActive"/> は特殊アクションのトリガー用の判定（スティックは ±55、L2／R2 は 100 より大きい）で、
    /// こちらは現行の <c>Mapping.GetBoolActionMapping</c> と同じ判定（スティックは角度の扇形、L2／R2 は 0 より大きい、ジャイロは 0 より大きい／小さい）。
    /// 既存の <see cref="ITriggerInput"/> とその偽物を変えないために、別のインターフェースにした。
    /// </summary>
    public interface IControlTriggerInput : ITriggerInput
    {
        /// <summary>Controls タブの割り当てから見て、ボタンが押されているか。</summary>
        bool IsControlActive(DS4Controls control);
    }
}
