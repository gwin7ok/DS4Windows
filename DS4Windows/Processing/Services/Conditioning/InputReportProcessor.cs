using DS4Windows.Processing.Touchpad;
using System;
using System.Diagnostics;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;

namespace DS4Windows.Processing.Services.Conditioning
{
    /// <summary>
    /// <see cref="IInputReportProcessor"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService.On_Report</c> の本文を、フィールド名の置換だけでそのまま移したもの（呼び出しの順序・条件・ログは変えていない）。
    /// ホットパス（1 レポートごとに呼ばれる）のため、スロット状態の配列の参照はコンストラクターで一度だけ取得して <c>readonly</c> で保持し、
    /// 1 回あたりの新規割り当てを増やさない（計測: 対応するテスト）。
    /// </summary>
    public sealed class InputReportProcessor : IInputReportProcessor
    {
        private readonly ControllerState[] _currentState;
        private readonly ControllerState[] _tempState;
        private readonly TouchpadMouse[] _touchPad;
        private readonly ControllerState[] _mappedState = new ControllerState[ControllerLimits.MaxControllerCount];
        private readonly ControllerStateExposed[] _exposedState = new ControllerStateExposed[ControllerLimits.MaxControllerCount];
        private readonly double[] _processingDelayMs = new double[ControllerLimits.MaxControllerCount];

        private readonly ILatencyMonitor _latencyMonitor;
        private readonly FirstReportActionLogger _firstReportLogger;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMapping _mapping;
        private readonly ITouchpadToggleService _touchpadToggle;
        private readonly IProfileActionProvider _profileActionProvider;
        private readonly IAppSettingsService _appSettings;
        private readonly IOscService _oscService;
        private readonly IVirtualPadService _virtualPad;

        public bool RecordingMacro { get; set; }
        public bool IsMeasuringProcessingDelay { get; set; }
        public double[] ProcessingDelayMs => _processingDelayMs;
        public ControllerStateExposed[] ExposedState => _exposedState;

        // ライトバーの強制表示の状態
        private readonly ILightbarService _lightbar;

        public InputReportProcessor(ILightbarService lightbar, 
            IDeviceSlotState slots,
            ILatencyMonitor latencyMonitor,
            FirstReportActionLogger firstReportLogger,
            IAppearanceSettingsService appearanceSettings,
            IProfileSettingsService profileSettings,
            IMapping mapping,
            ITouchpadToggleService touchpadToggle,
            IProfileActionProvider profileActionProvider,
            IAppSettingsService appSettings,
            IOscService oscService,
            IVirtualPadService virtualPad)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            _currentState = slots.CurrentStates;
            _tempState = slots.TempStates;
            _touchPad = slots.TouchPads;
            ArgumentNullException.ThrowIfNull(latencyMonitor);
            _latencyMonitor = latencyMonitor;
            ArgumentNullException.ThrowIfNull(firstReportLogger);
            _firstReportLogger = firstReportLogger;
            ArgumentNullException.ThrowIfNull(appearanceSettings);
            _appearanceSettings = appearanceSettings;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(mapping);
            _mapping = mapping;
            ArgumentNullException.ThrowIfNull(touchpadToggle);
            _touchpadToggle = touchpadToggle;
            ArgumentNullException.ThrowIfNull(profileActionProvider);
            _profileActionProvider = profileActionProvider;
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(oscService);
            _oscService = oscService;
            ArgumentNullException.ThrowIfNull(virtualPad);
            _virtualPad = virtualPad;

            // CurrentStates[i] は DeviceSlotState が作る。ExposedState は、それを参照して作る
            for (int i = 0; i < _currentState.Length; i++)
            {
                _mappedState[i] = new ControllerState();
                _exposedState[i] = new ControllerStateExposed(_currentState[i]);
            }
        }

        public ControllerState GetMappedState(int ind) => _mappedState[ind];

        public double GetProcessingDelay(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _processingDelayMs.Length)
                return _processingDelayMs[deviceIndex];
            return 0.0;
        }

        public void OnReport(InputDeviceCore device, EventArgs e, int ind)
        {
            if (ind != -1)
            {
                _latencyMonitor.OnReport(device, ind);

                ControllerState cState = _currentState[ind];
                device.GetRawCurrentState(cState);

                ControllerState pState = device.GetPreviousStateRef();
                //device.getPreviousState(PreviousState[ind]);
                //ControllerState pState = PreviousState[ind];


                if (device.firstReport && device.IsSynced())
                {
                    // Emit missing-action logs once per profile-apply (respect suppression).
                    // Note: Profile logging is already done by ApplyProfile in PrepareConnectedInputControllerSettingEvents
                    _firstReportLogger.Emit(ind);

                    device.firstReport = false;
                }

                if (_appearanceSettings.UseIconChoice == TrayIconChoice.Battery)
                {
                    _appearanceSettings.InvokeBatteryChanged(cState.Battery);
                }

                if (_profileSettings.GetEnableTouchToggle(ind))
                {
                    _touchpadToggle.CheckForToggle(ind, cState, pState);
                }

                cState = _mapping.SetCurveAndDeadzone(ind, cState, _tempState[ind], _profileSettings);

                if (!RecordingMacro && (_profileSettings.GetUseTempProfile(ind) ||
                    _profileSettings.ContainsCustomAction(ind) || _profileSettings.ContainsCustomExtras(ind) ||
                    _profileActionProvider.GetProfileActionCount(ind) > 0))
                {
                    ControllerState tempMapState = _mappedState[ind];

                    if (_appSettings.UseOscSender)
                    {
                        _oscService.PreMappingStep(ind, cState, tempMapState);
                    }

                    _mapping.MapCustom(ind, cState, tempMapState, _exposedState[ind], _touchPad[ind]);

                    // Copy current Touchpad and Gyro data
                    // Might change to use new ControllerState.CopyExtrasTo method
                    tempMapState.Motion = cState.Motion;
                    tempMapState.ds4Timestamp = cState.ds4Timestamp;
                    tempMapState.FrameCounter = cState.FrameCounter;
                    tempMapState.TouchPacketCounter = cState.TouchPacketCounter;
                    tempMapState.TrackPadTouch0 = cState.TrackPadTouch0;
                    tempMapState.TrackPadTouch1 = cState.TrackPadTouch1;

                    if (_appSettings.UseOscServer)
                    {
                        _oscService.PostMappingStep(ind, tempMapState);
                    }

                    cState = tempMapState;

                }

                if (!_profileSettings.UseDInputOnlyArray[ind])
                {
                    _virtualPad.SendReport(ind, cState);

                    // 仮想コントローラー出力完了時の処理遅延を計測
                    if (IsMeasuringProcessingDelay)
                    {
                        long start = device.lastInputReportTimestamp;
                        if (start > 0)
                        {
                            long end = Stopwatch.GetTimestamp();
                            _processingDelayMs[ind] = (end - start) * (1000.0 / Stopwatch.Frequency);
                        }
                    }
                    //testNewReport(ref x360reports[ind], cState, ind);
                    //x360controls[ind]?.SendReport(x360reports[ind]);

                    //x360Bus.Parse(cState, processingData[ind].Report, ind);
                    // We push the translated Xinput state, and simultaneously we
                    // pull back any possible rumble data coming from Xinput consumers.
                    /*if (x360Bus.Report(processingData[ind].Report, processingData[ind].Rumble))
                    {
                        byte Big = processingData[ind].Rumble[3];
                        byte Small = processingData[ind].Rumble[4];

                        if (processingData[ind].Rumble[1] == 0x08)
                        {
                            _rumble.SetDevRumble(device, Big, Small, ind);
                        }
                    }
                    */
                }

                // Output any synthetic events.
                _mapping.Commit(ind, _profileSettings);

                // Update the Lightbar color
                _lightbar.Update(device, ind);
            }
        }
    }
}
