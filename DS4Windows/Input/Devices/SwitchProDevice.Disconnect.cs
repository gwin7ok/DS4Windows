using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Hid;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using DS4Windows.Input.Reports;

namespace DS4Windows.Input.Devices
{
    /// <summary>切断と、出力レポートの書き込み。</summary>
    public partial class SwitchProDevice
    {
        public override bool DisconnectWireless(bool callRemoval = false)
        {
            bool result = false;
            result = DisconnectBT(callRemoval);
            //StopOutputUpdate();
            //Detach();
            return result;
        }

        public override bool DisconnectBT(bool callRemoval = false)
        {
            StopOutputUpdate();
            Detach();

            uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000c;

            byte[] btAddr = new byte[8];
            string[] sbytes = Mac.Split(':');
            for (int i = 0; i < 6; i++)
            {
                // parse hex byte in reverse order
                btAddr[5 - i] = Convert.ToByte(sbytes[i], 16);
            }

            long lbtAddr = BitConverter.ToInt64(btAddr, 0);

            IntPtr btHandle = IntPtr.Zero;
            bool success = false;
            NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS p = new NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS();
            p.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS));
            IntPtr searchHandle = NativeMethods.BluetoothFindFirstRadio(ref p, ref btHandle);
            int bytesReturned = 0;

            while (!success && btHandle != IntPtr.Zero)
            {
                success = NativeMethods.DeviceIoControl(btHandle, IOCTL_BTH_DISCONNECT_DEVICE, ref lbtAddr, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);
                NativeMethods.CloseHandle(btHandle);
                if (!success)
                {
                    if (!NativeMethods.BluetoothFindNextRadio(searchHandle, ref btHandle))
                        btHandle = IntPtr.Zero;
                }
            }

            NativeMethods.BluetoothFindRadioClose(searchHandle);
            AppLogger.LogDebug("Disconnect successful: " + success);
            success = true;

            if (callRemoval)
            {
                isDisconnecting = true;
                Removal?.Invoke(this, EventArgs.Empty);
            }

            return success;
        }

        public override bool DisconnectDongle(bool remove = false)
        {
            StopOutputUpdate();
            Detach();

            if (remove)
            {
                isDisconnecting = true;
                Removal?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                isRemoved = true;
            }

            return true;
        }

        public void Detach()
        {
            bool result;

            if (connectionOpened)
            {
                // Disable Gyro
                byte[] tmpOffBuffer = new byte[] { 0x0 };
                Subcommand(0x40, tmpOffBuffer, 1, checkResponse: true);

                // Possibly disable rumble? Leave commented
                tmpOffBuffer = new byte[] { 0x0 };
                Subcommand(0x48, tmpOffBuffer, 1, checkResponse: true);

                // Revert back to low power state
                byte[] powerChoiceArray = new byte[] { 0x01 };
                Subcommand(SwitchProSubCmd.SET_LOW_POWER_STATE, powerChoiceArray, 1, checkResponse: true);

                if (conType == ConnectionType.USB)
                {
                    byte[] data = new byte[64];
                    data[0] = 0x80; data[1] = 0x05;
                    result = hDevice.WriteOutputReportViaControl(data);

                    data[0] = 0x80; data[1] = 0x06;
                    result = hDevice.WriteOutputReportViaControl(data);
                }
            }

            connectionOpened = false;
        }

        public void WriteReport()
        {
            MergeStates();

            bool dirty;
            double tempRatio = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow / 255.0;
            dirty = tempRatio != 0 || tempRatio != currentLeftAmpRatio;
            currentLeftAmpRatio = tempRatio;

            tempRatio = currentHap.rumbleState.RumbleMotorStrengthRightLightFast / 255.0;
            dirty = dirty || tempRatio != 0 || tempRatio != currentRightAmpRatio;
            currentRightAmpRatio = tempRatio;

            if (dirty)
            {
                PrepareRumbleData(rumbleReportBuffer);
                bool result = hDevice.WriteOutputReportViaInterrupt(rumbleReportBuffer, 100);
            }
        }

        private void CalculateDeviceSlotMask()
        {
            // Map 1-15 as a set of 4 LED lights
            switch (deviceSlotNumber)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    deviceSlotMask = (byte)(1 << deviceSlotNumber);
                    break;
                case 4:
                    deviceSlotMask = 0x01 | 0x02;
                    break;
                case 5:
                    deviceSlotMask = 0x01 | 0x04;
                    break;
                case 6:
                    deviceSlotMask = 0x01 | 0x08;
                    break;

                case 7:
                    deviceSlotMask = 0x02 | 0x04;
                    break;
                case 8:
                    deviceSlotMask = 0x02 | 0x08;
                    break;

                case 9:
                    deviceSlotMask = 0x04 | 0x08;
                    break;

                case 10:
                    deviceSlotMask = 0x01 | 0x02 | 0x04;
                    break;
                case 11:
                    deviceSlotMask = 0x01 | 0x02 | 0x08;
                    break;
                case 12:
                    deviceSlotMask = 0x01 | 0x04 | 0x08;
                    break;

                case 13:
                    deviceSlotMask = 0x02 | 0x04 | 0x08;
                    break;

                case 14:
                    deviceSlotMask = 0x01 | 0x02 | 0x04 | 0x08;
                    break;
                default:
                    deviceSlotMask = 0x00;
                    break;
            }
        }
    }
}
