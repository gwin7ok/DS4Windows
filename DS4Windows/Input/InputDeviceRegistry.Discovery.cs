using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Core.Platform;

namespace DS4Windows.Input
{
    /// <summary>接続された機器の探索と、非表示の排他機器の整理。</summary>
    public partial class InputDeviceRegistry
    {
        private bool detectNewControllers = true;
        private long timestamp;
        private bool DetectNewControllers
        {
            get => detectNewControllers;
            set
            {
                if (detectNewControllers) timestamp = Stopwatch.GetTimestamp();

                detectNewControllers = value;
            }
        }
        private bool IsRealDS4(IHidTransport hDevice)
        {
            var layerSettings = LayerSettings;
            if (layerSettings == null || !layerSettings.UseMoonlight) return !DevicePathProbe.CheckIfVirtualDevice(hDevice.DevicePath);
            if (!layerSettings.UseAdvancedMoonlight)
            {
                // this approach should work on most devices, but not on my pc for some reason
                if (hDevice.Attributes.VendorId == 1356 && hDevice.Attributes.ProductId == 1476) return true;
            }
            else
            {
                // all of this basically tries to add a 5 seconds cooldown when a new device is connected and moonlight
                // support is on to avoid getting 8 virtual connected devices from one physical one
                if (!DetectNewControllers)
                {
                    var curTimestamp = Stopwatch.GetTimestamp();
                    if (curTimestamp - timestamp > 5 * TimeSpan.TicksPerSecond) DetectNewControllers = true;
                }

                // Moonlight detection
                if (hDevice.Attributes.VendorId == 1356 && hDevice.Attributes.ProductId == 1476)
                {
                    if (DetectNewControllers)
                    {
                        DetectNewControllers = false;
                        return true;
                    }
                    return DetectNewControllers;
                }
            }
            return !DevicePathProbe.CheckIfVirtualDevice(hDevice.DevicePath);
        }

        // Enumerates ds4 controllers in the system
        public void FindControllers()
        {
            lock (Devices)
            {
                IEnumerable<IHidTransport> hDevices = HidDevices.EnumerateDS4(knownDevices, LayerSettings);

                // HID 以外の通信路の機器は、機器ごとの情報を供給元が持つ（パスで引く）。以降は HID の機器と同じ流れ
                Dictionary<string, VidPidInfo>? sourceInfos = null;
                if (transportSources.Length > 0)
                {
                    sourceInfos = new Dictionary<string, VidPidInfo>();
                    var sourceDevices = new List<IHidTransport>();
                    foreach (IInputTransportSource source in transportSources)
                    {
                        foreach (InputTransportCandidate candidate in source.Enumerate())
                        {
                            sourceInfos[candidate.Transport.DevicePath] = candidate.Info;
                            sourceDevices.Add(candidate.Transport);
                        }
                    }

                    hDevices = hDevices.Concat(sourceDevices);
                }

                VidPidInfo InfoOf(IHidTransport d) =>
                    sourceInfos != null && sourceInfos.TryGetValue(d.DevicePath, out VidPidInfo? found)
                        ? found
                        : knownDevices.Single(x => x.vid == d.Attributes.VendorId && x.pid == d.Attributes.ProductId);

                hDevices = hDevices.Where(d => PreparePendingDevice!(d, InfoOf(d))); // 列挙の前に、呼び出し側が設定する

                hDevices = hDevices.Where(IsRealDS4).Select(dev => dev);
                //hDevices = from dev in hDevices where IsRealDS4(dev) select dev;
                // Sort Bluetooth first in case USB is also connected on the same controller.
                hDevices = hDevices.OrderBy<IHidTransport, ConnectionType>((IHidTransport d) =>
                {
                    // Need VidPidInfo instance to get CheckConnectionDelegate and
                    // check the connection type
                    VidPidInfo metainfo = InfoOf(d);

                    //return InputDeviceCore.HidConnectionType(d);
                    return metainfo.checkConnection(d);
                });

                List<IHidTransport> tempList = hDevices.ToList();
                purgeHiddenExclusiveDevices();
                tempList.AddRange(DisabledDevices);
                int devCount = tempList.Count();
                string devicePlural = "device" + (devCount == 0 || devCount > 1 ? "s" : "");
                //Log.LogToGui("Found " + devCount + " possible " + devicePlural + ". Examining " + devicePlural + ".", false);

                for (int i = 0; i < devCount; i++)
                //foreach (HidDevice hDevice in hDevices)
                {
                    IHidTransport hDevice = tempList[i];
                    VidPidInfo metainfo = InfoOf(hDevice);

                    if (!metainfo.featureSet.HasFlag(VidPidFeatureSet.VendorDefinedDevice) &&
                        hDevice.Capabilities.UsagePage >= 0xFF00)
                        continue; // Ignore devices using Vendor-Defined HID Usage Pages (expected to ignore the Nacon Revolution Pro programming interface)
                    else if (DevicePaths.Contains(hDevice.DevicePath))
                        continue; // BT/USB endpoint already open once

                    if (!hDevice.IsOpen)
                    {
                        hDevice.OpenDevice(isExclusiveMode);
                        if (!hDevice.IsOpen && isExclusiveMode)
                        {
                            try
                            {
                                // Check if running with elevated permissions
                                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                                WindowsPrincipal principal = new WindowsPrincipal(identity);
                                bool elevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                                if (!elevated)
                                {
                                    // Tell the client to launch routine to re-enable a device
                                    RequestElevationArgs eleArgs =
                                        new RequestElevationArgs(DevicePathProbe.GetInstanceIdFromDevicePath(hDevice.DevicePath));
                                    RequestElevation?.Invoke(eleArgs);
                                    if (eleArgs.StatusCode == RequestElevationArgs.STATUS_SUCCESS)
                                    {
                                        hDevice.OpenDevice(isExclusiveMode);
                                    }
                                }
                                else
                                {
                                    ReEnableDevice(DevicePathProbe.GetInstanceIdFromDevicePath(hDevice.DevicePath));
                                    hDevice.OpenDevice(isExclusiveMode);
                                }
                            }
                            catch (Exception) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
                        }

                        // TODO in exclusive mode, try to hold both open when both are connected
                        if (isExclusiveMode && !hDevice.IsOpen)
                            hDevice.OpenDevice(false);
                    }

                    if (hDevice.IsOpen)
                    {
                        string? serial = ReadSerial(metainfo, hDevice);

                        bool validSerial = !serial!.Equals(InputDeviceCore.BLANK_SERIAL); // 読めた機器だけが来る（null なら従来どおり NullReferenceException）
                        bool newdev = true;
                        if (validSerial && deviceSerials.Contains(serial))
                        {
                            // Check if Quick Charge flag is engaged
                            if (serialDevices.TryGetValue(serial, out InputDeviceCore? tempDev) && tempDev.ReadyQuickChargeDisconnect)
                            {
                                // Need to disconnect callback here to avoid deadlock
                                tempDev.Removal -= On_Removal;
                                // Call inner removal process here instead
                                InnerRemoveDevice(tempDev);
                                // Disconnect wireless device
                                tempDev.DisconnectWireless();
                            }
                            // happens when the BT endpoint already is open and the USB is plugged into the same host
                            else if (isExclusiveMode && hDevice.IsExclusive &&
                                !DisabledDevices.Contains(hDevice))
                            {
                                // Grab reference to exclusively opened HidDevice so device
                                // stays hidden to other processes
                                DisabledDevices.Add(hDevice);
                                //DevicePaths.Add(hDevice.DevicePath);
                                newdev = false;
                            }
                            else
                            {
                                // Using shared mode. Serial already exists. Ignore device
                                newdev = false;
                            }
                        }

                        if (newdev && validSerial)
                        {
                            InputDeviceCore? ds4Device = CreateDevice(metainfo.inputDevType, hDevice, metainfo.name, metainfo.featureSet);
                            //InputDeviceCore ds4Device = new InputDeviceCore(hDevice, metainfo.name, metainfo.featureSet);
                            if (ds4Device == null)
                            {
                                // No compatible device type was found. Skip
                                continue;
                            }

                            ds4Device.LayerSettings = LayerSettings;
                            PrepareDS4Init?.Invoke(ds4Device);
                            ds4Device.PostInit();
                            //Thread.Sleep(100);
                            PostDS4Init?.Invoke(ds4Device);
                            //ds4Device.Removal += On_Removal;
                            if (!ds4Device.ExitOutputThread)
                            {
                                Devices.Add(hDevice.DevicePath, ds4Device);
                                DevicePaths.Add(hDevice.DevicePath);
                                deviceSerials.Add(serial);
                                serialDevices.Add(serial, ds4Device);
                                string foundMsg = $"{DS4Windows.Properties.Resources.FoundController} {ds4Device.GetMacAddress()} ({ds4Device.GetConnectionType()}) ({ds4Device.DisplayName}).";
                                AppLogger.Log(AppLogLevel.Info, foundMsg, LogOptions.UserFacing);
                                // 旧 UI.Services.NotificationService.ShowToast（中で AppLogger.LogToTray を呼ぶだけの静的な窓口）。
                                // 第1層から UI への参照をなくすため、直接呼ぶ。LogToTray は通知の例外を自分で捕まえるので、挙動は同じ
                                AppLogger.LogToTray(foundMsg);
                            }
                        }
                    }
                }
            }
        }

        private void purgeHiddenExclusiveDevices()
        {
            int disabledDevCount = DisabledDevices.Count;
            if (disabledDevCount > 0)
            {
                List<IHidTransport> disabledDevList = new List<IHidTransport>();
                for (var devEnum = DisabledDevices.GetEnumerator(); devEnum.MoveNext();)
                //for (int i = 0, arlen = disabledDevCount; i < arlen; i++)
                {
                    //HidDevice tempDev = DisabledDevices.ElementAt(i);
                    IHidTransport tempDev = devEnum.Current;
                    if (tempDev != null)
                    {
                        if (tempDev.IsOpen && tempDev.IsConnected)
                        {
                            disabledDevList.Add(tempDev);
                        }
                        else if (tempDev.IsOpen)
                        {
                            if (!tempDev.IsConnected)
                            {
                                try
                                {
                                    tempDev.CloseDevice();
                                }
                                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                            }

                            if (DevicePaths.Contains(tempDev.DevicePath))
                            {
                                DevicePaths.Remove(tempDev.DevicePath);
                            }
                        }
                    }
                }

                DisabledDevices.Clear();
                DisabledDevices.AddRange(disabledDevList);
            }
        }
    }
}
