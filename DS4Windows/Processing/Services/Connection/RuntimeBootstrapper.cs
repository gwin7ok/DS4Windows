using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// <see cref="IRuntimeBootstrapper"/> の実装（DI の Singleton）。
    /// 3 つのフックは、互いの生成の順序にだけ依存するため、コンストラクターではなく <see cref="Initialize"/> で、サービスの開始より前に結ぶ。
    /// </summary>
    public class RuntimeBootstrapper : IRuntimeBootstrapper
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly StickMotionState _stickMotion;
        private readonly IDeviceSlotState _slots;
        private readonly IDeviceRegistryHooks _deviceRegistryHooks;
        private readonly IAppliedProfileRecorder _appliedProfileRecorder;
        private readonly IBackendFailureHandler _backendFailureHandler;
        private readonly IBatteryStatusLogger _batteryStatusLogger;
        private readonly IVirtualPadService _virtualPad;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IEventThreadDispatcher _eventThread;
        private readonly object _gate = new object();
        private bool _initialized;

        public RuntimeBootstrapper(
            IProfileSettingsService profileSettings,
            StickMotionState stickMotion,
            IDeviceSlotState slots,
            IDeviceRegistryHooks deviceRegistryHooks,
            IAppliedProfileRecorder appliedProfileRecorder,
            IBackendFailureHandler backendFailureHandler,
            IBatteryStatusLogger batteryStatusLogger,
            IVirtualPadService virtualPad,
            IOutputSlotService outputSlotService,
            IEventThreadDispatcher eventThread)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(stickMotion);
            _stickMotion = stickMotion;
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(deviceRegistryHooks);
            _deviceRegistryHooks = deviceRegistryHooks;
            ArgumentNullException.ThrowIfNull(appliedProfileRecorder);
            _appliedProfileRecorder = appliedProfileRecorder;
            ArgumentNullException.ThrowIfNull(backendFailureHandler);
            _backendFailureHandler = backendFailureHandler;
            ArgumentNullException.ThrowIfNull(batteryStatusLogger);
            _batteryStatusLogger = batteryStatusLogger;
            ArgumentNullException.ThrowIfNull(virtualPad);
            _virtualPad = virtualPad;
            ArgumentNullException.ThrowIfNull(outputSlotService);
            _outputSlotService = outputSlotService;
            ArgumentNullException.ThrowIfNull(eventThread);
            _eventThread = eventThread;
        }

        public void Initialize()
        {
            lock (_gate)
            {
                if (_initialized) return;
                _initialized = true;
            }

            Crc32Algorithm.InitializeTable(InputDeviceCore.DefaultPolynomial);

            for (int i = 0, length = _slots.Controllers.Length; i < length; i++)
            {
                int slot = i;
                _profileSettings.ForSlot(slot).Triggers.L2Output.TwoStageModeChanged += (sender, e) =>
                {
                    _stickMotion.L2TwoStageMappingData[slot].Reset();
                };

                _profileSettings.ForSlot(slot).Triggers.R2Output.TwoStageModeChanged += (sender, e) =>
                {
                    _stickMotion.R2TwoStageMappingData[slot].Reset();
                };
            }

            // 適用のたびに、そのスロットのコントローラー（MAC）に、適用したプロファイルを記憶する（再接続で使う）
            _appliedProfileRecorder.Attach();
            _deviceRegistryHooks.Attach();
            _backendFailureHandler.Attach();
            _batteryStatusLogger.Attach();
        }

        public void Shutdown()
        {
            _virtualPad.ShutDownOutputSlots();
            _outputSlotService.SaveOutputSlots();
            _eventThread.Shutdown();
        }
    }
}
