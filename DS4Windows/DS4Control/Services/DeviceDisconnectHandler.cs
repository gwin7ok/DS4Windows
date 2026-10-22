using System;
using System.Threading.Tasks;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）: <see cref="IDeviceDisconnectHandler"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>On_DS4Removal</c>／<c>On_SyncChange</c>／<c>On_SerialChange</c> を、そのまま移したもの
    /// （スロットの配列は、<see cref="IDeviceSlotHost"/> から、呼び出しのたびに引く）。切断の後始末の順序は、旧コードのまま保つ（ガードレール: 切断時クリーンアップ）。
    /// </summary>
    public sealed class DeviceDisconnectHandler : IDeviceDisconnectHandler
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMapping _mapping;
        private readonly IProfileSlotApplier _profileSlotApplier;
        private readonly IOscService _oscService;
        private readonly IAppSettingsService _appSettings;
        private readonly ILatencyMonitor _latencyMonitor;
        private readonly IDeviceStateService _deviceStateService;
        private readonly IVirtualPadService _virtualPad;
        private readonly Func<IOutputSlotService> _outputSlotService;
        private readonly Func<IDeviceSlotHost> _host;
        private readonly int _controllerSlotLimit;

        public event Action<string, bool> DebugMessage;

        public DeviceDisconnectHandler(IProfileSettingsService profileSettings, IMapping mapping, IProfileSlotApplier profileSlotApplier,
            IOscService oscService, IAppSettingsService appSettings, ILatencyMonitor latencyMonitor, IDeviceStateService deviceStateService,
            IVirtualPadService virtualPad, Func<IOutputSlotService> outputSlotService, IEnvironmentService environmentService,
            Func<IDeviceSlotHost> host)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
            _deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            if (environmentService == null) throw new ArgumentNullException(nameof(environmentService));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）。旧 ControlService と同じ配列を指す
        private OutContType[] ActiveOutDevType => _outputSlotService().ActiveOutDevType;

        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public void OnRemoval(object sender, EventArgs e)
        {
            IDeviceSlotHost host = _host();
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = host.DS4Controllers.Length; ind == -1 && i < arlength; i++)
            {
                if (host.DS4Controllers[i] != null && device.getMacAddress() == host.DS4Controllers[i].getMacAddress())
                    ind = i;
            }

            if (ind != -1)
            {
                bool removingStatus = false;
                lock (device.removeLocker)
                {
                    if (!device.IsRemoving)
                    {
                        removingStatus = true;
                        device.IsRemoving = true;
                    }
                }

                if (removingStatus)
                {
                    host.CurrentStates[ind].Battery = host.PreviousStates[ind].Battery = 0; // Reset for the next connection's initial status change.
                    if (!_profileSettings.UseDInputOnlyArray[ind])
                    {
                        _virtualPad.UnplugOutDev(ind, device);
                    }
                    else if (!device.PrimaryDevice)
                    {
                        OutputDevice outDev = _virtualPad.OutputDevices[ind];
                        if (outDev != null)
                        {
                            outDev.RemoveFeedback(ind);
                            _virtualPad.OutputDevices[ind] = null;
                        }
                    }

                    // Use Task to reset device synth state and commit it
                    Task.Run(() =>
                    {
                        _mapping.Commit(ind, _profileSettings);
                    }).Wait();

                    try
                    {
                        _mapping.HandleDeviceDisconnect(ind);
                    }
                    catch { }

                    // Phase8-Step6 W2: 物理的な切断だけが、エンジンの状態と登録簿の束縛を消す（HandleDeviceDisconnect は適用のたびにも呼ばれるため、そこには足さない）
                    _profileSlotApplier.ReleaseSlot(ind);

                    string removed = DS4WinWPF.Properties.Resources.ControllerWasRemoved.Replace("*Mac address*", (ind + 1).ToString());
                    if (device.getBattery() <= 20 &&
                        device.getConnectionType() == ConnectionType.BT && !device.isCharging())
                    {
                        removed += ". " + DS4WinWPF.Properties.Resources.ChargeController;
                    }

                    LogDebug(removed);
                    AppLogger.LogToTray(removed);
                    /*Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (sw.ElapsedMilliseconds < XINPUT_UNPLUG_SETTLE_TIME)
                    {
                        // Use SpinWait to keep control of current thread. Using Sleep could potentially
                        // cause other events to get run out of order
                        System.Threading.Thread.SpinWait(500);
                    }
                    sw.Stop();
                    */

                    device.IsRemoved = true;
                    device.Synced = false;
                    host.DS4Controllers[ind] = null;
                    _oscService.ResetDeviceState(ind);
                    //eventDispatcher.Invoke(() =>
                    //{
                    host.SlotManager.RemoveController(device, ind);
                    if (_appSettings.UseOscSender)
                    {
                        _oscService.SendPlug(ind, false);
                    }
                    //});

                    host.TouchPads[ind] = null;
                    _latencyMonitor.Reset(ind);
                    _profileSettings.UseDInputOnlyArray[ind] = true;
                    ActiveOutDevType[ind] = OutContType.None;
                    /* Leave up to Auto Profile system to change the following flags? */
                    //Global.useTempProfile[ind] = false;
                    //Global.tempprofilename[ind] = string.Empty;
                    //Global.tempprofileDistance[ind] = false;

                    //Thread.Sleep(XINPUT_UNPLUG_SETTLE_TIME);
                }
            }
        }

        public void OnSyncChange(object sender, EventArgs e)
        {
            IDeviceSlotHost host = _host();
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = _controllerSlotLimit; ind == -1 && i < arlength; i++)
            {
                DS4Device tempDev = host.DS4Controllers[i];
                if (tempDev != null && device == tempDev)
                    ind = i;
            }

            if (ind >= 0)
            {
                bool synced = device.isSynced();

                if (!synced)
                {
                    if (!_profileSettings.UseDInputOnlyArray[ind])
                    {
                        ActiveOutDevType[ind] = OutContType.None;
                        _virtualPad.UnplugOutDev(ind, device);
                    }
                }
                else
                {
                    if (!_profileSettings.GetDInputOnly(ind))
                    {
                        host.TouchPads[ind].ReplaceOneEuroFilterPair();
                        //host.TouchPads[ind].ReplaceOneEuroFilterPair();

                        host.TouchPads[ind].Cursor.ReplaceOneEuroFilterPair();
                        host.TouchPads[ind].Cursor.SetupLateOneEuroFilters();
                        _virtualPad.PluginOutDev(ind, device);
                    }
                }
            }
        }

        public void OnSerialChange(object sender, EventArgs e)
        {
            IDeviceSlotHost host = _host();
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = ControllerLimits.MaxControllerCount; ind == -1 && i < arlength; i++)
            {
                DS4Device tempDev = host.DS4Controllers[i];
                if (tempDev != null && device == tempDev)
                    ind = i;
            }

            if (ind >= 0)
            {
                _deviceStateService.OnDeviceSerialChange(host, ind, device.getMacAddress());
            }
        }
    }
}
