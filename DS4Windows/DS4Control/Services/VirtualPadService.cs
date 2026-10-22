using System;
using System.Threading;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Control;
using Nefarius.ViGEm.Client;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: <see cref="IVirtualPadService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の仮想パッド出力系（ViGEm クライアントの生成・破棄 <c>StartViGEm</c>／<c>StopViGEm</c>、<c>EstablishOutDevice</c>、振動・ライトバーの戻り
    /// <c>EstablishOutFeedback</c>／<c>RemoveOutFeedback</c>、割り当て・取り外し <c>PluginOutDev</c>／<c>UnplugOutDev</c>、入力に紐づかない仮想パッドの抜き差し、
    /// 状態 <c>outputslotMan</c>／<c>outputDevices</c>／<c>vigemTestClient</c>）を、そのまま移したもの（呼び出しの順序は変えていない。ガードレール: ドライバ破棄順序、切断時クリーンアップ）。
    /// <c>ControlService</c> が持つもの（振動のブースト込みの設定、イベントスレッド）は <see cref="IVirtualPadHost"/> 経由で、デバイスは <see cref="IDeviceStateAccessor"/> 経由で、
    /// いずれも遅延して受ける（循環を避ける）。実機（ViGEmBus とコントローラー）でなければ確かめられない部分は、台帳 §36 の確認項目で担保する。
    /// TODO(技術的負債): PluginOutDev の X360 の枝と DS4 の枝は約 80 行がほぼ同じ。実機確認のあと、別コミットで畳む（Phase9-E-Plan.md §13 の Q3）。
    /// </summary>
    public sealed class VirtualPadService : IVirtualPadService
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IProfileSettingsService _profileSettings;
        private readonly Func<IOutputSlotService> _outputSlotServiceFactory;
        private readonly Func<IDeviceStateAccessor> _deviceAccessor;
        private readonly Func<IVirtualPadHost> _host;

        private readonly OutputSlotManager outputslotMan;
        private readonly OutputDevice[] outputDevices = new OutputDevice[ControllerLimits.MaxControllerCount];
        private ViGEmClient vigemTestClient = null;
        private Thread tempThread;

        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）ため、初回に1回だけ取得する（旧 ControlService と同じ）
        private OutContType[] _activeOutDevType;
        private OutContType[] ActiveOutDevType
            => _activeOutDevType ?? (_activeOutDevType = _outputSlotServiceFactory().ActiveOutDevType);

        public event Action<string, bool> DebugMessage;

        public VirtualPadService(IEnvironmentService environmentService, IProfileSettingsService profileSettings,
            Func<IOutputSlotService> outputSlotServiceFactory, Func<IDeviceStateAccessor> deviceAccessor, Func<IVirtualPadHost> host)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _outputSlotServiceFactory = outputSlotServiceFactory ?? throw new ArgumentNullException(nameof(outputSlotServiceFactory));
            _deviceAccessor = deviceAccessor ?? throw new ArgumentNullException(nameof(deviceAccessor));
            _host = host ?? throw new ArgumentNullException(nameof(host));

            // 旧 ControlService のコンストラクターにあった生成
            outputslotMan = new OutputSlotManager(() => _environmentService.ViGEmBusVersion, _environmentService.ControllerSlotLimit);
        }

        public OutputSlotManager OutputSlotManager => outputslotMan;

        public OutputDevice[] OutputDevices => outputDevices;

        public bool HasViGEmClient => vigemTestClient != null;

        public void QueueOnEventThread(Action action)
        {
            _host().QueueOnEventThread(action);
        }

        // 旧 ControlService.LogDebug(string, bool)。購読者（ControlService）が Debug イベントへ流す
        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public void StartViGEm()
        {
            // Refresh internal ViGEmBus info
            _environmentService.RefreshViGEmBusInfo();
            if (_environmentService.IsRunningSupportedViGEmBus())
            {
                tempThread = new Thread(() =>
                {
                    try
                    {
                        vigemTestClient = new ViGEmClient();
                    }
                    catch { }
                });
                tempThread.Priority = ThreadPriority.AboveNormal;
                tempThread.IsBackground = true;
                tempThread.Start();
                while (tempThread.IsAlive)
                {
                    Thread.SpinWait(500);
                }
            }

            tempThread = null;
        }

        public void StopViGEm()
        {
            if (vigemTestClient != null)
            {
                vigemTestClient.Dispose();
                vigemTestClient = null;
            }
        }

        // 恒久スロット（OutSlotDevice.ReserveStatus.Permanent）を、サービスの開始時に割り当てる（ControlService.Start から呼ばれる）。
        public void AssignInitialDevices()
        {
            foreach (OutSlotDevice slotDevice in outputslotMan.OutputSlots)
            {
                if (slotDevice.CurrentReserveStatus ==
                    OutSlotDevice.ReserveStatus.Permanent)
                {
                    OutputDevice outDevice = EstablishOutDevice(0, slotDevice.PermanentType);
                    outputslotMan.DeferredPlugin(outDevice, -1, "", outputDevices, slotDevice.PermanentType);
                }
            }
            /*OutSlotDevice slotDevice =
                outputslotMan.FindExistUnboundSlotType(OutContType.X360);

            if (slotDevice == null)
            {
                slotDevice = outputslotMan.FindOpenSlot();
                slotDevice.CurrentReserveStatus = OutSlotDevice.ReserveStatus.Permanent;
                slotDevice.PermanentType = OutContType.X360;
                OutputDevice outDevice = EstablishOutDevice(0, OutContType.X360);
                Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
                outputslotMan.DeferredPlugin(tempXbox, -1, outputDevices, OutContType.X360);
            }
            */

            /*slotDevice = outputslotMan.FindExistUnboundSlotType(OutContType.X360);
            if (slotDevice == null)
            {
                slotDevice = outputslotMan.FindOpenSlot();
                slotDevice.CurrentReserveStatus = OutSlotDevice.ReserveStatus.Permanent;
                slotDevice.DesiredType = OutContType.X360;
                OutputDevice outDevice = EstablishOutDevice(1, OutContType.X360);
                Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
                outputslotMan.DeferredPlugin(tempXbox, 1, outputDevices);
            }*/
        }

        private OutputDevice EstablishOutDevice(int index, OutContType contType)
        {
            OutputDevice temp = null;
            temp = outputslotMan.AllocateController(contType, vigemTestClient);
            return temp;
        }

        public void EstablishOutFeedback(int index, OutContType contType,
            OutputDevice outDevice, DS4Device device)
        {
            int devIndex = index;

            if (contType == OutContType.X360)
            {
                Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
                Nefarius.ViGEm.Client.Targets.Xbox360FeedbackReceivedEventHandler p = (sender, args) =>
                {
                    //Trace.WriteLine(string.Format("Rumble ({0}, {1}) {2}",
                    //    args.LargeMotor, args.SmallMotor, DateTime.Now.ToString("hh:mm:ss.FFFF")));
                    _host().SetDevRumble(device, args.LargeMotor, args.SmallMotor, devIndex);
                };
                tempXbox.cont.FeedbackReceived += p;
                tempXbox.forceFeedbacksDict.Add(index, p);
            }
            else if (contType == OutContType.DS4)
            {
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
            //else if (contType == OutContType.DS4)
            //{
            //    DS4OutDevice tempDS4 = outDevice as DS4OutDevice;
            //    LightbarSettingInfo deviceLightbarSettingsInfo = Global.LightbarSettingsInfo[devIndex];

            //    Nefarius.ViGEm.Client.Targets.DualShock4FeedbackReceivedEventHandler p = (sender, args) =>
            //    {
            //        bool useRumble = false; bool useLight = false;
            //        byte largeMotor = args.LargeMotor;
            //        byte smallMotor = args.SmallMotor;
            //        //SetDevRumble(device, largeMotor, smallMotor, devIndex);
            //        DS4Color color = new DS4Color(args.LightbarColor.Red,
            //                args.LightbarColor.Green,
            //                args.LightbarColor.Blue);

            //        //Console.WriteLine("IN EVENT");
            //        //Console.WriteLine("Rumble ({0}, {1}) | Light ({2}, {3}, {4}) {5}",
            //        //    largeMotor, smallMotor, color.red, color.green, color.blue, DateTime.Now.ToString("hh:mm:ss.FFFF"));

            //        if (largeMotor != 0 || smallMotor != 0)
            //        {
            //            useRumble = true;
            //        }

            //        // Let games to control lightbar only when the mode is Passthru (otherwise DS4Windows controls the light)
            //        if (deviceLightbarSettingsInfo.Mode == LightbarMode.Passthru && (color.red != 0 || color.green != 0 || color.blue != 0))
            //        {
            //            useLight = true;
            //        }

            //        if (!useRumble && !useLight)
            //        {
            //            //Console.WriteLine("Fallback");
            //            if (device.LeftHeavySlowRumble != 0 || device.RightLightFastRumble != 0)
            //            {
            //                useRumble = true;
            //            }
            //            else if (deviceLightbarSettingsInfo.Mode == LightbarMode.Passthru &&
            //                (device.LightBarColor.red != 0 ||
            //                device.LightBarColor.green != 0 ||
            //                device.LightBarColor.blue != 0))
            //            {
            //                useLight = true;
            //            }
            //        }

            //        if (useRumble)
            //        {
            //            //Console.WriteLine("Perform rumble");
            //            SetDevRumble(device, largeMotor, smallMotor, devIndex);
            //        }

            //        if (useLight)
            //        {
            //            //Console.WriteLine("Change lightbar color");
            //            /*DS4HapticState haptics = new DS4HapticState
            //            {
            //                LightBarColor = color,
            //            };
            //            device.SetHapticState(ref haptics);
            //            */

            //            DS4LightbarState lightState = new DS4LightbarState
            //            {
            //                LightBarColor = color,
            //            };
            //            device.SetLightbarState(ref lightState);
            //        }

            //        //Console.WriteLine();
            //    };

            //    tempDS4.cont.FeedbackReceived += p;
            //    tempDS4.forceFeedbacksDict.Add(index, p);
            //}
        }

        public void RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx)
        {
            if (contType == OutContType.X360)
            {
                Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
                tempXbox.RemoveFeedback(inIdx);
                //tempXbox.cont.FeedbackReceived -= tempXbox.forceFeedbackCall;
                //tempXbox.forceFeedbackCall = null;
            }
            else if (contType == OutContType.DS4)
            {
                DS4OutDevice tempDS4 = outDevice as DS4OutDevice;
                tempDS4.RemoveFeedback(inIdx);
            }
            //else if (contType == OutContType.DS4)
            //{
            //    DS4OutDevice tempDS4 = outDevice as DS4OutDevice;
            //    tempDS4.RemoveFeedback(inIdx);
            //    //tempDS4.cont.FeedbackReceived -= tempDS4.forceFeedbackCall;
            //    //tempDS4.forceFeedbackCall = null;
            //}
        }

        // TODO(技術的負債・Phase9 9-E E-4): 呼び出し元 0 件（旧 ControlService から移した）。画面は AttachUnboundOutDev（スロット指定）を使っている。
        // 使う予定が無ければ、9-F で削除をユーザーへ提案する（規約 2.4）。
        public void AttachNewUnboundOutDev(OutContType contType)
        {
            OutSlotDevice slotDevice = outputslotMan.FindOpenSlot();
            if (slotDevice != null &&
                slotDevice.CurrentAttachedStatus == OutSlotDevice.AttachedStatus.UnAttached)
            {
                OutputDevice outDevice = EstablishOutDevice(-1, contType);
                outputslotMan.DeferredPlugin(outDevice, -1, "", outputDevices, contType);
            }
        }

        public void AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType)
        {
            if (slotDevice.CurrentAttachedStatus == OutSlotDevice.AttachedStatus.UnAttached &&
                slotDevice.CurrentInputBound == OutSlotDevice.InputBound.Unbound)
            {
                OutputDevice outDevice = EstablishOutDevice(-1, contType);
                outputslotMan.DeferredPlugin(outDevice, -1, "", outputDevices, contType);
            }
        }

        public void DetachUnboundOutDev(OutSlotDevice slotDevice)
        {
            if (slotDevice.CurrentInputBound == OutSlotDevice.InputBound.Unbound)
            {
                OutputDevice dev = slotDevice.OutputDevice;
                string tempType = dev.GetDeviceType();
                slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Unbound;
                outputslotMan.DeferredRemoval(dev, -1, outputDevices, false);
            }
        }

        public void PluginOutDev(int index, DS4Device device)
        {
            OutContType contType = _profileSettings.OutContType[index];

            OutSlotDevice slotDevice = null;
            if (!_profileSettings.GetDInputOnly(index))
            {
                slotDevice = outputslotMan.FindExistUnboundSlotType(contType);
            }

            if (_profileSettings.UseDInputOnlyArray[index])
            {
                bool success = false;
                if (contType == OutContType.X360)
                {
                    ActiveOutDevType[index] = OutContType.X360;

                    if (slotDevice == null)
                    {
                        slotDevice = outputslotMan.FindOpenSlot();
                        if (slotDevice != null)
                        {
                            Xbox360OutDevice tempXbox = EstablishOutDevice(index, OutContType.X360)
                            as Xbox360OutDevice;
                            //outputDevices[index] = tempXbox;

                            // Enable ViGem feedback callback handler only if lightbar/rumble data output is enabled (if those are disabled then no point enabling ViGem callback handler call)
                            if (_profileSettings.EnableOutputDataToDS4[index])
                            {
                                EstablishOutFeedback(index, OutContType.X360, tempXbox, device);

                                if (device.JointDeviceSlotNumber != -1)
                                {
                                    DS4Device tempDS4Device = _deviceAccessor().GetController(device.JointDeviceSlotNumber);
                                    if (tempDS4Device != null)
                                    {
                                        EstablishOutFeedback(device.JointDeviceSlotNumber, OutContType.X360, tempXbox, tempDS4Device);
                                    }
                                }
                            }

                            outputslotMan.DeferredPlugin(tempXbox, index, $"{device.DisplayName} [{device.MacAddress}]", outputDevices, contType);
                            //slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Bound;

                            success = true;
                        }
                        else
                        {
                            LogDebug("Failed. No open output slot found");
                        }
                    }
                    else
                    {
                        slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Bound;
                        Xbox360OutDevice tempXbox = slotDevice.OutputDevice as Xbox360OutDevice;

                        // Enable ViGem feedback callback handler only if lightbar/rumble data output is enabled (if those are disabled then no point enabling ViGem callback handler call)
                        if (_profileSettings.EnableOutputDataToDS4[index])
                        {
                            EstablishOutFeedback(index, OutContType.X360, tempXbox, device);

                            if (device.JointDeviceSlotNumber != -1)
                            {
                                DS4Device tempDS4Device = _deviceAccessor().GetController(device.JointDeviceSlotNumber);
                                if (tempDS4Device != null)
                                {
                                    EstablishOutFeedback(device.JointDeviceSlotNumber, OutContType.X360, tempXbox, tempDS4Device);
                                }
                            }
                        }

                        outputDevices[index] = tempXbox;
                        slotDevice.CurrentType = contType;
                        success = true;
                    }

                    //tempXbox.Connect();
                    //LogDebug("X360 Controller #" + (index + 1) + " connected");
                }
                else if (contType == OutContType.DS4)
                {
                    ActiveOutDevType[index] = OutContType.DS4;
                    if (slotDevice == null)
                    {
                        slotDevice = outputslotMan.FindOpenSlot();
                        if (slotDevice != null)
                        {
                            DS4OutDevice tempDS4 = EstablishOutDevice(index, OutContType.DS4)
                            as DS4OutDevice;

                            // Enable ViGem feedback callback handler only if DS4 lightbar/rumble data output is enabled (if those are disabled then no point enabling ViGem callback handler call)
                            if (_profileSettings.EnableOutputDataToDS4[index])
                            {
                                EstablishOutFeedback(index, OutContType.DS4, tempDS4, device);

                                if (device.JointDeviceSlotNumber != -1)
                                {
                                    DS4Device tempDS4Device = _deviceAccessor().GetController(device.JointDeviceSlotNumber);
                                    if (tempDS4Device != null)
                                    {
                                        EstablishOutFeedback(device.JointDeviceSlotNumber, OutContType.DS4, tempDS4, tempDS4Device);
                                    }
                                }
                            }

                            outputslotMan.DeferredPlugin(tempDS4, index, $"{device.DisplayName} [{device.MacAddress}]", outputDevices, contType);
                            //slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Bound;

                            success = true;
                        }
                        else
                        {
                            LogDebug("Failed. No open output slot found");
                        }
                    }
                    else
                    {
                        slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Bound;
                        DS4OutDevice tempDS4 = slotDevice.OutputDevice as DS4OutDevice;

                        // Enable ViGem feedback callback handler only if lightbar/rumble data output is enabled (if those are disabled then no point enabling ViGem callback handler call)
                        if (_profileSettings.EnableOutputDataToDS4[index])
                        {
                            EstablishOutFeedback(index, OutContType.DS4, tempDS4, device);

                            if (device.JointDeviceSlotNumber != -1)
                            {
                                DS4Device tempDS4Device = _deviceAccessor().GetController(device.JointDeviceSlotNumber);
                                if (tempDS4Device != null)
                                {
                                    EstablishOutFeedback(device.JointDeviceSlotNumber, OutContType.DS4, tempDS4, tempDS4Device);
                                }
                            }
                        }

                        outputDevices[index] = tempDS4;
                        slotDevice.CurrentType = contType;
                        success = true;
                    }

                    //DS4OutDevice tempDS4 = new DS4OutDevice(vigemTestClient);
                    //DS4OutDevice tempDS4 = outputslotMan.AllocateController(OutContType.DS4, vigemTestClient)
                    //    as DS4OutDevice;
                    //outputDevices[index] = tempDS4;

                    //tempDS4.Connect();
                    //LogDebug("DS4 Controller #" + (index + 1) + " connected");
                }

                // Need to check for possible ViGEmBus failure here
                if (success && slotDevice.OutputDevice != null)
                {
                    LogDebug($"Associated input controller #{index + 1} ({device.DisplayName}) to virtual {slotDevice.OutputDevice.GetDeviceType()} Controller in{(slotDevice.PermanentType != OutContType.None ? " permanent" : "")} output slot #{slotDevice.Index + 1}");
                    _profileSettings.UseDInputOnlyArray[index] = false;
                }
            }
        }

        public void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false)
        {
            if (!_profileSettings.UseDInputOnlyArray[index])
            {
                //OutContType contType = Global.OutContType[index];
                OutputDevice dev = outputDevices[index];
                OutSlotDevice slotDevice = outputslotMan.GetOutSlotDevice(dev);
                if (dev != null && slotDevice != null)
                {
                    string tempType = dev.GetDeviceType();
                    LogDebug($"Disassociated virtual {tempType} Controller in{(slotDevice.CurrentReserveStatus == OutSlotDevice.ReserveStatus.Permanent ? " permanent" : "")} output slot #{slotDevice.Index + 1} from input controller #{index + 1} ({device.DisplayName})", false);

                    OutContType currentType = ActiveOutDevType[index];
                    outputDevices[index] = null;
                    ActiveOutDevType[index] = OutContType.None;
                    if ((slotDevice.CurrentAttachedStatus == OutSlotDevice.AttachedStatus.Attached &&
                        slotDevice.CurrentReserveStatus == OutSlotDevice.ReserveStatus.Dynamic) || force)
                    {
                        //slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Unbound;
                        outputslotMan.DeferredRemoval(dev, index, outputDevices, immediate);
                    }
                    else if (slotDevice.CurrentAttachedStatus == OutSlotDevice.AttachedStatus.Attached)
                    {
                        slotDevice.CurrentInputBound = OutSlotDevice.InputBound.Unbound;
                        dev.ResetState();
                        dev.RemoveFeedbacks();
                        //RemoveOutFeedback(currentType, dev);
                    }
                    //dev.Disconnect();
                    //LogDebug(tempType + " Controller # " + (index + 1) + " unplugged");
                }

                _profileSettings.UseDInputOnlyArray[index] = true;
            }
        }
    }
}
