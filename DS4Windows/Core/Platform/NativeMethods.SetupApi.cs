using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DS4Windows.Core.Platform
{
    /// <summary>デバイスの列挙・設定（setupapi.dll・cfgmgr32.dll）の宣言。</summary>
    internal static partial class NativeMethods
    {
        internal const int MAX_DEV_LEN = 1000;
        internal const int SPDRP_ADDRESS = 0x1c;
        internal const int SPDRP_BUSNUMBER = 0x15;
        internal const int SPDRP_BUSTYPEGUID = 0x13;
        internal const int SPDRP_CAPABILITIES = 0xf;
        internal const int SPDRP_CHARACTERISTICS = 0x1b;
        internal const int SPDRP_CLASS = 7;
        internal const int SPDRP_CLASSGUID = 8;
        internal const int SPDRP_COMPATIBLEIDS = 2;
        internal const int SPDRP_CONFIGFLAGS = 0xa;
        internal const int SPDRP_DEVICE_POWER_DATA = 0x1e;
        internal const int SPDRP_DEVICEDESC = 0;
        internal const int SPDRP_DEVTYPE = 0x19;
        internal const int SPDRP_DRIVER = 9;
        internal const int SPDRP_ENUMERATOR_NAME = 0x16;
        internal const int SPDRP_EXCLUSIVE = 0x1a;
        internal const int SPDRP_FRIENDLYNAME = 0xc;
        internal const int SPDRP_HARDWAREID = 1;
        internal const int SPDRP_LEGACYBUSTYPE = 0x14;
        internal const int SPDRP_LOCATION_INFORMATION = 0xd;
        internal const int SPDRP_LOWERFILTERS = 0x12;
        internal const int SPDRP_MFG = 0xb;
        internal const int SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0xe;
        internal const int SPDRP_REMOVAL_POLICY = 0x1f;
        internal const int SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x20;
        internal const int SPDRP_REMOVAL_POLICY_OVERRIDE = 0x21;
        internal const int SPDRP_SECURITY = 0x17;
        internal const int SPDRP_SECURITY_SDS = 0x18;
        internal const int SPDRP_SERVICE = 4;
        internal const int SPDRP_UI_NUMBER = 0x10;
        internal const int SPDRP_UI_NUMBER_DESC_FORMAT = 0x1d;

        internal const uint CM_LOCATE_DEVNODE_NORMAL = 0x00;
        internal const uint CM_LOCATE_DEVNODE_PHANTOM = 0x01;
        internal const uint CM_LOCATE_DEVNODE_CANCELREMOVE = 0x02;

        internal const int SPDRP_UPPERFILTERS = 0x11;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal int cbSize;
            internal System.Guid InterfaceClassGuid;
            internal int Flags;
            internal IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            internal int cbSize;
            internal Guid ClassGuid;
            internal int DevInst;
            internal IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            internal int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            internal string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DEVPROPKEY
        {
            public Guid fmtid;
            public ulong pid;
        }

        [Flags]
        internal enum DEVPROPTYPE : ulong
        {
            DEVPROP_TYPEMOD_ARRAY = 0x00001000,
            DEVPROP_TYPEMOD_LIST = 0x00002000,

            DEVPROP_TYPE_EMPTY = 0x00000000,  // nothing, no property data
            DEVPROP_TYPE_NULL = 0x00000001,  // null property data
            DEVPROP_TYPE_SBYTE = 0x00000002,  // 8-bit signed int (SBYTE)
            DEVPROP_TYPE_BYTE = 0x00000003,  // 8-bit unsigned int (BYTE)
            DEVPROP_TYPE_INT16 = 0x00000004,  // 16-bit signed int (SHORT)
            DEVPROP_TYPE_UINT16 = 0x00000005,  // 16-bit unsigned int (USHORT)
            DEVPROP_TYPE_INT32 = 0x00000006,  // 32-bit signed int (LONG)
            DEVPROP_TYPE_UINT32 = 0x00000007,  // 32-bit unsigned int (ULONG)
            DEVPROP_TYPE_INT64 = 0x00000008,  // 64-bit signed int (LONG64)
            DEVPROP_TYPE_UINT64 = 0x00000009,  // 64-bit unsigned int (ULONG64)
            DEVPROP_TYPE_FLOAT = 0x0000000A,  // 32-bit floating-point (FLOAT)
            DEVPROP_TYPE_DOUBLE = 0x0000000B,  // 64-bit floating-point (DOUBLE)
            DEVPROP_TYPE_DECIMAL = 0x0000000C,  // 128-bit data (DECIMAL)
            DEVPROP_TYPE_GUID = 0x0000000D,  // 128-bit unique identifier (GUID)
            DEVPROP_TYPE_CURRENCY = 0x0000000E,  // 64 bit signed int currency value (CURRENCY)
            DEVPROP_TYPE_DATE = 0x0000000F,  // date (DATE)
            DEVPROP_TYPE_FILETIME = 0x00000010,  // filetime (FILETIME)
            DEVPROP_TYPE_BOOLEAN = 0x00000011,  // 8-bit boolean (DEVPROP_BOOLEAN)
            DEVPROP_TYPE_STRING = 0x00000012,  // null-terminated string
            DEVPROP_TYPE_STRING_LIST = (DEVPROP_TYPE_STRING | DEVPROP_TYPEMOD_LIST), // multi-sz string list
            DEVPROP_TYPE_SECURITY_DESCRIPTOR = 0x00000013,  // self-relative binary SECURITY_DESCRIPTOR
            DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING = 0x00000014,  // security descriptor string (SDDL format)
            DEVPROP_TYPE_DEVPROPKEY = 0x00000015,  // device property key (DEVPROPKEY)
            DEVPROP_TYPE_DEVPROPTYPE = 0x00000016,  // device property type (DEVPROPTYPE)
            DEVPROP_TYPE_BINARY = (DEVPROP_TYPE_BYTE | DEVPROP_TYPEMOD_ARRAY),  // custom binary data
            DEVPROP_TYPE_ERROR = 0x00000017,  // 32-bit Win32 system error code
            DEVPROP_TYPE_NTSTATUS = 0x00000018, // 32-bit NTSTATUS code
            DEVPROP_TYPE_STRING_INDIRECT = 0x00000019, // string resource (@[path\]<dllname>,-<strId>)

            MAX_DEVPROP_TYPE = 0x00000019,
            MAX_DEVPROP_TYPEMOD = 0x00002000,

            DEVPROP_MASK_TYPE = 0x00000FFF,
            DEVPROP_MASK_TYPEMOD = 0x0000F000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_CLASSINSTALL_HEADER
        {
            internal int cbSize;
            internal int installFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_PROPCHANGE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER classInstallHeader;
            internal int stateChange;
            internal int scope;
            internal int hwProfile;
        }

        internal static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc =
            new DEVPROPKEY { fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2), pid = 4 };

        internal static DEVPROPKEY DEVPKEY_Device_DeviceDesc =
            new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 2 };

        internal static DEVPROPKEY DEVPKEY_Device_HardwareIds =
            new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 3 };

        internal static DEVPROPKEY DEVPKEY_Device_UINumber =
            new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 18 };

        internal static DEVPROPKEY DEVPKEY_Device_DriverVersion =
            new DEVPROPKEY { fmtid = new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6), pid = 3 };

        internal static DEVPROPKEY DEVPKEY_Device_Manufacturer =
            new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 13 };

        internal static DEVPROPKEY DEVPKEY_Device_Provider =
            new DEVPROPKEY { fmtid = new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6), pid = 9 };

        internal static DEVPROPKEY DEVPKEY_Device_Parent =
            new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid = 8 };

        internal static DEVPROPKEY DEVPKEY_Device_Siblings =
            new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid = 10 };

        internal static DEVPROPKEY DEVPKEY_Device_InstanceId =
            new DEVPROPKEY { fmtid = new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57), pid = 256 };

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetDeviceRegistryPropertyW", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, int propertyVal, ref int propertyRegDataType, byte[]? propertyBuffer, int propertyBufferSize, ref int requiredSize);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
        public static extern bool SetupDiGetDeviceProperty(IntPtr deviceInfo, ref SP_DEVINFO_DATA deviceInfoData, ref DEVPROPKEY propkey, ref ulong propertyDataType, byte[]? propertyBuffer, int propertyBufferSize, ref int requiredSize, uint flags);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfacePropertyW", SetLastError = true)]
        public static extern bool SetupDiGetDeviceInterfaceProperty(IntPtr deviceInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref DEVPROPKEY propkey, ref ulong propertyDataType, byte[]? propertyBuffer, int propertyBufferSize, ref int requiredSize, uint flags);

        [DllImport("setupapi.dll")]
        static internal extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, int memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll")]
        internal static extern int SetupDiCreateDeviceInfoList(ref Guid classGuid, int hwndParent);

        [DllImport("setupapi.dll")]
        internal static extern IntPtr SetupDiCreateDeviceInfoList(IntPtr guid, int hwndParent);

        [DllImport("setupapi.dll")]
        static internal extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiOpenDeviceInfo(IntPtr deviceInfoSet, string deviceInstanceId, IntPtr parent, uint flags, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll")]
        static internal extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll")]
        static internal extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern IntPtr SetupDiGetClassDevs(ref System.Guid classGuid, string? enumerator, int hwndParent, int flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string? enumerator, int hwndParent, int flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
        static internal extern bool SetupDiGetDeviceInterfaceDetailBuffer(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, ref SP_PROPCHANGE_PARAMS classInstallParams, int classInstallParamsSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern bool SetupDiCallClassInstaller(int installFunction, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static internal extern bool SetupDiGetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, char[] deviceInstanceId, Int32 deviceInstanceIdSize, ref int requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static internal extern bool SetupDiClassGuidsFromName(string ClassName, ref Guid ClassGuidArray1stItem, UInt32 ClassGuidArraySize, out UInt32 RequiredSize);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        static internal extern uint CM_Get_Device_Interface_Property(string pszDeviceInterface, ref DEVPROPKEY PropertyKey, out DEVPROPTYPE PropertyType, byte[]? PropertyBuffer, ref uint PropertyBufferSize, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        static internal extern uint CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

        [DllImport("cfgmgr32.dll")]
        static internal extern uint CM_Get_Parent(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll")]
        static internal extern uint CM_Get_Child(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll")]
        static internal extern uint CM_Get_Sibling(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        static internal extern uint CM_Get_Device_IDW(uint dnDevInst, System.Text.StringBuilder buffer, int bufferLen, uint ulFlags);
    }
}
