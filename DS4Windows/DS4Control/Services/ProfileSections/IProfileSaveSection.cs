namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: <c>LegacyProfileXmlSerializer.SaveProfile</c> の節（保存の順序で連続した要素の範囲）の契約。
    /// 節は、<c>SaveProfile</c>（指揮役）が定めた順序で 1 回ずつ呼ばれ、ルート要素へ子要素を足していく。状態を持たない（保存 1 回分の状態は <see cref="ProfileSaveContext"/>）。
    /// DI には登録しない（Q3＝A）。
    /// </summary>
    internal interface IProfileSaveSection
    {
        void Save(ProfileSaveContext context);
    }
}
