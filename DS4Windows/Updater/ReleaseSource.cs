namespace DS4WinWPF
{
    /// <summary>
    /// 更新チェック・更新ダウンロード・「リリースページを開く」が参照する GitHub リポジトリの定義（1 か所）。
    /// 以前は、<c>Changelog</c>（API の URL）と <c>UpdaterWindow</c>（ページの URL 5 か所、DS4Updater のリポジトリ）に、リポジトリ名が散らばっていた。
    /// コンパイル時の定数だけを持つ（状態を持たない）。リポジトリを移すときは、ここだけを書き換える。
    /// <para>
    /// 移行の方針（リポジトリを祖父から新たにフォークし直す場合）: すでに配布した古いバージョンは、旧リポジトリの URL を埋め込んでいるため変えられない。
    /// そのため、<b>旧リポジトリに、新しいリポジトリの URL をここへ埋め込んだ「橋渡し」のバージョンを Release として出す</b>。
    /// 古いバージョンのユーザーは、いつもの更新チェックでそれを見つけて更新し、以降は新しいリポジトリを見る。旧リポジトリは、橋渡しが行き渡るまで残す
    /// （削除・非公開にすると、古いバージョンの更新チェックが失敗する）。
    /// </para>
    /// </summary>
    public static class ReleaseSource
    {
        /// <summary>DS4Windows 本体のリポジトリ（<c>所有者/名前</c>）。</summary>
        public const string Repository = "gwin7ok/DS4Windows-Vader4Pro";

        /// <summary>DS4Updater のリポジトリ（<c>所有者/名前</c>）。DS4Windows 本体のリポジトリとは別に移せる。</summary>
        public const string UpdaterRepository = "gwin7ok/DS4Updater";

        /// <summary>リポジトリのトップページ。</summary>
        public const string RepositoryPageUri = "https://github.com/" + Repository;

        /// <summary>リリースの一覧の API（変更履歴の取得）。</summary>
        public const string ReleasesApiUri = "https://api.github.com/repos/" + Repository + "/releases";

        /// <summary>最新のリリースの API（更新チェック）。</summary>
        public const string LatestReleaseApiUri = "https://api.github.com/repos/" + Repository + "/releases/latest";

        /// <summary>最新のリリースのページ（更新に失敗したときに開く）。</summary>
        public const string LatestReleasePageUri = "https://github.com/" + Repository + "/releases/latest";

        /// <summary>貢献者の一覧のファイル。</summary>
        public const string ContributorsPageUri = "https://github.com/" + Repository + "/blob/master/contributors.txt";

        /// <summary>仮想コントローラー（Moonlight）対応の説明ドキュメント。</summary>
        public const string MoonlightDocPageUri = "https://github.com/" + Repository + "/blob/master/doc/moonlight_support.md";

        /// <summary>DS4Updater の最新のリリースの API（DS4Updater が見つからないときの取得）。</summary>
        public const string UpdaterLatestReleaseApiUri = "https://api.github.com/repos/" + UpdaterRepository + "/releases/latest";
    }
}
