/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using DS4Windows.Core.Platform;

namespace DS4Windows.Input
{

    /// <summary>
    /// 接続されたコントローラーの登録簿と、HID 機器の列挙・オープン・取り外しの手順（インスタンス。DI の Singleton）。
    /// 状態（開いているデバイスの表、シリアルの重複の検出、排他モードの記憶）はこのインスタンスが持つ。
    /// 設定値は、コンストラクターで受ける（null のときは、Moonlight 無効・詳細ログ無効として扱う。補助プロセスと一部のテスト）。
    /// </summary>
    public class DS4Devices
    {
        // (HID device path, DS4Device)
        private readonly Dictionary<string, DS4Device> Devices = new Dictionary<string, DS4Device>();
        // (MacAddress, DS4Device)
        private readonly Dictionary<string, DS4Device> serialDevices = new Dictionary<string, DS4Device>();
        private readonly HashSet<string> deviceSerials = new HashSet<string>();
        private readonly HashSet<string> DevicePaths = new HashSet<string>();
        // Keep instance of opened exclusive mode devices not in use (Charging while using BT connection)
        private readonly List<HidDevice> DisabledDevices = new List<HidDevice>();
        private readonly Stopwatch sw = new Stopwatch();
        public event RequestElevationDelegate RequestElevation;
        public PrepareInitDelegate PrepareDS4Init = null;
        public PrepareInitDelegate PostDS4Init = null;
        public CheckPendingDevice PreparePendingDevice = null;
        public bool isExclusiveMode = false;
        internal const int SONY_VID = 0x054C;
        internal const int RAZER_VID = 0x1532;
        internal const int NACON_VID = 0x146B;
        internal const int HORI_VID = 0x0F0D;
        internal const int NINTENDO_VENDOR_ID = 0x57e;
        internal const int SWITCH_PRO_PRODUCT_ID = 0x2009;
        internal const int JOYCON_L_PRODUCT_ID = 0x2006;
        internal const int JOYCON_R_PRODUCT_ID = 0x2007;
        internal const int JOYCON_CHARGING_GRIP_PRODUCT_ID = 0x200E;

        // https://support.steampowered.com/kb_article.php?ref=5199-TOKV-4426&l=english web site has a list of other PS4 compatible device VID/PID values and brand names.
        // However, not all those are guaranteed to work with DS4Windows app so support is added case by case when users of DS4Windows app tests non-official DS4 gamepads.
        // 対応機器の表は、種類ごとのカートリッジ（IInputDeviceCartridge。Input/Devices/*DeviceCartridge.cs）が持つ。機種を足すときは、該当するカートリッジの表へ 1 行足す



        // 第 1 層が読む設定値（Moonlight／詳細ログ）。未設定（null）のときは、Moonlight 無効・詳細ログ無効として扱う。
        public DS4Windows.Input.Abstractions.IDeviceLayerSettings LayerSettings { get; }

        private readonly IInputDeviceCartridge[] cartridges;
        // 全カートリッジの表をつないだもの（VID／PID は表の中で一意。順序は結果に影響しない）
        private readonly VidPidInfo[] knownDevices;

        public DS4Devices(DS4Windows.Input.Abstractions.IDeviceLayerSettings layerSettings, IEnumerable<IInputDeviceCartridge> cartridges)
        {
            LayerSettings = layerSettings;
            this.cartridges = (cartridges ?? throw new ArgumentNullException(nameof(cartridges))).ToArray();
            knownDevices = this.cartridges.SelectMany(c => c.KnownDevices).ToArray();
        }

        // 種類に対応するカートリッジでデバイスを作る（対応するカートリッジがなければ null）
        private DS4Device CreateDevice(InputDeviceType type, HidDevice hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            for (int i = 0; i < cartridges.Length; i++)
            {
                if (cartridges[i].Handles(type))
                    return cartridges[i].Create(type, hidDevice, displayName, featureSet);
            }

            return null;
        }

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
        private bool IsRealDS4(HidDevice hDevice)
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
        public void findControllers()
        {
            lock (Devices)
            {
                IEnumerable<HidDevice> hDevices = HidDevices.EnumerateDS4(knownDevices, LayerSettings);

                hDevices = hDevices.Where(d =>
                {
                    VidPidInfo metainfo = knownDevices.Single(x => x.vid == d.Attributes.VendorId &&
                        x.pid == d.Attributes.ProductId);
                    return PreparePendingDevice(d, metainfo);
                });

                hDevices = hDevices.Where(IsRealDS4).Select(dev => dev);
                //hDevices = from dev in hDevices where IsRealDS4(dev) select dev;
                // Sort Bluetooth first in case USB is also connected on the same controller.
                hDevices = hDevices.OrderBy<HidDevice, ConnectionType>((HidDevice d) =>
                {
                    // Need VidPidInfo instance to get CheckConnectionDelegate and
                    // check the connection type
                    VidPidInfo metainfo = knownDevices.Single(x => x.vid == d.Attributes.VendorId &&
                        x.pid == d.Attributes.ProductId);

                    //return DS4Device.HidConnectionType(d);
                    return metainfo.checkConnection(d);
                });

                List<HidDevice> tempList = hDevices.ToList();
                purgeHiddenExclusiveDevices();
                tempList.AddRange(DisabledDevices);
                int devCount = tempList.Count();
                string devicePlural = "device" + (devCount == 0 || devCount > 1 ? "s" : "");
                //Log.LogToGui("Found " + devCount + " possible " + devicePlural + ". Examining " + devicePlural + ".", false);

                for (int i = 0; i < devCount; i++)
                //foreach (HidDevice hDevice in hDevices)
                {
                    HidDevice hDevice = tempList[i];
                    VidPidInfo metainfo = knownDevices.Single(x => x.vid == hDevice.Attributes.VendorId &&
                        x.pid == hDevice.Attributes.ProductId);

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
                                    reEnableDevice(DevicePathProbe.GetInstanceIdFromDevicePath(hDevice.DevicePath));
                                    hDevice.OpenDevice(isExclusiveMode);
                                }
                            }
                            catch (Exception) { }
                        }

                        // TODO in exclusive mode, try to hold both open when both are connected
                        if (isExclusiveMode && !hDevice.IsOpen)
                            hDevice.OpenDevice(false);
                    }

                    if (hDevice.IsOpen)
                    {
                        //string serial = hDevice.ReadSerial();
                        string serial = DS4Device.BLANK_SERIAL;
                        if (metainfo.inputDevType == InputDeviceType.DualSense)
                        {
                            serial = hDevice.ReadSerial(DualSenseDevice.SERIAL_FEATURE_ID);
                        }
                        else if (metainfo.inputDevType == InputDeviceType.DS4 &&
                            metainfo.checkConnection(hDevice) == ConnectionType.SONYWA ||
                            metainfo.inputDevType == InputDeviceType.DS3)
                        {
                            serial = hDevice.GenerateFakeHwSerial();
                        }
                        else if (metainfo.inputDevType == InputDeviceType.JoyConGrip)
                        {
                            // Blank serial will mean that a JoyCon is not docked to a side
                            serial = JoyConDevice.ReadUSBSerial(hDevice);
                        }
                        else
                        {
                            serial = hDevice.ReadSerial(DS4Device.SERIAL_FEATURE_ID);
                        }

                        bool validSerial = !serial.Equals(DS4Device.BLANK_SERIAL);
                        bool newdev = true;
                        if (validSerial && deviceSerials.Contains(serial))
                        {
                            // Check if Quick Charge flag is engaged
                            if (serialDevices.TryGetValue(serial, out DS4Device tempDev) && tempDev.ReadyQuickChargeDisconnect)
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
                            DS4Device ds4Device = CreateDevice(metainfo.inputDevType, hDevice, metainfo.name, metainfo.featureSet);
                            //DS4Device ds4Device = new DS4Device(hDevice, metainfo.name, metainfo.featureSet);
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
                                string foundMsg = $"{DS4Windows.Properties.Resources.FoundController} {ds4Device.getMacAddress()} ({ds4Device.getConnectionType()}) ({ds4Device.DisplayName}).";
                                AppLogger.Log(AppLogLevel.Info, foundMsg, LogOptions.UserFacing);
                                // Phase14b-Step5（R-02）: 旧 UI.Services.NotificationService.ShowToast（中で AppLogger.LogToTray を呼ぶだけの静的な窓口）。
                                // 第1層から UI への参照をなくすため、直接呼ぶ。LogToTray は通知の例外を自分で捕まえるので、挙動は同じ
                                AppLogger.LogToTray(foundMsg);
                            }
                        }
                    }
                }
            }
        }

        // Returns DS4 controllers that were found and are running
        public IEnumerable<DS4Device> getDS4Controllers()
        {
            lock (Devices)
            {
                DS4Device[] controllers = new DS4Device[Devices.Count];
                Devices.Values.CopyTo(controllers, 0);
                return controllers;
            }
        }

        public void stopControllers()
        {
            lock (Devices)
            {
                IEnumerable<DS4Device> devices = Devices.Values.ToArray();
                //foreach (DS4Device device in devices)
                //for (int i = 0, devCount = devices.Count(); i < devCount; i++)
                for (var devEnum = devices.GetEnumerator(); devEnum.MoveNext();)
                {
                    DS4Device device = devEnum.Current;
                    //DS4Device device = devices.ElementAt(i);
                    device.StopUpdate();
                    //device.runRemoval();
                    device.HidDevice.CloseDevice();
                }

                Devices.Clear();
                DevicePaths.Clear();
                deviceSerials.Clear();
                DisabledDevices.Clear();
                serialDevices.Clear();
            }
        }

        // Called when devices is disconnected, timed out or has input reading failure
        public void On_Removal(object sender, EventArgs e)
        {
            DS4Device device = (DS4Device)sender;
            RemoveDevice(device);
        }

        public void RemoveDevice(DS4Device device)
        {
            lock (Devices)
            {
                InnerRemoveDevice(device);
            }
        }

        private void InnerRemoveDevice(DS4Device device)
        {
            if (device != null)
            {
                device.HidDevice.CloseDevice();
                Devices.Remove(device.HidDevice.DevicePath);
                DevicePaths.Remove(device.HidDevice.DevicePath);
                deviceSerials.Remove(device.MacAddress);
                serialDevices.Remove(device.MacAddress);
                //purgeHiddenExclusiveDevices();
            }
        }

        public void UpdateSerial(object sender, EventArgs e)
        {
            lock (Devices)
            {
                DS4Device device = (DS4Device)sender;
                if (device != null)
                {
                    string devPath = device.HidDevice.DevicePath;
                    string serial = device.getMacAddress();
                    if (Devices.ContainsKey(devPath))
                    {
                        deviceSerials.Remove(serial);
                        serialDevices.Remove(serial);
                        device.updateSerial();
                        serial = device.getMacAddress();
                        if (DS4Device.isValidSerial(serial))
                        {
                            deviceSerials.Add(serial);
                            serialDevices.Add(serial, device);
                        }

                        if (device.ShouldRunCalib())
                            device.RefreshCalibration();
                    }
                }
            }
        }

        private void purgeHiddenExclusiveDevices()
        {
            int disabledDevCount = DisabledDevices.Count;
            if (disabledDevCount > 0)
            {
                List<HidDevice> disabledDevList = new List<HidDevice>();
                for (var devEnum = DisabledDevices.GetEnumerator(); devEnum.MoveNext();)
                //for (int i = 0, arlen = disabledDevCount; i < arlen; i++)
                {
                    //HidDevice tempDev = DisabledDevices.ElementAt(i);
                    HidDevice tempDev = devEnum.Current;
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
                                catch { }
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

        public void reEnableDevice(string deviceInstanceId)
        {
            bool success;
            Guid hidGuid = new Guid();
            NativeMethods.HidD_GetHidGuid(ref hidGuid);
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidGuid, deviceInstanceId, 0, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
            success = NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            if (!success)
            {
                throw new Exception("Error getting device info data, error code = " + Marshal.GetLastWin32Error());
            }
            success = NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 1, ref deviceInfoData); // Checks that we have a unique device
            if (success)
            {
                throw new Exception("Can't find unique device");
            }

            NativeMethods.SP_PROPCHANGE_PARAMS propChangeParams = new NativeMethods.SP_PROPCHANGE_PARAMS();
            propChangeParams.classInstallHeader.cbSize = Marshal.SizeOf(propChangeParams.classInstallHeader);
            propChangeParams.classInstallHeader.installFunction = NativeMethods.DIF_PROPERTYCHANGE;
            propChangeParams.stateChange = NativeMethods.DICS_DISABLE;
            propChangeParams.scope = NativeMethods.DICS_FLAG_GLOBAL;
            propChangeParams.hwProfile = 0;
            success = NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
            if (!success)
            {
                throw new Exception("Error setting class install params, error code = " + Marshal.GetLastWin32Error());
            }
            success = NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
            // TEST: If previous SetupDiCallClassInstaller fails, just continue
            // otherwise device will likely get permanently disabled.
            /*if (!success)
            {
                throw new Exception("Error disabling device, error code = " + Marshal.GetLastWin32Error());
            }
            */

            //System.Threading.Thread.Sleep(50);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 500)
            {
                // Use SpinWait to keep control of current thread. Using Sleep could potentially
                // cause other events to get run out of order
                System.Threading.Thread.SpinWait(250);
            }
            sw.Stop();

            propChangeParams.stateChange = NativeMethods.DICS_ENABLE;
            success = NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
            if (!success)
            {
                throw new Exception("Error setting class install params, error code = " + Marshal.GetLastWin32Error());
            }
            success = NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
            if (!success)
            {
                throw new Exception("Error enabling device, error code = " + Marshal.GetLastWin32Error());
            }

            //System.Threading.Thread.Sleep(50);
            /*sw.Restart();
            while (sw.ElapsedMilliseconds < 50)
            {
                // Use SpinWait to keep control of current thread. Using Sleep could potentially
                // cause other events to get run out of order
                System.Threading.Thread.SpinWait(100);
            }
            sw.Stop();
            */

            NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }
}
