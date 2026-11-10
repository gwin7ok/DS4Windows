using System;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase9 9-E E-6e（Phase9-E6-Plan.md §3.4）: <see cref="IDeviceDisconnectHandler"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>On_DS4Removal</c>／<c>On_SyncChange</c>／<c>On_SerialChange</c> を、そのまま移したもの
    /// （スロットの配列は、<see cref="IDeviceSlotState"/> から受ける。Phase13-Step5 で直接注入に変更。旧: <c>Func&lt;IDeviceSlotHost&gt;</c>）。切断の後始末の順序は、旧コードのまま保つ（ガードレール: 切断時クリーンアップ）。
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
        private readonly OutContType[] _activeOutDevType;   // Phase13-Step7 7-c: 旧: 遅延取得デリゲート。配列の参照は再代入されない
        private readonly IDeviceSlotState _host;
        private readonly int _controllerSlotLimit;

        public DeviceDisconnectHandler(IProfileSettingsService profileSettings, IMapping mapping, IProfileSlotApplier profileSlotApplier,
            IOscService oscService, IAppSettingsService appSettings, ILatencyMonitor latencyMonitor, IDeviceStateService deviceStateService,
            IVirtualPadService virtualPad, IOutputDeviceTypeState outputDeviceTypes, IEnvironmentService environmentService,
            IDeviceSlotState host)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
            _deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _activeOutDevType = (outputDeviceTypes ?? throw new ArgumentNullException(nameof(outputDeviceTypes))).ActiveOutDevType;
            if (environmentService == null) throw new ArgumentNullException(nameof(environmentService));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // ActiveOutDevType の配列参照は再代入されない（要素書込みのみ）。旧 ControlService と同じ配列を指す
        private OutContType[] ActiveOutDevType => _activeOutDevType;

        public void OnRemoval(object sender, EventArgs e)
        {
            IDeviceSlotState host = _host;
            InputDeviceCore device = (InputDeviceCore)sender;
            int ind = -1;
            for (int i = 0, arlength = host.Controllers.Length; ind == -1 && i < arlength; i++)
            {
                if (host.Controllers[i] != null && device.getMacAddress() == host.Controllers[i].getMacAddress())
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

                    string removed = DS4Windows.Properties.Resources.ControllerWasRemoved.Replace("*Mac address*", (ind + 1).ToString());
                    if (device.getBattery() <= 20 &&
                        device.getConnectionType() == ConnectionType.BT && !device.isCharging())
                    {
                        removed += ". " + DS4Windows.Properties.Resources.ChargeController;
                    }

                    AppLogger.Log(AppLogLevel.Info, removed, LogOptions.UserFacing);
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
                    host.Controllers[ind] = null;
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
                    // Phase11 W6 K-W6-11（ユーザー承認済み仕様）: 切断したら一時プロファイルの状態は維持しない。
                    // 再接続では通常のルールでプロファイルが決まり、その後に AutoProfileService.CheckProfilesForConnectedController が
                    // 前面のプログラムに従って自動プロファイルを割り当てる（上のコメントアウトは、この仕様へ置き換えた）。
                    _profileSettings.SetTempProfileName(ind, string.Empty);
                    _profileSettings.SetUseTempProfile(ind, false);
                    _profileSettings.SetTempProfileDistance(ind, false);

                    //Thread.Sleep(XINPUT_UNPLUG_SETTLE_TIME);
                }
            }
        }

        public void OnSyncChange(object sender, EventArgs e)
        {
            IDeviceSlotState host = _host;
            InputDeviceCore device = (InputDeviceCore)sender;
            int ind = -1;
            for (int i = 0, arlength = _controllerSlotLimit; ind == -1 && i < arlength; i++)
            {
                InputDeviceCore tempDev = host.Controllers[i];
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
            IDeviceSlotState host = _host;
            InputDeviceCore device = (InputDeviceCore)sender;
            int ind = -1;
            for (int i = 0, arlength = ControllerLimits.MaxControllerCount; ind == -1 && i < arlength; i++)
            {
                InputDeviceCore tempDev = host.Controllers[i];
                if (tempDev != null && device == tempDev)
                    ind = i;
            }

            if (ind >= 0)
            {
                _deviceStateService.OnDeviceSerialChange(host, ind, device.getMacAddress());   // Phase13-Step5: 送り主は IDeviceSlotState（旧: ControlService）。購読者はいない
            }
        }
    }
}
