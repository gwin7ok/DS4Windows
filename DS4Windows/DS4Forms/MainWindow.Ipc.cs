using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase9 9-F F-2b（Phase9-F-Plan.md §6）: MainWindow の IPC コマンド（WM_COPYDATA）の処理。旧 <c>WndProc</c> の巨大な if の連なりを、コマンドごとのメソッドへ分けた。
    /// コマンドの受け付け順（if／else if の順序）、条件、例外の握りつぶし、query の結果の書き込みは、従来のまま。UI の部品に触れるため、MainWindow の partial に置く。
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// WM_COPYDATA: DS4Win のコマンドが、文字列として lpData のバッファに入っている。例外は、すべて握りつぶす（致命的ではないため）。
        /// </summary>
        private void HandleCopyData(IntPtr lParam)
        {
            try
            {
                App.COPYDATASTRUCT cds = (App.COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(App.COPYDATASTRUCT));
                if (IpcCommandParser.IsValidSize(cds.cbData))
                {
                    byte[] buffer = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, buffer, 0, cds.cbData);
                    string[] strData = IpcCommandParser.Parse(buffer);

                    if (strData.Length >= 1)
                    {
                        if (strData[0] == "start")
                        {
                            IpcStart();
                        }
                        else if (strData[0] == "stop")
                        {
                            IpcStop();
                        }
                        else if (strData[0] == "cycle")
                        {
                            IpcCycle();
                        }
                        else if (strData[0] == "shutdown")
                        {
                            IpcShutdown();
                        }
                        else if (strData[0] == "disconnect")
                        {
                            IpcDisconnect(strData);
                        }
                        else if ((strData[0] == "changeledcolor") && strData.Length >= 5)
                        {
                            IpcChangeLedColor(strData);
                        }
                        else if ((strData[0] == "loadprofile" || strData[0] == "loadtempprofile") && strData.Length >= 3)
                        {
                            IpcLoadProfile(strData);
                        }
                        else if (strData[0] == "outputslot" && strData.Length >= 3)
                        {
                            IpcOutputSlot(strData);
                        }
                        else if (strData[0] == "query" && strData.Length >= 3)
                        {
                            IpcQuery(strData);
                        }
                    }
                }
            }
            catch
            {
                // Eat all exceptions in WM_COPYDATA because exceptions here are not fatal for DS4Windows background app
            }
        }


        /// <summary>
        /// IPC: start（動いていなければ、サービスを開始する）。
        /// </summary>
        private void IpcStart()
        {
            if (!controlService.running)
                ChangeService();
        }

        /// <summary>
        /// IPC: stop（動いていれば、サービスを停止する）。
        /// </summary>
        private void IpcStop()
        {
            if (controlService.running)
                ChangeService();
        }

        /// <summary>
        /// IPC: cycle（サービスを切り替える）。
        /// </summary>
        private void IpcCycle()
        {
            ChangeService();
        }

        /// <summary>
        /// IPC: shutdown（コントローラーを切断してから、アプリを終了する）。
        /// </summary>
        private void IpcShutdown()
        {
            // Force disconnect all gamepads before closing the app to avoid "Are you sure you want to close the app" messagebox
            if (controlService.running)
                ChangeService();

            // Call closing method and let it to close editor wnd (if it is open) before proceeding to the actual "app closed" handler
            MainDS4Window_Closing(null, new System.ComponentModel.CancelEventArgs());
            MainDS4Window_Closed(this, new System.EventArgs());
        }

        /// <summary>
        /// IPC: disconnect[.device#]（ワイヤレスのコントローラーを切断する）。
        /// </summary>
        private void IpcDisconnect(string[] strData)
        {
            int tdevice = -1;
            // Command syntax: Disconnect[.device#] (fex Disconnect.1)
            // Disconnect all wireless controllers. ex. (Disconnect)
            if (strData.Length == 1)
            {
                // Attempt to disconnect all wireless controllers
                // Opt to make copy of Dictionary before iterating over contents
                var dictCopy = new Dictionary<int, CompositeDeviceModel>(conLvViewModel.ControllerDict);
                foreach (KeyValuePair<int, CompositeDeviceModel> pair in dictCopy)
                {
                    pair.Value.RequestDisconnect();
                }
            }
            else
            {
                // Attempt to disconnect one wireless controller
                if (int.TryParse(strData[1], out tdevice)) tdevice--;

                if (conLvViewModel.ControllerDict.TryGetValue(tdevice, out CompositeDeviceModel model))
                {
                    model.RequestDisconnect();
                }
            }
        }

        /// <summary>
        /// IPC: changeledcolor.device#.red.green.blue（ライトバーの色を変える）。
        /// </summary>
        private void IpcChangeLedColor(string[] strData)
        {
            int tdevice = -1;
            // Command syntax: changeledcolor.device#.red.gree.blue (ex changeledcolor.1.255.0.0)
            if (int.TryParse(strData[1], out tdevice))
                tdevice--;
            if (tdevice >= 0 && tdevice < ControlService.MAX_DS4_CONTROLLER_COUNT)
            {
                byte.TryParse(strData[2], out byte red);
                byte.TryParse(strData[3], out byte green);
                byte.TryParse(strData[4], out byte blue);

                conLvViewModel.ControllerCol[tdevice].UpdateCustomLightColor(Color.FromRgb(red, green, blue));
            }
        }

        /// <summary>
        /// IPC: loadprofile／loadtempprofile.device#.profileName（プロファイルを読み込む）。
        /// </summary>
        private void IpcLoadProfile(string[] strData)
        {
            int tdevice = -1;
            // Command syntax: LoadProfile.device#.profileName (fex LoadProfile.1.GameSnake or LoadTempProfile.1.WebBrowserSet)
            if (int.TryParse(strData[1], out tdevice)) tdevice--;

            if (tdevice >= 0 && tdevice < ControlService.MAX_DS4_CONTROLLER_COUNT &&
                    File.Exists(pathService.AppDataPath + "\\Profiles\\" + strData[2] + ".xml"))
            {
                if (strData[0] == "loadprofile")
                {
                    int idx = profileListHolder.ProfileListCol.Select((item, index) => new { item, index }).
                            Where(x => x.item.Name == strData[2]).Select(x => x.index).DefaultIfEmpty(-1).First();

                    if (idx >= 0 && tdevice < conLvViewModel.ControllerCol.Count)
                    {
                        CompositeDeviceModel devitem = conLvViewModel.ControllerCol[tdevice];
                        if (devitem?.Device != null)
                        {
                            string prolog = string.Format(Properties.Resources.UsingProfile,
                                (tdevice + 1).ToString(), strData[2], $"{devitem.Device.Battery}");
                            profileAppService.ApplyProfile(tdevice, strData[2], false, true,
                                DS4Windows.ProfileChangeSource.Manual, prolog);
                        }
                    }
                    else
                    {
                        // Preset profile name for later loading
                        profileRepo.ProfilePath[tdevice] = strData[2];
                        //Global.LoadProfile(tdevice, true, controlService);
                    }
                }
                else
                {
                    Task.Run(() =>
                    {
                        DS4Device device = conLvViewModel.ControllerCol[tdevice].Device;
                        if (device != null)
                        {
                            device.HaltReportingRunAction(() =>
                            {
                                // Phase8-Step5 W2（Q6＝A）: この経路は適用の後始末を通らないため、読込の成功後に束縛を差し替える
                                if (profileRepo.LoadTempProfile(tdevice, strData[2], true, controlService))
                                    profileAppService.PublishBindings(tdevice);
                            });
                        }
                    }).Wait();
                }

                DS4Device device = conLvViewModel.ControllerCol[tdevice].Device;
                if (device != null)
                {
                    string prolog = string.Format(Properties.Resources.UsingProfile, (tdevice + 1).ToString(), strData[2], $"{device.Battery}");
                    controlService.LogDebug(prolog);
                }
            }
        }

        /// <summary>
        /// IPC: outputslot.slot#.Unplug／PlugDS4／PlugX360（出力スロットの抜き差し）。
        /// </summary>
        private void IpcOutputSlot(string[] strData)
        {
            int tdevice = -1;
            // Command syntax:
            //    OutputSlot.slot#.Unplug
            //    OutputSlot.slot#.PlugDS4
            //    OutputSlot.slot#.PlugX360
            if (int.TryParse(strData[1], out tdevice))
                tdevice--;

            if (tdevice >= 0 && tdevice < ControlService.MAX_DS4_CONTROLLER_COUNT)
            {
                strData[2] = strData[2].ToLower();
                // Phase6-Step10-5: 抜き差しの正式な入口（IOutputSlotService）経由。
                // 事前条件の確認と UI スレッドへの切り替えは、サービスが行う
                if (strData[2] == "unplug")
                    outputSlotService.UnplugSlot(tdevice);
                else if (strData[2] == "plugds4")
                    outputSlotService.PluginSlot(tdevice, OutContType.DS4);
                else if (strData[2] == "plugx360")
                    outputSlotService.PluginSlot(tdevice, OutContType.X360);
            }
        }

        /// <summary>
        /// IPC: query.device#.name（値を問い合わせ、結果を MMF へ書き込む）。
        /// </summary>
        private void IpcQuery(string[] strData)
        {
            int tdevice = -1;
            string propName;
            string propValue = String.Empty;

            // Command syntax: QueryProfile.device#.Name (fex "Query.1.ProfileName" would print out the name of the active profile in controller 1)
            if (int.TryParse(strData[1], out tdevice))
                tdevice--;

            if (tdevice >= 0 && tdevice < ControlService.MAX_DS4_CONTROLLER_COUNT)
            {
                // Name of the property to query from a profile or DS4Windows app engine
                propName = strData[2].ToLower();

                if (propName == "profilename")
                {
                    if (profileSettingsService.GetUseTempProfile(tdevice))
                        propValue = profileSettingsService.GetTempProfileName(tdevice);
                    else
                        propValue = profileRepo.ProfilePath[tdevice];
                }
                else if (propName == "outconttype")
                    // Phase6-Step5-1: 常に None を返していた孤立配列（OutputSlotService.GetOutputDeviceType）ではなく、
                    // 正本であるプロファイル設定（永続設定の OutContType）を返す
                    propValue = profileSettingsService.OutContType[tdevice].ToString();
                else if (propName == "activeoutdevtype")
                    propValue = outputSlotService.ActiveOutDevType[tdevice].ToString();
                else if (propName == "usedinputonly")
                    propValue = profileSettingsService.UseDInputOnlyArray[tdevice].ToString();

                else if (propName == "devicevidpid" && controlService.DS4Controllers[tdevice] != null)
                    propValue = $"VID={controlService.DS4Controllers[tdevice].HidDevice.Attributes.VendorHexId}, PID={controlService.DS4Controllers[tdevice].HidDevice.Attributes.ProductHexId}";
                else if (propName == "devicepath" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].HidDevice.DevicePath;
                else if (propName == "macaddress" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].MacAddress;
                else if (propName == "displayname" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].DisplayName;
                else if (propName == "conntype" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].ConnectionType.ToString();
                else if (propName == "exclusivestatus" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].CurrentExclusiveStatus.ToString();
                else if (propName == "battery" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].Battery.ToString();
                else if (propName == "charging" && controlService.DS4Controllers[tdevice] != null)
                    propValue = controlService.DS4Controllers[tdevice].Charging.ToString();
                else if (propName == "outputslottype")
                    propValue = controlService.OutputslotMan.OutputSlots[tdevice].CurrentType.ToString();
                else if (propName == "outputslotpermanenttype")
                    propValue = controlService.OutputslotMan.OutputSlots[tdevice].PermanentType.ToString();
                else if (propName == "outputslotattachedstatus")
                    propValue = controlService.OutputslotMan.OutputSlots[tdevice].CurrentAttachedStatus.ToString();
                else if (propName == "outputslotinputbound")
                    propValue = controlService.OutputslotMan.OutputSlots[tdevice].CurrentInputBound.ToString();

                else if (propName == "apprunning")
                    propValue = controlService.running.ToString(); // Controller idx value is ignored, but it still needs to be in 1..4 range in a cmdline call
            }

            // Write out the property value to MMF result data file and notify a client process that the data is available
            ((Application.Current) as App).WriteIPCResultDataMMF(propValue);
        }
    }
}
