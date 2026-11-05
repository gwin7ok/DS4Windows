using System;
using System.Diagnostics;
using System.IO;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase13-Step5b: <see cref="IInputReportProcessor"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService.On_Report</c> の本文を、フィールド名の置換だけでそのまま移したもの（呼び出しの順序・条件・ログは変えていない）。
    /// ホットパス（1 レポートごとに呼ばれる）のため、スロット状態の配列の参照はコンストラクターで一度だけ取得して <c>readonly</c> で保持し、
    /// 1 回あたりの新規割り当てを増やさない（計測: <c>Phase13Step5bOnReportTimingBenchmark</c>、特性: <c>Phase13Step5bOnReportCharacterizationTests</c>）。
    /// </summary>
    public sealed class InputReportProcessor : IInputReportProcessor
    {
        private readonly DS4State[] _currentState;
        private readonly DS4State[] _tempState;
        private readonly Mouse[] _touchPad;
        private readonly DS4State[] _mappedState = new DS4State[ControllerLimits.MaxControllerCount];
        private readonly DS4StateExposed[] _exposedState = new DS4StateExposed[ControllerLimits.MaxControllerCount];
        private readonly double[] _processingDelayMs = new double[ControllerLimits.MaxControllerCount];

        private readonly ILatencyMonitor _latencyMonitor;
        private readonly IPathService _pathService;
        private readonly IProfileRepository _profileRepository;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IVirtualKBM _virtualKBM;
        private readonly IMapping _mapping;
        private readonly ITouchpadToggleService _touchpadToggle;
        private readonly IProfileActionProvider _profileActionProvider;
        private readonly IAppSettingsService _appSettings;
        private readonly IOscService _oscService;
        private readonly IVirtualPadService _virtualPad;

        // Phase13-Step7 7-c: 旧: 遅延取得デリゲート（遅延取得）。配列の参照は再代入されないため、コンストラクターで保持する（ホットパスは読み取りだけ）
        private readonly OutContType[] _activeOutDevType;
        private OutContType[] ActiveOutDevType => _activeOutDevType;

        public bool RecordingMacro { get; set; }
        public bool IsMeasuringProcessingDelay { get; set; }
        public double[] ProcessingDelayMs => _processingDelayMs;
        public DS4StateExposed[] ExposedState => _exposedState;

        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public InputReportProcessor(DS4Windows.Core.Model.LightbarOverrides lightbar, 
            IDeviceSlotState slots,
            ILatencyMonitor latencyMonitor,
            IPathService pathService,
            IProfileRepository profileRepository,
            IAppearanceSettingsService appearanceSettings,
            IProfileSettingsService profileSettings,
            IVirtualKBM virtualKBM,
            IMapping mapping,
            ITouchpadToggleService touchpadToggle,
            IProfileActionProvider profileActionProvider,
            IAppSettingsService appSettings,
            IOscService oscService,
            IVirtualPadService virtualPad,
            IOutputDeviceTypeState outputDeviceTypes)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            _currentState = slots.CurrentStates;
            _tempState = slots.TempStates;
            _touchPad = slots.TouchPads;
            _latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _touchpadToggle = touchpadToggle ?? throw new ArgumentNullException(nameof(touchpadToggle));
            _profileActionProvider = profileActionProvider ?? throw new ArgumentNullException(nameof(profileActionProvider));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _activeOutDevType = (outputDeviceTypes ?? throw new ArgumentNullException(nameof(outputDeviceTypes))).ActiveOutDevType;

            // 旧 ControlService のコンストラクターの for ループ（MappedState／CurrentState／ExposedState）を、ここへまとめた。
            // ExposedState は CurrentStates[i] を参照するため、同じ場所で、CurrentStates[i] を作った後に作る（順序は旧と同じ）
            for (int i = 0; i < _currentState.Length; i++)
            {
                _mappedState[i] = new DS4State();
                _currentState[i] = new DS4State();
                _exposedState[i] = new DS4StateExposed(_currentState[i]);
            }
        }

        public DS4State GetMappedState(int ind) => _mappedState[ind];

        public double GetProcessingDelay(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _processingDelayMs.Length)
                return _processingDelayMs[deviceIndex];
            return 0.0;
        }

        public void OnReport(DS4Device device, EventArgs e, int ind)
        {
            if (ind != -1)
            {
                _latencyMonitor.OnReport(device, ind);

                DS4State cState, tempControlState;
                if (!device.PerformStateMerge)
                {
                    cState = _currentState[ind];
                    device.getRawCurrentState(cState);
                    tempControlState = _currentState[ind];
                }
                else
                {
                    cState = device.JointState;
                    device.MergeStateData(cState);
                    // Need to copy state object info for use in UDP server
                    cState.CopyTo(_currentState[ind]);
                    tempControlState = _currentState[ind];
                }

                DS4State pState = device.getPreviousStateRef();
                //device.getPreviousState(PreviousState[ind]);
                //DS4State pState = PreviousState[ind];


                if (device.firstReport && device.isSynced())
                {
                    // Only send Log message when device is considered a primary device
                    if (device.PrimaryDevice)
                    {
                        // Emit missing-action logs once per profile-apply (respect suppression).
                        // Note: Profile logging is already done by ApplyProfile in PrepareConnectedInputControllerSettingEvents
                        if (File.Exists(Path.Combine(_pathService.AppDataPath, "Profiles", $"{_profileRepository.ProfilePath[ind]}.xml")))
                        {
                            try
                            {
                                _profileRepository.EmitMissingActionLogsForDevice(ind, false);
                            }
                            catch { }
                        }
                    }

                    device.firstReport = false;
                }

                if (device.PrimaryDevice && _appearanceSettings.UseIconChoice == TrayIconChoice.Battery)
                {
                    _appearanceSettings.InvokeBatteryChanged(cState.Battery);
                }

                if (!device.PrimaryDevice)
                {
                    // Make sure a joined device is still linked
                    int jointInd = device.JointDeviceSlotNumber;
                    if (device.OutputMapGyro &&
                        jointInd != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
                    {
                        // Output changes from Gyro data early. Seems better to ME... REE
                        GyroOutMode imuOutMode = _profileSettings.GetGyroOutMode(device.JointDeviceSlotNumber);
                        if (imuOutMode != GyroOutMode.None)
                        {
                            if (imuOutMode == GyroOutMode.Mouse)
                            {
                                _virtualKBM.Sync();
                            }
                            else if (imuOutMode == GyroOutMode.MouseJoystick)
                            {
                                // Add new Mapping method and add data to
                                // parent device state
                                DS4State tempMapState = _mappedState[jointInd];
                                _mapping.TempMouseJoystick(jointInd, tempMapState);
                                if (!_profileSettings.UseDInputOnlyArray[jointInd])
                                {
                                    _virtualPad.OutputDevices[jointInd]?.ConvertandSendReport(tempMapState, jointInd);
                                }
                            }
                        }
                    }
                    else if (!device.OutputMapGyro)
                    {
                        // Copy for use in UDP
                        tempControlState.Motion = device.GetRawCurrentStateRef().Motion;
                    }

                    // Skip mapping routine if part of a joined device
                    return;
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
                    DS4State tempMapState = _mappedState[ind];

                    if (_appSettings.UseOscSender)
                    {
                        _oscService.PreMappingStep(ind, cState, tempMapState);
                    }

                    _mapping.MapCustom(ind, cState, tempMapState, _exposedState[ind], _touchPad[ind]);

                    // Copy current Touchpad and Gyro data
                    // Might change to use new DS4State.CopyExtrasTo method
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
                    // Perform this virtual trigger button check in post
                    if (ActiveOutDevType[ind] == OutContType.DS4)
                    {
                        DS4TriggerOutputMode trigMode = _profileSettings.OutputDS4TriggerMode[ind];
                        if (trigMode == DS4TriggerOutputMode.Default)
                        {
                            cState.L2Btn = cState.L2 > 0;
                            cState.R2Btn = cState.R2 > 0;
                        }
                        else if (trigMode == DS4TriggerOutputMode.Buttons)
                        {
                            cState.L2Btn = cState.L2 > 0;
                            cState.R2Btn = cState.R2 > 0;
                            // Disable analog output
                            cState.L2 = 0;
                            cState.R2 = 0;
                        }
                    }

                    _virtualPad.OutputDevices[ind]?.ConvertandSendReport(cState, ind);

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
                // Phase9 Steering 削除 W3: 旧「出力デバイスがないとき［UseDInputOnly］の、ステアリングの vJoy 軸への送出」（else 側）は、機能ごと削除した（vJoy も削除）。

                // Output any synthetic events.
                _mapping.Commit(ind, _profileSettings);

                // Update the Lightbar color
                DS4LightBar.updateLightBar(device, ind, _profileSettings, _lightbar);

                if (device.PerformStateMerge)
                {
                    device.PreserveMergedStateData();
                }

                if (device.PerformStateMerge && !device.OutputMapGyro)
                {
                    // Copy for use in UDP
                    tempControlState.Motion = device.GetRawCurrentStateRef().Motion;
                }
            }
        }
    }
}
