using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase10-Step4d W1（Phase10-Step4d-Plan.md §3）: プロファイルの固定 ID と名前（ファイル名）を結ぶ登録簿。
    /// ID は、プロファイル XML のルート直下の <c>&lt;ProfileId&gt;</c>（GUID の <c>N</c> 形式）に保存する。名前を変えても変わらない。
    /// W1 では、ID の割り当て・検索だけを提供し、参照は名前のまま（参照を ID に替えるのは W3〜W4）。名前は、拡張子なしのファイル名。
    /// </summary>
    public interface IProfileIdentityService
    {
        /// <summary>
        /// すべてのプロファイルのファイルを調べ、ID を持たないものに ID を書き、ID が重複しているもの（ファイルのコピー）に新しい ID を振って書く。
        /// 重複では、作成日時が古いほうが元の ID を保つ（同じなら名前順で先のもの）。起動時に 1 回呼ぶ。ID を書けなかったファイルの名前の一覧を返す
        /// （読み取り専用、壊れたファイルなど。これらは、参照を保存する直前の <see cref="EnsurePersisted"/> で、改めて試す）。
        /// </summary>
        IReadOnlyList<string> EnsureAllPersisted();

        /// <summary>名前のプロファイルの ID を読む。ファイルがない・ID がないときは null。</summary>
        string GetId(string profileName);

        /// <summary>ID のプロファイルの名前を探す。見つからなければ null。</summary>
        string GetName(string profileId);

        /// <summary>名前のプロファイルが ID を持つようにし、その ID を返す（なければ書く）。ファイルがない・書けないときは null。</summary>
        string EnsurePersisted(string profileName);

        /// <summary>
        /// 参照の解決。ID でプロファイルを探し、見つかればその名前を返す。見つからないとき（古いファイル、手編集）は、<paramref name="nameHint"/> の
        /// プロファイルがあれば、その名前を返す。どちらもなければ偽。
        /// </summary>
        bool TryResolve(string profileId, string nameHint, out string profileName);
    }
}
