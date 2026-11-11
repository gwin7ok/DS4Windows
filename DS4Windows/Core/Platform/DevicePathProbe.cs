using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// Phase7-Step10d W2（Q5＝A）: 旧 <c>Global.GetInstanceIdFromDevicePath／CheckIfVirtualDevice</c> ほか。デバイスパスから調べる、
    /// 状態を持たない Windows の判定（内容は移動のみ）。
    /// </summary>
    public static class DevicePathProbe
    {
        internal static string GetStringDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string result = string.Empty;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            Guid hidGuid = new Guid();
            NativeMethods.HidD_GetHidGuid(ref hidGuid);
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(IntPtr.Zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                result = dataBuffer.ToUTF16String();
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static string GetInstanceIdFromDevicePath(string devicePath)
        {
            string result = string.Empty;
            uint requiredSize = 0;
            NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, null, ref requiredSize, 0);
            if (requiredSize > 0)
            {
                byte[] buffer = new byte[requiredSize];
                NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, buffer, ref requiredSize, 0);
                result = buffer.ToUTF16String();
            }

            return result;
        }

        /// <summary>
        /// 機器のインスタンス ID の下（子孫）にある、HID の機器のインスタンス ID（<c>HID\…</c>）を返す。
        /// Xbox 360 の USB の親の下には、USB の子（IG_00）、その下に HID の子がある。XInput を隠すには、親と HID の子の両方が要る。
        /// </summary>
        public static IReadOnlyList<string> GetHidDescendantInstanceIds(string parentInstanceId)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(parentInstanceId) ||
                NativeMethods.CM_Locate_DevNodeW(out uint parent, parentInstanceId, 0) != 0)
            {
                return result;
            }

            CollectHidDescendants(parent, result, 0);
            return result;
        }

        private static void CollectHidDescendants(uint node, List<string> result, int depth)
        {
            if (depth > 8 || NativeMethods.CM_Get_Child(out uint child, node, 0) != 0)
            {
                return;
            }

            while (true)
            {
                var id = new StringBuilder(512);
                if (NativeMethods.CM_Get_Device_IDW(child, id, id.Capacity, 0) == 0 &&
                    id.ToString().StartsWith(@"HID\", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(id.ToString());
                }

                CollectHidDescendants(child, result, depth + 1);
                if (NativeMethods.CM_Get_Sibling(out uint next, child, 0) != 0)
                {
                    break;
                }

                child = next;
            }
        }

        internal static string[] GetStringArrayDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string[] result = null;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            IntPtr zero = IntPtr.Zero;
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                string tempStr = Encoding.Unicode.GetString(dataBuffer);
                string[] hardwareIds = tempStr.TrimEnd(new char[] { '\0', '\0' }).Split('\0');
                result = hardwareIds;
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static bool CheckIfVirtualDevice(string devicePath)
        {
            bool result = false;
            bool excludeMatchFound = false;

            var instanceId = GetInstanceIdFromDevicePath(devicePath);
            var testInstanceId = instanceId;
            while (!string.IsNullOrEmpty(testInstanceId))
            {
                var hardwareIds = GetStringArrayDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_HardwareIds);
                if (hardwareIds != null)
                {
                    // hardware IDs of root hubs/controllers that emit supported virtual devices as sources
                    var excludedIds = new[]
                    {
                        @"ROOT\HIDGAMEMAP", // reWASD
                        @"ROOT\VHUSB3HC", // VirtualHere
                    };

                    excludeMatchFound = hardwareIds.Any(id => excludedIds.Contains(id.ToUpper()));
                    if (excludeMatchFound)
                    {
                        break;
                    }
                }

                // Check for potential non-present device as well
                string parentInstanceId = GetStringDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_Parent);

                // Found root enumerator. Use instanceId of device one layer lower in final check
                if (parentInstanceId.Equals(@"HTREE\ROOT\0", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                testInstanceId = parentInstanceId;
            }

            if (!excludeMatchFound &&
                !string.IsNullOrEmpty(testInstanceId) &&
                (testInstanceId.StartsWith(@"ROOT\SYSTEM", StringComparison.OrdinalIgnoreCase)
                || testInstanceId.StartsWith(@"ROOT\USB", StringComparison.OrdinalIgnoreCase)))
            {
                result = true;
            }

            return result;
        }
    }
}
