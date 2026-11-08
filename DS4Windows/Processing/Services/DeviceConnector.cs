using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase9 9-E E-6f1（Phase9-E6-Plan.md §9.2）: <see cref="IDeviceConnector"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>PrepareConnectedInputControllerSettingEvents</c>／<c>BeginPrepareConnectedInputController</c>／<c>CheckQuickCharge</c> を、そのまま移したもの
    /// （E-6a〜E-6e で部品へ移した処理は、その部品を呼ぶ。スロットの配列と接続中の数は <see cref="IDeviceSlotState"/>、入力レポートの処理は <see cref="IInputReportProcessor"/> を直接注入で受ける［Phase13-Step5b。旧: <c>Func&lt;IDeviceSlotHost&gt;</c>］。呼び出しの順序は変えない）。
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
        private readonly OutContType[] _activeOutDevType;   // Phase13-Step7 7-c: 旧: 遅延取得デリゲート。配列の参照は再代入されない
        private readonly IDeviceConnectionSetup _deviceConnectionSetup;
        private readonly IInputDeviceModuleRegistry _deviceModules;
        private readonly IUdpMotionService _udpMotion;
        private readonly IHidHideService _hidHideService;
        private readonly IDeviceSlotState _slots;   // Phase13-Step5: スロットの配列（直接注入）
        private readonly IInputReportProcessor _inputReport;   // Phase13-Step5b: 旧 Func<IDeviceSlotHost>.OnReport（入力レポートの処理）

        public DeviceConnector(IProfileSettingsService profileSettings, IProfileXmlStore profileXmlStore, IAppSettingsService appSettings,
            IOscService oscService, IDs4DeviceRegistry deviceRegistry, IDeviceDisconnectHandler deviceDisconnect, IVirtualKBM virtualKBM,
            StickMotionState stickMotion, SyntheticInputState syntheticInput, IConnectionProfileSelector connectionProfileSelector,
            IVirtualPadService virtualPad, IOutputDeviceTypeState outputDeviceTypes, IDeviceConnectionSetup deviceConnectionSetup,
            IInputDeviceModuleRegistry deviceModules, IUdpMotionService udpMotion, IHidHideService hidHideService, IDeviceSlotState slots, IInputReportProcessor inputReport)
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
            _activeOutDevType = (outputDeviceTypes ?? throw new ArgumentNullException(nameof(outputDeviceTypes))).ActiveOutDevType;
            _deviceConnectionSetup = deviceConnectionSetup ?? throw new ArgumentNullException(nameof(deviceConnectionSetup));
            _deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _inputReport = inputReport ?? throw new ArgumentNullException(nameof(inputReport));
        }

        // ActiveOutDevType の配列参照は再代入されない（要素書込みのみ）。旧 ControlService と同じ配列を指す
        private OutContType[] ActiveOutDevType => _activeOutDevType;

        public void BeginPrepare(DS4Device device, bool showlog = false)
        {
            if (_hidHideService.IsAffected(device))
            {
                //device.CurrentExclusiveStatus = DeviceExclusiveStatus.HidGuardAffected;
                _hidHideService.MarkAffected(device);
            }

            //Task task = new Task(() => { Thread.Sleep(5); WarnExclusiveModeFailure(device); });
            //task.Start();

            _deviceModules.Resolve(device.DeviceType).HookSettings(device, () => _slots.ActiveControllers);
        }

        public void PrepareConnected(int numControllers, DS4Device device, int index)
        {
            IInputReportProcessor inputReport = _inputReport;
            IDeviceSlotState slots = _slots;
            Mouse[] touchPads = slots.TouchPads;
            _profileSettings.RefreshExtrasButtons(index, _deviceModules.Resolve(device.DeviceType).GetKnownExtraButtons(device));
            _profileXmlStore.LoadControllerConfigsForDevice(device);
            device.LoadStoreSettings();
            device.CheckControllerNumDeviceSettings(numControllers);

            slots.SlotManager.AddController(device, index);
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
            // Phase11 W6 K-W6-3: プロファイルの適用のログ・通知が電池残量を使うため、その前に、最初の入力レポートから電池残量を取り込む
            // （StartUpdate はこのあとのため、読み取りのスレッドはまだ動いていない）
            device.PrimeBatteryReading();

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
                    DS4Device tempDev = slots.DS4Controllers[otherIdx];
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
                inputReport.OnReport(sender, e, tempIdx);
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
