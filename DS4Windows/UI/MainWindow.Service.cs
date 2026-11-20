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
    /// <summary>サービスの開始・停止に伴うイベントの結線、ホットキー、スワイプ。</summary>
    public partial class MainWindow
    {
        private void SetupEvents()
        {
            WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.PropertyChanged += OnLocalizationCultureChanged;
            mainWinVM.ServiceStarted += ControlServiceStarted;
            mainWinVM.RunningChanged += ControlServiceChanged;
            mainWinVM.PreServiceStop += PrepareForServiceStop;
            //root.rootHubtest.RunningChanged += ControlServiceChanged;
            conLvViewModel.ControllerCol.CollectionChanged += ControllerCol_CollectionChanged;
            trayAppearance.SubscribeNotifications();

            // 型付きプロファイル変更イベントを購読
            AppLogger.ProfileChanged += OnProfileChanged;

            AppLogger.GuiLog += UpdateLastStatusMessage;
            logvm.LogItems.CollectionChanged += LogItems_CollectionChanged;
            trayIconVM.RequestShutdown += TrayIconVM_RequestShutdown;
            trayIconVM.ProfileSelected += TrayIconVM_ProfileSelected;
            trayIconVM.RequestMinimize += TrayIconVM_RequestMinimize;
            trayIconVM.RequestOpen += TrayIconVM_RequestOpen;
            trayIconVM.RequestServiceChange += TrayIconVM_RequestServiceChange;
            settingsWrapVM.PropertyChanged += SettingsWrapVM_PropertyChanged;
            ApplyProcessPriorityFromSettings();

            autoProfControl.AutoDebugChanged += AutoProfControl_AutoDebugChanged;
            _autoProfileService.RequestServiceChange += AutoprofileChecker_RequestServiceChange;
            // 自動プロファイルは接続中のスロットにだけ適用する。接続した直後に、最後の前景ウィンドウで照合し直す
            _autoProfileService.AttachHotplugRecheck();
            autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
            //autoProfControl.AutoProfVM.AutoProfileSystemChange += AutoProfVM_AutoProfileSystemChange;
            mainWinVM.FullTabsEnabledChanged += MainWinVM_FullTabsEnabledChanged;

            bool wmiConnected = false;
            WqlEventQuery q = new WqlEventQuery();
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            q.EventClassName = "Win32_PowerManagementEvent";

            try
            {
                scope.Connect();
            }
            catch (COMException) { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            catch (ManagementException) { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }

            if (scope.IsConnected)
            {
                wmiConnected = true;
                managementEvWatcher = new ManagementEventWatcher(scope, q);
                managementEvWatcher.EventArrived += PowerEventArrive;
                try
                {
                    managementEvWatcher.Start();
                }
                catch (ManagementException) { wmiConnected = false; }
                catch (COMException) { wmiConnected = false; }
            }

            if (!wmiConnected)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Ui_WmiFailed");
            }
        }

        /// <summary>設定の優先度を、プロセスへ反映する。管理者でなく RealTime を選んだときは、High へ戻した番号を設定へ書き戻す。</summary>
        private void ApplyProcessPriorityFromSettings()
        {
            int effective = mainWinVM.ApplyProcessPriority(settingsWrapVM.ProcessPriorityIndex);
            if (effective != settingsWrapVM.ProcessPriorityIndex)
            {
                settingsWrapVM.ProcessPriorityIndex = effective;
            }
        }

        private void ControlServiceStarted(object? sender, EventArgs e)
        {
            if (mainWinVM.SwipeProfiles)
            {
                ChangeHotkeysStatus(true);
            }

            CheckAutoProfileStatus();
        }

        private void AutoProfVM_AutoProfileSystemChange(AutoProfilesViewModel sender, bool state)
        {
            if (state)
            {
                _autoProfileService.SetEnabled(true);
                autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
            }
            else
            {
                _autoProfileService.SetEnabled(false);
                autoProfileHolder.AutoProfileColl.CollectionChanged -= AutoProfileColl_CollectionChanged;
            }
        }

        private void ChangeHotkeysStatus(bool state)
        {
            if (state)
            {
                hotkeysTimerActive = true;
                if (hotkeysTimer == null) return;
                hotkeysTimer.Elapsed += HotkeysTimer_Elapsed;
                hotkeysTimer.Start();
            }
            else
            {
                hotkeysTimerActive = false;
                if (hotkeysTimer == null) return;
                hotkeysTimer.Stop();
                hotkeysTimer.Elapsed -= HotkeysTimer_Elapsed;
            }
        }

        private void HotkeysTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            hotkeysTimer?.Stop();

            // 例外が出ても始め直す（止まったままになると、スワイプでの切替が以後動かなくなる）。
            // 終了のとき（hotkeysTimerActive が偽）は、始め直さない。
            GuardedTimerTick.Run(
                SwipeProfileTick,
                () => hotkeysTimerActive,
                () => hotkeysTimer?.Start(),
                ex => DS4Windows.Core.Logging.AppLogger.LogError($"HotkeysTimer tick failed: {ex}"));
        }

        private void SwipeProfileTick()
        {
            if (!mainWinVM.SwipeProfiles) return;

            // 一覧はホットプラグで別のスレッドから変わるため、読み取りロックの下で複製した行を列挙する
            foreach (CompositeDeviceModel item in conLvViewModel.SnapshotRows())
            {
                string slide = mainWinVM.TouchpadSlide(item.DevIndex);
                // 切替の通知は、プロファイルの適用処理（OnProfileChanged）が 1 回だけ出す
                if (slide == "left" || slide == "right")
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        item.SelectedIndex = swipeRule.NextIndex(item.SelectedIndex, item.ProfileListCol.Count, slide);
                    }));
                }
            }
        }

        private void ControlServiceChanged(object? sender, EventArgs e)
        {
            //Tester service = sender as Tester;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                UpdateStartStopText();
                mainWinVM.NotifyServiceStateChanged();
                slotManControl.IsEnabled = mainWinVM.ServiceRunning;
            }));
        }
    }
}
