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
    public class ControlService : IInstanceIdentifiable, DS4Windows.Services.IDeviceStateAccessor, DI.IUdpMotionHost, DI.IVirtualPadHost, DI.IDeviceSlotHost, DI.IServiceLifecycleHost
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
        // Phase9 9-E E-6f2: 状態は ServiceLifecycle が持つ。Phase9 9-F F-5b: 外部の参照が 0 件の activeControllers／loopControllers／inServiceTask の転送を削除した。
        // TODO(Phase9 9-F F-5c 以降): running は、外部の 4 つの利用者（AutoProfileService／App／MainWindow／TrayIconViewModel）がまだ ControlService を直接持つため、転送を残している。
        //   利用者が IServiceLifecycle.Running を受けるように切り替えた時点で削除する（Start／Stop／slotManager など、ControlService の他のメンバーの切り離しと同時に行う）。
        public Mouse[] touchPad = new Mouse[MAX_DS4_CONTROLLER_COUNT];
        public bool running { get => _lifecycle.Running; set => _lifecycle.Running = value; }
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

        // Phase9 9-E E-6e: 切断・同期の処理（DeviceDisconnectHandler）へ、スロットごとの状態を渡す窓口（配列の参照は再代入されない）
        DS4Device[] DI.IDeviceSlotHost.DS4Controllers => DS4Controllers;
        DS4State[] DI.IDeviceSlotHost.CurrentStates => CurrentState;
        DS4State[] DI.IDeviceSlotHost.PreviousStates => PreviousState;
        Mouse[] DI.IDeviceSlotHost.TouchPads => touchPad;
        ControllerSlotManager DI.IDeviceSlotHost.SlotManager => slotManager;
        int DI.IDeviceSlotHost.ActiveControllers => _lifecycle.ActiveControllers;
        void DI.IDeviceSlotHost.OnReport(DS4Device device, EventArgs e, int ind) => On_Report(device, e, ind);

        // Phase9 9-E E-6f2: サービスの開始・停止（ServiceLifecycle）が頼る、イベントスレッドの実行とキーボード／マウス出力の初期化
        void DI.IServiceLifecycleHost.InvokeOnEventThread(Action action) => eventDispatcher.Invoke(action);
        void DI.IServiceLifecycleHost.InitializeOutputKbmHandler() => InitOutputKBMHandler();

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
        public bool suspending { get => _lifecycle.Suspending; set => _lifecycle.Suspending = value; }

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

        // Phase9 9-E E-5: 入力レポートの処理から切り出した部品（遅延の監視、タッチパッドの切り替え、押されている入力の探索）
        private readonly DI.ILatencyMonitor _latencyMonitor;
        private readonly DI.ITouchpadToggleService _touchpadToggle;
        private readonly DI.IActiveInputProbe _activeInputProbe;

        // Phase9 9-E E-6a: 接続時のプロファイルの選択と適用
        private readonly DI.IConnectionProfileSelector _connectionProfileSelector;

        // Phase9 9-E E-6b: 接続時の設定の反映とイベントの配線
        private readonly DI.IDeviceConnectionSetup _deviceConnectionSetup;

        // Phase9 9-E E-6c: デバイスの種類ごとの部品（カートリッジ）
        private readonly DI.IInputDeviceModuleRegistry _deviceModules;

        // Phase9 9-E E-6d: Joy-Con の結合の手順
        private readonly DI.IJoyConLinker _joyConLinker;

        // Phase9 9-E E-6e: 切断・同期・シリアル変更の処理
        private readonly DI.IDeviceDisconnectHandler _deviceDisconnect;

        // Phase9 9-E E-6f1: 接続の組み立て
        private readonly DI.IDeviceConnector _deviceConnector;

        // Phase9 9-E E-6f2: 開始・停止・ホットプラグと、その状態・イベント
        private readonly DI.IServiceLifecycle _lifecycle;

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
            DI.IVirtualPadService virtualPadService,
            DI.ILatencyMonitor latencyMonitor,
            DI.ITouchpadToggleService touchpadToggle,
            DI.IActiveInputProbe activeInputProbe,
            DI.IConnectionProfileSelector connectionProfileSelector,
            DI.IDeviceConnectionSetup deviceConnectionSetup,
            DI.IInputDeviceModuleRegistry deviceModules,
            DI.IJoyConLinker joyConLinker,
            DI.IDeviceDisconnectHandler deviceDisconnect,
            DI.IDeviceConnector deviceConnector,
            DI.IServiceLifecycle lifecycle)
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
            this._latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
            this._touchpadToggle = touchpadToggle ?? throw new ArgumentNullException(nameof(touchpadToggle));
            this._activeInputProbe = activeInputProbe ?? throw new ArgumentNullException(nameof(activeInputProbe));
            this._connectionProfileSelector = connectionProfileSelector ?? throw new ArgumentNullException(nameof(connectionProfileSelector));
            this._deviceConnectionSetup = deviceConnectionSetup ?? throw new ArgumentNullException(nameof(deviceConnectionSetup));
            this._deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
            this._joyConLinker = joyConLinker ?? throw new ArgumentNullException(nameof(joyConLinker));
            this._deviceDisconnect = deviceDisconnect ?? throw new ArgumentNullException(nameof(deviceDisconnect));
            this._deviceConnector = deviceConnector ?? throw new ArgumentNullException(nameof(deviceConnector));
            this._lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            // 開始・停止のログとイベントも、これまでどおり ControlService の Debug イベント／公開イベント（送り主は ControlService）へ流す
            this._lifecycle.DebugMessage += (data, warning) => LogDebug(data, warning);
            this._lifecycle.ServiceStarted += (sender, e) => ServiceStarted?.Invoke(this, e);
            this._lifecycle.PreServiceStop += (sender, e) => PreServiceStop?.Invoke(this, e);
            this._lifecycle.ServiceStopped += (sender, e) => ServiceStopped?.Invoke(this, e);
            this._lifecycle.RunningChanged += (sender, e) => RunningChanged?.Invoke(this, e);
            this._lifecycle.HotplugController += (device, index) => HotplugController?.Invoke(this, device, index);
            // 切断・同期の処理のログも、これまでどおり ControlService の Debug イベントへ流す
            this._deviceDisconnect.DebugMessage += (data, warning) => LogDebug(data, warning);
            // 設定の反映のログも、これまでどおり ControlService の Debug イベントへ流す
            this._deviceConnectionSetup.DebugMessage += (data, warning) => LogDebug(data, warning);
            // 遅延の監視・タッチパッドの切り替えのログも、これまでどおり ControlService の Debug イベントへ流す
            this._latencyMonitor.DebugMessage += (data, warning) => LogDebug(data, warning);
            this._touchpadToggle.DebugMessage += (data, warning) => LogDebug(data, warning);
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
                _lifecycle.LoopControllers = false;
                while (_lifecycle.InServiceTask)
                    Thread.SpinWait(1000);

                LogDebug(string.Format(DS4WinWPF.Translations.Strings.ViGEmPluginFailure, errorCode),
                    true);
                Stop();
            }));
        }

        // Phase9 9-E E-6c: 種類ごとの処理（Joy-Con の結合の初期値）は IInputDeviceModule へ移した
        public void PostDS4DeviceInit(DS4Device device)
        {
            _deviceModules.Resolve(device.DeviceType).OnDeviceInit(device, deviceOptions);
        }



        // Phase9 9-E E-6c: 種類ごとの有効設定の判定は IInputDeviceModule へ移した（扱う部品がない種類は false）
        public bool CheckForSupportedDevice(HidDevice device, VidPidInfo metaInfo)
        {
            return _deviceModules.Resolve(metaInfo.inputDevType).IsEnabled(deviceOptions);
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

        // Phase9 9-E E-6f2: 本体は ServiceLifecycle へ移した（薄い委譲。呼び出し側は 9-F まで変えない）
        public bool Start(bool showlog = true)
        {
            return _lifecycle.Start(showlog);
        }

        // Phase9 9-E E-3: 旧 PrepareDevUDPMotion（モーションの送出のハンドラー）は UdpMotionService.PrepareDeviceMotion へ移した。



        public void PrepareAbort()
        {
            _lifecycle.PrepareAbort();
        }

        public bool Stop(bool showlog = true, bool immediateUnplug = false)
        {
            return _lifecycle.Stop(showlog, immediateUnplug);
        }

        public bool HotPlug()
        {
            return _lifecycle.HotPlug();
        }





        // Phase9 9-E E-3: 旧 ResetUdpSmoothingFilters／ChangeUdpSmoothingAttrs（スムージングのフィルター）は UdpMotionService へ移した。

        // Phase9 9-E E-6b: 設定の反映（CheckProfileOptions／CheckLauchProfileOption）とイベントの配線（SetupInitialHookEvents）は DeviceConnectionSetup へ移した
        // CheckProfileOptions は外部の呼び出し（BackingStore）があるため、薄い委譲として残す
        public void CheckProfileOptions(int ind, DS4Device device, bool startUp = false)
        {
            _deviceConnectionSetup.ApplyProfileOptions(ind, device, touchPad[ind], startUp);
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
            _deviceConnectionSetup.AttachTouchpad(ind, device, touchPad);
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





        // Called when DS4 is disconnected or timed out


        // Phase9 9-E E-5: 旧 lag／inWarnMonitor／tempStrings（遅延の監視の状態）は LatencyMonitor へ移した。

        // Called every time a new input report has arrived
        protected void On_Report(DS4Device device, EventArgs e, int ind)
        {
            if (ind != -1)
            {
                _latencyMonitor.OnReport(device, ind);

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
                    _touchpadToggle.CheckForToggle(ind, cState, pState);
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

        // Phase9 9-E E-5: 判定の本体（約 70 行）は ActiveInputProbe へ移した
        public DS4Controls GetActiveInputControl(int ind)
        {
            return _activeInputProbe.GetActive(CurrentState[ind], touchPad[ind], DS4Controllers[ind] != null);
        }

        public Dispatcher EventDispatcher { get => eventDispatcher; }
        public OutputSlotManager OutputslotMan { get => _virtualPad.OutputSlotManager; }

        // Phase9 9-E E-5: 判定の本体は TouchpadToggleService へ移した（状態 touchreleased／touchslid も）
        protected void CheckForTouchToggle(int deviceID, DS4State cState, DS4State pState)
        {
            _touchpadToggle.CheckForToggle(deviceID, cState, pState);
        }

        public void StartTPOff(int deviceID)
        {
            _touchpadToggle.StartTouchpadOff(deviceID);
        }

        public string TouchpadSlide(int ind)
        {
            return _touchpadToggle.Slide(ind, CurrentState[ind], touchPad[ind], DS4Controllers[ind] != null);
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