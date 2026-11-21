using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input
{
    /// <summary>切断の処理（Bluetooth・ドングル・無線）。</summary>
    public partial class InputDeviceCore
    {
        public virtual bool DisconnectWireless(bool callRemoval = false)
        {
            bool result = false;
            if (conType == ConnectionType.BT)
            {
                result = DisconnectBT(callRemoval);
            }
            else if (conType == ConnectionType.SONYWA)
            {
                result = DisconnectDongle(callRemoval);
            }

            return result;
        }

        public virtual bool DisconnectBT(bool callRemoval = false)
        {
            if (Mac != null)
            {
                // Wait for output report to be written
                StopOutputUpdate();
                AppLogger.LogDebug("Trying to disconnect BT device " + Mac);
                bool success = false;

                lock (outputReport)
                {
                    success = BluetoothRadioDisconnect.TrySend(Mac);
                }

                success = true; // XXX return value indicates failure, but it still works?
                if (success)
                {
                    IsDisconnecting = true;

                    if (callRemoval)
                    {
                        Removal?.Invoke(this, EventArgs.Empty);

                        //System.Threading.Tasks.Task.Factory.StartNew(() => { Removal?.Invoke(this, EventArgs.Empty); });
                    }
                }

                return success;
            }

            return false;
        }

        public virtual bool DisconnectDongle(bool remove = false)
        {
            bool result = false;
            byte[] disconnectReport = new byte[SONYWA_FEATURE_REPORT_LENGTH];
            disconnectReport[0] = 0xe2;
            disconnectReport[1] = 0x02;
            Array.Clear(disconnectReport, 2, SONYWA_FEATURE_REPORT_LENGTH - 2);

            if (remove)
                StopOutputUpdate();

            lock (outputReport)
            {
                result = hDevice.WriteFeatureReport(disconnectReport);
            }

            if (result && remove)
            {
                isDisconnecting = true;

                Removal?.Invoke(this, EventArgs.Empty);

                //System.Threading.Tasks.Task.Factory.StartNew(() => { Removal?.Invoke(this, EventArgs.Empty); });
                //Removal?.Invoke(this, EventArgs.Empty);
            }
            else if (result && !remove)
            {
                isRemoved = true;
            }

            return result;
        }
    }
}
