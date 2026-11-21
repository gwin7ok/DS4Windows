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

            bool success = BluetoothRadioDisconnect.TrySend(Mac);
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
            if (connectionOpened)
            {
                _session.Shutdown(conType);
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

        private void CalculateDeviceSlotMask() => deviceSlotMask = SwitchProSlotMask.FromSlot(deviceSlotNumber);
    }
}
