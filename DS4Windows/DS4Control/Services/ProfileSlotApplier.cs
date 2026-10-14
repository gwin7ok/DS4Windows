namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IProfileSlotApplier"/> の既定実装。現行の <c>Global.ApplyProfileToSlot</c> へ薄く委譲し、
    /// 適用処理・ログ・通知の挙動を従来と完全に同一に保つ。
    /// </summary>
    public class ProfileSlotApplier : IProfileSlotApplier
    {
        // TODO(技術的負債): 現状は Global.ApplyProfileToSlot（→ Global.ApplyProfile）へ委譲している。
        // （Phase6-Step10-4-2f: K1 の「スロット5〜8の拒否」「戻り値の破棄」は是正済み。以下の「接続処理中の Halt の入れ子」のみ未整理。）
        // IProfileApplicationService.ApplyProfile は、スロット5〜8を拒否していた（deviceIndex >= 4 のハードコード。是正済み）こと、
        // および接続処理中の Halt が入れ子になり得ることから、現時点では置換できない（Phase6-Step2-Plan.md §0.3.5 K1）。
        // K1 の是正後、Global.ApplyProfileToSlot の「フェーズG」（IProfileApplicationService への完全委譲）で差し替える。
        public bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source)
            => Global.ApplyProfileToSlot(slotIndex, profileName, source);
    }
}