using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase14b-Step17（決定 D10）: 更新の確認と変更履歴の取得（GitHub のリリース API）。実装は <c>Core/Utilities/Changelog</c>（DI の Singleton。HttpClient を注入）。
    /// 旧: 静的クラス <c>Changelog</c> が、アプリの起点の静的フィールド <c>App.requestClient</c> を直接使っていた。
    /// </summary>
    public interface IChangelogService
    {
        /// <summary>新しい版があるか（結果は 1 回目の問い合わせの後、<paramref name="allowCached"/> が true のとき使い回す）。</summary>
        /// <param name="lastVersionChecked">スキップ済みの版（IAppSettingsService.LastVersionChecked）</param>
        bool CheckNewerVersionExists(out Version version, string lastVersionChecked, bool allowCached = true, bool respectSkip = true);

        /// <summary>版 → 変更履歴の本文（プレリリースを除く。<paramref name="allVersions"/> が false なら今の版より新しいものだけ）。</summary>
        Task<Dictionary<Version, string>> GetChangelog(bool allVersions = false);

        /// <summary>変更履歴を Markdown にまとめたもの（「**Full Changelog**」の行は除く）。</summary>
        Task<string> GetChangelogMarkdown(bool allVersions = false);
    }
}
