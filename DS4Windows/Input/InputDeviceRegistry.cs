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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Core.Platform;

namespace DS4Windows.Input
{

    /// <summary>
    /// 接続されたコントローラーの登録簿と、HID 機器の列挙・オープン・取り外しの手順（インスタンス。DI の Singleton）。
    /// 状態（開いているデバイスの表、シリアルの重複の検出、排他モードの記憶）はこのインスタンスが持つ。
    /// 設定値は、コンストラクターで受ける（null のときは、Moonlight 無効・詳細ログ無効として扱う。補助プロセスと一部のテスト）。
    /// </summary>
    public partial class InputDeviceRegistry
    {
        // (HID device path, InputDeviceCore)
        private readonly Dictionary<string, InputDeviceCore> Devices = new Dictionary<string, InputDeviceCore>();
        // (MacAddress, InputDeviceCore)
        private readonly Dictionary<string, InputDeviceCore> serialDevices = new Dictionary<string, InputDeviceCore>();
        private readonly HashSet<string> deviceSerials = new HashSet<string>();
        private readonly HashSet<string> DevicePaths = new HashSet<string>();
        // Keep instance of opened exclusive mode devices not in use (Charging while using BT connection)
        private readonly List<IHidTransport> DisabledDevices = new List<IHidTransport>();
        private readonly Stopwatch sw = new Stopwatch();
        public event RequestElevationDelegate? RequestElevation;
        public PrepareInitDelegate? PrepareDS4Init = null;
        public PrepareInitDelegate? PostDS4Init = null;
        public CheckPendingDevice? PreparePendingDevice = null;
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

        // HID の列挙に載らない通信路（HID を持たない Xbox の XInput など）の供給元
        private readonly IInputTransportSource[] transportSources;

        public InputDeviceRegistry(DS4Windows.Input.Abstractions.IDeviceLayerSettings layerSettings, IEnumerable<IInputDeviceCartridge> cartridges)
            : this(layerSettings, cartridges, Array.Empty<IInputTransportSource>())
        {
        }

        public InputDeviceRegistry(DS4Windows.Input.Abstractions.IDeviceLayerSettings layerSettings, IEnumerable<IInputDeviceCartridge> cartridges,
            IEnumerable<IInputTransportSource> transportSources)
        {
            LayerSettings = layerSettings;
            this.cartridges = (cartridges ?? throw new ArgumentNullException(nameof(cartridges))).ToArray();
            knownDevices = this.cartridges.SelectMany(c => c.KnownDevices).ToArray();
            this.transportSources = (transportSources ?? throw new ArgumentNullException(nameof(transportSources))).ToArray();
        }

        // 種類に対応するカートリッジでシリアルを読む（対応するカートリッジがなければ空のシリアル）
        private string? ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            for (int i = 0; i < cartridges.Length; i++)
            {
                if (cartridges[i].Handles(info.inputDevType))
                    return cartridges[i].ReadSerial(info, hidDevice);
            }

            return InputDeviceCore.BLANK_SERIAL;
        }

        // 種類に対応するカートリッジでデバイスを作る（対応するカートリッジがなければ null）
        private InputDeviceCore? CreateDevice(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            for (int i = 0; i < cartridges.Length; i++)
            {
                if (cartridges[i].Handles(type))
                    return cartridges[i].Create(type, hidDevice, displayName, featureSet);
            }

            return null;
        }

        // Returns DS4 controllers that were found and are running
        public IEnumerable<InputDeviceCore> GetControllers()
        {
            lock (Devices)
            {
                InputDeviceCore[] controllers = new InputDeviceCore[Devices.Count];
                Devices.Values.CopyTo(controllers, 0);
                return controllers;
            }
        }

        public void StopControllers()
        {
            lock (Devices)
            {
                IEnumerable<InputDeviceCore> devices = Devices.Values.ToArray();
                //foreach (InputDeviceCore device in devices)
                //for (int i = 0, devCount = devices.Count(); i < devCount; i++)
                for (var devEnum = devices.GetEnumerator(); devEnum.MoveNext();)
                {
                    InputDeviceCore device = devEnum.Current;
                    //InputDeviceCore device = devices.ElementAt(i);
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
        public void On_Removal(object? sender, EventArgs e)
        {
            InputDeviceCore device = (InputDeviceCore)sender!; // 購読元が自分自身を渡す
            RemoveDevice(device);
        }

        public void RemoveDevice(InputDeviceCore device)
        {
            lock (Devices)
            {
                InnerRemoveDevice(device);
            }
        }

        private void InnerRemoveDevice(InputDeviceCore device)
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

        public void UpdateSerial(object? sender, EventArgs e)
        {
            lock (Devices)
            {
                InputDeviceCore device = (InputDeviceCore)sender!; // 購読元が自分自身を渡す
                if (device != null)
                {
                    string devPath = device.HidDevice.DevicePath;
                    string serial = device.MacAddress;
                    if (Devices.ContainsKey(devPath))
                    {
                        deviceSerials.Remove(serial);
                        serialDevices.Remove(serial);
                        device.UpdateSerial();
                        serial = device.MacAddress;
                        if (InputDeviceCore.IsValidSerial(serial))
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

        public void ReEnableDevice(string deviceInstanceId)
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
