using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using Microsoft.Win32.SafeHandles;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// <see cref="IXusbInterfaceSource"/> の実装（Windows の SetupAPI と XUSB ドライバーの IOCTL）。読み取りだけを行い、機器の状態は変えない。
    /// 使う IOCTL（実機で確認した。管理者権限は不要）: <c>GET_INFORMATION</c>（0x80006000。末尾 4 バイトが VID／PID）と、
    /// <c>GET_LED_STATE</c>（0x8000E008。入力 3 バイトの 0。3 バイト目が LED の模様で、6〜9 が XInput の番号 0〜3）。
    /// </summary>
    public sealed class XusbInterfaceSource : IXusbInterfaceSource
    {
        private static readonly Guid XusbInterfaceGuid = new Guid("EC87F1E3-C13B-4100-B5F7-8B84D54260CB");

        private const uint IoctlGetInformation = 0x80006000;
        private const uint IoctlGetLedState = 0x8000E008;
        private const int LedPatternOfPlayer0 = 6;
        private const int PlayerCount = 4;

        public IReadOnlyList<XusbInterfaceInfo> Enumerate()
        {
            var result = new List<XusbInterfaceInfo>();
            foreach (string path in EnumeratePaths())
            {
                if (!TryReadVidPid(path, out int vid, out int pid))
                {
                    continue;
                }

                result.Add(new XusbInterfaceInfo(path, DevicePathProbe.GetInstanceIdFromDevicePath(path), vid, pid, ReadPlayerIndex(path)));
            }

            return result;
        }

        public int ReadPlayerIndex(string devicePath)
        {
            byte[] output = new byte[3];
            if (!TryIoctl(devicePath, IoctlGetLedState, output, out int returned) || returned < 3)
            {
                return -1;
            }

            int index = output[2] - LedPatternOfPlayer0;
            return index >= 0 && index < PlayerCount ? index : -1;
        }

        private static bool TryReadVidPid(string path, out int vid, out int pid)
        {
            byte[] output = new byte[12];
            if (!TryIoctl(path, IoctlGetInformation, output, out int returned) || returned < 12)
            {
                vid = 0;
                pid = 0;
                return false;
            }

            vid = output[8] | (output[9] << 8);
            pid = output[10] | (output[11] << 8);
            return true;
        }

        private static bool TryIoctl(string path, uint code, byte[] output, out int returned)
        {
            returned = 0;
            using (SafeFileHandle handle = CreateFile(path, GenericRead | GenericWrite, ShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    return false;
                }

                return DeviceIoControl(handle, code, new byte[3], 3, output, output.Length, out returned, IntPtr.Zero);
            }
        }

        private static IEnumerable<string> EnumeratePaths()
        {
            Guid guid = XusbInterfaceGuid;
            IntPtr set = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
            if (set == IntPtr.Zero || set == new IntPtr(-1))
            {
                yield break;
            }

            try
            {
                for (int i = 0; ; i++)
                {
                    var data = new SpDeviceInterfaceData();
                    data.cbSize = Marshal.SizeOf(data);
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data))
                    {
                        yield break;
                    }

                    string path = ReadInterfacePath(set, ref data);
                    if (!string.IsNullOrEmpty(path))
                    {
                        yield return path;
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(set);
            }
        }

        private static string ReadInterfacePath(IntPtr set, ref SpDeviceInterfaceData data)
        {
            SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out int required, IntPtr.Zero);
            if (required <= 0)
            {
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal(required);
            try
            {
                Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);   // cbSize（構造体の固定部分の大きさ）
                if (!SetupDiGetDeviceInterfaceDetail(set, ref data, buffer, required, out required, IntPtr.Zero))
                {
                    return null;
                }

                return Marshal.PtrToStringAuto(IntPtr.Add(buffer, 4));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint ShareReadWrite = 3;
        private const uint OpenExisting = 3;
        private const int DigcfPresent = 0x2;
        private const int DigcfDeviceInterface = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        private struct SpDeviceInterfaceData
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr deviceInfoData, ref Guid interfaceClassGuid, int index, ref SpDeviceInterfaceData data);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref SpDeviceInterfaceData data, IntPtr detail, int detailSize, out int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll")]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, byte[] input, int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);
    }
}
