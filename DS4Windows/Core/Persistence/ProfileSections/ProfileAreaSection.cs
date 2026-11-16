namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// プロファイルの 1 つの領域の、読み込みの節（ファイル順で連続する 1 つ以上）と保存の節の対。
    /// 領域を足すときに、読み込みと保存を同時に宣言させ、片方だけの追加を防ぐ。
    /// 読み込みは宣言した順に、保存は 1 回だけ呼ぶ。節は状態を持たない（1 回分の状態は文脈）ので、順序と呼び出し回数は元の指揮役と同じ。
    /// 領域どうしの呼び出し順（読み込みはファイル順、保存は XML の順序。ゴールデンが固定）は、指揮役が決める。
    /// </summary>
    internal sealed class ProfileAreaSection : IProfileLoadSection, IProfileSaveSection
    {
        private readonly IProfileLoadSection[] _loads;
        private readonly IProfileSaveSection _save;

        public ProfileAreaSection(string name, IProfileSaveSection save, params IProfileLoadSection[] loads)
        {
            Name = name;
            _save = save ?? throw new System.ArgumentNullException(nameof(save));
            _loads = loads == null || loads.Length == 0 ? throw new System.ArgumentException("読み込みの節が必要", nameof(loads)) : loads;
        }

        /// <summary>領域の名前（診断用）。</summary>
        public string Name { get; }

        public void Load(ProfileLoadContext context)
        {
            foreach (IProfileLoadSection load in _loads) load.Load(context);
        }

        public void Save(ProfileSaveContext context) => _save.Save(context);
    }
}
