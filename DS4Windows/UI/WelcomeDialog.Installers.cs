using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using DS4Windows.Core.Platform;

namespace DS4Windows.UI
{
    /// <summary>ViGEm・HidHide・FakerInput の導入。流れは <see cref="ExternalComponentInstaller"/> が持ち、ここは定義と、画面（ボタンの文言・操作の有効／無効）への写しだけ。</summary>
    public partial class WelcomeDialog
    {
        private Task InstallViGEm() =>
            RunInstall(vigemInstallBtn, new ExternalComponentSpec(installFileName, installDL, () =>
            {
                _environmentService.RefreshViGEmBusInfo();   // 導入したばかりの ViGEmBus の情報を読み直す
                return _environmentService.IsViGEmBusInstalled();
            }));

        private Task InstallHidHide() =>
            RunInstall(hidHideInstallBtn, new ExternalComponentSpec(InstHidHideFileNameX64, InstallerHidHideX64, _environmentService.IsHidHideInstalled));

        private Task InstallFakerInput() =>
            RunInstall(fakerInputInstallBtn, new ExternalComponentSpec(instFakerInputFileName, installFakerInputDL, _environmentService.IsFakerInputInstalled));

        private async Task RunInstall(ContentControl button, ExternalComponentSpec spec)
        {
            _viewModel.InstallRunning = true;

            // Progress は、作った画面のスレッドへ通知を戻す
            Progress<InstallProgress> progress = new Progress<InstallProgress>(p =>
            {
                switch (p.Stage)
                {
                    case InstallStage.Downloading:
                        button.Content = Properties.Resources.Downloading.Replace("*number*%", p.Percent.ToString("P"));
                        break;
                    case InstallStage.Installing:
                        button.Content = Properties.Resources.Installing;
                        break;
                    case InstallStage.Completed:
                        button.Content = Properties.Resources.InstallComplete;
                        _viewModel.InstallRunning = false;
                        break;
                    case InstallStage.Failed:
                        button.Content = Properties.Resources.InstallFailed;
                        _viewModel.InstallRunning = false;
                        break;
                }
            });

            await _installer.InstallAsync(spec, progress, _closing.Token);
        }
    }
}
