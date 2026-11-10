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
            this.changelogService = changelogService ?? throw new ArgumentNullException(nameof(changelogService));
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        }

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
