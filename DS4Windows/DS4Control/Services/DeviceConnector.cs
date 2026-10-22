using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6f1（Phase9-E6-Plan.md §9.2）: <see cref="IDeviceConnector"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>PrepareConnectedInputControllerSettingEvents</c>／<c>BeginPrepareConnectedInputController</c>／<c>CheckQuickCharge</c> を、そのまま移したもの
    /// （E-6a〜E-6e で部品へ移した処理は、その部品を呼ぶ。スロットの配列は <see cref="IDeviceSlotHost"/> から、呼び出しのたびに引く。呼び出しの順序は変えない）。
    /// </summary>
    public sealed class DeviceConnector : IDeviceConnector
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IAppSettingsService _appSettings;
        private readonly IOscService _oscService;
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly IDeviceDisconnectHandler _deviceDisconnect;
        private readonly IVirtualKBM _virtualKBM;
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        private readonly IConnectionProfileSelector _connectionProfileSelector;
        private readonly IVirtualPadService _virtualPad;
        private readonly Func<IOutputSlotService> _outputSlotService;
        private readonly IDeviceConnectionSetup _deviceConnectionSetup;
        private readonly IInputDeviceModuleRegistry _deviceModules;
        private readonly IUdpMotionService _udpMotion;
        private readonly IHidHideService _hidHideService;
        private readonly Func<IDeviceSlotHost> _host;

        public DeviceConnector(IProfileSettingsService profileSettings, IProfileXmlStore profileXmlStore, IAppSettingsService appSettings,
            IOscService oscService, IDs4DeviceRegistry deviceRegistry, IDeviceDisconnectHandler deviceDisconnect, IVirtualKBM virtualKBM,
            StickMotionState stickMotion, SyntheticInputState syntheticInput, IConnectionProfileSelector connectionProfileSelector,
            IVirtualPadService virtualPad, Func<IOutputSlotService> outputSlotService, IDeviceConnectionSetup deviceConnectionSetup,
            IInputDeviceModuleRegistry deviceModules, IUdpMotionService udpMotion, IHidHideService hidHideService, Func<IDeviceSlotHost> host)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
            _deviceDisconnect = deviceDisconnect ?? throw new ArgumentNullException(nameof(deviceDisconnect));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _connectionProfileSelector = connectionProfileSelector ?? throw new ArgumentNullException(nameof(connectionProfileSelector));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _deviceConnectionSetup = deviceConnectionSetup ?? throw new ArgumentNullException(nameof(deviceConnectionSetup));
            _deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）。旧 ControlService と同じ配列を指す
        private OutContType[] ActiveOutDevType => _outputSlotService().ActiveOutDevType;

        public void BeginPrepare(DS4Device device, bool showlog = false)
        {
            if (_hidHideService.IsAffected(device))
            {
                //device.CurrentExclusiveStatus = DS4Device.ExclusiveStatus.HidGuardAffected;
                _hidHideService.MarkAffected(device);
            }

            //Task task = new Task(() => { Thread.Sleep(5); WarnExclusiveModeFailure(device); });
            //task.Start();

            _deviceModules.Resolve(device.DeviceType).HookSettings(device, () => _host().ActiveControllers);
        }

        public void PrepareConnected(int numControllers, DS4Device device, int index)
        {
            IDeviceSlotHost host = _host();
            Mouse[] touchPads = host.TouchPads;
            _profileSettings.RefreshExtrasButtons(index, _deviceModules.Resolve(device.DeviceType).GetKnownExtraButtons(device));
            _profileXmlStore.LoadControllerConfigsForDevice(device);
            device.LoadStoreSettings();
            device.CheckControllerNumDeviceSettings(numControllers);

            host.SlotManager.AddController(device, index);
            if (_appSettings.UseOscSender)
            {
                _oscService.SendPlug(index, true);
            }
            device.Removal += _deviceDisconnect.OnRemoval;
            device.Removal += _deviceRegistry.OnRemoval;
            device.SyncChange += _deviceDisconnect.OnSyncChange;
            device.SyncChange += _deviceRegistry.UpdateSerial;
            device.SerialChange += _deviceDisconnect.OnSerialChange;
            device.ChargingChanged += OnChargingChanged;

            touchPads[index] = new Mouse(index, device, _profileSettings, _virtualKBM, _stickMotion, _syntheticInput);
            // Phase7-Step5 W1: DS3 のジャイロトリガー（Controls モード）の判定を、このスロットの Mouse へつなぐ。
            // DS4SixAxis は ControlService を直接参照しない（旧: Program.rootHub。DI 移行後は参照が失われていた）。
            device.SixAxis.GyroControlsTriggerActive =
                slot => touchPads[slot]?.IsGyroTriggerActive(GyroOutMode.Controls) ?? false;
            bool profileLoaded = false;
            bool useAutoProfile = _profileSettings.GetUseTempProfile(index);

            // Phase9 9-E E-6a: プロファイルの選択と適用（初回接続／再接続、Linked、一時プロファイル）は ConnectionProfileSelector へ移した
            bool isValidSerial = device.isValidSerial();
            profileLoaded = _connectionProfileSelector.ApplyOnConnect(index, useAutoProfile, isValidSerial, isValidSerial ? device.getMacAddress() : null);

            if (profileLoaded || useAutoProfile)
            {
                device.LightBarColor = _profileSettings.GetMainColor(index);

                if (!_profileSettings.GetDInputOnly(index) && device.isSynced())
                {
                    if (device.PrimaryDevice)
                    {
                        _virtualPad.PluginOutDev(index, device);
                    }
                    else if (device.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
                    {
                        int otherIdx = device.JointDeviceSlotNumber;
                        OutputDevice tempOutDev = _virtualPad.OutputDevices[otherIdx];
                        if (tempOutDev != null)
                        {
                            OutContType tempConType = ActiveOutDevType[otherIdx];
                            _virtualPad.EstablishOutFeedback(index, tempConType, tempOutDev, device);
                            _virtualPad.OutputDevices[index] = tempOutDev;
                            ActiveOutDevType[index] = tempConType;
                        }
                    }
                }
                else
                {
                    _profileSettings.UseDInputOnlyArray[index] = true;
                    ActiveOutDevType[index] = OutContType.None;
                }

                if (device.PrimaryDevice && device.OutputMapGyro)
                {
                    _deviceConnectionSetup.AttachTouchpad(index, device, touchPads);
                }
                else if (device.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
                {
                    int otherIdx = device.JointDeviceSlotNumber;
                    DS4Device tempDev = host.DS4Controllers[otherIdx];
                    if (tempDev != null)
                    {
                        int mappedIdx = tempDev.PrimaryDevice ? otherIdx : index;
                        DS4Device gyroDev = device.OutputMapGyro ? device : (tempDev.OutputMapGyro ? tempDev : null);
                        if (gyroDev != null)
                        {
                            _deviceConnectionSetup.AttachTouchpad(mappedIdx, gyroDev, touchPads);
                        }
                    }
                }

                _deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);
                _deviceConnectionSetup.WireInitialHooks(index, device);
            }

            int tempIdx = index;
            device.Report += (sender, e) =>
            {
                host.OnReport(sender, e, tempIdx);
            };

            if (_udpMotion.HasServer && index < _udpMotion.SlotCount)
            {
                _udpMotion.PrepareDeviceMotion(device, tempIdx);
            }

            device.StartUpdate();
        }

        private void OnChargingChanged(object sender, EventArgs e)
        {
            DS4Device device = sender as DS4Device;
            if (device.ConnectionType == ConnectionType.BT && _appSettings.QuickCharge &&
                device.Charging)
            {
                // Set disconnect flag here. Later Hotplug event will check
                // for presence of flag and remove the device then
                device.ReadyQuickChargeDisconnect = true;
            }
        }
    }
}
