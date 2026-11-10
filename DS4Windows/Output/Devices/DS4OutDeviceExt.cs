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

using DS4Windows.Core.Model;
using DS4Windows.Output.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DS4Windows.Input;

namespace DS4Windows.Output.Devices
{
    class DS4OutDeviceExt : DS4OutDevice
    {
        private byte[] rawOutReportEx = new byte[63];
        private DS4_REPORT_EX outDS4Report;
        private Thread awaitOutBuffThread;
        private bool awaitThreadLoopRunning;
        public delegate void ReceivedOutBufferHandler(DS4OutDeviceExt sender,
            byte[] reportData);

        public event ReceivedOutBufferHandler ReceivedOutBuffer;

        public Dictionary<int, ReceivedOutBufferHandler> outBufferFeedbacksDict =
            new Dictionary<int, ReceivedOutBufferHandler>();
        private CancellationTokenSource tokenSource;

        public DS4OutDeviceExt(IVirtualPadBackend backend) : base(backend)
        {
        }

        public DS4OutDeviceExt(IVirtualPadBackend backend, bool useAwaitOutputBuffer) :
            this(backend)
        {
            canUseAwaitOutputBuffer = useAwaitOutputBuffer;
        }

        public override unsafe void ConvertandSendReport(DS4State state, int device)
        {
            if (!connected) return;

            ushort tempButtons = 0;
            Ds4DPadDirection tempDPad = Ds4DPadDirection.None;
            ushort tempSpecial = 0;

            unchecked
            {
                MapButtons(state, out tempButtons, out tempDPad, out tempSpecial);

                outDS4Report.wButtons = tempButtons;
                // Frame counter is high 6 bits. Low 2 bits is for extra buttons (PS, TP Click)
                outDS4Report.bSpecial = (byte)(tempSpecial | (state.FrameCounter << 2));
                outDS4Report.wButtons |= (ushort)tempDPad;
            }

            outDS4Report.bTriggerL = state.L2;
            outDS4Report.bTriggerR = state.R2;

            // Phase9 Steering 削除 W3: 旧「ステアリング（ジャイロでハンドル操作）の軸の出力先」の switch は削除した（機能ごと削除。Phase9-SteeringWheel-Removal-Plan.md）。通常の軸の出力だけを行う
            outDS4Report.bThumbLX = state.LX;
            outDS4Report.bThumbLY = state.LY;
            outDS4Report.bThumbRX = state.RX;
            outDS4Report.bThumbRY = state.RY;

            // Only worry about mapping 1 touch packet
            outDS4Report.bTouchPacketsN = 1;
            outDS4Report.sCurrentTouch.bPacketCounter = state.TouchPacketCounter;
            outDS4Report.sCurrentTouch.bIsUpTrackingNum1 = state.TrackPadTouch0.RawTrackingNum;
            outDS4Report.sCurrentTouch.bTouchData1[0] = (byte)(state.TrackPadTouch0.X & 0xFF);
            outDS4Report.sCurrentTouch.bTouchData1[1] = (byte)((state.TrackPadTouch0.X >> 8) & 0x0F | (state.TrackPadTouch0.Y << 4) & 0xF0);
            outDS4Report.sCurrentTouch.bTouchData1[2] = (byte)(state.TrackPadTouch0.Y >> 4);

            outDS4Report.sCurrentTouch.bIsUpTrackingNum2 = state.TrackPadTouch1.RawTrackingNum;
            outDS4Report.sCurrentTouch.bTouchData2[0] = (byte)(state.TrackPadTouch1.X & 0xFF);
            outDS4Report.sCurrentTouch.bTouchData2[1] = (byte)((state.TrackPadTouch1.X >> 8) & 0x0F | (state.TrackPadTouch1.Y << 4) & 0xF0);
            outDS4Report.sCurrentTouch.bTouchData2[2] = (byte)(state.TrackPadTouch1.Y >> 4);

            // Flip some coordinates back to DS4 device coordinate system
            //outDS4Report.wGyroX = (short)-state.Motion.gyroYawFull;
            //outDS4Report.wGyroY = (short)state.Motion.gyroPitchFull;
            outDS4Report.wGyroX = (short)state.Motion.gyroPitchFull;
            outDS4Report.wGyroY = (short)-state.Motion.gyroYawFull;
            outDS4Report.wGyroZ = (short)-state.Motion.gyroRollFull;
            outDS4Report.wAccelX = (short)-state.Motion.accelXFull;
            outDS4Report.wAccelY = (short)-state.Motion.accelYFull;
            outDS4Report.wAccelZ = (short)state.Motion.accelZFull;

            // USB DS4 v.1 battery level range is [0-11]
            outDS4Report.bBatteryLvlSpecial = (byte)(state.Battery / 11);

            outDS4Report.wTimestamp = state.ds4Timestamp;

            DS4OutDeviceExtras.CopyBytes(ref outDS4Report, rawOutReportEx);
            //Console.WriteLine("TEST: {0}, {1} {2}", outDS4Report.wGyroX, rawOutReportEx[12], rawOutReportEx[13]);
            //Console.WriteLine("OUTPUT: {0}", string.Join(", ", rawOutReportEx));
            cont.SubmitRawReport(rawOutReportEx);
        }

        public override void ResetState(bool submit = true)
        {
            outDS4Report = default(DS4_REPORT_EX);
            outDS4Report.wButtons &= unchecked((ushort)~0X0F);
            outDS4Report.wButtons |= 0x08;
            outDS4Report.bThumbLX = 0x80;
            outDS4Report.bThumbLY = 0x80;
            outDS4Report.bThumbRX = 0x80;
            outDS4Report.bThumbRY = 0x80;
            DS4OutDeviceExtras.CopyBytes(ref outDS4Report, rawOutReportEx);

            if (submit)
            {
                cont.SubmitRawReport(rawOutReportEx);
            }
        }

        public override void StartOutputBufferThread()
        {
            if (awaitOutBuffThread == null)
            {
                awaitOutBuffThread = new Thread(RunFetchAwaitBufferThread);
                awaitOutBuffThread.Name = "Virtual DS4 Output Buffer Thread";
                awaitOutBuffThread.Priority = ThreadPriority.Normal;
                awaitOutBuffThread.IsBackground = true;
                awaitOutBuffThread.Start();
            }
        }

        private void RunFetchAwaitBufferThread()
        {
            awaitThreadLoopRunning = false;
            tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            // Add extra delay. Attempt to wait for ViGEmBus to be ready
            Thread.Sleep(1000);

            bool working = true;
            awaitThreadLoopRunning = true;
            while (working)
            {
                if (!ct.IsCancellationRequested)
                {
                    byte[] reportData = null;
                    bool reportGrab = false;
                    try
                    {
                        reportData = cont.AwaitRawOutputReport(100, out bool timedOut);
                        if (!timedOut)
                        {
                            reportGrab = true;
                        }
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Will typically fall here after closing connection to
                        // ViGEmBus if file handle is still open
                        working = false;
                    }
                    catch (Exception)
                    {
                        working = false;
                    }

                    if (!working)
                    {
                        break;
                    }

                    if (reportGrab)
                    {
                        ReceivedOutBuffer?.Invoke(this, reportData);
                    }
                }
                else
                {
                    working = false;
                }
            }

            awaitThreadLoopRunning = false;
        }

        public override void RemoveFeedbacks()
        {
            if (!canUseAwaitOutputBuffer)
            {
                base.RemoveFeedbacks();
            }
            else
            {
                foreach (KeyValuePair<int, ReceivedOutBufferHandler> pair in outBufferFeedbacksDict)
                {
                    ReceivedOutBuffer -= pair.Value;
                }

                outBufferFeedbacksDict.Clear();
            }
        }

        public override void RemoveFeedback(int inIdx)
        {
            if (!canUseAwaitOutputBuffer)
            {
                base.RemoveFeedback(inIdx);
            }
            else
            {
                if (outBufferFeedbacksDict.TryGetValue(inIdx, out ReceivedOutBufferHandler handler))
                {
                    ReceivedOutBuffer -= handler;
                    outBufferFeedbacksDict.Remove(inIdx);
                }
            }
        }

        public override void Disconnect()
        {
            // Flag CancellationTokenSource before performing Disconnect.
            // More of a precaution than anything
            tokenSource?.Cancel();
            base.Disconnect();

            // Check if thread loop has started. If true, call Interrupt on the thread.
            // Might be overkill but try anyway
            if (awaitThreadLoopRunning)
            {
                if (awaitOutBuffThread != null)
                {
                    if (!awaitOutBuffThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin))
                    {
                        awaitOutBuffThread.Interrupt();
                    }

                    awaitOutBuffThread.Join();
                }
            }
        }
    }
}
