using System;
using Sensorit.Base;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
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
            IVirtualPadService virtualPad,
            IOutputSlotService outputSlotService,
            IEventThreadDispatcher eventThread)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _deviceRegistryHooks = deviceRegistryHooks ?? throw new ArgumentNullException(nameof(deviceRegistryHooks));
            _appliedProfileRecorder = appliedProfileRecorder ?? throw new ArgumentNullException(nameof(appliedProfileRecorder));
            _backendFailureHandler = backendFailureHandler ?? throw new ArgumentNullException(nameof(backendFailureHandler));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _eventThread = eventThread ?? throw new ArgumentNullException(nameof(eventThread));
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
        }

        public void Shutdown()
        {
            _virtualPad.ShutDownOutputSlots();
            _outputSlotService.SaveOutputSlots();
            _eventThread.Shutdown();
        }
    }
}
