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
    /// <summary>ログの表示・書き出し・状態メッセージ。</summary>
    public partial class MainWindow
    {
        private void LogItems_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    int count = logListView.Items.Count;
                    if (count > 0)
                    {
                        logListView.ScrollIntoView(logvm.LogItems[count - 1]);
                    }
                }));
            }
        }

        private void UpdateLastStatusMessage(object? sender, DS4Windows.Core.Logging.DebugEventArgs e)
        {
            if (e.Temporary)
                return;

            if (!string.IsNullOrEmpty(e.Data) && (e.Data.StartsWith("[DI]") || e.Data.StartsWith("[Legacy]")))
                return;

            lastLogMsg.Message = e.Data;
            lastLogMsg.Warning = e.Warning;
        }

        private void OnProfileChanged(object? sender, DS4Windows.Core.Logging.ProfileChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    int devIndex = e.DeviceIndex;
                    string prof = e.ProfileName ?? string.Empty;
                    // 残量は一覧のモデルを経由せず、スロットのデバイスから直接読む
                    // （再接続の直後は一覧の行がまだ無く、"0" と表示されていた）
                    string battery = _autoProfileService.BatteryText(devIndex);

                    CompositeDeviceModel? item = null;
                    if (conLvViewModel != null && conLvViewModel.ControllerDict != null && conLvViewModel.ControllerDict.ContainsKey(devIndex))
                        item = conLvViewModel.ControllerDict[devIndex];

                    string prolog;
                    // If the event is from AutoProfile, use the AutoTempProfile resource
                    if (e.Source == DS4Windows.Core.Logging.ProfileChangeSource.AutoProfile)
                        prolog = string.Format(DS4Windows.Properties.Resources.UsingAutoTempProfile, (devIndex + 1).ToString(), prof);
                    else
                        prolog = string.Format(Properties.Resources.UsingProfile, (devIndex + 1).ToString(), prof, battery);

                    // 修正前:
                    // if (mainWinVM.ProfileChangedNotification)
                    // {
                    //     ShowProfileSwitchNotification(message);
                    // }

                    // 修正後（判定をメソッド側に委譲し、シンプルに1行で呼ぶ）:
                    // 2026-10-19: 自動プロファイル由来は AutoProfileSwitchNotifyChoice と接続の有無で判定する（AutoProfileNotificationPolicy）
                    if (_autoProfileService.ShouldNotify(e.Source, item != null))
                        trayAppearance.ShowProfileSwitchNotification(prolog);
                }
                catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[MainWindow:697] ignored {caught.GetType().Name}: {caught.Message}"); }
            }));
        }

        internal void ShowSelectedLogMessage()
        {
            int idx = logListView.SelectedIndex;
            if (idx > -1)
            {
                LogItem temp = logvm.LogItems[idx];
                LogMessageDisplay msgBox = new LogMessageDisplay(temp.Message ?? string.Empty);
                msgBox.Owner = this;
                msgBox.ShowDialog();
                //MessageBox.Show(temp.Message, "Log");
            }
        }

        internal void ExportLog()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text Documents (*.txt)|*.txt";
            dialog.Title = "Select Export File";
            // TODO: Expose config dir
            dialog.InitialDirectory = mainWinVM.AppDataPath;
            if (dialog.ShowDialog() == true)
            {
                LogWriter logWriter = new LogWriter(dialog.FileName, logvm.LogItems.ToList());
                logWriter.Process();
            }
        }

        private void RestoreLogColumnWidths()
        {
            var gridView = logListView.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 2)
            {
                int[] widths = layout.LogColumnWidths;
                gridView.Columns[0].Width = widths[0];
                gridView.Columns[1].Width = widths[1];
            }
        }
    }
}
