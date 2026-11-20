using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HttpProgress;

namespace DS4Windows.UI
{
    /// <summary>ViGEm・HidHide・FakerInput の導入。</summary>
    public partial class WelcomeDialog
    {
        private async Task InstallViGEm()
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{installFileName}");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            if (File.Exists(tempInstFileName))
            {
                File.Delete(tempInstFileName);
            }

            EnableControls(false);
            await ViGEmDownloadLaunch();

            /*WebClient wb = new WebClient();
            wb.DownloadFileAsync(new Uri(InstallerDL), exepath + $"\\{installFileName}");

            wb.DownloadProgressChanged += wb_DownloadProgressChanged;
            wb.DownloadFileCompleted += wb_DownloadFileCompleted;
            */
        }

        private async Task ViGEmDownloadLaunch()
        {
            Progress<ICopyProgress> progress = new Progress<ICopyProgress>(x => // Please see "Notes on IProgress<T>"
            {
                // This is your progress event!
                // It will fire on every buffer fill so don't do anything expensive.
                // Writing to the console IS expensive, so don't do the following in practice...
                vigemInstallBtn.Content = Properties.Resources.Downloading.Replace("*number*%",
                    x.PercentComplete.ToString("P"));
                //Console.WriteLine(x.PercentComplete.ToString("P"));
            });

            string finalFilename = Path.Combine(Path.GetTempPath(), $"{installFileName}");
            bool success = false;
            using (var downloadStream = new FileStream(tempInstFileName, FileMode.CreateNew))
            {
                HttpResponseMessage response = await _httpClient.GetAsync(installDL,
                    downloadStream, progress);
                success = response.IsSuccessStatusCode;
            }

            if (success)
            {
                File.Move(tempInstFileName, finalFilename);
            }
            success = false; // Reset for later check

            if (File.Exists(finalFilename))
            {
                //vigemInstallBtn.Content = Properties.Resources.OpeningInstaller;
                ProcessStartInfo startInfo = new ProcessStartInfo(finalFilename);
                startInfo.UseShellExecute = true; // Needed to run program as admin
                monitorProc = Process.Start(startInfo);
                vigemInstallBtn.Content = Properties.Resources.Installing;
                success = true;
            }

            if (success)
            {
                EnableControls(false);

                monitorTimer = new System.Timers.Timer();
                monitorTimer.Elapsed += ViGEmInstallTimer_Tick;
                monitorTimer.Start();
            }
            else
            {
                vigemInstallBtn.Content = Properties.Resources.InstallFailed;
                EnableControls(true);
            }
        }

        private void ViGEmInstallTimer_Tick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            (sender as System.Timers.Timer)?.Stop();
            bool finished = false;
            if (monitorProc != null && monitorProc.HasExited)
            {
                // Retrieve info about installed ViGEmBus device if found
                _environmentService.RefreshViGEmBusInfo();
                if (_environmentService.IsViGEmBusInstalled())
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        vigemInstallBtn.Content = Properties.Resources.InstallComplete;
                        EnableControls(true);
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        vigemInstallBtn.Content = Properties.Resources.InstallFailed;
                        EnableControls(true);
                    }), null);
                }

                string currentFilePath = Path.Combine(Path.GetTempPath(), $"{installFileName}");
                File.Delete(currentFilePath);
                (sender as System.Timers.Timer)?.Stop();
                finished = true;
            }

            if (!finished)
            {
                (sender as System.Timers.Timer)?.Start();
            }
        }

        private async Task InstallHidHide()
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{InstHidHideFileNameX64}");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            string tempInstHidFileName = Path.Combine(Path.GetTempPath(), $"{InstHidHideFileNameX64}.tmp");
            if (File.Exists(tempInstHidFileName))
            {
                File.Delete(tempInstHidFileName);
            }

            EnableControls(false);
            await HidHideDownloadLaunch();
        }

        private async Task HidHideDownloadLaunch()
        {
            Progress<ICopyProgress> progress = new Progress<ICopyProgress>(x => // Please see "Notes on IProgress<T>"
            {
                // This is your progress event!
                // It will fire on every buffer fill so don't do anything expensive.
                // Writing to the console IS expensive, so don't do the following in practice...
                hidHideInstallBtn.Content = Properties.Resources.Downloading.Replace("*number*%",
                    x.PercentComplete.ToString("P"));
                //Console.WriteLine(x.PercentComplete.ToString("P"));
            });

            string tempInstHidFileName = Path.Combine(Path.GetTempPath(), $"{InstHidHideFileNameX64}.tmp");
            string finalFilename = Path.Combine(Path.GetTempPath(), $"{InstHidHideFileNameX64}");
            bool success = false;
            using (var downloadStream = new FileStream(tempInstHidFileName, FileMode.CreateNew))
            {
                HttpResponseMessage response = await _httpClient.GetAsync(InstallerHidHideX64,
                    downloadStream, progress);
                success = response.IsSuccessStatusCode;
            }

            if (success)
            {
                File.Move(tempInstHidFileName, finalFilename);
            }
            success = false; // Reset for later check

            if (File.Exists(finalFilename))
            {
                //vigemInstallBtn.Content = Properties.Resources.OpeningInstaller;
                ProcessStartInfo startInfo = new ProcessStartInfo(finalFilename);
                startInfo.UseShellExecute = true; // Needed to run program as admin
                monitorProc = Process.Start(startInfo);
                hidHideInstallBtn.Content = Properties.Resources.Installing;
                success = true;
            }

            if (success)
            {
                monitorTimer = new System.Timers.Timer();
                monitorTimer.Elapsed += HidHideInstallTimer_Elapsed;
                monitorTimer.Start();
            }
            else
            {
                hidHideInstallBtn.Content = Properties.Resources.InstallFailed;
                EnableControls(true);
            }
        }

        private void HidHideInstallTimer_Elapsed(object? sender,
            System.Timers.ElapsedEventArgs e)
        {
            (sender as System.Timers.Timer)?.Stop();
            bool finished = false;
            if (monitorProc != null && monitorProc.HasExited)
            {
                if (_environmentService.IsHidHideInstalled())
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        hidHideInstallBtn.Content = Properties.Resources.InstallComplete;
                        EnableControls(true);
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        hidHideInstallBtn.Content = Properties.Resources.InstallFailed;
                        EnableControls(true);
                    }), null);
                }

                string currentFilePath = Path.Combine(Path.GetTempPath(), $"{InstHidHideFileNameX64}");
                File.Delete(currentFilePath);
                (sender as System.Timers.Timer)?.Stop();
                finished = true;
            }

            if (!finished)
            {
                (sender as System.Timers.Timer)?.Start();
            }
        }

        private async Task InstallFakerInput()
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{installFileName}");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            string tempInstFakerInputName = Path.Combine(Path.GetTempPath(), $"{instFakerInputFileName}.tmp");
            if (File.Exists(tempInstFakerInputName))
            {
                File.Delete(tempInstFakerInputName);
            }

            EnableControls(false);
            await FakerInputDownloadLaunch();
        }

        private async Task FakerInputDownloadLaunch()
        {
            Progress<ICopyProgress> progress = new Progress<ICopyProgress>(x => // Please see "Notes on IProgress<T>"
            {
                // This is your progress event!
                // It will fire on every buffer fill so don't do anything expensive.
                // Writing to the console IS expensive, so don't do the following in practice...
                fakerInputInstallBtn.Content = Properties.Resources.Downloading.Replace("*number*%",
                    x.PercentComplete.ToString("P"));
                //Console.WriteLine(x.PercentComplete.ToString("P"));
            });

            string tempInstFakerInputFileName = Path.Combine(Path.GetTempPath(),
                $"{instFakerInputFileName}.tmp");
            string finalFilename = Path.Combine(Path.GetTempPath(), $"{instFakerInputFileName}");
            bool success = false;
            using (var downloadStream = new FileStream(tempInstFakerInputFileName, FileMode.CreateNew))
            {
                HttpResponseMessage response = await _httpClient.GetAsync(installFakerInputDL,
                    downloadStream, progress);
                success = response.IsSuccessStatusCode;
            }

            if (success)
            {
                File.Move(tempInstFakerInputFileName, finalFilename);
            }
            success = false; // Reset for later check

            if (File.Exists(finalFilename))
            {
                //vigemInstallBtn.Content = Properties.Resources.OpeningInstaller;
                ProcessStartInfo startInfo = new ProcessStartInfo(finalFilename);
                startInfo.UseShellExecute = true; // Needed to run program as admin
                monitorProc = Process.Start(startInfo);
                fakerInputInstallBtn.Content = Properties.Resources.Installing;
                success = true;
            }

            if (success)
            {
                monitorTimer = new System.Timers.Timer();
                monitorTimer.Elapsed += FakerInputInstallTimer_Elapsed;
                monitorTimer.Start();
            }
            else
            {
                fakerInputInstallBtn.Content = Properties.Resources.InstallFailed;
                EnableControls(true);
            }
        }

        private void FakerInputInstallTimer_Elapsed(object? sender,
            System.Timers.ElapsedEventArgs e)
        {
            (sender as System.Timers.Timer)?.Stop();
            bool finished = false;
            if (monitorProc != null && monitorProc.HasExited)
            {
                if (_environmentService.IsFakerInputInstalled())
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        fakerInputInstallBtn.Content = Properties.Resources.InstallComplete;
                        EnableControls(true);
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        fakerInputInstallBtn.Content = Properties.Resources.InstallFailed;
                        EnableControls(true);
                    }), null);
                }

                string currentFilePath = Path.Combine(Path.GetTempPath(), $"{instFakerInputFileName}");
                File.Delete(currentFilePath);
                (sender as System.Timers.Timer)?.Stop();
                finished = true;
            }

            if (!finished)
            {
                (sender as System.Timers.Timer)?.Start();
            }
        }
    }
}
