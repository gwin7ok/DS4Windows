using System;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 更新の確認の判断（画面に依存しない）。更新の確認の処理が残す版の記録ファイルの内容と、いまの版を比べ、
    /// 版が違うときだけ変更履歴のサービスへ新しい版があるかを問い合わせる。
    /// </summary>
    public sealed class UpdateCheckPolicy
    {
        private readonly IChangelogService changelogService;
        private readonly IAppSettingsService appSettingsService;

        public UpdateCheckPolicy(IChangelogService changelogService, IAppSettingsService appSettingsService)
        {
            ArgumentNullException.ThrowIfNull(changelogService);
            this.changelogService = changelogService;
            ArgumentNullException.ThrowIfNull(appSettingsService);
            this.appSettingsService = appSettingsService;
        }

        /// <summary>
        /// 新しい版があるかを問い合わせる。起動時の自動の確認（<paramref name="startup"/> が true）は、スキップした版を除く（キャッシュを使う）。
        /// 手動の確認は、スキップした版も含めて確認する。通信の失敗などの例外は、そのまま呼び出し側へ伝える。
        /// </summary>
        public bool TryFindNewerVersion(bool startup, out Version version)
            => changelogService.CheckNewerVersionExists(out version, appSettingsService.LastVersionChecked, false, startup);

        /// <summary>起動時の自動の確認が有効で、前回の確認から設定の間隔が経っているか。</summary>
        public bool IsStartupCheckDue()
        {
            if (!appSettingsService.CheckUpdateStartupEnabled) return false;

            int everyVal = appSettingsService.CheckEveryValue;
            int everyUnit = appSettingsService.CheckEveryUnit; // 0=hours, 1=days
            double hoursToWait = (everyUnit == 0) ? everyVal : everyVal * 24.0;
            return everyVal == 0 || DateTime.Now >= appSettingsService.LastChecked + TimeSpan.FromHours(hoursToWait);
        }

        /// <summary>確認した時刻を、いまにする。</summary>
        public void MarkChecked() => appSettingsService.LastChecked = DateTime.Now;

        /// <param name="currentVersion">いま動いている版。</param>
        /// <param name="recordedVersion">版の記録ファイルの内容（ファイルがなければ空）。</param>
        /// <param name="latest">新しい版があるとき、その版。</param>
        public UpdateCheckOutcome Evaluate(string currentVersion, string recordedVersion, out Version latest)
        {
            latest = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(recordedVersion) && currentVersion.CompareTo(recordedVersion) != 0)
                {
                    if (!Version.TryParse(recordedVersion, out _))
                        return UpdateCheckOutcome.Silent;

                    // 記録された版とは別に、キャッシュを使わず新しい問い合わせをする
                    if (changelogService.CheckNewerVersionExists(out latest, appSettingsService.LastVersionChecked, allowCached: false))
                        return UpdateCheckOutcome.UpdateAvailable;

                    return UpdateCheckOutcome.UpToDate;
                }

                return UpdateCheckOutcome.UpToDate;
            }
            catch
            {
                latest = null;
                return UpdateCheckOutcome.UpToDate;
            }
        }
    }
}
