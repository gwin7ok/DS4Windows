using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4WinWPF.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9）: <see cref="IServiceLifecycle"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の <c>Start</c>／<c>Stop</c>／<c>HotPlug</c>／<c>PrepareAbort</c> と、その状態
    /// （<c>running</c>／<c>loopControllers</c>／<c>inServiceTask</c>／<c>activeControllers</c>／<c>suspending</c>）、イベントを、そのまま移したもの。
    /// 安全弁（ViGEm の開始・破棄順序、<c>inServiceTask</c> による直列化、<c>Thread.Sleep</c>、イベントの発火順序）は、逐語で保つ（順序はソース走査のテストで固定）。
    /// スロットの配列は <see cref="IDeviceSlotHost"/>、イベントスレッドの実行とキーボード／マウス出力の初期化は <see cref="IServiceLifecycleHost"/>（いずれも <c>ControlService</c>）から、遅延して引く。
    /// </summary>
    public sealed class ServiceLifecycle : IServiceLifecycle
    {
        // 旧 MainWindow.ProcessPriorityClasses（UI の静的リスト）と同じ順序。第 2 層から UI を参照しないよう、ここに持つ（設定は添字で保存されている）
        private static readonly ProcessPriorityClass[] ProcessPriorityClasses =
        {
            ProcessPriorityClass.Normal, ProcessPriorityClass.AboveNormal,
            ProcessPriorityClass.High, ProcessPriorityClass.RealTime
        };

        private readonly IAppSettingsService _appSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly IVirtualKBM _virtualKBM;
        private readonly IOscService _oscService;
        private readonly IUdpMotionService _udpMotion;
        private readonly IHidHideService _hidHideService;
        private readonly IVirtualPadService _virtualPad;
        private readonly ILatencyMonitor _latencyMonitor;
        private readonly IJoyConLinker _joyConLinker;
        private readonly IDeviceConnector _deviceConnector;
        private readonly Func<IDeviceSlotHost> _slots;
        private readonly Func<IServiceLifecycleHost> _lifecycleHost;
        private readonly int _controllerSlotLimit;

        public bool Running { get; set; }

        public bool LoopControllers { get; set; } = true;

        public bool InServiceTask { get; set; }

        public int ActiveControllers { get; set; }

        public bool Suspending { get; set; }

        public event Action<string, bool> DebugMessage;

        public event EventHandler ServiceStarted;

        public event EventHandler PreServiceStop;

        public event EventHandler ServiceStopped;

        public event EventHandler RunningChanged;

        public event Action<DS4Device, int> HotplugController;

        public ServiceLifecycle(IAppSettingsService appSettings, IEnvironmentService environmentService, IProfileSettingsService profileSettings,
            IDs4DeviceRegistry deviceRegistry, IVirtualKBM virtualKBM, IOscService oscService, IUdpMotionService udpMotion,
            IHidHideService hidHideService, IVirtualPadService virtualPad, ILatencyMonitor latencyMonitor, IJoyConLinker joyConLinker,
            IDeviceConnector deviceConnector, Func<IDeviceSlotHost> slots, Func<IServiceLifecycleHost> lifecycleHost)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
            _joyConLinker = joyConLinker ?? throw new ArgumentNullException(nameof(joyConLinker));
            _deviceConnector = deviceConnector ?? throw new ArgumentNullException(nameof(deviceConnector));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _lifecycleHost = lifecycleHost ?? throw new ArgumentNullException(nameof(lifecycleHost));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
        }

        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public bool Start(bool showlog = true)
        {
            IDeviceSlotHost slots = _slots();
            InServiceTask = true;
            _virtualPad.StartViGEm();
            if (_virtualPad.HasViGEmClient)
            //if (x360Bus.Open() && x360Bus.Start())
            {
                // Initialize output KBM handler at start of ControlService
                _lifecycleHost().InitializeOutputKbmHandler();

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.Starting);

                Thread.Sleep(2000);

                bool runningAsAdmin = _environmentService.IsAdministrator();
                if (_virtualKBM.GetIdentifier() != FakerInputHandler.IDENTIFIER && !runningAsAdmin)
                {
                    string helpURL = @"https://ryochan7.github.io/ds4windows-site/troubleshooting/kb-mouse-issues/#windows-not-responding-to-ds4ws-kb-m-commands-in-some-situations";
                    LogDebug($"Some applications may block controller inputs. (Windows UAC Conflictions). Please go to {helpURL} for more information and workarounds.");
                }

                LogDebug($"Using output KB+M handler: {_virtualKBM.GetFullDisplayName()}");
                LogDebug($"Connection to ViGEmBus {_environmentService.ViGEmBusVersion} established");

                _deviceRegistry.IsExclusiveMode = _appSettings.UseExclusiveMode; //Re-enable Exclusive Mode

                _hidHideService.UpdateAttributes();

                if (showlog)
                {
                    LogDebug(DS4WinWPF.Properties.Resources.SearchingController);
                    LogDebug(_deviceRegistry.IsExclusiveMode ? DS4WinWPF.Properties.Resources.UsingExclusive : DS4WinWPF.Properties.Resources.UsingShared);
                }

                if (_appSettings.UseOscServer && !_oscService.HasListener)
                {
                    _oscService.ChangeListenerStatus(true);
                }

                if (_appSettings.UseOscSender && !_oscService.HasSender)
                {
                    _oscService.ChangeSenderStatus(true);
                }

                if (_appSettings.UseUdpServer && !_udpMotion.HasServer)
                {
                    _udpMotion.ChangeStatus(true, false);
                    while (_udpMotion.IsChangingStatus)
                    {
                        Thread.SpinWait(500);
                    }
                }

                try
                {
                    LoopControllers = true;
                    _virtualPad.AssignInitialDevices();   // Phase9 9-E E-4: 旧 AssignInitialDevices（恒久スロットの割り当て）は VirtualPadService へ移した

                    _lifecycleHost().InvokeOnEventThread(() =>
                    {
                        _deviceRegistry.FindControllers();
                    });

                    IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
                    int numControllers = devices.Count();
                    ActiveControllers = numControllers;
                    DS4LightBar.defaultLight = false;
                    int i = 0;
                    IJoinableDevice tempPrimaryJoyDev = null;
                    for (var devEnum = devices.GetEnumerator();
                        devEnum.MoveNext() && LoopControllers; i++)
                    {
                        DS4Device device = devEnum.Current;

                        _deviceConnector.BeginPrepare(device, showlog: true);

                        // Phase9 9-E E-6d: Joy-Con の結合の手順（先に来たものを主にして順に組む）は JoyConLinker へ移した
                        tempPrimaryJoyDev = _joyConLinker.LinkOnStart(new JoinableDeviceAdapter(device), tempPrimaryJoyDev,
                            _appSettings.DeviceOptions.JoyConDeviceOpts.LinkedMode);

                        slots.DS4Controllers[i] = device;
                        device.DeviceSlotNumber = i;
                        _deviceConnector.PrepareConnected(numControllers, device, index: i);

                        if (i >= _controllerSlotLimit) // out of Xinput devices!
                            break;
                    }
                }
                catch (Exception e)
                {
                    LogDebug(e.Message, true);
                    AppLogger.LogToTray(e.Message, true);
                }

                Running = true;

                _udpMotion.StartListeningIfCreated();   // Phase9 9-E E-3: 旧: _udpServer != null のときの待ち受けの開始（ブロックを UdpMotionService へ移した）
            }
            else
            {
                string logMessage = string.Empty;
                if (!_environmentService.IsViGEmBusInstalled())
                {
                    logMessage = "ViGEmBus is not installed";
                }
                else if (!_environmentService.IsRunningSupportedViGEmBus())
                {
                    logMessage = string.Format("Unsupported ViGEmBus found ({0}). Please install at least ViGEmBus 1.17.333.0", _environmentService.ViGEmBusVersion);
                }
                else
                {
                    logMessage = "Could not connect to ViGEmBus. Please check the status of the System device in Device Manager and if Visual C++ 2017 Redistributable is installed.";
                }

                LogDebug(logMessage);
                AppLogger.LogToTray(logMessage);
            }

            InServiceTask = false;
            _appSettings.RunHotPlug = true;
            ServiceStarted?.Invoke(this, EventArgs.Empty);
            RunningChanged?.Invoke(this, EventArgs.Empty);
            using var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClasses[_appSettings.ProcessPriority];
            return true;
        }

        public bool Stop(bool showlog = true, bool immediateUnplug = false)
        {
            IDeviceSlotHost slots = _slots();
            if (Running)
            {
                Running = false;
                _appSettings.RunHotPlug = false;
                InServiceTask = true;
                PreServiceStop?.Invoke(this, EventArgs.Empty);

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppingX360);

                LogDebug("Closing connection to ViGEmBus");

                bool anyUnplugged = false;
                for (int i = 0, arlength = slots.DS4Controllers.Length; i < arlength; i++)
                {
                    DS4Device tempDevice = slots.DS4Controllers[i];
                    if (tempDevice != null)
                    {
                        if ((_appSettings.DCBTatStop && !tempDevice.isCharging()) || Suspending)
                        {
                            if (tempDevice.getConnectionType() == ConnectionType.BT)
                            {
                                tempDevice.StopUpdate();
                                tempDevice.DisconnectBT(true);
                            }
                            else if (tempDevice.getConnectionType() == ConnectionType.SONYWA)
                            {
                                // Controller disconnect will complete on next attempted read.
                                // Do not use StopUpdate here
                                tempDevice.DisconnectDongle(true);
                            }
                            else
                            {
                                tempDevice.StopUpdate();
                            }
                        }
                        else
                        {
                            DS4LightBar.forcelight[i] = false;
                            DS4LightBar.forcedFlash[i] = 0;
                            DS4LightBar.defaultLight = true;
                            DS4LightBar.updateLightBar(slots.DS4Controllers[i], i, _profileSettings);
                            tempDevice.IsRemoved = true;
                            tempDevice.StopUpdate();
                            _deviceRegistry.RemoveDevice(tempDevice);
                            Thread.Sleep(50);
                        }

                        slots.CurrentStates[i].Battery = slots.PreviousStates[i].Battery = 0; // Reset for the next connection's initial status change.
                        OutputDevice tempout = _virtualPad.OutputDevices[i];
                        if (tempout != null)
                        {
                            _virtualPad.UnplugOutDev(i, tempDevice, immediate: immediateUnplug, force: true);
                            anyUnplugged = true;
                        }

                        //_virtualPad.OutputDevices[i] = null;
                        //useDInputOnly[i] = true;
                        //Global.activeOutDevType[i] = OutContType.None;
                        _profileSettings.UseDInputOnlyArray[i] = true;
                        slots.DS4Controllers[i] = null;
                        _oscService.ResetDeviceState(i);
                        slots.TouchPads[i] = null;
                        _latencyMonitor.Reset(i);
                    }
                }

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppingDS4);

                _deviceRegistry.StopControllers();
                slots.SlotManager.ClearControllerList();

                if (_oscService.HasListener)
                {
                    _oscService.ChangeListenerStatus(false);
                }

                if (_oscService.HasSender)
                {
                    _oscService.ChangeSenderStatus(false);
                }

                if (_udpMotion.HasServer)
                {
                    _udpMotion.ChangeStatus(false, true);
                }

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppedDS4Windows);

                while (_virtualPad.OutputSlotManager.RunningQueue)
                {
                    Thread.SpinWait(500);
                }
                _virtualPad.OutputSlotManager.Stop(true);

                if (anyUnplugged)
                {
                    Thread.Sleep(OutputSlotManager.DELAY_TIME);
                }

                _virtualPad.StopViGEm();

                // Disconnect from KBM system when stopping ControlService
                LogDebug($"Closing connection to output handler {_virtualKBM.GetDisplayName()}");
                _virtualKBM.Disconnect();
                InServiceTask = false;
                ActiveControllers = 0;
            }

            _appSettings.RunHotPlug = false;
            ServiceStopped?.Invoke(this, EventArgs.Empty);
            RunningChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool HotPlug()
        {
            IDeviceSlotHost slots = _slots();
            if (Running)
            {
                InServiceTask = true;
                LoopControllers = true;
                _lifecycleHost().InvokeOnEventThread(() =>
                {
                    _deviceRegistry.FindControllers();
                });

                IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
                int numControllers = devices.Count();
                ActiveControllers = numControllers;
                // Phase9 9-E E-6d: Joy-Con の結合の手順（主と副を先に探してから組む）は JoyConLinker へ移した
                IJoinableDevice tempPrimaryJoyDev = null;
                IJoinableDevice tempSecondaryJoyDev = null;
                _joyConLinker.FindPendingForHotPlug(devices.Select(d => (IJoinableDevice)new JoinableDeviceAdapter(d)),
                    _appSettings.DeviceOptions.JoyConDeviceOpts.LinkedMode, out tempPrimaryJoyDev, out tempSecondaryJoyDev);

                for (var devEnum = devices.GetEnumerator(); devEnum.MoveNext() && LoopControllers;)
                {
                    DS4Device device = devEnum.Current;

                    if (device.isDisconnectingStatus())
                        continue;

                    // Use local method rather than Func
                    bool checkAlreadyExists()
                    {
                        for (int Index = 0, arlength = slots.DS4Controllers.Length; Index < arlength; Index++)
                        {
                            if (slots.DS4Controllers[Index] != null &&
                                slots.DS4Controllers[Index].getMacAddress() == device.getMacAddress())
                            {
                                device.CheckControllerNumDeviceSettings(numControllers);
                                return true;
                            }
                        }

                        return false;
                    }

                    if (checkAlreadyExists())
                    {
                        continue;
                    }

                    for (int Index = 0, arlength = slots.DS4Controllers.Length;
                        Index < arlength && Index < _controllerSlotLimit; Index++)
                    {
                        if (slots.DS4Controllers[Index] == null)
                        {
                            _deviceConnector.BeginPrepare(device);

                            _joyConLinker.LinkOnHotPlug(new JoinableDeviceAdapter(device), ref tempPrimaryJoyDev, ref tempSecondaryJoyDev,
                                _appSettings.DeviceOptions.JoyConDeviceOpts.LinkedMode);

                            slots.DS4Controllers[Index] = device;
                            device.DeviceSlotNumber = Index;
                            _deviceConnector.PrepareConnected(numControllers, device, Index);

                            HotplugController?.Invoke(device, Index);
                            break;
                        }
                    }
                }

                InServiceTask = false;
            }

            return true;
        }

        public void PrepareAbort()
        {
            IDeviceSlotHost slots = _slots();
            for (int i = 0, arlength = slots.DS4Controllers.Length; i < arlength; i++)
            {
                DS4Device tempDevice = slots.DS4Controllers[i];
                if (tempDevice != null)
                {
                    tempDevice.PrepareAbort();
                }
            }
        }
    }
}
