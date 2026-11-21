using System;
using System.Windows;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Utilities;

namespace DS4Windows.UI
{
    /// <summary>ウィンドウの終了・大きさ・位置・トレイ。</summary>
    public partial class MainWindow
    {
        private void PrepareForServiceStop(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                trayIconVM.ClearContextMenu();
            }));

            ChangeHotkeysStatus(false);
        }

        private void TrayIconVM_RequestOpen(object? sender, EventArgs e)
        {
            if (!showAppInTaskbar)
            {
                Show();
            }

            WindowState = WindowState.Normal;
        }

        private void MainDS4Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (editor != null)
            {
                editor.Close();
                e.Cancel = true;
                return;
            }
            else if (contextclose)
            {
                return;
            }
            else if (mainWinVM.CloseMinimizes)
            {
                WindowState = WindowState.Minimized;
                e.Cancel = true;
                return;
            }

            // If this method was called directly without sender object then skip the confirmation dialogbox
            if (sender != null && conLvViewModel.ControllerCol.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(Properties.Resources.CloseConfirm, Properties.Resources.Confirm,
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void MainDS4Window_Closed(object sender, EventArgs e)
        {
            hotkeysTimerActive = false;
            hotkeysTimer?.Stop();
            _autoProfileService.Stop();
            //autoProfileHolder.Save();
            Util.UnregisterNotify(regHandle);
            if (regHandleXusb != IntPtr.Zero)
            {
                Util.UnregisterNotify(regHandleXusb);
            }

            // Phase5-Watchpoints-Investigation-Report Watchpoint 2対応:
            // Singletonサービス(controlService/profileRepo, SystemEvents)へのイベント購読を
            // 確実に解除する。二重呼び出しされる経路(1251行目)があるため冪等性を確保している。
            conLvViewModel?.Dispose();
            settingsWrapVM?.Dispose();

            // Attempt to dispose of notify icon early
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            Application.Current.Shutdown();
        }

        public void CheckMinStatus()
        {
            bool minToTask = mainWinVM.MinimizeToTaskbar;
            if (WindowState == WindowState.Minimized && !minToTask)
            {
                Hide();
                showAppInTaskbar = false;
            }
            else if (WindowState == WindowState.Normal && !minToTask)
            {
                Show();
                showAppInTaskbar = true;
            }
        }

        private void MainDS4Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // プロフィール編集画面が開いている場合はKeepsizeで制御
            if (WindowState != WindowState.Minimized && editor != null && editor.Keepsize && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                layout.FormWidth = logical.LogicalWidth;
                layout.FormHeight = logical.LogicalHeight;
                AppLogger.LogTrace($"MainWindow.SizeChanged: Saved Logical Width={layout.FormWidth} Height={layout.FormHeight}");
            }
            // 編集画面が開いていない場合は常に保存
            else if (WindowState != WindowState.Minimized && editor == null && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                layout.FormWidth = logical.LogicalWidth;
                layout.FormHeight = logical.LogicalHeight;
                AppLogger.LogTrace($"MainWindow.SizeChanged: Saved Logical Width={layout.FormWidth} Height={layout.FormHeight}");
            }
        }

        private void MainDS4Window_LocationChanged(object sender, EventArgs e)
        {
            // プロフィール編集画面が開いている場合はKeepsizeで制御
            if (WindowState != WindowState.Minimized && editor != null && editor.Keepsize && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                layout.FormLocationX = logical.LogicalX;
                layout.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.LocationChanged: Saved Logical X={layout.FormLocationX} Y={layout.FormLocationY}");
            }
            // 編集画面が開いていない場合は常に保存
            else if (WindowState != WindowState.Minimized && editor == null && !IsInitialShow)
            {
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                layout.FormLocationX = logical.LogicalX;
                layout.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.LocationChanged: Saved Logical X={layout.FormLocationX} Y={layout.FormLocationY}");
            }
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!showAppInTaskbar)
            {
                Show();
            }

            WindowState = WindowState.Normal;
        }
    }
}
