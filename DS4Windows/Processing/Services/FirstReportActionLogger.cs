using System;
using System.IO;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// 接続後の最初のレポートで、プロファイルのアクションが見つからないログを 1 回出す（プロファイルの適用ごとに 1 回。抑制は尊重する）。
    /// <see cref="InputReportProcessor"/> の依存を減らすために切り出した。最初のレポートでだけ呼ばれ、ホットパスには影響しない。
    /// プロファイルのログ自体は、接続時のプロファイル適用の側で出ている。
    /// </summary>
    public sealed class FirstReportActionLogger
    {
        private readonly IPathService _pathService;
        private readonly IProfileRepository _profileRepository;

        public FirstReportActionLogger(IPathService pathService, IProfileRepository profileRepository)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        }

        public void Emit(int ind)
        {
            if (File.Exists(Path.Combine(_pathService.AppDataPath, "Profiles", $"{_profileRepository.ProfilePath[ind]}.xml")))
            {
                try
                {
                    _profileRepository.EmitMissingActionLogsForDevice(ind, false);
                }
                catch { }
            }
        }
    }
}
