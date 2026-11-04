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
    public class ControlService : IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        // Might be useful for ScpVBus build
        public const int EXPANDED_CONTROLLER_COUNT = 8;
        public const int MAX_DS4_CONTROLLER_COUNT = ControllerLimits.MaxControllerCount;
        // Phase7-Step10e W2: 静的な互換シム CURRENT_DS4_CONTROLLER_LIMIT は削除した。値の SSOT は IEnvironmentService.ControllerSlotLimit / UsingMaxControllers。
        // ControlService 内部は注入値 _controllerSlotLimit を使用する（Phase6-Step2-Plan.md 決定O2/O3）。
        // Phase7-Step10b W4: USING_MAX_CONTROLLERS（静的シム）は、呼び出し元 3 箇所を IEnvironmentService.UsingMaxControllers の注入に替えて削除した。
        // Phase13-Step5: スロット状態の配列は IDeviceSlotState（DeviceSlotState）が所有する。ここは、コンストラクターで一度だけ取得した参照を保持する（ホットパス［On_Report］は従来どおりこのフィールドを読む。配列の参照は再代入されない）。
        // TODO(Phase13-Step10): 公開の DS4Controllers／touchPad／slotManager は、UI など約 50 か所が ControlService 経由で読んでいるため、同名のまま温存している。呼び出し側を IDeviceSlotState の直接注入へ切り替えた時点で削除する。
        private readonly DI.IDeviceSlotState _slots;
        private readonly DI.IInputReportProcessor _inputReport;   // Phase13-Step5b
        public readonly DS4Device[] DS4Controllers;
        public DS4Device GetController(int deviceIndex) => _slots.GetController(deviceIndex);
        // Phase9 9-E E-6f2: 状態は ServiceLifecycle が持つ。Phase9 9-F F-5b: 外部の参照が 0 件の activeControllers／loopControllers／inServiceTask の転送を削除した。
        // Phase13-Step9: running／suspending の転送は削除した（利用者の App／MainWindow／TrayIconViewModel は IServiceLifecycle を直接使う）。
        public readonly Mouse[] touchPad;
        private readonly DS4State[] CurrentState;
        private readonly DS4State[] PreviousState;
        private readonly DS4State[] TempState;
        public readonly ControllerSlotManager slotManager;
        // TODO(Phase13-Step10): 旧 recordingMacro／IsMeasuringProcessingDelay／ProcessingDelayMs／GetProcessingDelay（入力レポートの処理の作業用の状態）は、
        //   Phase13-Step5b で IInputReportProcessor へ移した。画面（RecordBox、ControllerReadingsControl）がまだ ControlService 経由で使うため、転送だけ残している
        //   （外部の利用者を IInputReportProcessor の直接注入へ切り替えた時点で、Step10 で削除する。決定: Phase13-Step5b-Plan §9 の 4）。
        public bool recordingMacro { get => _inputReport.RecordingMacro; set => _inputReport.RecordingMacro = value; }
        public event EventHandler<DebugEventArgs> Debug = null;
        bool[] buttonsdown = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        bool[] held = new bool[MAX_DS4_CONTROLLER_COUNT];
        int[] oldmouse = new int[MAX_DS4_CONTROLLER_COUNT] { -1, -1, -1, -1, -1, -1, -1, -1 };
        // Phase9 9-E E-4: 旧 公開フィールド outputDevices は VirtualPadService が持つ（同じ配列を返す窓口。呼び出し側は 9-F まで変えない）
        public OutputDevice[] outputDevices => _virtualPad.OutputDevices;

        // Phase13-Step5b: IDeviceSlotHost（ActiveControllers と OnReport の窓口）は削除した。接続中の数は IDeviceSlotState、入力レポートの処理は IInputReportProcessor

        // Phase13-Step3: イベントスレッドは IEventThreadDispatcher、KBM 出力の初期化は IOutputKbmInitializer へ移した（IServiceLifecycleHost は廃止）

        // ControllerReadings タブ表示中のみ有効化される遅延計測用
        // Phase13-Step5b: 実体は IInputReportProcessor へ移した（上の recordingMacro の TODO を参照。Step10 で削除）
        public bool IsMeasuringProcessingDelay { get => _inputReport.IsMeasuringProcessingDelay; set => _inputReport.IsMeasuringProcessingDelay = value; }
        public double[] ProcessingDelayMs => _inputReport.ProcessingDelayMs;
        public double GetProcessingDelay(int deviceIndex) => _inputReport.GetProcessingDelay(deviceIndex);
        // Phase9 9-E E-3: 旧 udpEuroPairAccel／udpEuroPairGyro（UDP モーションのスムージングのフィルター）は UdpMotionService へ移した
#pragma warning disable CS0169 // tempBusThread is intentionally unused currently
        Thread tempBusThread;
#pragma warning restore CS0169
        // Phase13-Step3: 旧 eventDispatchThread／eventDispatcher（イベントスレッド）は EventThreadDispatcher へ移した
        private readonly DI.IEventThreadDispatcher _eventThread;

        // Phase9 9-E E-3: 旧 _udpServer／udpOutBuffers／udpChangeStatus などは UdpMotionService へ移した
        // Phase9 9-E E-4: 旧 outputslotMan／vigemTestClient／tempThread は VirtualPadService へ移した
        private readonly DI.IVirtualPadService _virtualPad;
        public DI.IVirtualPadService VirtualPadService => _virtualPad;

        // Phase 3 Followup Step F-2: DS4Devices static access routed through DI.
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly DI.IProfileSettingsService _profileSettings;

        // Phase6-Step2-1 (PR-1): Global 直接参照の解消。SSOT は Global/BackingStore のまま、
        // 薄い委譲サービス経由で参照する（Pure DI: コンストラクタ注入のみ。AppHost.Services は使用しない）。
        private readonly DI.IAppSettingsService _appSettings;
        private readonly DI.IPathService _pathService;

        // Phase6-Step2-2 (PR-2)
        // Phase13-Step7 7-d: 旧: 遅延取得デリゲート（OutputSlotService → ControlService の逆依存の回避）をやめ、直接注入にした。
        // 逆依存は Phase9 E-4 で解消済みで、OutputSlotService は VirtualPadService だけを見る。
        private readonly DI.IOutputSlotService _outputSlotService;
        private OutContType[] _activeOutDevType;
        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）ため、初回に1回だけ取得する。
        // 以降は null 合体の1分岐のみで、ホットパスでもアロケーションは発生しない。
        private OutContType[] ActiveOutDevType
            => _activeOutDevType ?? (_activeOutDevType = _outputSlotService.ActiveOutDevType);
        private readonly DI.IProfileRepository _profileRepository;
        private readonly DI.IDeviceStateService _deviceStateService;

        // Phase6-Step2-3 (PR-3): KBM 出力の送出（IVirtualKBM）とハンドラのライフサイクル（IVirtualKBMLifecycle）を分離（決定D3）
        // Phase13-Step5b-5b-e: 旧 _virtualKBM／_appearanceSettings／_profileActionProvider／_specialActionRepository は、On_Report（InputReportProcessor へ移設）と Mapping の逆依存の解消で、使われなくなったため削除した

        // Phase6-Step2-5 (PR-5): 入力処理ホットパス（On_Report）で参照するサービス。
        // いずれも呼び出し1回あたりの新規オブジェクト割り当て・ログ・キャッシュを行わない（ゼロアロケーション方針）。
        // Phase7-Step5 W4（Q6＝A）: 旧 AppHost.GetService<IElevatedProcessLauncher>()。デバイス再有効化の昇格要求で使う
        private readonly Services.IElevatedProcessLauncher _elevatedProcessLauncher;
        // Phase7-Step8 W1: 旧 StickMotionState.Shared／SyntheticInputState.Shared（静的な窓口）。コンストラクターで受ける。
        private readonly StickMotionState _stickMotion;
        // Phase7-Step8 W2: 入力の評価の窓口（旧: Mapping の静的メソッド）。画面（ControllerReadingsControl、RecordBoxViewModel）も、これを経由して使う
        private readonly DI.IMapping _mapping;
        public DI.IMapping MappingService => _mapping;
        // コントローラースロット上限（現在接続台数ではない）。プロセス内で不変のためコンストラクタで1回だけ取得する。

        // ---- Phase6-Step3-2: Mapping.cs への引数渡し（S3方針）用の読み取り専用アクセサ ----
        // Mapping.cs の各メソッドは既に ControlService（ctrl）を引数として受け取っているため、
        // 新たな static Service Locator を追加せず、この既存の受け渡し経路を通じてサービスを渡す。
        internal DI.IProfileRepository ProfileRepository => _profileRepository;
        // Phase6-Step3-3: MapCustom の ButtonAbsMouseInfos（絶対マウス出力の座標系設定）参照を
        // ctrl 経由の引数渡しへ切り替えるために追加。既存の _profileSettings フィールドをそのまま公開するのみで、
        // 新規サービス・新規フィールドは追加していない。ButtonAbsMouseInfos 自体はその後の機能廃止
        // （Abs Mouse機能削除、copilot-instructions.md §2.2 例外規定）で無くなったが、本プロパティは
        // Step3-6（GetControlSettingsGroup 等）で再利用する汎用アクセサとして存置している。
        internal DI.IProfileSettingsService ProfileSettingsService => _profileSettings;

        // Phase9 9-E E-2: 旧 hidDeviceHiding*（HidHide の状態）は HidHideService へ移した

        // Phase9 9-E E-5: 入力レポートの処理から切り出した部品（遅延の監視、タッチパッドの切り替え、押されている入力の探索）

        // Phase9 9-E E-6b: 接続時の設定の反映とイベントの配線

        // Phase9 9-E E-6c: デバイスの種類ごとの部品（カートリッジ）
        private readonly DI.IInputDeviceModuleRegistry _deviceModules;

        // Phase9 9-E E-6e: 切断・同期・シリアル変更の処理

        // Phase9 9-E E-6f2: 開始・停止・ホットプラグと、その状態・イベント
        private readonly DI.IServiceLifecycle _lifecycle;

        private ControlServiceDeviceOptions deviceOptions;
        public ControlServiceDeviceOptions DeviceOptions { get => deviceOptions; }

        public event EventHandler ServiceStarted;
        public event EventHandler PreServiceStop;
        public event EventHandler ServiceStopped;
        public event EventHandler RunningChanged;
        //public event EventHandler HotplugFinished;
        public delegate void HotplugControllerHandler(ControlService sender, DS4Device device, int index);
        public event HotplugControllerHandler HotplugController;

        // Phase9 9-E E-1: 旧 oscState／oscCallback／oscListener／oscSender（OSC の状態・受信・送信）は OscService へ移した

        // Phase13-Step5: 旧 GetPadDetailForIdx（UDP モーションのパッドの情報）は UdpMotionService へ移した

        public ControlService(IDs4DeviceRegistry deviceRegistry,
            DI.IProfileSettingsService profileSettings,
            DI.IAppSettingsService appSettings,
            DI.IPathService pathService,
            DI.IOutputSlotService outputSlotService,
            DI.IProfileRepository profileRepository,
            DI.IDeviceStateService deviceStateService,
            Services.IElevatedProcessLauncher elevatedProcessLauncher,
            StickMotionState stickMotion,
            DI.IMapping mapping,
            DI.IVirtualPadService virtualPadService,
            DI.IServiceDebugRelay debugRelay,
            DI.IInputDeviceModuleRegistry deviceModules,
            DI.IServiceLifecycle lifecycle,
            DI.IEventThreadDispatcher eventThread,
            DI.IDeviceSlotState slots,
            DI.IInputReportProcessor inputReport)
        {
            // Phase13-Step5b: 入力レポートの処理は InputReportProcessor が持つ（MappedState／CurrentStates／ExposedState の初期化も、その先頭で済んでいる）
            this._inputReport = inputReport ?? throw new ArgumentNullException(nameof(inputReport));
            // Phase13-Step5: スロット状態の配列の参照を一度だけ取得して保持する（以降の読み書きは、従来どおりこれらのフィールドを通す）
            this._slots = slots ?? throw new ArgumentNullException(nameof(slots));
            this.DS4Controllers = slots.DS4Controllers;
            this.touchPad = slots.TouchPads;
            this.CurrentState = slots.CurrentStates;
            this.PreviousState = slots.PreviousStates;
            this.TempState = slots.TempStates;
            this.slotManager = slots.SlotManager;
            this._deviceRegistry = deviceRegistry;
            // TODO(技術的負債): 過渡期の防御コード。DI 経由の生成（ServiceRegistration）では profileSettings は常に非 null。
            // 旧シグネチャ（profileSettings = null）との互換のために残している。Phase6-Step12 で削除判断（Phase6-Step2-Plan.md 決定D2）。
            this._profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings)); // Phase6-Step10-4-5: Global のフォールバックを廃止
            this._appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this._pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            this._outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            this._profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            this._deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            // 適用のたびに、そのスロットのコントローラー（MAC）に、適用したプロファイルを記憶する（再接続で使う）
            this._profileRepository.SelectedProfileChanged += RememberAppliedProfile;
            this._elevatedProcessLauncher = elevatedProcessLauncher ?? throw new ArgumentNullException(nameof(elevatedProcessLauncher));
            this._stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            this._mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            this._virtualPad = virtualPadService ?? throw new ArgumentNullException(nameof(virtualPadService));
            this._deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
            this._lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this._eventThread = eventThread ?? throw new ArgumentNullException(nameof(eventThread));
            // 開始・停止のログとイベントも、これまでどおり ControlService の Debug イベント／公開イベント（送り主は ControlService）へ流す
            this._lifecycle.DebugMessage += (data, warning) => LogDebug(data, warning);
            this._lifecycle.ServiceStarted += (sender, e) => ServiceStarted?.Invoke(this, e);
            this._lifecycle.PreServiceStop += (sender, e) => PreServiceStop?.Invoke(this, e);
            this._lifecycle.ServiceStopped += (sender, e) => ServiceStopped?.Invoke(this, e);
            this._lifecycle.RunningChanged += (sender, e) => RunningChanged?.Invoke(this, e);
            this._lifecycle.HotplugController += (device, index) => HotplugController?.Invoke(this, device, index);
            // HidHide、UDP モーション、遅延の監視、タッチパッドの切り替え、接続時の設定、切断・同期のログも、これまでどおり ControlService の Debug イベントへ流す
            // （Phase13-Step10 10-e: 6 つのサービスの DebugMessage は IServiceDebugRelay がまとめて受ける）
            if (debugRelay == null) throw new ArgumentNullException(nameof(debugRelay));
            debugRelay.DebugMessage += (data, warning) => LogDebug(data, warning);
            // 仮想パッド系のログも、これまでどおり ControlService の Debug イベントへ流す
            this._virtualPad.DebugMessage += (data, warning) => LogDebug(data, warning);

            Crc32Algorithm.InitializeTable(DS4Device.DefaultPolynomial);

            // Phase13-Step3: イベントスレッドの生成は EventThreadDispatcher のコンストラクターへ移した

            for (int i = 0, arlength = DS4Controllers.Length; i < arlength; i++)
            {
                // Phase13-Step5b: MappedState／CurrentState／ExposedState の作成は InputReportProcessor のコンストラクターへ移した
                TempState[i] = new DS4State();
                PreviousState[i] = new DS4State();

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

        // Phase13-Step3: 旧 RefreshOutputKBMHandler（呼び出し元 0 件。38a41ffa で呼び出し元が削除された残骸）は削除、旧 InitOutputKBMHandler は OutputKbmInitializer へ移した。

        private void OutputslotMan_ViGEmFailure(object sender, int errorCode)
        {
            _eventThread.Queue((Action)(() =>
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
            _outputSlotService.SaveOutputSlots();

            _eventThread.Shutdown();
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

        // Phase13-Step10 10-c: CheckHidHidePresence／UpdateHidHiddenAttributes の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。

        // Phase9 9-E E-2: 旧 CheckAffected は HidHideService.IsAffected へ移した。

        /// <summary>
        /// Obtain extra mappable controls not on a DS4 that should be added
        /// to the checked inputs list. Keeps Mapping class from having to check
        /// extra Switch Pro and JoyCon buttons for DS4 controllers
        /// </summary>
        /// <param name="dev">Instance of input device</param>
        /// <returns>List of extra controls to check in Mapping class</returns>

        // Phase9 9-E E-2: 旧 ChangeExclusiveStatus は HidHideService.MarkAffected へ移した。

        // Phase13-Step3: 旧 QueueOnEventThread（イベントスレッドでの実行）は IEventThreadDispatcher.Queue へ移した

        // Phase13-Step5: 旧 IUdpMotionHost の GetCurrentState／GetTempState は IDeviceSlotState へ移した

        // Phase13-Step10 10-c: ChangeUDPStatus／ChangeOSCListenerStatus／ChangeOSCSenderStatus の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。

        // Phase9 9-E E-1: 旧 SendOsc（Phase6-Step9-3c の K9-1 の競合対策つき）は OscService.SendOsc へ移した。

        // Phase13-Step10 10-c: ChangeMotionEventStatus の転送も削除した。

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
        // Phase13-Step10 10-b: PluginOutDev／UnplugOutDev の転送は削除した（外部の利用者は 0 件。BackingStore／LegacyProfileXmlSerializer は IProfileLoadTarget を使う）。

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
        // Phase13-Step10 10-b: CheckProfileOptions の転送は削除した（外部の利用者は 0 件。BackingStore は IProfileLoadTarget を使う）。

        /// <summary>
        /// Perform Mapping property resetting as needed before loading profile settings
        /// </summary>
        /// <param name="device">Input device instance</param>
        // Phase13-Step6 6-e: 旧 PreLoadReset（プロファイル読込前のリセット）は IProfileLoadTarget.PreLoadReset（ProfileLoadTarget）へ移した（呼び出し元は LegacyProfileXmlSerializer だけだった）。

        // Phase13-Step10 10-b: TouchPadOn の転送は削除した（外部の利用者は 0 件）。

        // Phase13-Step6 6-b: 旧 GetDS4Battery（電池残量の文言）は IDeviceBatteryText へ移した（呼び出し元は DefaultBatteryCheckPort だけだった）

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

                // Phase11 W6 K-W6-11（S9-5 の実機で判明）: 一時プロファイル（自動プロファイル、一時の Load Profile）は記憶しない。
                // 記憶すると、切断前に自動プロファイル（verifyG）だった場合、再接続でそれが「通常のプロファイル」として最初から適用されてしまう。
                // 一時プロファイルの適用は LoadTempProfile が UseTempProfile を立てたあとで、このイベントが発火する
                if (_profileSettings.GetUseTempProfile(slot)) return;

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
        // Phase13-Step5b: 旧 On_Report（入力レポートの処理。約 200 行）は InputReportProcessor へ移した

        // Phase9 9-E E-1: 旧 OSCPostMappingStep／OSCPreMappingStep／CompareAndSendChangesToOSC（OSC の入力への重ね合わせと変化の送信）は OscService へ移した。

        // Phase13-Step10 10-d: GetActiveInputControl の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。

        public OutputSlotManager OutputslotMan { get => _virtualPad.OutputSlotManager; }

        // Phase9 9-E E-5: 判定の本体は TouchpadToggleService へ移した（状態 touchreleased／touchslid も）
        // Phase13-Step10 10-b: CheckForTouchToggle／StartTPOff の転送は削除した（外部の利用者は 0 件。StartTPOff は IProfileLoadTarget が持つ）。

        // Phase13-Step10 10-c: TouchpadSlide の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。

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

        // Phase13-Step10 10-d: setRumble の転送は削除した（利用者は IRuntimeFeatureSwitch.SetRumble を使う）。

        // Phase13-Step3: 旧 SetDevRumble（ブースト・反転つきの振動）は IDeviceRumbleService へ移した

        public DS4State getDS4State(int ind)
        {
            return CurrentState[ind];
        }

        public DS4State getDS4StateTemp(int ind)
        {
            return TempState[ind];
        }
    }
}