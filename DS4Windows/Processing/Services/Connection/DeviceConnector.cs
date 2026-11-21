using DS4Windows.Processing.Touchpad;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// <see cref="IDeviceConnector"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>PrepareConnectedInputControllerSettingEvents</c>／<c>BeginPrepareConnectedInputController</c>／<c>CheckQuickCharge</c> を、そのまま移したもの
    /// （E-6a〜E-6e で部品へ移した処理は、その部品を呼ぶ。スロットの配列と接続中の数は <see cref="IDeviceSlotState"/>、入力レポートの処理は <see cref="IInputReportProcessor"/> を直接注入で受ける［Phase13-Step5b。旧: <c>Func&lt;IDeviceSlotHost&gt;</c>］。呼び出しの順序は変えない）。
    /// </summary>
    public sealed class DeviceConnector : IDeviceConnector
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IAppSettingsService _appSettings;
        private readonly DeviceLifecycleHooks _lifecycleHooks;
        private readonly IConnectionProfileSelector _connectionProfileSelector;
        private readonly IDeviceConnectionSetup _deviceConnectionSetup;
        private readonly IInputDeviceModuleRegistry _deviceModules;
        private readonly IHidHideService _hidHideService;
        private readonly IDeviceSlotState _slots;   // スロットの配列（直接注入）
        private readonly IInputReportProcessor _inputReport;   // 旧 Func<IDeviceSlotHost>.OnReport（入力レポートの処理）
        private readonly ITouchpadMouseFactory _mouseFactory;
        private readonly IConnectedDeviceOutputs _outputs;

        public DeviceConnector(IProfileSettingsService profileSettings, IProfileXmlStore profileXmlStore, IAppSettingsService appSettings,
            DeviceLifecycleHooks lifecycleHooks, IConnectionProfileSelector connectionProfileSelector,
            IDeviceConnectionSetup deviceConnectionSetup, IInputDeviceModuleRegistry deviceModules, IHidHideService hidHideService,
            IDeviceSlotState slots, IInputReportProcessor inputReport, ITouchpadMouseFactory mouseFactory, IConnectedDeviceOutputs outputs)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(profileXmlStore);
            _profileXmlStore = profileXmlStore;
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(lifecycleHooks);
            _lifecycleHooks = lifecycleHooks;
            ArgumentNullException.ThrowIfNull(connectionProfileSelector);
            _connectionProfileSelector = connectionProfileSelector;
            ArgumentNullException.ThrowIfNull(deviceConnectionSetup);
            _deviceConnectionSetup = deviceConnectionSetup;
            ArgumentNullException.ThrowIfNull(deviceModules);
            _deviceModules = deviceModules;
            ArgumentNullException.ThrowIfNull(hidHideService);
            _hidHideService = hidHideService;
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(inputReport);
            _inputReport = inputReport;
            ArgumentNullException.ThrowIfNull(mouseFactory);
            _mouseFactory = mouseFactory;
            ArgumentNullException.ThrowIfNull(outputs);
            _outputs = outputs;
        }

        public void BeginPrepare(InputDeviceCore device, bool showlog = false)
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

        public void PrepareConnected(int numControllers, InputDeviceCore device, int index)
        {
            IInputReportProcessor inputReport = _inputReport;
            IDeviceSlotState slots = _slots;
            TouchpadMouse[] touchPads = slots.TouchPads;
            _profileSettings.RefreshExtrasButtons(index, _deviceModules.Resolve(device.DeviceType).GetKnownExtraButtons(device));
            _profileXmlStore.LoadControllerConfigsForDevice(device);
            device.LoadStoreSettings();
            device.CheckControllerNumDeviceSettings(numControllers);

            slots.SlotManager.AddController(device, index);
            _outputs.NotifyPlugged(index);
            _lifecycleHooks.Attach(device);
            device.ChargingChanged += OnChargingChanged;

            touchPads[index] = _mouseFactory.Create(index, device);
            // プロファイルの適用のログ・通知が電池残量を使うため、その前に、最初の入力レポートから電池残量を取り込む
            // （StartUpdate はこのあとのため、読み取りのスレッドはまだ動いていない）
            device.PrimeBatteryReading();

            bool profileLoaded = false;
            bool useAutoProfile = _profileSettings.GetUseTempProfile(index);

            // プロファイルの選択と適用（初回接続／再接続、Linked、一時プロファイル）は ConnectionProfileSelector へ移した
            bool IsValidSerial = device.IsValidSerial();
            profileLoaded = _connectionProfileSelector.ApplyOnConnect(index, useAutoProfile, IsValidSerial, IsValidSerial ? device.GetMacAddress() : null);

            if (profileLoaded || useAutoProfile)
            {
                device.LightBarColor = _profileSettings.GetMainColor(index);

                _outputs.PlugVirtualPad(index, device);

                _deviceConnectionSetup.AttachTouchpad(index, device, touchPads);

                _deviceConnectionSetup.ApplyProfileOptions(index, device, touchPads[index]);
                _deviceConnectionSetup.WireInitialHooks(index, device);
            }

            int tempIdx = index;
            device.Report += (sender, e) =>
            {
                inputReport.OnReport(sender, e, tempIdx);
            };

            _outputs.PrepareMotion(tempIdx, device);

            device.StartUpdate();
        }

        private void OnChargingChanged(object? sender, EventArgs e)
        {
            InputDeviceCore? device = sender as InputDeviceCore;
            if (device!.ConnectionType == ConnectionType.BT && _appSettings.QuickCharge && // 実行中は設定済み（null なら従来どおり NullReferenceException）
                device.Charging)
            {
                // Set disconnect flag here. Later Hotplug event will check
                // for presence of flag and remove the device then
                device.ReadyQuickChargeDisconnect = true;
            }
        }
    }
}
