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
    /// <summary>Windows のメッセージ（デバイスの変化・電源・ホットプラグ）の処理。</summary>
    public partial class MainWindow
    {
        private void PowerEventArrive(object sender, EventArrivedEventArgs e)
        {
            short evType = Convert.ToInt16(e.NewEvent.GetPropertyValue("EventType"));
            switch (evType)
            {
                // Wakeup from Suspend
                case POWER_RESUME:
                    {
                        mainWinVM.Suspending = false;

                        if (wasrunning)
                        {
                            wasrunning = false;
                            Dispatcher.Invoke(() =>
                            {
                                mainWinVM.IsServiceBusy = true;
                            });

                            AppLogger.Log(AppLogLevel.Info, DS4Windows.Translations.Strings.WakeupFromSuspend, LogOptions.UserFacing);
                            //controlService.LogDebug($"{Thread.CurrentThread.ManagedThreadId}");

                            //Thread.Sleep(60000);
                            //controlService.Start();

                            //Task startupTask = Task.Run(() =>
                            Task startupTask = Task.Delay(5000).ContinueWith(t =>
                            {
                                mainWinVM.StartService();
                            });

                            // Log exceptions that might occur
                            Util.LogAssistBackgroundTask(startupTask);
                        }
                    }

                    break;
                // Entering Suspend
                case POWER_SUSPEND:
                    {
                        mainWinVM.Suspending = true;

                        if (mainWinVM.ServiceRunning)
                        {
                            //Dispatcher.Invoke(() =>
                            //{
                            //    mainWinVM.IsServiceBusy = true;
                            //});

                            mainWinVM.StopService(immediateUnplug: true);
                            wasrunning = true;

                            Thread.Sleep(1000); // 待機の理由: サービスの再起動の間隔（バックグラウンドのスレッドの中）
                        }
                    }

                    break;

                default: break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (!mainWinVM.FirstRun)
            {
                WindowPlacementHelper.ApplyPlacement(this, layout.FormLocationX, layout.FormLocationY, layout.FormWidth, layout.FormHeight, startMinimized);

                // ApplyPlacement 後に WPF 側でサイズや位置が変更される可能性があるため、
                // レイアウト完了後に実際のウィンドウ矩形と WPF プロパティをログに残す。
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // WPF レベルのプロパティ
                        AppLogger.LogTrace($"PostApply: WPF Left={this.Left} Top={this.Top} Width={this.Width} Height={this.Height} ActualWidth={this.ActualWidth} ActualHeight={this.ActualHeight} WindowState={this.WindowState}");

                        // Win32 レベルの取得（物理ピクセル）
                        var phys = WindowPlacementHelper.GetPlacement(this);
                        AppLogger.LogTrace($"PostApply: GetPlacement physicalRect={phys}");

                        // 論理座標としての取得
                        var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                        AppLogger.LogTrace($"PostApply: GetLogicalPlacement => LogicalX={logical.LogicalX} LogicalY={logical.LogicalY} LogicalWidth={logical.LogicalWidth} LogicalHeight={logical.LogicalHeight}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogDebug($"PostApply: Failed to log post-apply window metrics: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }

            // Restore controller tab column widths
            RestoreControllerColumnWidths();
            RestoreLogColumnWidths();

            if (PresentationSource.FromVisual(this) is not HwndSource source) return;
            HookWindowMessages(source);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
            IntPtr lParam, ref bool handled)
        {
            // メッセージの種類ごとの処理は、メソッドへ分けた（順序・条件は従来のまま）
            switch (msg)
            {
                case Util.WM_DEVICECHANGE:
                    HandleDeviceChange(wParam);
                    break;
                case WM_COPYDATA:
                    HandleCopyData(lParam);   // MainWindow.Ipc.cs
                    break;
                default: break;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// デバイスの着脱（ホットプラグ）。カウンターのロックと、実行中のフラグ（inHotPlug）は、従来のまま。
        /// </summary>
        private void HandleDeviceChange(IntPtr wParam)
        {
            if (mainWinVM.RunHotPlug)
            {
                Int32 Type = wParam.ToInt32();
                if (Type == DBT_DEVICEARRIVAL ||
                    Type == DBT_DEVICEREMOVECOMPLETE)
                {
                    lock (hotplugCounterLock)
                    {
                        hotplugCounter++;
                    }

                    if (!inHotPlug)
                    {
                        inHotPlug = true;
                        Task hotplugTask = Task.Run(() => { InnerHotplug2(); });
                        // Log exceptions that might occur
                        Util.LogAssistBackgroundTask(hotplugTask);
                    }
                }
            }
        }

        private void InnerHotplug2()
        {
            inHotPlug = true;

            bool loopHotplug = false;

            lock (hotplugCounterLock)
            {
                loopHotplug = hotplugCounter > 0;
                hotplugCounter = 0;
            }

            mainWinVM.UpdateHidHiddenAttributes();
            while (loopHotplug == true)
            {
                Thread.Sleep(HOTPLUG_CHECK_DELAY); // 待機の理由: 機器の抜き差しの検出をまとめるための待ち（バックグラウンドのスレッドの中）
                mainWinVM.HotPlug();

                lock (hotplugCounterLock)
                {
                    loopHotplug = hotplugCounter > 0;
                    hotplugCounter = 0;
                }
            }

            inHotPlug = false;
        }

        private void HookWindowMessages(HwndSource source)
        {
            bool result = Util.RegisterNotify(source.Handle, mainWinVM.HidClassGuid, ref regHandle);
            if (!result)
            {
                Application.Current.Shutdown();   // 旧 App.Current（同じもの）
            }

            // HID を持たない Xbox 系コントローラーは、XUSB のインターフェースの着脱でしか分からない（登録に失敗しても、HID の機種は動くので続ける）
            try
            {
                Util.RegisterNotify(source.Handle, mainWinVM.XusbClassGuid, ref regHandleXusb);
            }
            catch (Exception)
            {
                regHandleXusb = IntPtr.Zero;
            }
        }
    }
}
