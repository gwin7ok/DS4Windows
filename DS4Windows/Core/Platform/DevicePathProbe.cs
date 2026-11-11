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
        /// XInput の入り口（XUSB インターフェース）を持つ機器のインスタンス ID から、隠す対象の一覧（USB の複合機器と、その下のすべての機器）を返す。
        /// 入り口の位置は機種で違う: Xbox 360 は USB の親（<c>USB\VID_…\シリアル</c>）、Xbox One 世代は HID の子（<c>HID\VID_…&amp;IG_00\…</c>）にある。
        /// どちらも、XInput を他のアプリに見せないには、複合機器とその下の機器（USB のインターフェース、HID）の全部が要る。
        /// </summary>
        public static IReadOnlyList<string> GetUsbDeviceTreeInstanceIds(string ownerInstanceId)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(ownerInstanceId))
            {
                return result;
            }

            string root = FindUsbDeviceRoot(ownerInstanceId, GetParentInstanceId);
            result.Add(root);
            if (NativeMethods.CM_Locate_DevNodeW(out uint node, root, 0) == 0)
            {
                CollectDescendants(node, result, 0);
            }

            // 持ち主が、複合機器の下にないとき（探せなかった場合）でも、持ち主は必ず含める
            if (!result.Contains(ownerInstanceId, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(ownerInstanceId);
            }

            return result;
        }

        /// <summary>
        /// 機器から親へたどり、USB のインターフェース（<c>&amp;IG_</c>・<c>&amp;MI_</c>）と HID を抜けた先の、USB の複合機器（または単体の USB 機器）を返す。
        /// USB の機器でなければ、元の機器のまま返す（無関係な機器の木を隠さない）。
        /// </summary>
        public static string FindUsbDeviceRoot(string instanceId, Func<string, string> parentOf)
        {
            string node = instanceId;
            for (int i = 0; i < 4 && IsInterfaceNode(node); i++)
            {
                string parent = parentOf(node);
                if (string.IsNullOrEmpty(parent))
                {
                    break;
                }

                node = parent;
            }

            return node.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase) && !IsInterfaceNode(node) ? node : instanceId;
        }

        private static bool IsInterfaceNode(string instanceId) =>
            instanceId.StartsWith(@"HID\", StringComparison.OrdinalIgnoreCase) ||
            instanceId.IndexOf("&IG_", StringComparison.OrdinalIgnoreCase) >= 0 ||
            instanceId.IndexOf("&MI_", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// 機器から親へたどり、Bluetooth LE の機器そのもののノード（<c>BTHLE\DEV_…</c>）を返す。見つからなければ空。
        /// 無効化と有効化で接続を切るのは、このノード（HID やサービスの子ではなく、機器の親）。
        /// </summary>
        public static string FindBluetoothLeDeviceNode(string instanceId, Func<string, string> parentOf)
        {
            string node = instanceId;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(node); i++)
            {
                if (node.StartsWith(@"BTHLE\DEV_", StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                node = parentOf(node);
            }

            return string.Empty;
        }

        public static string FindBluetoothLeDeviceNode(string instanceId) => FindBluetoothLeDeviceNode(instanceId, GetParentInstanceId);

        private static string GetParentInstanceId(string instanceId)
        {
            if (NativeMethods.CM_Locate_DevNodeW(out uint node, instanceId, 0) != 0 ||
                NativeMethods.CM_Get_Parent(out uint parent, node, 0) != 0)
            {
                return string.Empty;
            }

            var id = new StringBuilder(512);
            return NativeMethods.CM_Get_Device_IDW(parent, id, id.Capacity, 0) == 0 ? id.ToString() : string.Empty;
        }

        private static void CollectDescendants(uint node, List<string> result, int depth)
        {
            if (depth > 8 || NativeMethods.CM_Get_Child(out uint child, node, 0) != 0)
            {
                return;
            }

            while (true)
            {
                var id = new StringBuilder(512);
                if (NativeMethods.CM_Get_Device_IDW(child, id, id.Capacity, 0) == 0)
                {
                    result.Add(id.ToString());
                }

                CollectDescendants(child, result, depth + 1);
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
