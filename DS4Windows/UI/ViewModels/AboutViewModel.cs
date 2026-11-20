#nullable enable
using System;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Updater;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 「バージョン情報」画面の ViewModel。画面のリンクはすべて <see cref="OpenLinkCommand"/>（<c>CommandParameter</c> は URL）で開く。
    /// </summary>
    public sealed class AboutViewModel
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IShellLauncher _launcher;

        public AboutViewModel(IEnvironmentService environmentService, IShellLauncher launcher)
        {
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(launcher);
            _launcher = launcher;
            OpenLinkCommand = CommandGuard.CreateSync<string>(OpenLink, name: nameof(OpenLinkCommand));
        }

        public string? ApplicationVersion => _environmentService.ApplicationVersion;

        public string VersionText => $"v{_environmentService.ApplicationVersion}";

        public string AppTitle => "DS4Windows";

        public string GithubUrl => ReleaseSource.RepositoryPageUri;

        public string ChangelogUrl => ReleaseSource.RepositoryPageUri + "/releases";

        public string ContributorsUrl => ReleaseSource.ContributorsPageUri;

        /// <summary>URL を、既定のブラウザーで開く。</summary>
        public RelayCommand<string> OpenLinkCommand { get; }

        private void OpenLink(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                _launcher.OpenUrl(url);
            }
        }
    }
}
