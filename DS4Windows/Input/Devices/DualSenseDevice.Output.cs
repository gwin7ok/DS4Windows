using System;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Reports;

namespace DS4Windows.Input.Devices
{
    /// <summary>出力レポートの組み立てと送信（LED・トリガー効果・振動）。</summary>
    public partial class DualSenseDevice
    {
        protected override void WriteOutputFrame()
        {
            PrepareOutReport();
            if (outputDirty)
            {
                WriteReport();
                Haptics.Current.dirty = false;
                previousHapticState = Haptics.Current;
            }

            outputDirty = false;
            Haptics.Current.dirty = false;
        }

        private void SendEmptyOutputReport()
        {
            int reportOffset = conType == ConnectionType.BT ? 1 : 0;
            Array.Clear(outputReport, 0, outputReport.Length);

            outputReport[0] = conType == ConnectionType.USB ? OUTPUT_REPORT_ID_USB :
                OUTPUT_REPORT_ID_BT;

            // Disable haptics and trigger motors
            outputReport[1 + reportOffset] = useRumble ? (byte)0x0F : (byte)0x0C;
            outputReport[2 + reportOffset] = 0x15; // Toggle all LED lights. 0x01 | 0x04 | 0x10

            // Set Lightbar to white
            outputReport[45 + reportOffset] = 0xFF; 
            outputReport[46 + reportOffset] = 0xFF;
            outputReport[47 + reportOffset] = 0xFF;

            if (conType == ConnectionType.BT)
            {
                outputReport[1] = OUTPUT_REPORT_ID_DATA;

                // Need to calculate and populate CRC32 data so controller will accept the report
                uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, BT_OUTPUT_REPORT_LENGTH - 4);
                outputReport[74] = (byte)calcCrc32;
                outputReport[75] = (byte)(calcCrc32 >> 8);
                outputReport[76] = (byte)(calcCrc32 >> 16);
                outputReport[77] = (byte)(calcCrc32 >> 24);
            }

            WriteReport();
            //hDevice.fileStream.Flush();
        }

        private void SendInitialBTOutputReport()
        {
            Array.Clear(outputReport, 0, outputReport.Length);

            outputReport[0] = OUTPUT_REPORT_ID_BT; // Report ID
            outputReport[1] = OUTPUT_REPORT_ID_DATA;
            outputReport[3] = 0x15; // Toggle all LED lights. 0x01 | 0x04 | 0x10

            // Need to calculate and populate CRC32 data so controller will accept the report
            uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
            calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, BT_OUTPUT_REPORT_LENGTH - 4);
            outputReport[74] = (byte)calcCrc32;
            outputReport[75] = (byte)(calcCrc32 >> 8);
            outputReport[76] = (byte)(calcCrc32 >> 16);
            outputReport[77] = (byte)(calcCrc32 >> 24);

            WriteReport();
        }

        private unsafe void PrepareOutReport()
        {
            MergeStates();

            bool change = false;
            bool rumbleSet = Haptics.Current.IsRumbleSet();

            var outputState = new DualSenseOutputState
            {
                UseRumble = useRumble,
                UseAccurateRumble = useAccurateRumble,
                RumbleRightLightFast = Haptics.Current.rumbleState.RumbleMotorStrengthRightLightFast,
                RumbleLeftHeavySlow = Haptics.Current.rumbleState.RumbleMotorStrengthLeftHeavySlow,
                LightbarColor = Haptics.Current.lightbarState.LightBarColor,
                HapticPowerLevel = hapticPowerLevel,
                MuteLedByte = muteLEDByte,
                PlayerLedMask = activePlayerLEDMask,
                R2Effect = r2EffectData,
                L2Effect = l2EffectData,
            };

            if (conType == ConnectionType.USB)
            {
                DualSenseOutputReportBuilder.Fill(outputReport, false, in outputState);

                if (Haptics.Current.dirty || !previousHapticState.Equals(Haptics.Current))
                {
                    change = true;
                }
                /*fixed (byte* bytePrevBuff = outputReport, byteTmpBuff = outReportBuffer)
                {
                    for (int i = 0, arlen = USB_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = bytePrevBuff[i] != byteTmpBuff[i];
                }
                */

                outputDirty |= HapticStandby.ShouldSend(standbySw, change, rumbleSet);
                //bool res = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
                //Console.WriteLine("STAUTS: {0}", res);
            }
            else
            {
                //outReportBuffer[0] = OUTPUT_REPORT_ID_BT; // Report ID
                DualSenseOutputReportBuilder.Fill(outputReport, true, in outputState);

                change = Haptics.Current.dirty || !previousHapticState.Equals(Haptics.Current);

                outputDirty |= HapticStandby.ShouldSend(standbySw, change, rumbleSet);

                // コントローラーが受け付けるように、CRC32 を付ける（出力しないフレームは 0 のまま）
                DualSenseOutputReportBuilder.ApplyBluetoothCrc(outputReport, outputBTCrc32Head, outputDirty);

                /*fixed (byte* bytePrevBuff = outputReport, byteTmpBuff = outReportBuffer)
                {
                    for (int i = 0, arlen = BT_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = bytePrevBuff[i] != byteTmpBuff[i];
                }
                */

                /*if (change)
                {
                    outputPendCount = OUTPUT_MIN_COUNT_BT;
                    //Console.WriteLine("DIRTY");
                    outputDirty = true;
                    
                    //outReportBuffer.CopyTo(outputReport, 0);
                }
                else if (outputPendCount >= 1)
                {
                    outputPendCount--;
                    outputDirty = outputPendCount >= 1;
                }
                */

                //outputDirty = true;

                //bool res = hDevice.WriteOutputReportViaControl(outputReport);
                //Console.WriteLine("STAUTS: {0}", res);
            }
        }

        private bool WriteReport()
        {
            bool result;
            if (conType == ConnectionType.BT)
            {
                // DualSense seems to only accept output data via the Interrupt endpoint
                result = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
                //result = hDevice.WriteOutputReportViaControl(outputReport);
            }
            else
            {
                result = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
            }

            //Console.WriteLine("STAUTS: {0}", result);
            return result;
        }

        private void CalculateDeviceSlotMask() => deviceSlotMask = DualSenseLedMasks.FromSlot(deviceSlotNumber);

        private void PrepareMuteLEDByte()
        {
            if (nativeOptionsStore != null)
            {
                muteLEDByte = DualSenseLedMasks.MuteLed(nativeOptionsStore.MuteLedMode);
            }
        }

        private void PreparePlayerLEDBarByte()
        {
            if (nativeOptionsStore != null)
            {
                if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.Off)
                {
                    activePlayerLEDMask = 0x00;
                }
                else if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.On)
                {
                    activePlayerLEDMask = deviceSlotMask;
                }
                else if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.BatteryPercentage)
                {
                    activePlayerLEDMask = DualSenseLedMasks.BatteryBar(battery);
                }
            }
        }

        public override void PrepareTriggerEffect(TriggerId trigger, TriggerEffects effect, TriggerEffectSettings effectSettings)
        {
            if (trigger == TriggerId.LeftTrigger)
            {
                l2EffectData.ChangeData(effect, effectSettings);
            }
            else if (trigger == TriggerId.RightTrigger)
            {
                r2EffectData.ChangeData(effect, effectSettings);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Trigger Id");
            }

            QueueEvent(() =>
            {
                outputDirty = true;
                Haptics.Current.dirty = true;
                PrepareOutReport();
            });
        }
    }
}
