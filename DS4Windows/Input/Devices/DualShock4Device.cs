using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// DualShock 4（USB／Bluetooth／ワイヤレスアダプター）。読み取りの骨組み（<see cref="InputLoopDevice"/>）の差し込み口で、
    /// DS4 のレポート（BT は 0x11＋CRC32）の検証、電池、タイムスタンプ、タッチパッド、動き、アダプターの同期、アイドル切断を書く。
    /// </summary>
    public class DualShock4Device : InputLoopDevice
    {
        private static readonly IInputReportParser ReportParser = new Ds4ReportParser();
        private const int CRC32_NUM_ATTEMPTS = 10;

        private readonly TimestampClock timestampClock = new TimestampClock(ushort.MaxValue, 16u);
        private DS4ControllerOptions nativeOptionsStore;
        private const byte DEFAULT_BT_REPORT_TYPE = 0x11;
        private byte knownGoodBTOutputReportType = DEFAULT_BT_REPORT_TYPE;
        private int btOutputPayloadLen = BT_OUTPUT_REPORT_LENGTH;

        //private const byte DEFAULT_OUTPUT_FEATURES = 0xF7;
        private const byte DEFAULT_OUTPUT_FEATURES = 0x07;
        //private const byte COPYCAT_OUTPUT_FEATURES = 0xF3;
        private const byte COPYCAT_OUTPUT_FEATURES = 0x03;
        private byte outputFeaturesByte = DEFAULT_OUTPUT_FEATURES;

        private const int BT_OUTPUT_REPORT_0x15_LENGTH = BT_OUTPUT_REPORT_LENGTH;
        private const int BT_OUTPUT_REPORT_0x11_LENGTH = 78;
        private byte[] outputBTCrc32Head = new byte[] { 0xA2 };

        private long rawReceivedTime;
        private bool forceWrite;
        private int crcOffset;
        private int crcPos = BT_INPUT_REPORT_CRC32_POS;

        public DualShock4Device(IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
            : base(hidDevice, displayName, featureSet)
        {
        }

        public override void PostInit()
        {
            conType = HidConnectionType(hDevice);
            Mac = hDevice.ReadSerial(SerialReportID);

            //HidDevice hidDevice = hDevice;
            deviceType = DS4Windows.Core.Model.InputDeviceType.DS4;
            gyroMouseSensSettings = new GyroMouseSens();
            optionsStore = nativeOptionsStore = new DS4ControllerOptions(deviceType);
            SetupOptionsEvents();

            if (conType == ConnectionType.USB || conType == ConnectionType.SONYWA)
            {
                inputReport = new byte[64];
                outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];
                outReportBuffer = new byte[hDevice.Capabilities.OutputReportByteLength];
                if (conType == ConnectionType.USB)
                {
                    warnInterval = WARN_INTERVAL_USB;
                    synced = true;
                }
                else
                {
                    warnInterval = WARN_INTERVAL_BT;
                    runCalib = synced = isValidSerial();
                }
            }
            else
            {
                btInputReport = new byte[BT_INPUT_REPORT_LENGTH];
                inputReport = new byte[BT_INPUT_REPORT_LENGTH - 2];
                // If OnlyOutputData0x05 feature is not set then use the default DS4 output buffer size. However, some Razer gamepads use 32 bytes output buffer and output data type 0x05 in BT mode (writeData fails if the code tries to write too many unnecessary bytes)
                if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                {
                    // Default DS4 logic while writing data to gamepad
                    outputReport = new byte[BT_OUTPUT_REPORT_LENGTH];
                    outReportBuffer = new byte[BT_OUTPUT_REPORT_LENGTH];

                    // Buffer len and output report payload len will differ
                    btOutputPayloadLen = BT_OUTPUT_REPORT_0x11_LENGTH;
                }
                else
                {
                    // Use the gamepad specific output buffer size (but minimum of 15 bytes to avoid out-of-index errors in this app)
                    outputReport = new byte[hDevice.Capabilities.OutputReportByteLength <= 15 ? 15 : hDevice.Capabilities.OutputReportByteLength];
                    outReportBuffer = new byte[hDevice.Capabilities.OutputReportByteLength <= 15 ? 15 : hDevice.Capabilities.OutputReportByteLength];

                    // Use custom buffer len
                    btOutputPayloadLen = outputReport.Length;
                }
                warnInterval = WARN_INTERVAL_BT;
                synced = isValidSerial();
            }

            if (runCalib)
                RefreshCalibration();

            // if (!hDevice.IsFileStreamOpen())
            // {
            //     hDevice.OpenFileStream(outputReport.Length);
            // }

            // Temporarily disable this check as it does not seem to help
            // detect fake DS4 controllers
            //if (conType == ConnectionType.BT &&
            //    !featureSet.HasFlag(VidPidFeatureSet.NoOutputData) &&
            //    !featureSet.HasFlag(VidPidFeatureSet.OnlyOutputData0x05))
            //{
            //    CheckOutputReportTypes();
            //}

            sendOutputReport(true, true, false); // initialize the output report (don't force disconnect the gamepad on initialization even if writeData fails because some fake DS4 gamepads don't support writeData over BT)

            // Mark PostInit complete so StartUpdate won't race with uninitialized buffers
            postInitDone = true;
        }

        // TODO: Possibly remove method
        private void CheckOutputReportTypes()
        {
            // Use Tuple here for convenience
            var reportIds = new (byte Id, int Length)[]
            {
                (0x15, BT_OUTPUT_REPORT_0x15_LENGTH),
                (0x11, BT_OUTPUT_REPORT_0x11_LENGTH),
            };

            byte finalReport = 0x00;
            foreach (var element in reportIds)
            {
                int len = element.Length;
                byte[] outputBuffer = new byte[element.Length];
                outputBuffer[0] = element.Id;
                //outputBuffer[1] = (byte)(0xC0 | 0x04);
                outputBuffer[2] = 0xA0;

                // Need to calculate and populate CRC-32 data so controller will accept the report
                uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputBuffer, 0, len - 4);
                outputBuffer[len - 4] = (byte)calcCrc32;
                outputBuffer[len - 3] = (byte)(calcCrc32 >> 8);
                outputBuffer[len - 2] = (byte)(calcCrc32 >> 16);
                outputBuffer[len - 1] = (byte)(calcCrc32 >> 24);

                if (WriteOutput(outputBuffer))
                {
                    finalReport = element.Id;
                    knownGoodBTOutputReportType = element.Id;
                    outputReport = new byte[len];
                    outReportBuffer = new byte[len];
                    break;
                }
            }

            if (finalReport == 0x00)
            {
                ModifyFeatureSetFlag(VidPidFeatureSet.NoOutputData, true);
            }
        }

        private void TimeoutTestThread()
        {
            while (!timeoutExecuted)
            {
                if (timeoutEvent)
                {
                    timeoutExecuted = true;
                    this.sendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                }
                else
                {
                    timeoutEvent = true;
                    Thread.Sleep(READ_STREAM_TIMEOUT);
                }
            }
        }

        public override void RefreshCalibration()
        {
            byte[] calibration = new byte[41];
            calibration[0] = conType == ConnectionType.BT ? (byte)0x05 : (byte)0x02;

            if (conType == ConnectionType.BT)
            {
                bool found = false;
                for (int tries = 0; !found && tries < 5; tries++)
                {
                    hDevice.readFeatureData(calibration);
                    uint recvCrc32 = calibration[DS4_FEATURE_REPORT_5_CRC32_POS] |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 1] << 8) |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 2] << 16) |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 3] << 24);

                    uint calcCrc32 = ~Crc32Algorithm.Compute(new byte[] { 0xA3 });
                    calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref calibration, 0, DS4_FEATURE_REPORT_5_LEN - 4);
                    bool validCrc = recvCrc32 == calcCrc32;
                    if (!validCrc && tries >= 5)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_GyroCalibrationFailed");
                        continue;
                    }
                    else if (validCrc)
                    {
                        found = true;
                    }
                }

                sixAxis.setCalibrationData(ref calibration, conType == ConnectionType.USB);

                if (hDevice.Attributes.ProductId == 0x5C4 && hDevice.Attributes.VendorId == 0x054C &&
                    sixAxis.fixupInvertedGyroAxis())
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Device_YawAxisFixed", Mac.ToString());
            }
            else
            {
                hDevice.readFeatureData(calibration);
                sixAxis.setCalibrationData(ref calibration, conType == ConnectionType.USB);
            }
        }

        public override void StartUpdate()
        {
            this.inputReportErrorCount = 0;

            // Ensure PostInit finished; wait briefly if needed to avoid race where StartUpdate
            // runs before output buffers are initialized in PostInit.
            int waited = 0;
            while (!postInitDone && waited < 500)
            {
                Thread.Sleep(25);
                waited += 25;
            }

            if (ds4Input == null)
            {
                if (conType == ConnectionType.BT)
                {
                    timeoutCheckThread = new Thread(TimeoutTestThread);
                    timeoutCheckThread.Priority = ThreadPriority.BelowNormal;
                    timeoutCheckThread.Name = "DS4 Timeout thread: " + Mac;
                    timeoutCheckThread.IsBackground = true;
                    timeoutCheckThread.Start();
                }
                else
                {
                    ds4Output = new Thread(OutReportCopy);
                    ds4Output.Priority = ThreadPriority.Normal;
                    ds4Output.Name = "DS4 Arr Copy thread: " + Mac;
                    ds4Output.IsBackground = true;
                    ds4Output.Start();
                }

                ds4Input = new Thread(RunInputLoop);
                ds4Input.Priority = ThreadPriority.AboveNormal;
                ds4Input.Name = "DS4 Input thread: " + Mac;
                ds4Input.IsBackground = true;
                ds4Input.Start();
            }
            else
            {
                AppLogger.LogDebug("Thread already running for DS4: " + Mac);
            }
        }

        private unsafe void PrepareOutputReportInner(ref bool change, ref bool haptime)
        {
            bool usingBT = conType == ConnectionType.BT;

            if (usingBT && (this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
            {
                outReportBuffer[0] = knownGoodBTOutputReportType;
                //outReportBuffer[0] = 0x15;
                //outReportBuffer[1] = (byte)(0x80 | btPollRate); // input report rate
                outReportBuffer[1] = (byte)(0xC0 | btPollRate); // input report rate
                //outReportBuffer[2] = 0xA0;

                // Headphone volume L (0x10), Headphone volume R (0x20), Mic volume (0x40), Speaker volume (0x80)
                // enable rumble (0x01), lightbar (0x02), flash (0x04). Default: 0x07
                outReportBuffer[3] = outputFeaturesByte;
                outReportBuffer[4] = 0x04;

                outReportBuffer[6] = currentHap.rumbleState.RumbleMotorStrengthRightLightFast; // fast motor
                outReportBuffer[7] = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow; // slow motor
                outReportBuffer[8] = currentHap.lightbarState.LightBarColor.red; // red
                outReportBuffer[9] = currentHap.lightbarState.LightBarColor.green; // green
                outReportBuffer[10] = currentHap.lightbarState.LightBarColor.blue; // blue
                outReportBuffer[11] = currentHap.lightbarState.LightBarFlashDurationOn; // flash on duration
                outReportBuffer[12] = currentHap.lightbarState.LightBarFlashDurationOff; // flash off duration

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = BT_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || currentHap.dirty;
                /*if (change)
                {
                    Console.WriteLine("CHANGE: {0} {1} {2} {3} {4} {5}", currentHap.LightBarColor.red, currentHap.LightBarColor.green, currentHap.LightBarColor.blue, currentHap.RumbleMotorStrengthRightLightFast, currentHap.RumbleMotorStrengthLeftHeavySlow, DateTime.Now.ToString());
                }
                */

                haptime = haptime || change;
            }
            else
            {
                outReportBuffer[0] = 0x05;
                // Headphone volume L (0x10), Headphone volume R (0x20), Mic volume (0x40), Speaker volume (0x80)
                // enable rumble (0x01), lightbar (0x02), flash (0x04). Default: 0x07
                outReportBuffer[1] = outputFeaturesByte;
                outReportBuffer[2] = 0x04;
                outReportBuffer[4] = currentHap.rumbleState.RumbleMotorStrengthRightLightFast; // fast motor
                outReportBuffer[5] = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow; // slow  motor
                outReportBuffer[6] = currentHap.lightbarState.LightBarColor.red; // red
                outReportBuffer[7] = currentHap.lightbarState.LightBarColor.green; // green
                outReportBuffer[8] = currentHap.lightbarState.LightBarColor.blue; // blue
                outReportBuffer[9] = currentHap.lightbarState.LightBarFlashDurationOn; // flash on duration
                outReportBuffer[10] = currentHap.lightbarState.LightBarFlashDurationOff; // flash off duration

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = USB_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || currentHap.dirty;
                haptime = haptime || change;
            }
        }

        protected override void sendOutputReport(bool synchronous, bool force = false, bool quitOutputThreadOnError = true)
        {
            MergeStates();
            //setTestRumble();
            //setHapticState();

            bool quitOutputThread = false;
            bool usingBT = conType == ConnectionType.BT;

            // Defensive: output buffers should be initialized in PostInit.
            // If they are not yet initialized (race or early call), log and skip to avoid NRE.
            if (outputReport == null || outReportBuffer == null)
            {
                AppLogger.Log(AppLogLevel.Warn, $"sendOutputReport: output buffers not initialized (outputReport={(outputReport == null)}, outReportBuffer={(outReportBuffer == null)})", LogOptions.UserFacing);
                return;
            }

            // Some gamepads don't support lightbar and rumble, so no need to write out anything (writeOut always fails, so DS4Windows would accidentally force quit the gamepad connection).
            // If noOutputData featureSet flag is set then don't try to write out anything to the gamepad device.
            if ((this.featureSet & VidPidFeatureSet.NoOutputData) != 0)
            {
                if (exitOutputThread == false && (IsRemoving || IsRemoved))
                {
                    // Gamepad disconnecting or disconnected. Signal closing of OutputUpdate thread
                    StopOutputUpdate();
                    exitOutputThread = true;
                }

                return;
            }

            //bool output = outputPendCount > 0, change = force;
            bool change = force;
            bool haptime = force || standbySw.ElapsedMilliseconds >= 4000L;

            PrepareOutputReportInner(ref change, ref haptime);

            if (rumbleAutostopTimer.IsRunning)
            {
                // Workaround to a bug in ViGem driver. Force stop potentially stuck rumble motor on the next output report if there haven't been new rumble events within X seconds
                if (rumbleAutostopTimer.ElapsedMilliseconds >= rumbleAutostopTime)
                    setRumble(0, 0);
            }

            if (synchronous)
            {
                if (haptime)
                {
                    if (change)
                    {
                        standbySw.Reset();
                    }
                    else
                        standbySw.Restart();
                    //standbySw.Restart();

                    if (usingBT)
                    {
                        outReportBuffer.CopyTo(outputReport, 0);

                        if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                        {
                            // Need to calculate and populate CRC-32 data so controller will accept the report
                            //int len = outputReport.Length;
                            int len = btOutputPayloadLen;
                            uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                            calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, len - 4);
                            outputReport[len - 4] = (byte)calcCrc32;
                            outputReport[len - 3] = (byte)(calcCrc32 >> 8);
                            outputReport[len - 2] = (byte)(calcCrc32 >> 16);
                            outputReport[len - 1] = (byte)(calcCrc32 >> 24);

                            //Console.WriteLine("Write CRC-32 to output report");
                        }
                    }

                    try
                    {
                        if (!writeOutput())
                        {
                            if (quitOutputThreadOnError)
                            {
                                int winError = Marshal.GetLastWin32Error();

                                // Logfile notification that the gamepad is force disconnected because of writeOutput failed
                                if (quitOutputThread == false && !isDisconnecting)
                                    AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_WriteConnectionLost", winError);

                                quitOutputThread = true;
                            }
                        }
                    }
                    catch { } // If it's dead already, don't worry about it.

                    if (!usingBT)
                    {
                        lock (outReportBuffer)
                        {
                            Monitor.Pulse(outReportBuffer);
                        }
                    }
                }
            }
            else
            {
                //for (int i = 0, arlen = outputReport.Length; !change && i < arlen; i++)
                //    change = outputReport[i] != outReportBuffer[i];

                if (haptime)
                {
                    if (change)
                    {
                        standbySw.Reset();
                    }

                    Monitor.Pulse(outReportBuffer);
                }
            }

            if (quitOutputThread)
            {
                StopOutputUpdate();
                exitOutputThread = true;
            }

            currentHap.dirty = false;
        }

        // Perform outReportBuffer copy on a separate thread to save
        // time on main input thread
        private void OutReportCopy()
        {
            try
            {
                while (!exitOutputThread)
                {
                    // Defensive: if buffers are not initialized, wait briefly and retry
                    if (outReportBuffer == null || outputReport == null)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    lock (outReportBuffer)
                    {
                        outReportBuffer.CopyTo(outputReport, 0);
                        Monitor.Wait(outReportBuffer);
                    }
                }
            }
            catch (ThreadInterruptedException) { }
        }

        private void PrepareOutputFeaturesByte()
        {
            if (nativeOptionsStore != null)
            {
                if (nativeOptionsStore.IsCopyCat)
                {
                    outputFeaturesByte = COPYCAT_OUTPUT_FEATURES;
                }
                else
                {
                    outputFeaturesByte = DEFAULT_OUTPUT_FEATURES;
                }
            }
        }

        private void SetupOptionsEvents()
        {
            if (nativeOptionsStore != null)
            {
                nativeOptionsStore.IsCopyCatChanged += (sender, e) =>
                {
                    PrepareOutputFeaturesByte();
                };
            }
        }

        public override void LoadStoreSettings()
        {
            if (nativeOptionsStore != null)
            {
                PrepareOutputFeaturesByte();
            }
        }

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            forceWrite = false;
            crcOffset = 0;
            crcPos = BT_INPUT_REPORT_CRC32_POS;

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
        }

        // 切断のときは、出力を止めて Removal を通知し、スレッドを抜ける（exitInputThread は立てない）
        protected override unsafe FrameResult ReceiveFrame()
        {
            // Sony DS4 and compatible gamepads send data packets with 0x11 type code in BT mode.
            if (conType == ConnectionType.BT && (this.featureSet & VidPidFeatureSet.OnlyInputData0x01) == 0)
            {
                HidReadStatus res = hDevice.ReadFile(btInputReport);
                if (res == HidReadStatus.Success)
                {
                    rawReceivedTime = Stopwatch.GetTimestamp();
                    timeoutEvent = false;

                    fixed (byte* byteP = &btInputReport[2], imp = inputReport)
                    {
                        for (int j = 0; j < BT_INPUT_REPORT_LENGTH - 2; j++)
                        {
                            imp[j] = byteP[j];
                        }
                    }

                    uint recvCrc32 = btInputReport[BT_INPUT_REPORT_CRC32_POS] |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 1] << 8) |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 2] << 16) |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 3] << 24);

                    uint calcCrc32 = ~Crc32Algorithm.CalculateFasterBT78Hash(ref HamSeed, ref btInputReport, ref crcOffset, ref crcPos);
                    if (recvCrc32 != calcCrc32)
                    {
                        cState.PacketCounter = pState.PacketCounter + 1; //still increase so we know there were lost packets

                        // If the incoming data packet does not have the native DS4 type or CRC-32 checks keep failing. Fail out and disconnect controller.
                        if (this.inputReportErrorCount >= CRC32_NUM_ATTEMPTS)
                        {
                            AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_CrcFailedDisconnect", Mac.ToString(), CRC32_NUM_ATTEMPTS);

                            readWaitEv.Reset();
                            sendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                            StopOutputUpdate();
                            isDisconnecting = true;
                            RunRemoval();

                            timeoutExecuted = true;
                            return FrameResult.Terminate;
                        }

                        this.inputReportErrorCount++;
                        readWaitEv.Reset();
                        return FrameResult.Skip;
                    }

                    this.inputReportErrorCount = 0;
                }
                else
                {
                    if (res == HidReadStatus.WaitTimedOut)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedTimeout", Mac.ToString());
                    }
                    else
                    {
                        int winError = Marshal.GetLastWin32Error();
                        AppLogger.LogDebug($"{Mac} {DateTime.UtcNow.ToString("o")}> disconnect due to read failure: {winError.ToString("x8")}");
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedReadFailure", Mac.ToString(), winError);
                    }

                    readWaitEv.Reset();
                    sendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();

                    timeoutExecuted = true;
                    return FrameResult.Terminate;
                }
            }
            else
            {
                HidReadStatus res = hDevice.ReadFile(inputReport);
                if (res != HidReadStatus.Success)
                {
                    if (res == HidReadStatus.WaitTimedOut)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedTimeout", Mac.ToString());
                    }
                    else
                    {
                        int winError = Marshal.GetLastWin32Error();
                        AppLogger.LogDebug($"{Mac} {DateTime.UtcNow.ToString("o")}> disconnect due to read failure: {winError.ToString("x8")}");
                    }

                    readWaitEv.Reset();
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();

                    timeoutExecuted = true;
                    return FrameResult.Terminate;
                }

                rawReceivedTime = Stopwatch.GetTimestamp();
            }

            readWaitEv.Wait();
            readWaitEv.Reset();
            return FrameResult.Accept;
        }

        // フレームの時刻は、読み取りが返った直後の時刻（待機中の印の解除を待つ前）を使う
        protected override long GetFrameTicks()
        {
            lastInputReportTimestamp = rawReceivedTime; // 受信開始時刻を保持
            return rawReceivedTime;
        }

        // BT は 0x11 のレポートだけを扱う
        protected override bool IsExpectedReport()
        {
            return !(conType == ConnectionType.BT && btInputReport[0] != 0x11 && (this.featureSet & VidPidFeatureSet.OnlyInputData0x01) == 0);
        }

        protected override unsafe void ProcessFrame()
        {
            unchecked
            {
                ReportParser.Parse(inputReport, cState);

                if ((this.featureSet & VidPidFeatureSet.NoBatteryReading) == 0)
                {
                    byte tempByte = inputReport[30];
                    bool tempCharging = (tempByte & 0x10) != 0;
                    UpdateCharging(tempCharging);

                    int maxBatteryValue = charging ? BATTERY_MAX_USB : BATTERY_MAX;
                    int tempBattery = (tempByte & 0x0f) * 100 / maxBatteryValue;
                    tempBattery = Math.Min(tempBattery, 100);
                    UpdateBattery(tempBattery);

                    cState.Battery = (byte)battery;
                    if (tempByte != priorInputReport30)
                    {
                        priorInputReport30 = tempByte;
                    }
                }
                else
                {
                    // Some gamepads don't send battery values in DS4 compatible data fields, so use dummy 99% value to avoid constant low battery warnings
                    priorInputReport30 = 0x0F;
                    battery = 99;
                    cState.Battery = 99;
                }

                uint tempStamp = (uint)((ushort)(inputReport[11] << 8) | inputReport[10]);
                timestampClock.Advance(tempStamp, lastTimeElapsedDouble * .001, pState.totalMicroSec, out double elapsedDeltaTime, out ulong clockTotalMicroSec);
                cState.totalMicroSec = clockTotalMicroSec;

                cState.elapsedTime = elapsedDeltaTime;
                cState.ds4Timestamp = (ushort)tempStamp;

                if (conType == ConnectionType.SONYWA)
                {
                    bool controllerSynced = inputReport[31] == 0;
                    if (controllerSynced != synced)
                    {
                        runCalib = synced = controllerSynced;
                        RaiseSyncChange();
                        if (synced)
                        {
                            forceWrite = true;
                            sixAxis.ResetContinuousCalibration();
                        }
                        else
                        {
                            standbySw.Reset();
                            sixAxis.StopContinuousCalibration();
                        }
                    }
                }

                // XXX DS4State mapping needs fixup, turn touches into an array[4] of structs.  And include the touchpad details there instead.
                try
                {
                    // Only care if one touch packet is detected. Other touch packets
                    // don't seem to contain relevant data. ds4drv does not use them either.
                    for (int touches = Math.Max((int)(inputReport[-1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET - 1]), 1), touchOffset = 0; touches > 0; touches--, touchOffset += 9)
                    {
                        cState.TouchPacketCounter = inputReport[-1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset];
                        cState.Touch1 = (inputReport[0 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // finger 1 detected
                        cState.Touch1Identifier = (byte)(inputReport[0 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                        cState.Touch2 = (inputReport[4 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // finger 2 detected
                        cState.Touch2Identifier = (byte)(inputReport[4 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                        cState.Touch1Finger = cState.Touch1 || cState.Touch2; // >= 1 touch detected
                        cState.Touch2Fingers = cState.Touch1 && cState.Touch2; // 2 touches detected
                        int touchX = (((inputReport[2 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) << 8) | inputReport[1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset]);
                        cState.TouchLeft = touchX >= DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                        cState.TouchRight = touchX < DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                        // Even when idling there is still a touch packet indicating no touch 1 or 2
                        if (synced)
                        {
                            touchpad.handleTouchpad(inputReport, cState, DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET, touchOffset);
                        }
                    }
                }
                catch (Exception ex) { frameError = $"Touchpad: {ex.Message}"; }

                // Store Gyro and Accel values. Use pointers here as it seems faster than using Array.Copy
                fixed (byte* pbInput = &inputReport[13], pbGyro = gyro, pbAccel = accel)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        pbGyro[i] = pbInput[i];
                    }

                    for (int i = 6; i < 12; i++)
                    {
                        pbAccel[i - 6] = pbInput[i];
                    }

                    if (synced)
                    {
                        sixAxis.handleSixaxis(pbGyro, pbAccel, cState, elapsedDeltaTime);
                    }
                }

                ds4InactiveFrame = cState.FrameCounter == pState.FrameCounter;
                if (!ds4InactiveFrame)
                {
                    isRemoved = false;
                }
            }
        }

        protected override bool ProcessIdle()
        {
            if (EvaluateIdleDisconnect(utcNow))
            {
                if (conType == ConnectionType.BT)
                {
                    if (DisconnectBT(true))
                    {
                        timeoutExecuted = true;
                        return true; // all done
                    }
                }
                else if (conType == ConnectionType.SONYWA)
                {
                    DisconnectDongle();
                }
            }

            return false;
        }

        protected override void WriteOutputFrame()
        {
            sendOutputReport(true, forceWrite);
            forceWrite = false;
        }
    }
}
