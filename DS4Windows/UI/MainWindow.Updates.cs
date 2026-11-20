using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.ComponentModel;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Utilities;
using DS4Windows.Translations;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>更新の確認と更新ウィンドウの表示。</summary>
    public partial class MainWindow
    {
        public void LateChecks(ArgumentParser parser)
        {
            Task tempTask = Task.Run(() =>
            {
                mainWinVM.CheckDrivers();
                if (!parser.Stop)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        mainWinVM.IsServiceBusy = true;
                    }));
                    Thread.Sleep(1000); // 待機の理由: サービスの再起動の間隔（バックグラウンドのスレッドの中）
                    mainWinVM.StartService();
                    //root.rootHubtest.Start();
                }
            });

            // Log exceptions that might occur
            Util.LogAssistBackgroundTask(tempTask);
#if !BETA_VERSION
            tempTask = Task.Delay(100).ContinueWith(_ =>
            {
                // Use explicit startup-check settings persisted in Profiles.xml
                if (updateCheckPolicy.IsStartupCheckDue())
                {
                    try
                    {
                        if (updateCheckPolicy.TryFindNewerVersion(startup: true, out var version) && version != null)
                        {
                            DisplayUpdaterWindow(version.ToString());
                        }
                    }
                    catch
                    {
                        Dispatcher.Invoke(() => MessageBox.Show(Strings.FailedToRetrieveLatestVersion, "DS4Windows Updater"));
                        // bubble the exception up to allow to see what's wrong in the log
                        throw;
                    }

                    updateCheckPolicy.MarkChecked();
                }

                // Check if main window closing was requested from app update.
                // Quit task early
                //if (contextclose)
                //{
                //    return;
                //}
            });
#endif
            Util.LogAssistBackgroundTask(tempTask);
        }

        private void DisplayUpdaterWindow(string version)
        {
            MessageBoxResult result = MessageBoxResult.No;
            Dispatcher.Invoke(() =>
            {
                var updaterWin = new UpdaterWindow(version, mainWinVM.IsAdministrator, _viewModelFactory.CreateUpdaterWindowViewModel(version));
                updaterWin.ShowDialog();
                result = updaterWin.Result;
            });
        }

        private void Check_Version(bool showstatus = false)
        {
            string? version = mainWinVM.ApplicationVersion;
            string newversion = string.Empty;
            string versionFilePath = Path.Combine(mainWinVM.AppDataPath, "version.txt");
            ulong lastVersionNum = mainWinVM.LastVersionCheckedNum;
            //ulong lastVersion = Global.CompileVersionNumberFromString("2.1.1");

            bool versionFileExists = File.Exists(versionFilePath);
            if (versionFileExists)
            {
                newversion = File.ReadAllText(versionFilePath).Trim();
                //newversion = "2.1.3";
            }

            // 判断は UpdateCheckPolicy が行い、ここは結果に応じた画面の表示と、記録ファイルの後始末だけをする
            try
            {
                var outcome = updateCheckPolicy.Evaluate(version ?? string.Empty, newversion, out Version? latest);
                if (outcome == UpdateCheckOutcome.UpdateAvailable)
                {
                    Dispatcher.Invoke(() => DisplayUpdaterWindow(latest!.ToString()));   // 新しい版があるときは、必ず版が返る
                }
                else if (outcome == UpdateCheckOutcome.UpToDate && showstatus)
                {
                    Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
                }
            }
            catch
            {
                // 画面の表示で例外が出たときも、元の処理どおり、確認を求められたときは「最新です」と知らせる
                if (showstatus)
                    Dispatcher.Invoke(() => MessageBox.Show(Properties.Resources.UpToDate, "DS4Windows Updater"));
            }
            finally
            {
                if (versionFileExists)
                    File.Delete(versionFilePath);
            }
        }
    }
}
