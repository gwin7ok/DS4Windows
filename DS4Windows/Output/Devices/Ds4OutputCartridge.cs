using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output.Abstractions;
using DS4Windows.Output.Backend;

namespace DS4Windows.Output.Devices
{
    /// <summary>
    /// DualShock 4 の仮想パッドの出力のカートリッジ。作る型（基本／拡張）は ViGEmBus のバージョンで決まる（<see cref="DS4OutDeviceFactory"/>）。
    /// 戻りは振動とライトバーで、出力バッファを受け取れる型（<see cref="DS4OutDeviceExt"/>）のときだけつなぐ。
    /// </summary>
    public sealed class Ds4OutputCartridge : IOutputDeviceCartridge
    {
        private readonly IProfileSettingsService _profileSettings;

        public Ds4OutputCartridge(IProfileSettingsService profileSettings)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public OutContType Type => OutContType.DS4;

        public OutputDevice Create(IVirtualPadBackend backend, Version busVersion)
        {
            return DS4OutDeviceFactory.CreateDS4Device(backend, busVersion);
        }

        public OutputDevice Narrow(OutputDevice device) => device as DS4OutDevice;

        public void AttachFeedback(int index, OutputDevice outDevice, InputDeviceCore device)
        {
            int devIndex = index;
                            DS4OutDevice tempDS4 = outDevice as DS4OutDevice;
                            if (tempDS4.CanUseAwaitOutputBuffer)
                            {
            #pragma warning disable CS0219 // useRumble assigned but not read; keep variable for possible future use
                                DS4OutDeviceExt.ReceivedOutBufferHandler processOutBuffAction = (DS4OutDeviceExt sender, byte[] reportData) =>
                                {
                                    /*
                                    //DS4OutputBufferData outputData = new DS4OutputBufferData();
                                    DS4OutputBufferData outputData =
                                        DS4OutDeviceExtras.ConvertOutputBufferArrayToStruct(reportData);

                                    bool useRumble = false; bool useLight = false;
                                    byte flashOn = 0; byte flashOff = 0;
                                    DS4Color? color = null;

                                    //Trace.WriteLine(string.Join(" ", reportData));

                                    if ((outputData.featureFlags & DS4OutDevice.RUMBLE_FEATURE_FLAG) != 0)
                                    {
                                        useRumble = true;
                                        device.setRumble(outputData.rightFastRumble, outputData.leftSlowRumble);
                                        //SetDevRumble(device, devour[4], devour[5], devIndex);
                                    }

                                    if ((outputData.featureFlags & DS4OutDevice.LIGHTBAR_FEATURE_FLAG) != 0)
                                    {
                                        useLight = true;
                                        color = new DS4Color(outputData.lightbarRedColor,
                                            outputData.lightbarGreenColor,
                                            outputData.lightbarBlueColor);
                                    }
                                    else
                                    {
                                        color = device.LightBarColor;
                                    }

                                    if ((outputData.featureFlags & DS4OutDevice.FLASH_FEATURE_FLAG) != 0)
                                    {
                                        useLight = true;
                                        flashOn = outputData.flashOnDuration;
                                        flashOff = outputData.flashOffDuration;
                                    }
                                    else
                                    {
                                        ref DS4LightbarState currentLight =
                                            ref device.GetLightbarStateRef();

                                        flashOn = currentLight.LightBarFlashDurationOn;
                                        flashOff = currentLight.LightBarFlashDurationOff;
                                    }

                                    if (useLight)
                                    {
                                        DS4LightbarState lightState = new DS4LightbarState
                                        {
                                            LightBarColor = (DS4Color)color,
                                            LightBarFlashDurationOn = flashOn,
                                            LightBarFlashDurationOff = flashOff,
                                        };
                                        device.SetLightbarState(ref lightState);
                                    }
                                    //*/

                                    //*
            #pragma warning disable CS0219 // suppress useRumble assigned but not read in this legacy handler
                                    unchecked
                                    {
                                        //Trace.WriteLine($"INDEX: {devIndex}");
                                        //Trace.WriteLine(string.Join(" ", reportData));
                                        //Trace.WriteLine("");

                                        bool useRumble = false; bool useLight = false;
                                        byte flashOn = 0; byte flashOff = 0;
                                        DS4Color? color = null;
                                        if ((reportData[1] & DS4OutDevice.RUMBLE_FEATURE_FLAG) != 0)
                                        {
                                            useRumble = true;
                                            if (_profileSettings.InverseRumbleMotors[devIndex])
                                                device.setRumble(reportData[5], reportData[4]);
                                            else
                                                device.setRumble(reportData[4], reportData[5]);
                                            //SetDevRumble(device, devour[4], devour[5], devIndex);
                                        }

                                        if ((reportData[1] & DS4OutDevice.LIGHTBAR_FEATURE_FLAG) != 0)
                                        {
                                            useLight = true;
                                            color = new DS4Color(reportData[6],
                                                reportData[7],
                                                reportData[8]);
                                        }
                                        else
                                        {
                                            color = device.LightBarColor;
                                        }

                                        if ((reportData[1] & DS4OutDevice.FLASH_FEATURE_FLAG) != 0)
                                        {
                                            useLight = true;
                                            flashOn = reportData[9];
                                            flashOff = reportData[10];
                                        }
                                        else
                                        {
                                            ref DS4LightbarState currentLight =
                                                ref device.GetLightbarStateRef();

                                            flashOn = currentLight.LightBarFlashDurationOn;
                                            flashOff = currentLight.LightBarFlashDurationOff;
                                        }

                                        if (useLight)
                                        {
                                            DS4LightbarState lightState = new DS4LightbarState
                                            {
                                                LightBarColor = (DS4Color)color,
                                                LightBarFlashDurationOn = flashOn,
                                                LightBarFlashDurationOff = flashOff,
                                            };
                                            device.SetLightbarState(ref lightState);
                                        }
                                    }
                                    //*/
                                };

                                DS4OutDeviceExt tempDS4Ext = tempDS4 as DS4OutDeviceExt;
            #pragma warning restore CS0219
                                tempDS4Ext.ReceivedOutBuffer += processOutBuffAction;
                                tempDS4Ext.outBufferFeedbacksDict.TryAdd(index, processOutBuffAction);
                                tempDS4Ext.StartOutputBufferThread();
                            }
        }

        public void RemoveFeedback(OutputDevice outDevice, int inputIndex)
        {
            int inIdx = inputIndex;
            DS4OutDevice tempDS4 = outDevice as DS4OutDevice;
            tempDS4.RemoveFeedback(inIdx);
        }
    }
}
