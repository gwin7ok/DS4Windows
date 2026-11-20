using System;
using System.Diagnostics;
using DS4Windows.Core.Abstractions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>サービスの開始・停止・再起動、ドライバー・更新の確認、優先度の変更。</summary>
    public partial class MainWindowsViewModel
    {
        private async Task SetUdpServerAsync(bool on)
        {
            // 切るときはモーション送出を先に止めてからポートを閉じ、入れるときはポートを開いてからモーション送出を始める
            if (!on)
            {
                _features.ChangeMotionEventStatus(on);
                await Task.Delay(200).ConfigureAwait(true);
                _features.ChangeUdpStatus(on);
            }
            else
            {
                _features.ChangeUdpStatus(on);
                await Task.Delay(200).ConfigureAwait(true);
                _features.ChangeMotionEventStatus(on);
            }
        }

        private async Task DriverSetupAsync()
        {
            IsServiceBusy = true;
            try
            {
                await Task.Run(() =>
                {
                    if (_lifecycle.Running)
                        _lifecycle.Stop();
                }).ConfigureAwait(true);
            }
            finally
            {
                IsServiceBusy = false;
            }

            await Task.Run(() => _launcher.RunElevatedAndWait(_pathService.ExecutablePath, "-driverinstall")).ConfigureAwait(true);
            _environmentService.RefreshHidHideInfo();
            _environmentService.RefreshFakerInputInfo();
            Navigator?.RefreshDriverCheck();
        }

        /// <summary>
        /// 設定の優先度の番号を、このアプリのプロセスへ反映する。管理者でなければ RealTime は使えないため、通知して High にする。
        /// 戻り値は、実際に反映した番号（設定の表示を合わせるために、呼び出し側が設定へ書き戻す）。
        /// </summary>
        public int ApplyProcessPriority(int index)
        {
            var classes = DS4Windows.Core.Platform.ProcessPriorityOptions.Classes;
            if (index < 0 || index >= classes.Count) return index;

            System.Diagnostics.ProcessPriorityClass selected = classes[index];
            if (!_environmentService.IsAdministrator() && selected == System.Diagnostics.ProcessPriorityClass.RealTime)
            {
                _dialogs.ShowMessage(DS4Windows.Translations.Strings.RealTimeNoAdmin);
                selected = System.Diagnostics.ProcessPriorityClass.High;
                index = DS4Windows.Core.Platform.ProcessPriorityOptions.IndexOf(selected);
            }

            _priority.Apply(selected);
            return index;
        }

        private async Task CheckUpdatesAsync()
        {
            const string title = "DS4Windows Updater";
            Version? version;
            bool found;
            try
            {
                // 手動の確認は、以前にスキップした版を無視する
                (found, version) = await Task.Run(() =>
                {
                    bool result = _updateCheckPolicy.TryFindNewerVersion(startup: false, out Version? latest);
                    return (result, latest);
                }).ConfigureAwait(true);
            }
            catch
            {
                _dialogs.ShowMessage(DS4Windows.Translations.Strings.FailedToRetrieveLatestVersion, title);
                // 原因をログで見られるよう、共通の窓口（CommandGuard）へ渡す
                throw;
            }

            if (found && version != null)
                Navigator?.ShowUpdater(version.ToString());
            else
                _dialogs.ShowMessage(DS4Windows.Properties.Resources.UpToDate, title);
        }

        private async Task ToggleServiceAsync()
        {
            // 開始・停止は、状態の変更の通知（NotifyServiceStateChanged）が来るまで、ボタンを押せなくする
            IsServiceBusy = true;
            try
            {
                await Task.Run(() =>
                {
                    if (_lifecycle.Running)
                        _lifecycle.Stop(immediateUnplug: true);
                    else
                        _lifecycle.Start();
                }).ConfigureAwait(true);
            }
            catch
            {
                // 失敗しても状態の通知が来ないことがあるため、ボタンを戻して、例外は共通の窓口（CommandGuard）へ渡す
                IsServiceBusy = false;
                throw;
            }
        }

        private async Task RestartServiceAsync(bool isXbox)
        {
            IsServiceBusy = true;
            if (isXbox) HideXboxToggleEnabled = false; else HideDs4ToggleEnabled = false;
            try
            {
                await Task.Run(() =>
                {
                    _lifecycle.Stop();
                    _lifecycle.Start();
                }).ConfigureAwait(true);
            }
            finally
            {
                if (isXbox) HideXboxToggleEnabled = true; else HideDs4ToggleEnabled = true;
                IsServiceBusy = false;
            }
        }

        public void CheckDrivers()
        {
            bool deriverinstalled = _environmentService.IsViGEmBusInstalled();
            if (!deriverinstalled || !_environmentService.IsRunningSupportedViGEmBus())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = $"{_pathService.ExecutablePath}";
                startInfo.Arguments = "-driverinstall";
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
                try
                {
                    using (Process? temp = Process.Start(startInfo))
                    {
                    }
                }
                catch (Exception caught) when (caught is System.ComponentModel.Win32Exception or System.InvalidOperationException or System.NotSupportedException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
            }
        }
    }
}
