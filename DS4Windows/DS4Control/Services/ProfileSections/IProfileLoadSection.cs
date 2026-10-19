namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W2（Phase8-Step4b-Plan.md §3.2）: <c>LegacyProfileXmlSerializer.LoadProfile</c> の節（ファイル順で連続した要素の範囲）の契約。
    /// 節は、<c>LoadProfile</c>（指揮役）が定めた順序で 1 回ずつ呼ばれる。状態を持たない（読込 1 回分の状態は <see cref="ProfileLoadContext"/>）。
    /// DI には登録しない（Q3＝A）。
    /// </summary>
    internal interface IProfileLoadSection
    {
        void Load(ProfileLoadContext context);
    }
}
