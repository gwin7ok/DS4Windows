#nullable enable

using System;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;

namespace DS4Windows.Input
{
    /// <summary>Bluetooth の無線機（ラジオ）へ、MAC アドレスの機器の切断を依頼する OS 呼び出し。状態を持たない。</summary>
    public static class BluetoothRadioDisconnect
    {
        private const uint IoctlBthDisconnectDevice = 0x41000c;

        /// <summary>全ラジオへ順に切断を依頼し、最初に成功したところで止める。成功したかを返す。</summary>
        public static bool TrySend(string mac)
        {
            IntPtr btHandle = IntPtr.Zero;
            byte[] btAddr = new byte[8];
            string[] sbytes = mac.Split(':');
            for (int i = 0; i < 6; i++)
            {
                // 16 進のバイトを逆順に並べる
                btAddr[5 - i] = Convert.ToByte(sbytes[i], 16);
            }

            long lbtAddr = BitConverter.ToInt64(btAddr, 0);
            bool success = false;

            NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS p = new NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS();
            p.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS));
            IntPtr searchHandle = NativeMethods.BluetoothFindFirstRadio(ref p, ref btHandle);
            int bytesReturned = 0;

            while (!success && btHandle != IntPtr.Zero)
            {
                success = NativeMethods.DeviceIoControl(btHandle, IoctlBthDisconnectDevice, ref lbtAddr, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);
                NativeMethods.CloseHandle(btHandle);
                if (!success)
                {
                    if (!NativeMethods.BluetoothFindNextRadio(searchHandle, ref btHandle))
                        btHandle = IntPtr.Zero;
                }
            }

            NativeMethods.BluetoothFindRadioClose(searchHandle);
            AppLogger.LogDebug("Disconnect successful: " + success);
            return success;
        }
    }
}
