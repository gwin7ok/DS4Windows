/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Control;
using Nefarius.ViGEm.Client;
using Sensorit.Base;
using SharpOSC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DS4WinWPF.DS4Forms;

namespace DS4Windows
{
#pragma warning disable CS0219 // some local variables are assigned but not read in legacy handlers
    public class ControlService : IInstanceIdentifiable, DS4Windows.Services.IDeviceStateAccessor, DI.IUdpMotionHost, DI.IVirtualPadHost
    {
        public int InstanceId => this.GetHashCode();
        // Might be useful for ScpVBus build
        public const int EXPANDED_CONTROLLER_COUNT = 8;
        public const int MAX_DS4_CONTROLLER_COUNT = ControllerLimits.MaxControllerCount;
        // Phase7-Step10e W2: 静的な互換シム CURRENT_DS4_CONTROLLER_LIMIT は削除した。値の SSOT は IEnvironmentService.ControllerSlotLimit / UsingMaxControllers。
        // ControlService 内部は注入値 _controllerSlotLimit を使用する（Phase6-Step2-Plan.md 決定O2/O3）。
        // Phase7-Step10b W4: USING_MAX_CONTROLLERS（静的シム）は、呼び出し元 3 箇所を IEnvironmentService.UsingMaxControllers の注入に替えて削除した。
        public DS4Device[] DS4Controllers = new DS4Device[MAX_DS4_CONTROLLER_COUNT];
        public DS4Device GetController(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= DS4Controllers.Length) return null;
            return DS4Controllers[deviceIndex];
        }
        public int activeControllers = 0;
        public Mouse[] touchPad = new Mouse[MAX_DS4_CONTROLLER_COUNT];
        public bool running = false;
        public bool loopControllers = true;
        public bool inServiceTask = false;
        private DS4State[] MappedState = new DS4State[MAX_DS4_CONTROLLER_COUNT];
        private DS4State[] CurrentState = new DS4State[MAX_DS4_CONTROLLER_COUNT];
        private DS4State[] PreviousState = new DS4State[MAX_DS4_CONTROLLER_COUNT];
        private DS4State[] TempState = new DS4State[MAX_DS4_CONTROLLER_COUNT];
        public DS4StateExposed[] ExposedState = new DS4StateExposed[MAX_DS4_CONTROLLER_COUNT];
        public ControllerSlotManager slotManager = new ControllerSlotManager();
        public bool recordingMacro = false;
        public event EventHandler<DebugEventArgs> Debug = null;
        bool[] buttonsdown = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        bool[] held = new bool[MAX_DS4_CONTROLLER_COUNT];
        int[] oldmouse = new int[MAX_DS4_CONTROLLER_COUNT] { -1, -1, -1, -1, -1, -1, -1, -1 };
        // Phase9 9-E E-4: 旧 公開フィールド outputDevices は VirtualPadService が持つ（同じ配列を返す窓口。呼び出し側は 9-F まで変えない）
        public OutputDevice[] outputDevices => _virtualPad.OutputDevices;

        // ControllerReadings タブ表示中のみ有効化される遅延計測用
        public bool IsMeasuringProcessingDelay = false;
        public double[] ProcessingDelayMs = new double[MAX_DS4_CONTROLLER_COUNT];
        public double GetProcessingDelay(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < ProcessingDelayMs.Length)
                return ProcessingDelayMs[deviceIndex];
            return 0.0;
        }
        // Phase9 9-E E-3: 旧 udpEuroPairAccel／udpEuroPairGyro（UDP モーションのスムージングのフィルター）は UdpMotionService へ移した
#pragma warning disable CS0169 // tempBusThread is intentionally unused currently
        Thread tempBusThread;
#pragma warning restore CS0169
        Thread eventDispatchThread;
        Dispatcher eventDispatcher;
        public bool suspending;

        // Phase9 9-E E-3: 旧 _udpServer／udpOutBuffers／udpChangeStatus などは UdpMotionService へ移した
        private readonly DI.IUdpMotionService _udpMotion;
        // Phase9 9-E E-4: 旧 outputslotMan／vigemTestClient／tempThread は VirtualPadService へ移した
        private readonly DI.IVirtualPadService _virtualPad;
        public DI.IVirtualPadService VirtualPadService => _virtualPad;

        // Phase 3 Followup Step F-2: DS4Devices static access routed through DI.
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly DI.IProfileSettingsService _profileSettings;

        // Phase6-Step2-1 (PR-1): Global 直接参照の解消。SSOT は Global/BackingStore のまま、
        // 薄い委譲サービス経由で参照する（Pure DI: コンストラクタ注入のみ。AppHost.Services は使用しない）。
        private readonly DI.IAppSettingsService _appSettings;
        private readonly DI.IEnvironmentService _environmentService;
        private readonly DI.IPathService _pathService;

        // Phase6-Step2-2 (PR-2)
        // TODO(技術的負債): OutputSlotService → ControlService の逆依存を避けるための遅延解決（決定D1）。
        // ControlService のコンストラクタ内では呼ばない。逆依存の除去は Phase6-Step5 で行い、その時点で直接注入へ戻す。
        private readonly Func<DI.IOutputSlotService> _outputSlotServiceFactory;
        private OutContType[] _activeOutDevType;
        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）ため、初回に1回だけ取得する。
        // 以降は null 合体の1分岐のみで、ホットパスでもアロケーションは発生しない。
        private OutContType[] ActiveOutDevType
            => _activeOutDevType ?? (_activeOutDevType = _outputSlotServiceFactory().ActiveOutDevType);
        private readonly DI.IProfileRepository _profileRepository;
        private readonly DI.IDeviceStateService _deviceStateService;
        private readonly DI.IProfileXmlStore _profileXmlStore;
        private readonly Services.IProfileSlotApplier _profileSlotApplier;

        // Phase6-Step2-3 (PR-3): KBM 出力の送出（IVirtualKBM）とハンドラのライフサイクル（IVirtualKBMLifecycle）を分離（決定D3）
        private readonly Services.IVirtualKBM _virtualKBM;
        private readonly Services.IVirtualKBMLifecycle _kbmLifecycle;

        // Phase6-Step2-5 (PR-5): 入力処理ホットパス（On_Report）で参照するサービス。
        // いずれも呼び出し1回あたりの新規オブジェクト割り当て・ログ・キャッシュを行わない（ゼロアロケーション方針）。
        private readonly DI.IAppearanceSettingsService _appearanceSettings;
        private readonly DI.IProfileActionProvider _profileActionProvider;
        // Phase6-Step3-2: Mapping.cs からの引数渡し（S3方針）用。ControlService 自体はこの値を使わない。
        private readonly DI.ISpecialActionRepository _specialActionRepository;
        // Phase7-Step5 W4（Q6＝A）: 旧 AppHost.GetService<IElevatedProcessLauncher>()。デバイス再有効化の昇格要求で使う
        private readonly Services.IElevatedProcessLauncher _elevatedProcessLauncher;
        // Phase7-Step8 W1: 旧 StickMotionState.Shared／SyntheticInputState.Shared（静的な窓口）。コンストラクターで受ける。
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        // Phase7-Step8 W2: 入力の評価の窓口（旧: Mapping の静的メソッド）。画面（ControllerReadingsControl、RecordBoxViewModel）も、これを経由して使う
        private readonly DI.IMapping _mapping;
        public DI.IMapping MappingService => _mapping;
        // コントローラースロット上限（現在接続台数ではない）。プロセス内で不変のためコンストラクタで1回だけ取得する。
        private readonly int _controllerSlotLimit;

        // ---- Phase6-Step3-2: Mapping.cs への引数渡し（S3方針）用の読み取り専用アクセサ ----
        // Mapping.cs の各メソッドは既に ControlService（ctrl）を引数として受け取っているため、
        // 新たな static Service Locator を追加せず、この既存の受け渡し経路を通じてサービスを渡す。
        internal DI.IProfileActionProvider ProfileActionProvider => _profileActionProvider;
        internal DI.IProfileXmlStore ProfileXmlStore => _profileXmlStore;
        internal DI.IProfileRepository ProfileRepository => _profileRepository;
        internal DI.ISpecialActionRepository SpecialActionRepository => _specialActionRepository;
        // Phase6-Step3-3: MapCustom の ButtonAbsMouseInfos（絶対マウス出力の座標系設定）参照を
        // ctrl 経由の引数渡しへ切り替えるために追加。既存の _profileSettings フィールドをそのまま公開するのみで、
        // 新規サービス・新規フィールドは追加していない。ButtonAbsMouseInfos 自体はその後の機能廃止
        // （Abs Mouse機能削除、copilot-instructions.md §2.2 例外規定）で無くなったが、本プロパティは
        // Step3-6（GetControlSettingsGroup 等）で再利用する汎用アクセサとして存置している。
        internal DI.IProfileSettingsService ProfileSettingsService => _profileSettings;

        // Phase9 9-E E-2: 旧 hidDeviceHiding*（HidHide の状態）は HidHideService へ移した
        private readonly DI.IHidHideService _hidHideService;

        private ControlServiceDeviceOptions deviceOptions;
        public ControlServiceDeviceOptions DeviceOptions { get => deviceOptions; }

        private DS4WinWPF.ArgumentParser cmdParser;

        public event EventHandler ServiceStarted;
        public event EventHandler PreServiceStop;
        public event EventHandler ServiceStopped;
        public event EventHandler RunningChanged;
        //public event EventHandler HotplugFinished;
        public delegate void HotplugControllerHandler(ControlService sender, DS4Device device, int index);
        public event HotplugControllerHandler HotplugController;

        // Phase9 9-E E-1: 旧 oscState／oscCallback／oscListener／oscSender（OSC の状態・受信・送信）は OscService へ移した
        private readonly DI.IOscService _oscService;

        public void GetPadDetailForIdx(int padIdx, ref DualShockPadMeta meta)
        {
            //meta = new DualShockPadMeta();
            meta.PadId = (byte)padIdx;
            meta.Model = DsModel.DS4;

            var d = DS4Controllers[padIdx];
            if (d == null)
            {
                meta.PadMacAddress = null;
                meta.PadState = DsState.Disconnected;
                meta.ConnectionType = DsConnection.None;
                meta.Model = DsModel.None;
                meta.BatteryStatus = 0;
                meta.IsActive = false;
                return;
                //return meta;
            }

            bool isValidSerial = false;
            string stringMac = d.getMacAddress();
            if (!string.IsNullOrEmpty(stringMac))
            {
                stringMac = string.Join("", stringMac.Split(':'));
                //stringMac = stringMac.Replace(":", "").Trim();
                meta.PadMacAddress = System.Net.NetworkInformation.PhysicalAddress.Parse(stringMac);
                isValidSerial = d.isValidSerial();
            }

            if (!isValidSerial)
            {
                //meta.PadMacAddress = null;
                meta.PadState = DsState.Disconnected;
            }
            else
            {
                if (d.isSynced() || d.IsAlive())
                    meta.PadState = DsState.Connected;
                else
                    meta.PadState = DsState.Reserved;
            }

            meta.ConnectionType = (d.getConnectionType() == ConnectionType.USB) ? DsConnection.Usb : DsConnection.Bluetooth;
            meta.IsActive = !d.isDS4Idle();

            int batteryLevel = d.getBattery();
            if (d.isCharging() && batteryLevel >= 100)
                meta.BatteryStatus = DsBattery.Charged;
            else
            {
                if (batteryLevel >= 95)
                    meta.BatteryStatus = DsBattery.Full;
                else if (batteryLevel >= 70)
                    meta.BatteryStatus = DsBattery.High;
                else if (batteryLevel >= 50)
                    meta.BatteryStatus = DsBattery.Medium;
                else if (batteryLevel >= 20)
                    meta.BatteryStatus = DsBattery.Low;
                else if (batteryLevel >= 5)
                    meta.BatteryStatus = DsBattery.Dying;
                else
                    meta.BatteryStatus = DsBattery.None;
            }

            //return meta;
        }

        public ControlService(DS4WinWPF.ArgumentParser cmdParser, IDs4DeviceRegistry deviceRegistry,
            DI.IProfileSettingsService profileSettings,
            DI.IAppSettingsService appSettings,
            DI.IEnvironmentService environmentService,
            DI.IPathService pathService,
            Func<DI.IOutputSlotService> outputSlotServiceFactory,
            DI.IProfileRepository profileRepository,
            DI.IDeviceStateService deviceStateService,
            DI.IProfileXmlStore profileXmlStore,
            Services.IProfileSlotApplier profileSlotApplier,
            Services.IVirtualKBM virtualKBM,
            Services.IVirtualKBMLifecycle kbmLifecycle,
            DI.IAppearanceSettingsService appearanceSettings,
            DI.IProfileActionProvider profileActionProvider,
            DI.ISpecialActionRepository specialActionRepository,
            Services.IElevatedProcessLauncher elevatedProcessLauncher,
            StickMotionState stickMotion,
            SyntheticInputState syntheticInput,
            DI.IMapping mapping,
            DI.IOscService oscService,
            DI.IHidHideService hidHideService,
            DI.IUdpMotionService udpMotionService,
            DI.IVirtualPadService virtualPadService)
        {
            this.cmdParser = cmdParser;
            this._deviceRegistry = deviceRegistry;
            // TODO(技術的負債): 過渡期の防御コード。DI 経由の生成（ServiceRegistration）では profileSettings は常に非 null。
            // 旧シグネチャ（profileSettings = null）との互換のために残している。Phase6-Step12 で削除判断（Phase6-Step2-Plan.md 決定D2）。
            this._profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings)); // Phase6-Step10-4-5: Global のフォールバックを廃止
            this._appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this._environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this._pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            this._controllerSlotLimit = _environmentService.ControllerSlotLimit;
            this._outputSlotServiceFactory = outputSlotServiceFactory ?? throw new ArgumentNullException(nameof(outputSlotServiceFactory));
            this._profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            this._deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            // 適用のたびに、そのスロットのコントローラー（MAC）に、適用したプロファイルを記憶する（再接続で使う）
            this._profileRepository.SelectedProfileChanged += RememberAppliedProfile;
            this._profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            this._profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            this._virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            this._kbmLifecycle = kbmLifecycle ?? throw new ArgumentNullException(nameof(kbmLifecycle));
            this._appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            this._profileActionProvider = profileActionProvider ?? throw new ArgumentNullException(nameof(profileActionProvider));
            this._specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            this._elevatedProcessLauncher = elevatedProcessLauncher ?? throw new ArgumentNullException(nameof(elevatedProcessLauncher));
            this._stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            this._syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            this._mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            this._oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            this._hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
            this._udpMotion = udpMotionService ?? throw new ArgumentNullException(nameof(udpMotionService));
            this._virtualPad = virtualPadService ?? throw new ArgumentNullException(nameof(virtualPadService));
            // 仮想パッド系のログも、これまでどおり ControlService の Debug イベントへ流す
            this._virtualPad.DebugMessage += (data, warning) => LogDebug(data, warning);
            // UDP モーションのログも、これまでどおり ControlService の Debug イベントへ流す
            this._udpMotion.DebugMessage += (data, warning) => LogDebug(data, warning);
            // HidHide のログは、これまでどおり ControlService の Debug イベント（GUI のログ、ログファイル）へ流す
            this._hidHideService.DebugMessage += (data, warning) => LogDebug(data, warning);

            Crc32Algorithm.InitializeTable(DS4Device.DefaultPolynomial);

            eventDispatchThread = new Thread(() =>
            {
                Dispatcher currentDis = Dispatcher.CurrentDispatcher;
                eventDispatcher = currentDis;
                Dispatcher.Run();
            });
            eventDispatchThread.IsBackground = true;
            eventDispatchThread.Priority = ThreadPriority.BelowNormal;
            eventDispatchThread.Name = "ControlService Events";
            eventDispatchThread.Start();

            for (int i = 0, arlength = DS4Controllers.Length; i < arlength; i++)
            {
                MappedState[i] = new DS4State();
                CurrentState[i] = new DS4State();
                TempState[i] = new DS4State();
                PreviousState[i] = new DS4State();
                ExposedState[i] = new DS4StateExposed(CurrentState[i]);

                int tempDev = i;
                _profileSettings.L2OutputSettings[i].TwoStageModeChanged += (sender, e) =>
                {
                    _stickMotion.L2TwoStageMappingData[tempDev].Reset();
                };

                _profileSettings.R2OutputSettings[i].TwoStageModeChanged += (sender, e) =>
                {
                    _stickMotion.R2TwoStageMappingData[tempDev].Reset();
                };
            }

            // Phase9 9-E E-4: 旧 outputslotMan の生成は VirtualPadService のコンストラクターへ移した
            //outputslotMan.SlotAssigned += OutputslotMan_SlotAssigned;
            deviceOptions = _appSettings.DeviceOptions;

            _deviceRegistry.RequestElevation += DS4Devices_RequestElevation;
            _deviceRegistry.PrepareDS4Init = PrepareDS4DeviceInit;
            _deviceRegistry.PostDS4Init = PostDS4DeviceInit;
            _deviceRegistry.PreparePendingDevice = CheckForSupportedDevice;
            _virtualPad.OutputSlotManager.ViGEmFailure += OutputslotMan_ViGEmFailure;

            // Phase9 9-E E-3: スムージング設定の変更の購読（旧 ChangeUdpSmoothingAttrs）は UdpMotionService のコンストラクターへ移した

            // Phase9 9-E E-1: 旧 CreateOSCCallback() は OscService へ移した（受信のコールバックは OscService.HandleMessage）

            //oscListener = new UDPListener(Global.getOSCServerPortNum(), callback: oscCallback);
            //AppLogger.LogToGui("OSC LISTENER STARTED", false);
        }

        //private void OutputslotMan_SlotAssigned(OutputSlotManager sender, int slotNum, OutSlotDevice outSlotDev)
        //{
        //    LogDebug($"Associated input controller #{outSlotDev.InputIndex + 1} ({outSlotDev.InputDisplayString}) to virtual {outSlotDev.OutputDevice.GetDeviceType()} Controller in{(outSlotDev.PermanentType != OutContType.None ? " permanent" : "")} output slot #{outSlotDev.Index + 1}");
        //}

        // Phase9 9-E E-1: 旧 MapMonitoringOscMessageToCommand／CreateOSCCallback（OSC の受信の解釈）は OscService へ移した。

        public void RefreshOutputKBMHandler()
        {
            // ハンドラの Disconnect と破棄（null 判定を含む）は IVirtualKBMLifecycle が担う（決定D3）。
            _kbmLifecycle.ReleaseHandler();

            if (_profileSettings.OutputKBMMapping != null)
            {
                _profileSettings.OutputKBMMapping = null;
            }

            InitOutputKBMHandler();
        }

        private void InitOutputKBMHandler()
        {
            string attemptVirtualkbmHandler = cmdParser.VirtualkbmHandler;
            _kbmLifecycle.DetermineHandler(attemptVirtualkbmHandler);

            bool handlerConnected = false;
            try
            {
                handlerConnected = _virtualKBM.Connect();
            }
            catch { }

            if (!handlerConnected &&
                attemptVirtualkbmHandler != VirtualKBMFactory.GetFallbackHandlerIdentifier())
            {
                _kbmLifecycle.SwitchToFallbackHandler();
            }
            else
            {
                // Connection was made. Check if version number should get populated
                if (_virtualKBM.GetIdentifier() == FakerInputHandler.IDENTIFIER)
                {
                    _kbmLifecycle.ApplyFakerInputVersion();
                }
            }

            _kbmLifecycle.InitializeMapping(_virtualKBM.GetIdentifier());
            _profileSettings.OutputKBMMapping.PopulateConstants();
            _profileSettings.OutputKBMMapping.PopulateMappings();
        }

        private void OutputslotMan_ViGEmFailure(object sender, int errorCode)
        {
            eventDispatcher.BeginInvoke((Action)(() =>
            {
                loopControllers = false;
                while (inServiceTask)
                    Thread.SpinWait(1000);

                LogDebug(string.Format(DS4WinWPF.Translations.Strings.ViGEmPluginFailure, errorCode),
                    true);
                Stop();
            }));
        }

        public void PostDS4DeviceInit(DS4Device device)
        {
            if (device.DeviceType == InputDevices.InputDeviceType.JoyConL ||
                device.DeviceType == InputDevices.InputDeviceType.JoyConR)
            {
                if (deviceOptions.JoyConDeviceOpts.LinkedMode == JoyConDeviceOptions.LinkMode.Joined)
                {
                    InputDevices.JoyConDevice tempJoyDev = device as InputDevices.JoyConDevice;
                    tempJoyDev.PerformStateMerge = true;

                    if (device.DeviceType == InputDevices.InputDeviceType.JoyConL)
                    {
                        tempJoyDev.PrimaryDevice = true;
                        if (deviceOptions.JoyConDeviceOpts.JoinGyroProv == JoyConDeviceOptions.JoinedGyroProvider.JoyConL)
                        {
                            tempJoyDev.OutputMapGyro = true;
                        }
                        else
                        {
                            tempJoyDev.OutputMapGyro = false;
                        }
                    }
                    else
                    {
                        tempJoyDev.PrimaryDevice = false;
                        if (deviceOptions.JoyConDeviceOpts.JoinGyroProv == JoyConDeviceOptions.JoinedGyroProvider.JoyConR)
                        {
                            tempJoyDev.OutputMapGyro = true;
                        }
                        else
                        {
                            tempJoyDev.OutputMapGyro = false;
                        }
                    }
                }
            }
        }

        private void PrepareDS4DeviceSettingHooks(DS4Device device)
        {
            if (device.DeviceType == InputDevices.InputDeviceType.DualSense)
            {
                InputDevices.DualSenseDevice tempDSDev = device as InputDevices.DualSenseDevice;

                DualSenseControllerOptions dSOpts = tempDSDev.NativeOptionsStore;
                dSOpts.LedModeChanged += (sender, e) => { tempDSDev.CheckControllerNumDeviceSettings(activeControllers); };
            }
            else if (device.DeviceType == InputDevices.InputDeviceType.JoyConL ||
                device.DeviceType == InputDevices.InputDeviceType.JoyConR)
            {
            }
        }

        public bool CheckForSupportedDevice(HidDevice device, VidPidInfo metaInfo)
        {
            bool result = false;
            switch (metaInfo.inputDevType)
            {
                case InputDevices.InputDeviceType.DS4:
                    result = deviceOptions.DS4DeviceOpts.Enabled;
                    break;
                case InputDevices.InputDeviceType.DualSense:
                    result = deviceOptions.DualSenseOpts.Enabled;
                    break;
                case InputDevices.InputDeviceType.SwitchPro:
                    result = deviceOptions.SwitchProDeviceOpts.Enabled;
                    break;
                case InputDevices.InputDeviceType.JoyConL:
                case InputDevices.InputDeviceType.JoyConR:
                case InputDevices.InputDeviceType.JoyConGrip:
                    result = deviceOptions.JoyConDeviceOpts.Enabled;
                    break;
                case InputDevices.InputDeviceType.DS3:
                    result = deviceOptions.DS3DeviceOpts.Enabled;
                    break;
                default:
                    break;
            }

            return result;
        }

        public void PrepareDS4DeviceInit(DS4Device device)
        {
            // Does nothing now
        }

        public void ShutDown()
        {
            _virtualPad.OutputSlotManager.ShutDown();
            // Phase7-Step5 W3（Q9＝A）: 契約（IOutputSlotService）経由に切り替えた。同じスロット管理と保存先で OutputSlotPersist.WriteConfig を呼ぶ
            _outputSlotServiceFactory().SaveOutputSlots();

            eventDispatcher.InvokeShutdown();
            eventDispatcher = null;

            eventDispatchThread.Join();
            eventDispatchThread = null;
        }

        private void DS4Devices_RequestElevation(RequestElevationArgs args)
        {
            // Phase 3 Step 3-5: try DI-based IElevatedProcessLauncher first,
            // fall back to the original direct Process.Start implementation.
            bool handled = false;
            try
            {
                var launcher = _elevatedProcessLauncher;
                if (launcher != null)
                {
                    int? exitCode = launcher.RelaunchElevated("re-enabledevice " + args.InstanceId, 30000);
                    if (exitCode.HasValue)
                    {
                        args.StatusCode = exitCode.Value;
                    }
                    handled = true;
                }
            }
            catch { }

            if (!handled)
            {
                // Launches an elevated child process to re-enable device
                ProcessStartInfo startInfo =
                    new ProcessStartInfo(_pathService.ExecutablePath);
                startInfo.Verb = "runas";
                startInfo.Arguments = "re-enabledevice " + args.InstanceId;
                startInfo.UseShellExecute = true;

                try
                {
                    Process child = Process.Start(startInfo);
                    if (!child.WaitForExit(30000))
                    {
                        child.Kill();
                    }
                    else
                    {
                        args.StatusCode = child.ExitCode;
                    }
                    child.Dispose();
                }
                catch { }
            }
        }

        // Phase9 9-E E-2: 旧 CheckHidHidePresence／UpdateHidHideAttributes の本体は HidHideService へ移した（薄い委譲。呼び出し側は 9-F まで変えない）
        public void CheckHidHidePresence(string ExePath = "", string ExeName = "Autoprofile Exe", bool AddExe = true) // Default value for D4W Startup
        {
            _hidHideService.CheckPresence(ExePath, ExeName, AddExe);
        }

        public void UpdateHidHiddenAttributes()
        {
            // 旧: HidHideInstalled の確認のあと UpdateHidHideAttributes（その中でも同じ確認）。確認は HidHideService.UpdateAttributes の中に 1 つにまとめた
            _hidHideService.UpdateAttributes();
        }

        // Phase9 9-E E-2: 旧 CheckAffected は HidHideService.IsAffected へ移した。

        /// <summary>
        /// Obtain extra mappable controls not on a DS4 that should be added
        /// to the checked inputs list. Keeps Mapping class from having to check
        /// extra Switch Pro and JoyCon buttons for DS4 controllers
        /// </summary>
        /// <param name="dev">Instance of input device</param>
        /// <returns>List of extra controls to check in Mapping class</returns>
        private List<DS4Controls> GetKnownExtraButtons(DS4Device dev)
        {
            List<DS4Controls> result = new List<DS4Controls>();
            switch (dev.DeviceType)
            {
                case InputDevices.InputDeviceType.DualSense:
                    {
                        InputDevices.DualSenseDevice tempDev = dev as InputDevices.DualSenseDevice;
                        if (tempDev != null &&
                            tempDev.SubType == InputDevices.DualSenseDevice.DeviceSubType.DSEdge)
                        {
                            // Added extra DualSense Edge buttons as extra in the mapper.
                            // Keeps from checking non-existent buttons on other device types.
                            result.AddRange(new DS4Controls[] { DS4Controls.FnL, DS4Controls.FnR, DS4Controls.BLP, DS4Controls.BRP });
                        }
                    }

                    break;
                case InputDevices.InputDeviceType.JoyConL:
                case InputDevices.InputDeviceType.JoyConR:
                    result.AddRange(new DS4Controls[] { DS4Controls.Capture, DS4Controls.SideL, DS4Controls.SideR, DS4Controls.FnL, DS4Controls.FnR });
                    break;
                case InputDevices.InputDeviceType.SwitchPro:
                    result.AddRange(new DS4Controls[] { DS4Controls.Capture });
                    break;
                default:
                    break;
            }

            return result;
        }

        // Phase9 9-E E-2: 旧 ChangeExclusiveStatus は HidHideService.MarkAffected へ移した。

        // Phase9 9-E E-3: 旧 TestQueueBus（イベントスレッドでの実行）は IUdpMotionHost.QueueOnEventThread として公開した
        public void QueueOnEventThread(Action temp)
        {
            eventDispatcher.BeginInvoke(() =>
            {
                temp?.Invoke();
            });
        }

        // IUdpMotionHost: UDP モーションの部品が使う、このクラスが持つ状態への窓口
        public DS4State GetCurrentState(int ind) => CurrentState[ind];

        public DS4State GetTempState(int ind) => TempState[ind];

        // Phase9 9-E E-3: 旧 ChangeUDPStatus の本体は UdpMotionService.ChangeStatus へ移した（薄い委譲。呼び出し側は 9-F まで変えない）
        public void ChangeUDPStatus(bool state, bool openPort = true)
        {
            _udpMotion.ChangeStatus(state, openPort);
        }

        public void ChangeOSCListenerStatus(bool state)
        {
            // Phase9 9-E E-1: 本体は OscService へ移した（薄い委譲。呼び出し側は 9-F まで変えない）
            _oscService.ChangeListenerStatus(state);
        }

        public void ChangeOSCSenderStatus(bool state)
        {
            _oscService.ChangeSenderStatus(state);
        }

        // Phase9 9-E E-1: 旧 SendOsc（Phase6-Step9-3c の K9-1 の競合対策つき）は OscService.SendOsc へ移した。

        public void ChangeMotionEventStatus(bool state)
        {
            _udpMotion.ChangeMotionEventStatus(state);
        }

        // Phase9 9-E E-3: 旧 udpChangeStatus／changingUDPPort／UseUDPPort は UdpMotionService へ移した（UseUDPPort は呼び出し元 0 件のため、ここには委譲を残さない）。

        private void WarnExclusiveModeFailure(DS4Device device)
        {
            if (_deviceRegistry.IsExclusiveMode && !device.isExclusive())
            {
                string message = DS4WinWPF.Properties.Resources.CouldNotOpenDS4.Replace("*Mac address*", device.getMacAddress()) + " " +
                    DS4WinWPF.Properties.Resources.QuitOtherPrograms;
                LogDebug(message, true);
                AppLogger.LogToTray(message, true);
            }
        }

        // Phase9 9-E E-4: 旧 StartViGEm／StopViGEm／AssignInitialDevices／EstablishOutDevice／EstablishOutFeedback／RemoveOutFeedback／AttachNewUnboundOutDev／
        // AttachUnboundOutDev／DetachUnboundOutDev／PluginOutDev／UnplugOutDev の本体は VirtualPadService へ移した。
        // PluginOutDev／UnplugOutDev は、外部からの呼び出し（BackingStore、LegacyProfileXmlSerializer）があるため、薄い委譲として残す（呼び出し側は 9-F まで変えない）。
        public void PluginOutDev(int index, DS4Device device)
        {
            _virtualPad.PluginOutDev(index, device);
        }

        public void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false)
        {
            _virtualPad.UnplugOutDev(index, device, immediate, force);
        }

        public bool Start(bool showlog = true)
        {
            inServiceTask = true;
            _virtualPad.StartViGEm();
            if (_virtualPad.HasViGEmClient)
            //if (x360Bus.Open() && x360Bus.Start())
            {
                // Initialize output KBM handler at start of ControlService
                InitOutputKBMHandler();

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.Starting);

                Thread.Sleep(2000);

                bool runningAsAdmin = _environmentService.IsAdministrator();
                if (_virtualKBM.GetIdentifier() != FakerInputHandler.IDENTIFIER && !runningAsAdmin)
                {
                    string helpURL = @"https://ryochan7.github.io/ds4windows-site/troubleshooting/kb-mouse-issues/#windows-not-responding-to-ds4ws-kb-m-commands-in-some-situations";
                    LogDebug($"Some applications may block controller inputs. (Windows UAC Conflictions). Please go to {helpURL} for more information and workarounds.");
                }

                LogDebug($"Using output KB+M handler: {_virtualKBM.GetFullDisplayName()}");
                LogDebug($"Connection to ViGEmBus {_environmentService.ViGEmBusVersion} established");

                _deviceRegistry.IsExclusiveMode = _appSettings.UseExclusiveMode; //Re-enable Exclusive Mode

                UpdateHidHiddenAttributes();

                if (showlog)
                {
                    LogDebug(DS4WinWPF.Properties.Resources.SearchingController);
                    LogDebug(_deviceRegistry.IsExclusiveMode ? DS4WinWPF.Properties.Resources.UsingExclusive : DS4WinWPF.Properties.Resources.UsingShared);
                }

                if (_appSettings.UseOscServer && !_oscService.HasListener)
                {
                    ChangeOSCListenerStatus(true);
                }

                if (_appSettings.UseOscSender && !_oscService.HasSender)
                {
                    ChangeOSCSenderStatus(true);
                }

                if (_appSettings.UseUdpServer && !_udpMotion.HasServer)
                {
                    ChangeUDPStatus(true, false);
                    while (_udpMotion.IsChangingStatus)
                    {
                        Thread.SpinWait(500);
                    }
                }

                try
                {
                    loopControllers = true;
                    _virtualPad.AssignInitialDevices();   // Phase9 9-E E-4: 旧 AssignInitialDevices（恒久スロットの割り当て）は VirtualPadService へ移した

                    eventDispatcher.Invoke(() =>
                    {
                        _deviceRegistry.FindControllers();
                    });

                    IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
                    int numControllers = devices.Count();
                    activeControllers = numControllers;
                    DS4LightBar.defaultLight = false;
                    int i = 0;
                    InputDevices.JoyConDevice tempPrimaryJoyDev = null;
                    for (var devEnum = devices.GetEnumerator();
                        devEnum.MoveNext() && loopControllers; i++)
                    {
                        DS4Device device = devEnum.Current;

                        BeginPrepareConnectedInputController(device, showlog: true);

                        if (deviceOptions.JoyConDeviceOpts.LinkedMode == JoyConDeviceOptions.LinkMode.Joined)
                        {
                            if ((device.DeviceType == InputDevices.InputDeviceType.JoyConL ||
                                device.DeviceType == InputDevices.InputDeviceType.JoyConR) && device.PerformStateMerge)
                            {
                                if (tempPrimaryJoyDev == null)
                                {
                                    tempPrimaryJoyDev = device as InputDevices.JoyConDevice;
                                }
                                else
                                {
                                    InputDevices.JoyConDevice currentJoyDev = device as InputDevices.JoyConDevice;
                                    tempPrimaryJoyDev.JointDevice = currentJoyDev;
                                    currentJoyDev.JointDevice = tempPrimaryJoyDev;

                                    tempPrimaryJoyDev.JointState = currentJoyDev.JointState;

                                    InputDevices.JoyConDevice parentJoy = tempPrimaryJoyDev;
                                    tempPrimaryJoyDev.Removal += (sender, args) =>
                                    {
                                        currentJoyDev.JointDevice = null;
                                    };
                                    currentJoyDev.Removal += (sender, args) =>
                                    {
                                        parentJoy.JointDevice = null;
                                    };

                                    tempPrimaryJoyDev = null;
                                }
                            }
                        }

                        DS4Controllers[i] = device;
                        device.DeviceSlotNumber = i;
                        PrepareConnectedInputControllerSettingEvents(numControllers, device, index: i);

                        if (i >= _controllerSlotLimit) // out of Xinput devices!
                            break;
                    }
                }
                catch (Exception e)
                {
                    LogDebug(e.Message, true);
                    AppLogger.LogToTray(e.Message, true);
                }

                running = true;

                _udpMotion.StartListeningIfCreated();   // Phase9 9-E E-3: 旧: _udpServer != null のときの待ち受けの開始（ブロックを UdpMotionService へ移した）
            }
            else
            {
                string logMessage = string.Empty;
                if (!_environmentService.IsViGEmBusInstalled())
                {
                    logMessage = "ViGEmBus is not installed";
                }
                else if (!_environmentService.IsRunningSupportedViGEmBus())
                {
                    logMessage = string.Format("Unsupported ViGEmBus found ({0}). Please install at least ViGEmBus 1.17.333.0", _environmentService.ViGEmBusVersion);
                }
                else
                {
                    logMessage = "Could not connect to ViGEmBus. Please check the status of the System device in Device Manager and if Visual C++ 2017 Redistributable is installed.";
                }

                LogDebug(logMessage);
                AppLogger.LogToTray(logMessage);
            }

            inServiceTask = false;
            _appSettings.RunHotPlug = true;
            ServiceStarted?.Invoke(this, EventArgs.Empty);
            RunningChanged?.Invoke(this, EventArgs.Empty);
            using var process = Process.GetCurrentProcess();
            process.PriorityClass = MainWindow.ProcessPriorityClasses[_appSettings.ProcessPriority];
            return true;
        }

        // Phase9 9-E E-3: 旧 PrepareDevUDPMotion（モーションの送出のハンドラー）は UdpMotionService.PrepareDeviceMotion へ移した。

        private void CheckQuickCharge(object sender, EventArgs e)
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

        public void PrepareAbort()
        {
            for (int i = 0, arlength = DS4Controllers.Length; i < arlength; i++)
            {
                DS4Device tempDevice = DS4Controllers[i];
                if (tempDevice != null)
                {
                    tempDevice.PrepareAbort();
                }
            }
        }

        public bool Stop(bool showlog = true, bool immediateUnplug = false)
        {
            if (running)
            {
                running = false;
                _appSettings.RunHotPlug = false;
                inServiceTask = true;
                PreServiceStop?.Invoke(this, EventArgs.Empty);

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppingX360);

                LogDebug("Closing connection to ViGEmBus");

                bool anyUnplugged = false;
                for (int i = 0, arlength = DS4Controllers.Length; i < arlength; i++)
                {
                    DS4Device tempDevice = DS4Controllers[i];
                    if (tempDevice != null)
                    {
                        if ((_appSettings.DCBTatStop && !tempDevice.isCharging()) || suspending)
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
                            DS4LightBar.forcelight[i] = false;
                            DS4LightBar.forcedFlash[i] = 0;
                            DS4LightBar.defaultLight = true;
                            DS4LightBar.updateLightBar(DS4Controllers[i], i, _profileSettings);
                            tempDevice.IsRemoved = true;
                            tempDevice.StopUpdate();
                            _deviceRegistry.RemoveDevice(tempDevice);
                            Thread.Sleep(50);
                        }

                        CurrentState[i].Battery = PreviousState[i].Battery = 0; // Reset for the next connection's initial status change.
                        OutputDevice tempout = outputDevices[i];
                        if (tempout != null)
                        {
                            UnplugOutDev(i, tempDevice, immediate: immediateUnplug, force: true);
                            anyUnplugged = true;
                        }

                        //outputDevices[i] = null;
                        //useDInputOnly[i] = true;
                        //Global.activeOutDevType[i] = OutContType.None;
                        _profileSettings.UseDInputOnlyArray[i] = true;
                        DS4Controllers[i] = null;
                        _oscService.ResetDeviceState(i);
                        touchPad[i] = null;
                        lag[i] = false;
                        inWarnMonitor[i] = false;
                    }
                }

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppingDS4);

                _deviceRegistry.StopControllers();
                slotManager.ClearControllerList();

                if (_oscService.HasListener)
                {
                    ChangeOSCListenerStatus(false);
                }

                if (_oscService.HasSender)
                {
                    ChangeOSCSenderStatus(false);
                }

                if (_udpMotion.HasServer)
                {
                    ChangeUDPStatus(false);
                }

                if (showlog)
                    LogDebug(DS4WinWPF.Properties.Resources.StoppedDS4Windows);

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
                LogDebug($"Closing connection to output handler {_virtualKBM.GetDisplayName()}");
                _virtualKBM.Disconnect();
                inServiceTask = false;
                activeControllers = 0;
            }

            _appSettings.RunHotPlug = false;
            ServiceStopped?.Invoke(this, EventArgs.Empty);
            RunningChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool HotPlug()
        {
            if (running)
            {
                inServiceTask = true;
                loopControllers = true;
                eventDispatcher.Invoke(() =>
                {
                    _deviceRegistry.FindControllers();
                });

                IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
                int numControllers = devices.Count();
                activeControllers = numControllers;
                InputDevices.JoyConDevice tempPrimaryJoyDev = null;
                InputDevices.JoyConDevice tempSecondaryJoyDev = null;

                if (deviceOptions.JoyConDeviceOpts.LinkedMode == JoyConDeviceOptions.LinkMode.Joined)
                {
                    tempPrimaryJoyDev = devices.Where(d =>
                        (d.DeviceType == InputDevices.InputDeviceType.JoyConL || d.DeviceType == InputDevices.InputDeviceType.JoyConR)
                         && d.PrimaryDevice && d.JointDeviceSlotNumber == -1).FirstOrDefault() as InputDevices.JoyConDevice;

                    tempSecondaryJoyDev = devices.Where(d =>
                        (d.DeviceType == InputDevices.InputDeviceType.JoyConL || d.DeviceType == InputDevices.InputDeviceType.JoyConR)
                        && !d.PrimaryDevice && d.JointDeviceSlotNumber == -1).FirstOrDefault() as InputDevices.JoyConDevice;
                }

                for (var devEnum = devices.GetEnumerator(); devEnum.MoveNext() && loopControllers;)
                {
                    DS4Device device = devEnum.Current;

                    if (device.isDisconnectingStatus())
                        continue;

                    // Use local method rather than Func
                    bool checkAlreadyExists()
                    {
                        for (int Index = 0, arlength = DS4Controllers.Length; Index < arlength; Index++)
                        {
                            if (DS4Controllers[Index] != null &&
                                DS4Controllers[Index].getMacAddress() == device.getMacAddress())
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

                    for (int Index = 0, arlength = DS4Controllers.Length;
                        Index < arlength && Index < _controllerSlotLimit; Index++)
                    {
                        if (DS4Controllers[Index] == null)
                        {
                            BeginPrepareConnectedInputController(device);

                            if (deviceOptions.JoyConDeviceOpts.LinkedMode == JoyConDeviceOptions.LinkMode.Joined)
                            {
                                if ((device.DeviceType == InputDevices.InputDeviceType.JoyConL ||
                                    device.DeviceType == InputDevices.InputDeviceType.JoyConR) && device.PerformStateMerge)
                                {
                                    if (device.PrimaryDevice &&
                                        tempSecondaryJoyDev != null)
                                    {
                                        InputDevices.JoyConDevice currentJoyDev = device as InputDevices.JoyConDevice;
                                        tempSecondaryJoyDev.JointDevice = currentJoyDev;
                                        currentJoyDev.JointDevice = tempSecondaryJoyDev;

                                        tempSecondaryJoyDev.JointState = currentJoyDev.JointState;

                                        InputDevices.JoyConDevice secondaryJoy = tempSecondaryJoyDev;
                                        secondaryJoy.Removal += (sender, args) =>
                                        {
                                            currentJoyDev.JointDevice = null;
                                        };
                                        currentJoyDev.Removal += (sender, args) =>
                                        {
                                            secondaryJoy.JointDevice = null;
                                        };

                                        tempSecondaryJoyDev = null;
                                        tempPrimaryJoyDev = null;
                                    }
                                    else if (!device.PrimaryDevice &&
                                        tempPrimaryJoyDev != null)
                                    {
                                        InputDevices.JoyConDevice currentJoyDev = device as InputDevices.JoyConDevice;
                                        tempPrimaryJoyDev.JointDevice = currentJoyDev;
                                        currentJoyDev.JointDevice = tempPrimaryJoyDev;

                                        tempPrimaryJoyDev.JointState = currentJoyDev.JointState;

                                        InputDevices.JoyConDevice parentJoy = tempPrimaryJoyDev;
                                        tempPrimaryJoyDev.Removal += (sender, args) =>
                                        {
                                            currentJoyDev.JointDevice = null;
                                        };
                                        currentJoyDev.Removal += (sender, args) =>
                                        {
                                            parentJoy.JointDevice = null;
                                        };

                                        tempPrimaryJoyDev = null;
                                    }
                                }
                            }

                            DS4Controllers[Index] = device;
                            device.DeviceSlotNumber = Index;
                            PrepareConnectedInputControllerSettingEvents(numControllers, device, Index);

                            HotplugController?.Invoke(this, device, Index);
                            break;
                        }
                    }
                }

                inServiceTask = false;
            }

            return true;
        }

        private void PrepareConnectedInputControllerSettingEvents(int numControllers, DS4Device device, int index)
        {
            _profileSettings.RefreshExtrasButtons(index, GetKnownExtraButtons(device));
            _profileXmlStore.LoadControllerConfigsForDevice(device);
            device.LoadStoreSettings();
            device.CheckControllerNumDeviceSettings(numControllers);

            slotManager.AddController(device, index);
            if (_appSettings.UseOscSender)
            {
                _oscService.SendPlug(index, true);
            }
            device.Removal += this.On_DS4Removal;
            device.Removal += _deviceRegistry.OnRemoval;
            device.SyncChange += this.On_SyncChange;
            device.SyncChange += _deviceRegistry.UpdateSerial;
            device.SerialChange += this.On_SerialChange;
            device.ChargingChanged += CheckQuickCharge;

            touchPad[index] = new Mouse(index, device, _profileSettings, _virtualKBM, _stickMotion, _syntheticInput);
            // Phase7-Step5 W1: DS3 のジャイロトリガー（Controls モード）の判定を、このスロットの Mouse へつなぐ。
            // DS4SixAxis は ControlService を直接参照しない（旧: Program.rootHub。DI 移行後は参照が失われていた）。
            device.SixAxis.GyroControlsTriggerActive =
                slot => touchPad[slot]?.IsGyroTriggerActive(GyroOutMode.Controls) ?? false;
            bool profileLoaded = false;
            bool useAutoProfile = _profileSettings.GetUseTempProfile(index);

            // 「初回接続か」「直前のプロファイル」は、スロット番号ではなくコントローラー（MAC）ごとに判定する
            // （別のコントローラーが空いたスロットに入っても、そのコントローラーにとっては初回。Reconnect-Profile-PerController-Fix-Plan.md）。
            // 有効なシリアルがない（MAC が使えない）ときは、従来のスロット単位。
            string connectedMac = device.isValidSerial() ? device.getMacAddress() : null;

            DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: device={index}, useAutoProfile={useAutoProfile}, isFirstConnection={_deviceStateService.IsFirstConnection(index, connectedMac)}");

            if (!useAutoProfile)
            {
                // ===== プロファイル選択ロジック =====
                string profileToApply;

                if (_deviceStateService.IsFirstConnection(index, connectedMac))
                {
                    // 初回接続: LinkedProfiles.xmlまたはOlderProfilePathからプロファイルを選択
                    DS4Windows.AppLogger.LogDebug($"FIRST CONNECTION detected for device {index}");

                    if (device.isValidSerial() && _profileRepository.ContainsLinkedProfile(device.getMacAddress()))
                    {
                        // Linked登録済み → Linkedを適用
                        profileToApply = _profileRepository.GetLinkedProfile(device.getMacAddress());
                        DS4Windows.AppLogger.LogDebug($"Using LINKED profile: '{profileToApply}' for device {index}");
                    }
                    else
                    {
                        // Linked未登録 → OlderProfilePathを使用
                        profileToApply = _profileRepository.OlderProfilePath[index];
                        DS4Windows.AppLogger.LogDebug($"Using OLDER profile: '{profileToApply}' for device {index}");
                    }

                    _deviceStateService.MarkConnected(index, connectedMac); // 初回接続完了マーク
                }
                else
                {
                    // 再接続: そのコントローラーが直前に適用していたプロファイル。記憶がなければ、既存のSelectedProfile（スロット）を使用
                    profileToApply = _deviceStateService.GetRememberedProfile(connectedMac);
                    if (string.IsNullOrWhiteSpace(profileToApply))
                        profileToApply = _profileRepository.SelectedProfile[index];
                    DS4Windows.AppLogger.LogDebug($"RECONNECTION detected for device {index}, using existing profile: '{profileToApply}'");
                }

                // ===== 共通処理: プロファイル設定とUI状態の更新 =====
                _profileRepository.SelectedProfile[index] = profileToApply;
                _deviceStateService.RememberProfile(connectedMac, profileToApply);   // このコントローラーの「直前のプロファイル」（適用のたびにも更新する）

                // LinkedProfiles.xmlの状態を確認してLinkedProfileUIとlinkedProfileCheckを設定
                if (device.isValidSerial() && _profileRepository.ContainsLinkedProfile(device.getMacAddress()))
                {
                    string linkedProfile = _profileRepository.GetLinkedProfile(device.getMacAddress());
                    _profileRepository.LinkedProfileUI[index] = linkedProfile;
                    _profileSettings.SetLinkedProfileCheck(index, true);
                }
                else
                {
                    _profileRepository.LinkedProfileUI[index] = string.Empty;
                    _profileSettings.SetLinkedProfileCheck(index, false);
                }

                // プロファイル適用（Phase5-Step14 フェーズD: 共通窓口 Global.ApplyProfileToSlot 経由に統一。契機5統合）
                DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: About to call ApplyProfileToSlot with '{profileToApply}'");
                profileLoaded = _profileSlotApplier.ApplyToSlot(index, profileToApply, DS4Windows.ProfileChangeSource.ControlService);
                DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: ApplyProfileToSlot returned {profileLoaded}");
            }
            else
            {
                // Phase8-Step6 W2: 一時プロファイル／AutoProfile を使う接続は、プロファイルを適用し直さない。切断で空になった束縛を、ここで差し替える
                _profileSlotApplier.PublishSlot(index);
            }

            if (profileLoaded || useAutoProfile)
            {
                device.LightBarColor = _profileSettings.GetMainColor(index);

                if (!_profileSettings.GetDInputOnly(index) && device.isSynced())
                {
                    if (device.PrimaryDevice)
                    {
                        PluginOutDev(index, device);
                    }
                    else if (device.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
                    {
                        int otherIdx = device.JointDeviceSlotNumber;
                        OutputDevice tempOutDev = outputDevices[otherIdx];
                        if (tempOutDev != null)
                        {
                            OutContType tempConType = ActiveOutDevType[otherIdx];
                            _virtualPad.EstablishOutFeedback(index, tempConType, tempOutDev, device);
                            outputDevices[index] = tempOutDev;
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
                    TouchPadOn(index, device);
                }
                else if (device.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
                {
                    int otherIdx = device.JointDeviceSlotNumber;
                    DS4Device tempDev = DS4Controllers[otherIdx];
                    if (tempDev != null)
                    {
                        int mappedIdx = tempDev.PrimaryDevice ? otherIdx : index;
                        DS4Device gyroDev = device.OutputMapGyro ? device : (tempDev.OutputMapGyro ? tempDev : null);
                        if (gyroDev != null)
                        {
                            TouchPadOn(mappedIdx, gyroDev);
                        }
                    }
                }

                CheckProfileOptions(index, device);
                SetupInitialHookEvents(index, device);
            }

            int tempIdx = index;
            device.Report += (sender, e) =>
            {
                this.On_Report(sender, e, tempIdx);
            };

            if (_udpMotion.HasServer && index < _udpMotion.SlotCount)
            {
                _udpMotion.PrepareDeviceMotion(device, tempIdx);
            }

            device.StartUpdate();
        }

        private void BeginPrepareConnectedInputController(DS4Device device, bool showlog = false)
        {
            if (_hidHideService.IsAffected(device))
            {
                //device.CurrentExclusiveStatus = DS4Device.ExclusiveStatus.HidGuardAffected;
                _hidHideService.MarkAffected(device);
            }

            //Task task = new Task(() => { Thread.Sleep(5); WarnExclusiveModeFailure(device); });
            //task.Start();

            PrepareDS4DeviceSettingHooks(device);
        }

        // Phase9 9-E E-3: 旧 ResetUdpSmoothingFilters／ChangeUdpSmoothingAttrs（スムージングのフィルター）は UdpMotionService へ移した。

        public void CheckProfileOptions(int ind, DS4Device device, bool startUp = false)
        {
            device.ModifyFeatureSetFlag(VidPidFeatureSet.NoOutputData, !_profileSettings.GetEnableOutputDataToDS4(ind));
            if (!_profileSettings.GetEnableOutputDataToDS4(ind))
                LogDebug("Output data to DS4 disabled. Lightbar and rumble events are not written to DS4 gamepad. If the gamepad is connected over BT then IdleDisconnect option is recommended to let DS4Windows to close the connection after long period of idling.");

            device.setIdleTimeout(_profileSettings.GetIdleDisconnectTimeout(ind));
            device.setBTPollRate(_profileSettings.GetBTPollRate(ind));

            // Phase6-Step12（デバウンス時間の回帰是正）: 接続中のデバイスのデバウンス時間を、適用するプロファイルの値へ更新する。
            // 移行前は、編集画面の保存が Global.DebouncingMsHasChanged() を呼んで更新していたが、9da60b85 で ApplyProfile に置き換わり更新されなくなった。
            device.Debouncer?.SetDuration(TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[ind]));

            touchPad[ind].ResetTrackAccel(_profileSettings.GetTrackballFriction(ind));
            touchPad[ind].ResetToggleGyroModes();

            //Global.TouchOutMode[ind] = TouchpadOutMode.MouseJoystick;
            touchPad[ind].PostSetup();

            _profileSettings.L2OutputSettings[ind].TrigEffectSettings.maxValue = (byte)(Math.Max(_profileSettings.L2ModInfo[ind].maxOutput, _profileSettings.L2ModInfo[ind].maxZone) / 100.0 * 255);
            _profileSettings.R2OutputSettings[ind].TrigEffectSettings.maxValue = (byte)(Math.Max(_profileSettings.R2ModInfo[ind].maxOutput, _profileSettings.R2ModInfo[ind].maxZone) / 100.0 * 255);

            device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[ind].TriggerEffect,
                _profileSettings.L2OutputSettings[ind].TrigEffectSettings);
            device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[ind].TriggerEffect,
                _profileSettings.R2OutputSettings[ind].TrigEffectSettings);

            device.RumbleAutostopTime = _profileSettings.GetRumbleAutostopTime(ind);
            device.setRumble(0, 0);
            device.LightBarColor = _profileSettings.GetMainColor(ind);

            // DualSense specific profile settings
            if (device is InputDevices.DualSenseDevice dualsense)
            {
                switch (_profileSettings.DualSenseRumbleEmulationMode[ind])
                {
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Disabled:
                        dualsense.UseRumble = false;
                        dualsense.UseAccurateRumble = false;
                        break;
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Legacy:
                        dualsense.UseRumble = true;
                        dualsense.UseAccurateRumble = false;
                        break;
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Accurate:
                    default:
                        dualsense.UseRumble = true;
                        dualsense.UseAccurateRumble = true;
                        break;
                }
                dualsense.HapticPowerLevel = _profileSettings.DualSenseHapticPowerLevel[ind];
            }

            if (!startUp)
            {
                CheckLauchProfileOption(ind, device);
            }
        }

        private void CheckLauchProfileOption(int ind, DS4Device device)
        {
            string programPath = _profileSettings.LaunchProgram[ind];
            if (programPath != string.Empty)
            {
                Process[] localAll = Process.GetProcesses();
                bool procFound = false;
                for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                {
                    try
                    {
                        string temp = localAll[procInd].MainModule.FileName;
                        if (temp == programPath)
                        {
                            procFound = true;
                        }
                    }
                    // Ignore any process for which this information
                    // is not exposed
                    catch { }
                }

                if (!procFound)
                {
                    Task processTask = new Task(() =>
                    {
                        Thread.Sleep(5000);
                        Process tempProcess = new Process();
                        tempProcess.StartInfo.FileName = programPath;
                        tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                        //tempProcess.StartInfo.UseShellExecute = false;
                        try { tempProcess.Start(); }
                        catch { }
                    });

                    processTask.Start();
                }
            }
        }

        private void SetupInitialHookEvents(int ind, DS4Device device)
        {
            _udpMotion.ResetSmoothingFilters(ind);   // Phase9 9-E E-3: 旧 ResetUdpSmoothingFilters

            // Phase9 Steering 削除 W2: 旧「ステアリング用のフィルター（OneEuroFilter）の作成と、設定［スムージング］との連動」は削除した（機能ごと削除）

            FlickStickSettings flickStickSettings = _profileSettings.LSOutputSettings[ind].outputSettings.flickSettings;
            flickStickSettings.RemoveRefreshEvents();
            flickStickSettings.SetRefreshEvents(_stickMotion.FlickMappingData[ind].flickFilter);

            flickStickSettings = _profileSettings.RSOutputSettings[ind].outputSettings.flickSettings;
            flickStickSettings.RemoveRefreshEvents();
            flickStickSettings.SetRefreshEvents(_stickMotion.FlickMappingData[ind].flickFilter);

            int tempIdx = ind;
            _profileSettings.L2OutputSettings[ind].ResetEvents();
            _profileSettings.L2ModInfo[ind].ResetEvents();
            _profileSettings.L2OutputSettings[ind].TriggerEffectChanged += (sender, e) =>
            {
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.L2ModInfo[ind].MaxOutputChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(Math.Max(tempInfo.maxOutput, tempInfo.maxZone) / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.L2ModInfo[ind].MaxZoneChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(Math.Max(tempInfo.maxOutput, tempInfo.maxZone) / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };

            _profileSettings.R2OutputSettings[ind].ResetEvents();
            _profileSettings.R2OutputSettings[ind].TriggerEffectChanged += (sender, e) =>
            {
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.R2ModInfo[ind].MaxOutputChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(tempInfo.maxOutput / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.R2ModInfo[ind].MaxZoneChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(tempInfo.maxOutput / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
        }

        /// <summary>
        /// Perform Mapping property resetting as needed before loading profile settings
        /// </summary>
        /// <param name="device">Input device instance</param>
        public void PreLoadReset(int ind)
        {
            //DS4Device inputDevice = DS4Controllers[ind];
            //if (inputDevice == null)
            //{
            //    return;
            //}
            // Skip running for test profile with no mapping data
            if (ind >= ControllerLimits.TestProfileIndex)
            {
                return;
            }

            // Reset current flick stick progress from previous profile
            _stickMotion.FlickMappingData[ind].Reset();

            // Reset delta accel processors for sticks
            _stickMotion.DeltaAccelProcessors[ind].LSProcessor.Reset();
            _stickMotion.DeltaAccelProcessors[ind].RSProcessor.Reset();

            // Reset some elements of current Mouse instance
            touchPad[ind]?.Reset();
        }

        public void TouchPadOn(int ind, DS4Device device)
        {
            Mouse tPad = touchPad[ind];
            //ITouchpadBehaviour tPad = touchPad[ind];
            device.Touchpad.TouchButtonDown += tPad.touchButtonDown;
            device.Touchpad.TouchButtonUp += tPad.touchButtonUp;
            device.Touchpad.TouchesBegan += tPad.touchesBegan;
            device.Touchpad.TouchesBegan += tPad.TouchStartedOrEnded;
            device.Touchpad.TouchesMoved += tPad.touchesMoved;
            device.Touchpad.TouchesEnded += tPad.touchesEnded;
            device.Touchpad.TouchesEnded += tPad.TouchStartedOrEnded;
            device.Touchpad.TouchUnchanged += tPad.touchUnchanged;
            //device.Touchpad.PreTouchProcess += delegate { touchPad[ind].populatePriorButtonStates(); };
            device.Touchpad.PreTouchProcess += (sender, args) => { touchPad[ind].populatePriorButtonStates(); };
            device.SixAxis.SixAccelMoved += tPad.sixaxisMoved;
            //LogDebug("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
            //Log.LogToTray("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
        }

        public string GetDS4Battery(int index)
        {
            DS4Device d = DS4Controllers[index];
            if (d != null)
            {
                string battery;
                if (!d.IsAlive())
                    battery = "...";

                if (d.isCharging())
                {
                    if (d.getBattery() >= 100)
                        battery = DS4WinWPF.Properties.Resources.Full;
                    else
                        battery = d.getBattery() + "%+";
                }
                else
                {
                    battery = d.getBattery() + "%";
                }

                return battery;
            }
            else
                return DS4WinWPF.Properties.Resources.NA;
        }

        // プロファイルを適用したとき（SelectedProfileChanged は、適用のたびに発火する）、そのスロットのコントローラーに、適用したプロファイルを記憶する。
        // 再接続（同じコントローラー）のとき、切断前のプロファイルを適用するため（Reconnect-Profile-PerController-Fix-Plan.md）。適用のスレッドから呼ばれる。
        private void RememberAppliedProfile(object sender, SelectedProfileChangedEventArgs e)
        {
            try
            {
                int slot = e.DeviceIndex;
                if (slot < 0 || slot >= DS4Controllers.Length) return;

                DS4Device device = DS4Controllers[slot];
                if (device == null || !device.isValidSerial()) return;

                _deviceStateService.RememberProfile(device.getMacAddress(), e.ProfileName);
            }
            catch
            {
                // 記憶できなくても、適用を止めない（再接続は、スロットの SelectedProfile に戻る）
            }
        }

        protected void On_SerialChange(object sender, EventArgs e)
        {
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = MAX_DS4_CONTROLLER_COUNT; ind == -1 && i < arlength; i++)
            {
                DS4Device tempDev = DS4Controllers[i];
                if (tempDev != null && device == tempDev)
                    ind = i;
            }

            if (ind >= 0)
            {
                _deviceStateService.OnDeviceSerialChange(this, ind, device.getMacAddress());
            }
        }

        protected void On_SyncChange(object sender, EventArgs e)
        {
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = _controllerSlotLimit; ind == -1 && i < arlength; i++)
            {
                DS4Device tempDev = DS4Controllers[i];
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
                        UnplugOutDev(ind, device);
                    }
                }
                else
                {
                    if (!_profileSettings.GetDInputOnly(ind))
                    {
                        touchPad[ind].ReplaceOneEuroFilterPair();
                        //touchPad[ind].ReplaceOneEuroFilterPair();

                        touchPad[ind].Cursor.ReplaceOneEuroFilterPair();
                        touchPad[ind].Cursor.SetupLateOneEuroFilters();
                        PluginOutDev(ind, device);
                    }
                }
            }
        }

        // Called when DS4 is disconnected or timed out
        protected void On_DS4Removal(object sender, EventArgs e)
        {
            DS4Device device = (DS4Device)sender;
            int ind = -1;
            for (int i = 0, arlength = DS4Controllers.Length; ind == -1 && i < arlength; i++)
            {
                if (DS4Controllers[i] != null && device.getMacAddress() == DS4Controllers[i].getMacAddress())
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
                    CurrentState[ind].Battery = PreviousState[ind].Battery = 0; // Reset for the next connection's initial status change.
                    if (!_profileSettings.UseDInputOnlyArray[ind])
                    {
                        UnplugOutDev(ind, device);
                    }
                    else if (!device.PrimaryDevice)
                    {
                        OutputDevice outDev = outputDevices[ind];
                        if (outDev != null)
                        {
                            outDev.RemoveFeedback(ind);
                            outputDevices[ind] = null;
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
                    DS4Controllers[ind] = null;
                    _oscService.ResetDeviceState(ind);
                    //eventDispatcher.Invoke(() =>
                    //{
                    slotManager.RemoveController(device, ind);
                    if (_appSettings.UseOscSender)
                    {
                        _oscService.SendPlug(ind, false);
                    }
                    //});

                    touchPad[ind] = null;
                    lag[ind] = false;
                    inWarnMonitor[ind] = false;
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

        public bool[] lag = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        public bool[] inWarnMonitor = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        private byte[] currentBattery = new byte[MAX_DS4_CONTROLLER_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0 };
        private bool[] charging = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        private string[] tempStrings = new string[MAX_DS4_CONTROLLER_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };

        // Called every time a new input report has arrived
        protected void On_Report(DS4Device device, EventArgs e, int ind)
        {
            if (ind != -1)
            {
                string devError = tempStrings[ind] = device.error;
                if (!string.IsNullOrEmpty(devError))
                {
                    LogDebug(devError);
                }

                if (inWarnMonitor[ind])
                {
                    int flashWhenLateAt = _appSettings.FlashWhenLateAt;
                    if (!lag[ind] && device.Latency >= flashWhenLateAt)
                    {
                        lag[ind] = true;
                        LagFlashWarning(device, ind, true);
                    }
                    else if (lag[ind] && device.Latency < flashWhenLateAt)
                    {
                        lag[ind] = false;
                        LagFlashWarning(device, ind, false);
                    }
                }
                else
                {
                    if (DateTime.UtcNow - device.firstActive > TimeSpan.FromSeconds(5))
                    {
                        inWarnMonitor[ind] = true;
                    }
                }

                DS4State cState, tempControlState;
                if (!device.PerformStateMerge)
                {
                    cState = CurrentState[ind];
                    device.getRawCurrentState(cState);
                    tempControlState = CurrentState[ind];
                }
                else
                {
                    cState = device.JointState;
                    device.MergeStateData(cState);
                    // Need to copy state object info for use in UDP server
                    cState.CopyTo(CurrentState[ind]);
                    tempControlState = CurrentState[ind];
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
                                DS4State tempMapState = MappedState[jointInd];
                                _mapping.TempMouseJoystick(jointInd, tempMapState);
                                if (!_profileSettings.UseDInputOnlyArray[jointInd])
                                {
                                    outputDevices[jointInd]?.ConvertandSendReport(tempMapState, jointInd);
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
                    CheckForTouchToggle(ind, cState, pState);
                }

                cState = device.Debouncer.ProcessInput(cState);

                cState = _mapping.SetCurveAndDeadzone(ind, cState, TempState[ind], _profileSettings);

                if (!recordingMacro && (_profileSettings.GetUseTempProfile(ind) ||
                    _profileSettings.ContainsCustomAction(ind) || _profileSettings.ContainsCustomExtras(ind) ||
                    _profileActionProvider.GetProfileActionCount(ind) > 0))
                {
                    DS4State tempMapState = MappedState[ind];

                    if (_appSettings.UseOscSender)
                    {
                        _oscService.PreMappingStep(ind, cState, tempMapState);
                    }

                    _mapping.MapCustom(ind, cState, tempMapState, ExposedState[ind], touchPad[ind], this);

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

                    outputDevices[ind]?.ConvertandSendReport(cState, ind);

                    // 仮想コントローラー出力完了時の処理遅延を計測
                    if (IsMeasuringProcessingDelay)
                    {
                        long start = device.lastInputReportTimestamp;
                        if (start > 0)
                        {
                            long end = Stopwatch.GetTimestamp();
                            ProcessingDelayMs[ind] = (end - start) * (1000.0 / Stopwatch.Frequency);
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
                            SetDevRumble(device, Big, Small, ind);
                        }
                    }
                    */
                }
                // Phase9 Steering 削除 W3: 旧「出力デバイスがないとき［UseDInputOnly］の、ステアリングの vJoy 軸への送出」（else 側）は、機能ごと削除した（vJoy も削除）。

                // Output any synthetic events.
                _mapping.Commit(ind, _profileSettings);

                // Update the Lightbar color
                DS4LightBar.updateLightBar(device, ind, _profileSettings);

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

        // Phase9 9-E E-1: 旧 OSCPostMappingStep／OSCPreMappingStep／CompareAndSendChangesToOSC（OSC の入力への重ね合わせと変化の送信）は OscService へ移した。

        private void LagFlashWarning(DS4Device device, int ind, bool on)
        {
            if (on)
            {
                lag[ind] = true;
                LogDebug(string.Format(DS4WinWPF.Properties.Resources.LatencyOverTen, (ind + 1), device.Latency), true);
                if (_appSettings.FlashWhenLate)
                {
                    DS4Color color = new DS4Color { red = 50, green = 0, blue = 0 };
                    DS4LightBar.forcedColor[ind] = color;
                    DS4LightBar.forcedFlash[ind] = 2;
                    DS4LightBar.forcelight[ind] = true;
                }
            }
            else
            {
                lag[ind] = false;
                LogDebug(DS4WinWPF.Properties.Resources.LatencyNotOverTen.Replace("*number*", (ind + 1).ToString()));
                DS4LightBar.forcelight[ind] = false;
                DS4LightBar.forcedFlash[ind] = 0;
                device.LightBarColor = _profileSettings.GetMainColor(ind);
            }
        }

        public DS4Controls GetActiveInputControl(int ind)
        {
            DS4State cState = CurrentState[ind];
            DS4StateExposed eState = ExposedState[ind];
            Mouse tp = touchPad[ind];
            DS4Controls result = DS4Controls.None;

            if (DS4Controllers[ind] != null)
            {
                if (Mapping.getBoolButtonMapping(cState.Cross))
                    result = DS4Controls.Cross;
                else if (Mapping.getBoolButtonMapping(cState.Circle))
                    result = DS4Controls.Circle;
                else if (Mapping.getBoolButtonMapping(cState.Triangle))
                    result = DS4Controls.Triangle;
                else if (Mapping.getBoolButtonMapping(cState.Square))
                    result = DS4Controls.Square;
                else if (Mapping.getBoolButtonMapping(cState.L1))
                    result = DS4Controls.L1;
                else if (Mapping.getBoolTriggerMapping(cState.L2))
                    result = DS4Controls.L2;
                else if (Mapping.getBoolButtonMapping(cState.L3))
                    result = DS4Controls.L3;
                else if (Mapping.getBoolButtonMapping(cState.R1))
                    result = DS4Controls.R1;
                else if (Mapping.getBoolTriggerMapping(cState.R2))
                    result = DS4Controls.R2;
                else if (Mapping.getBoolButtonMapping(cState.R3))
                    result = DS4Controls.R3;
                else if (Mapping.getBoolButtonMapping(cState.DpadUp))
                    result = DS4Controls.DpadUp;
                else if (Mapping.getBoolButtonMapping(cState.DpadDown))
                    result = DS4Controls.DpadDown;
                else if (Mapping.getBoolButtonMapping(cState.DpadLeft))
                    result = DS4Controls.DpadLeft;
                else if (Mapping.getBoolButtonMapping(cState.DpadRight))
                    result = DS4Controls.DpadRight;
                else if (Mapping.getBoolButtonMapping(cState.Share))
                    result = DS4Controls.Share;
                else if (Mapping.getBoolButtonMapping(cState.Options))
                    result = DS4Controls.Options;
                else if (Mapping.getBoolButtonMapping(cState.PS))
                    result = DS4Controls.PS;
                else if (Mapping.getBoolAxisDirMapping(cState.LX, true))
                    result = DS4Controls.LXPos;
                else if (Mapping.getBoolAxisDirMapping(cState.LX, false))
                    result = DS4Controls.LXNeg;
                else if (Mapping.getBoolAxisDirMapping(cState.LY, true))
                    result = DS4Controls.LYPos;
                else if (Mapping.getBoolAxisDirMapping(cState.LY, false))
                    result = DS4Controls.LYNeg;
                else if (Mapping.getBoolAxisDirMapping(cState.RX, true))
                    result = DS4Controls.RXPos;
                else if (Mapping.getBoolAxisDirMapping(cState.RX, false))
                    result = DS4Controls.RXNeg;
                else if (Mapping.getBoolAxisDirMapping(cState.RY, true))
                    result = DS4Controls.RYPos;
                else if (Mapping.getBoolAxisDirMapping(cState.RY, false))
                    result = DS4Controls.RYNeg;
                else if (Mapping.getBoolTouchMapping(tp.leftDown))
                    result = DS4Controls.TouchLeft;
                else if (Mapping.getBoolTouchMapping(tp.rightDown))
                    result = DS4Controls.TouchRight;
                else if (Mapping.getBoolTouchMapping(tp.multiDown))
                    result = DS4Controls.TouchMulti;
                else if (Mapping.getBoolTouchMapping(tp.upperDown))
                    result = DS4Controls.TouchUpper;
            }

            return result;
        }

        public bool[] touchreleased = new bool[MAX_DS4_CONTROLLER_COUNT] { true, true, true, true, true, true, true, true },
            touchslid = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };

        public Dispatcher EventDispatcher { get => eventDispatcher; }
        public OutputSlotManager OutputslotMan { get => _virtualPad.OutputSlotManager; }

        protected void CheckForTouchToggle(int deviceID, DS4State cState, DS4State pState)
        {
            if (_profileSettings.TouchOutMode[deviceID] != TouchpadOutMode.Controls && cState.Touch1 && pState.PS)
            {
                if (_profileSettings.TouchpadActiveArray[deviceID] && touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = false;
                    LogDebug(DS4WinWPF.Properties.Resources.TouchpadMovementOff);
                    AppLogger.LogToTray(DS4WinWPF.Properties.Resources.TouchpadMovementOff);
                    touchreleased[deviceID] = false;
                }
                else if (touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = true;
                    LogDebug(DS4WinWPF.Properties.Resources.TouchpadMovementOn);
                    AppLogger.LogToTray(DS4WinWPF.Properties.Resources.TouchpadMovementOn);
                    touchreleased[deviceID] = false;
                }
            }
            else
                touchreleased[deviceID] = true;
        }

        public void StartTPOff(int deviceID)
        {
            if (deviceID < _controllerSlotLimit)
            {
                _profileSettings.TouchpadActiveArray[deviceID] = false;
            }
        }

        public string TouchpadSlide(int ind)
        {
            DS4State cState = CurrentState[ind];
            string slidedir = "none";
            if (DS4Controllers[ind] != null && cState.Touch2 &&
               !(touchPad[ind].dragging || touchPad[ind].dragging2))
            {
                if (touchPad[ind].slideright && !touchslid[ind])
                {
                    slidedir = "right";
                    touchslid[ind] = true;
                }
                else if (touchPad[ind].slideleft && !touchslid[ind])
                {
                    slidedir = "left";
                    touchslid[ind] = true;
                }
                else if (!touchPad[ind].slideleft && !touchPad[ind].slideright)
                {
                    slidedir = "";
                    touchslid[ind] = false;
                }
            }

            return slidedir;
        }

        public void LogDebug(String Data, bool warning = false)
        {
            //Console.WriteLine(System.DateTime.Now.ToString("G") + "> " + Data);
            if (Debug != null)
            {
                DebugEventArgs args = new DebugEventArgs(Data, warning);
                OnDebug(this, args);
            }
        }

        public void OnDebug(object sender, DebugEventArgs args)
        {
            if (Debug != null)
                Debug(this, args);
        }

        // sets the rumble adjusted with rumble boost. General use method
        public void setRumble(byte heavyMotor, byte lightMotor, int deviceNum)
        {
            if (deviceNum < _controllerSlotLimit)
            {
                DS4Device device = DS4Controllers[deviceNum];
                if (device != null)
                    SetDevRumble(device, heavyMotor, lightMotor, deviceNum);
                //device.setRumble((byte)lightBoosted, (byte)heavyBoosted);
            }
        }

        // sets the rumble adjusted with rumble boost. Method more used for
        // report handling. Avoid constant checking for a device.
        public void SetDevRumble(DS4Device device,
            byte heavyMotor, byte lightMotor, int deviceNum)
        {
            byte boost = _profileSettings.GetRumbleBoost(deviceNum);
            uint lightBoosted = ((uint)lightMotor * (uint)boost) / 100;
            if (lightBoosted > 255)
                lightBoosted = 255;
            uint heavyBoosted = ((uint)heavyMotor * (uint)boost) / 100;
            if (heavyBoosted > 255)
                heavyBoosted = 255;

            if (_profileSettings.InverseRumbleMotors[deviceNum])
                device.setRumble((byte)heavyBoosted, (byte)lightBoosted);
            else
                device.setRumble((byte)lightBoosted, (byte)heavyBoosted);
        }

        public DS4State getDS4State(int ind)
        {
            return CurrentState[ind];
        }

        public DS4State getDS4StateMapped(int ind)
        {
            return MappedState[ind];
        }

        public DS4State getDS4StateTemp(int ind)
        {
            return TempState[ind];
        }
    }
}