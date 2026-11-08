using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Output.Kbm;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9）: <see cref="IServiceLifecycle"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の <c>Start</c>／<c>Stop</c>／<c>HotPlug</c>／<c>PrepareAbort</c> と、その状態
    /// （<c>running</c>／<c>loopControllers</c>／<c>inServiceTask</c>／<c>activeControllers</c>／<c>suspending</c>）、イベントを、そのまま移したもの。
    /// 安全弁（ViGEm の開始・破棄順序、<c>inServiceTask</c> による直列化、<c>Thread.Sleep</c>、イベントの発火順序）は、逐語で保つ（順序はソース走査のテストで固定）。
    /// スロットの配列は <see cref="IDeviceSlotState"/>（Phase13-Step5 で直接注入に変更。旧: <c>Func&lt;IDeviceSlotHost&gt;</c>）から受ける。イベントスレッドの実行とキーボード／マウス出力の初期化は、Phase13-Step3 で <see cref="IEventThreadDispatcher"/>／<see cref="IOutputKbmInitializer"/> へ移した。
    /// </summary>
    public sealed class ServiceLifecycle : IServiceLifecycle
    {
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
        private readonly IDeviceSlotState _slots;
        private readonly IEventThreadDispatcher _eventThread;
        private readonly IOutputKbmInitializer _kbmInitializer;
        private readonly int _controllerSlotLimit;

        public bool Running { get; set; }

        public bool LoopControllers { get; set; } = true;

        public bool InServiceTask { get; set; }

        public int ActiveControllers { get => _slots.ActiveControllers; set => _slots.ActiveControllers = value; }   // Phase13-Step5b: 実体は IDeviceSlotState（DeviceConnector も直接読む）

        private bool _suspending;

        /// <summary>
        /// スリープ中か。変えるとライトバーの停止中の印（<see cref="DS4Windows.Core.Model.LightbarOverrides.shuttingdown"/>）も同じ値にする。
        /// 画面（UI）がライトバーの状態を知らずに済むよう、両者の切り替えをここで一緒に行う。
        /// </summary>
        public bool Suspending
        {
            get => _suspending;
            set
            {
                _lightbar.shuttingdown = value;
                _suspending = value;
            }
        }

        public event EventHandler ServiceStarted;

        public event EventHandler PreServiceStop;

        public event EventHandler ServiceStopped;

        public event EventHandler RunningChanged;

        public event Action<DS4Device, int> HotplugController;

        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public ServiceLifecycle(DS4Windows.Core.Model.LightbarOverrides lightbar, IAppSettingsService appSettings, IEnvironmentService environmentService, IProfileSettingsService profileSettings,
            IDs4DeviceRegistry deviceRegistry, IVirtualKBM virtualKBM, IOscService oscService, IUdpMotionService udpMotion,
            IHidHideService hidHideService, IVirtualPadService virtualPad, ILatencyMonitor latencyMonitor, IJoyConLinker joyConLinker,
            IDeviceConnector deviceConnector, IDeviceSlotState slots,
            IEventThreadDispatcher eventThread, IOutputKbmInitializer kbmInitializer)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
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
            _eventThread = eventThread ?? throw new ArgumentNullException(nameof(eventThread));
            _kbmInitializer = kbmInitializer ?? throw new ArgumentNullException(nameof(kbmInitializer));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
        }

        public bool Start(bool showlog = true)
        {
            IDeviceSlotState slots = _slots;
            InServiceTask = true;
            _virtualPad.StartViGEm();
            if (_virtualPad.HasViGEmClient)
            //if (x360Bus.Open() && x360Bus.Start())
            {
                // Initialize output KBM handler at start of ControlService
                _kbmInitializer.Initialize();

                if (showlog)
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.Starting, LogOptions.UserFacing);

                Thread.Sleep(2000);

                bool runningAsAdmin = _environmentService.IsAdministrator();
                if (_virtualKBM.GetIdentifier() != FakerInputHandler.IDENTIFIER && !runningAsAdmin)
                {
                    string helpURL = @"https://ryochan7.github.io/ds4windows-site/troubleshooting/kb-mouse-issues/#windows-not-responding-to-ds4ws-kb-m-commands-in-some-situations";
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_UacConflict", helpURL);
                }

                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_KbmHandler", _virtualKBM.GetFullDisplayName());
                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_VigemConnected", _environmentService.ViGEmBusVersion);

                _deviceRegistry.IsExclusiveMode = _appSettings.UseExclusiveMode; //Re-enable Exclusive Mode

                _hidHideService.UpdateAttributes();

                if (showlog)
                {
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.SearchingController, LogOptions.UserFacing);
                    AppLogger.Log(AppLogLevel.Info, _deviceRegistry.IsExclusiveMode ? DS4Windows.Properties.Resources.UsingExclusive : DS4Windows.Properties.Resources.UsingShared, LogOptions.UserFacing);
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

                    _eventThread.Invoke(() =>
                    {
                        _deviceRegistry.FindControllers();
                    });

                    IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
                    int numControllers = devices.Count();
                    ActiveControllers = numControllers;
                    _lightbar.defaultLight = false;
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
                    AppLogger.Log(AppLogLevel.Warn, e.Message, LogOptions.UserFacing);
                    AppLogger.LogToTray(e.Message, true);
                }

                Running = true;

                _udpMotion.StartListeningIfCreated();   // Phase9 9-E E-3: 旧: _udpServer != null のときの待ち受けの開始（ブロックを UdpMotionService へ移した）
            }
            else
            {
                string logKey = "Log_Svc_VigemConnectFailed";
                object[] logArgs = null;
                if (!_environmentService.IsViGEmBusInstalled())
                {
                    logKey = "Log_Svc_VigemNotInstalled";
                }
                else if (!_environmentService.IsRunningSupportedViGEmBus())
                {
                    logKey = "Log_Svc_VigemUnsupported";
                    logArgs = new object[] { _environmentService.ViGEmBusVersion };
                }
                else
                {
                    logKey = "Log_Svc_VigemConnectFailed";
                }

                AppLogger.LogUser(AppLogLevel.Info, logKey, logArgs);
                AppLogger.LogToTray(AppLogger.UserText(logKey, logArgs));
            }

            InServiceTask = false;
            _appSettings.RunHotPlug = true;
            ServiceStarted?.Invoke(this, EventArgs.Empty);
            RunningChanged?.Invoke(this, EventArgs.Empty);
            using var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityOptions.Classes[_appSettings.ProcessPriority];
            return true;
        }

        public bool Stop(bool showlog = true, bool immediateUnplug = false)
        {
            IDeviceSlotState slots = _slots;
            if (Running)
            {
                Running = false;
                _appSettings.RunHotPlug = false;
                InServiceTask = true;
                PreServiceStop?.Invoke(this, EventArgs.Empty);

                if (showlog)
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.StoppingX360, LogOptions.UserFacing);

                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_VigemClosing");

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
                            _lightbar.forcelight[i] = false;
                            _lightbar.forcedFlash[i] = 0;
                            _lightbar.defaultLight = true;
                            DS4LightBar.updateLightBar(slots.DS4Controllers[i], i, _profileSettings, _lightbar);
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
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.StoppingDS4, LogOptions.UserFacing);

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
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.StoppedDS4Windows, LogOptions.UserFacing);

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
                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_KbmClosing", _virtualKBM.GetDisplayName());
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
            IDeviceSlotState slots = _slots;
            if (Running)
            {
                InServiceTask = true;
                LoopControllers = true;
                _eventThread.Invoke(() =>
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
            IDeviceSlotState slots = _slots;
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
