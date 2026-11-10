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
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Hid;
using DS4Windows.Output;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing
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
        private readonly DS4Windows.Processing.Abstractions.IInputReportProcessor _inputReport;   // Phase13-Step5b
        private readonly InputDeviceCore[] DS4Controllers;
        // Phase9 9-E E-6f2: 状態は ServiceLifecycle が持つ。Phase9 9-F F-5b: 外部の参照が 0 件の activeControllers／loopControllers／inServiceTask の転送を削除した。
        // Phase13-Step9: running／suspending の転送は削除した（利用者の App／MainWindow／TrayIconViewModel は IServiceLifecycle を直接使う）。
        private readonly DS4State[] PreviousState;
        private readonly DS4State[] TempState;
        bool[] buttonsdown = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
        bool[] held = new bool[MAX_DS4_CONTROLLER_COUNT];
        int[] oldmouse = new int[MAX_DS4_CONTROLLER_COUNT] { -1, -1, -1, -1, -1, -1, -1, -1 };

        // Phase13-Step5b: IDeviceSlotHost（ActiveControllers と OnReport の窓口）は削除した。接続中の数は IDeviceSlotState、入力レポートの処理は IInputReportProcessor

        // Phase13-Step3: イベントスレッドは IEventThreadDispatcher、KBM 出力の初期化は IOutputKbmInitializer へ移した（IServiceLifecycleHost は廃止）

        // Phase9 9-E E-3: 旧 udpEuroPairAccel／udpEuroPairGyro（UDP モーションのスムージングのフィルター）は UdpMotionService へ移した
#pragma warning disable CS0169 // tempBusThread is intentionally unused currently
        Thread tempBusThread;
#pragma warning restore CS0169
        // Phase13-Step3: 旧 eventDispatchThread／eventDispatcher（イベントスレッド）は EventThreadDispatcher へ移した
        private readonly DS4Windows.Processing.Abstractions.IEventThreadDispatcher _eventThread;

        // Phase9 9-E E-3: 旧 _udpServer／udpOutBuffers／udpChangeStatus などは UdpMotionService へ移した
        // Phase9 9-E E-4: 旧 outputslotMan／vigemTestClient／tempThread は VirtualPadService へ移した
        private readonly DS4Windows.Processing.Abstractions.IVirtualPadService _virtualPad;

        private readonly DS4Windows.Core.Abstractions.IProfileSettingsService _profileSettings;


        // Phase6-Step2-2 (PR-2)
        // Phase13-Step7 7-d: 旧: 遅延取得デリゲート（OutputSlotService → ControlService の逆依存の回避）をやめ、直接注入にした。
        // 逆依存は Phase9 E-4 で解消済みで、OutputSlotService は VirtualPadService だけを見る。
        private readonly DS4Windows.Processing.Abstractions.IOutputSlotService _outputSlotService;
        private OutContType[] _activeOutDevType;
        // ActiveOutDevType の配列参照は再代入されない（OutputSlotService の配列は要素書込みのみ）ため、初回に1回だけ取得する。
        // 以降は null 合体の1分岐のみで、ホットパスでもアロケーションは発生しない。
        private OutContType[] ActiveOutDevType
            => _activeOutDevType ?? (_activeOutDevType = _outputSlotService.ActiveOutDevType);
        private readonly DS4Windows.Core.Abstractions.IProfileRepository _profileRepository;

        // Phase6-Step2-3 (PR-3): KBM 出力の送出（IVirtualKBM）とハンドラのライフサイクル（IVirtualKBMLifecycle）を分離（決定D3）
        // Phase13-Step5b-5b-e: 旧 _virtualKBM／_appearanceSettings／_profileActionProvider／_specialActionRepository は、On_Report（InputReportProcessor へ移設）と Mapping の逆依存の解消で、使われなくなったため削除した

        // Phase6-Step2-5 (PR-5): 入力処理ホットパス（On_Report）で参照するサービス。
        // いずれも呼び出し1回あたりの新規オブジェクト割り当て・ログ・キャッシュを行わない（ゼロアロケーション方針）。
        // Phase7-Step8 W1: 旧 StickMotionState.Shared／SyntheticInputState.Shared（静的な窓口）。コンストラクターで受ける。
        private readonly StickMotionState _stickMotion;
        // Phase7-Step8 W2: 入力の評価の窓口（旧: Mapping の静的メソッド）。画面（ControllerReadingsControl、RecordBoxViewModel）も、これを経由して使う
        private readonly DS4Windows.Processing.Abstractions.IMapping _mapping;
        // コントローラースロット上限（現在接続台数ではない）。プロセス内で不変のためコンストラクタで1回だけ取得する。

        // ---- Phase6-Step3-2: Mapping.cs への引数渡し（S3方針）用の読み取り専用アクセサ ----
        // Mapping.cs の各メソッドは既に ControlService（ctrl）を引数として受け取っているため、
        // 新たな static Service Locator を追加せず、この既存の受け渡し経路を通じてサービスを渡す。
        internal DS4Windows.Core.Abstractions.IProfileRepository ProfileRepository => _profileRepository;
        // Phase6-Step3-3: MapCustom の ButtonAbsMouseInfos（絶対マウス出力の座標系設定）参照を
        // ctrl 経由の引数渡しへ切り替えるために追加。既存の _profileSettings フィールドをそのまま公開するのみで、
        // 新規サービス・新規フィールドは追加していない。ButtonAbsMouseInfos 自体はその後の機能廃止
        // （Abs Mouse機能削除、copilot-instructions.md §2.2 例外規定）で無くなったが、本プロパティは
        // Step3-6（GetControlSettingsGroup 等）で再利用する汎用アクセサとして存置している。
        internal DS4Windows.Core.Abstractions.IProfileSettingsService ProfileSettingsService => _profileSettings;

        // Phase9 9-E E-2: 旧 hidDeviceHiding*（HidHide の状態）は HidHideService へ移した

        // Phase9 9-E E-5: 入力レポートの処理から切り出した部品（遅延の監視、タッチパッドの切り替え、押されている入力の探索）

        // Phase9 9-E E-6b: 接続時の設定の反映とイベントの配線

        // デバイスの登録簿へのフック、適用プロファイルの記憶、仮想パッドの失敗の処理は、それぞれの部品が持つ（コンストラクターで結ぶ）

        // Phase9 9-E E-6e: 切断・同期・シリアル変更の処理


        //public event EventHandler HotplugFinished;

        // Phase9 9-E E-1: 旧 oscState／oscCallback／oscListener／oscSender（OSC の状態・受信・送信）は OscService へ移した

        // Phase13-Step5: 旧 GetPadDetailForIdx（UDP モーションのパッドの情報）は UdpMotionService へ移した

        public ControlService(
            DS4Windows.Core.Abstractions.IProfileSettingsService profileSettings,
            DS4Windows.Processing.Abstractions.IOutputSlotService outputSlotService,
            DS4Windows.Core.Abstractions.IProfileRepository profileRepository,
            StickMotionState stickMotion,
            DS4Windows.Processing.Abstractions.IMapping mapping,
            DS4Windows.Processing.Abstractions.IVirtualPadService virtualPadService,
            DS4Windows.Processing.Abstractions.IEventThreadDispatcher eventThread,
            DS4Windows.Processing.Abstractions.IDeviceSlotState slots,
            DS4Windows.Processing.Abstractions.IInputReportProcessor inputReport,
            DS4Windows.Processing.Abstractions.IDeviceRegistryHooks deviceRegistryHooks,
            DS4Windows.Processing.Abstractions.IAppliedProfileRecorder appliedProfileRecorder,
            DS4Windows.Processing.Abstractions.IBackendFailureHandler backendFailureHandler)
        {
            // Phase13-Step5b: 入力レポートの処理は InputReportProcessor が持つ（MappedState／CurrentStates／ExposedState の初期化も、その先頭で済んでいる）
            this._inputReport = inputReport ?? throw new ArgumentNullException(nameof(inputReport));
            // Phase13-Step5: スロット状態の配列の参照を一度だけ取得して保持する（以降の読み書きは、従来どおりこれらのフィールドを通す）
            this.DS4Controllers = slots.DS4Controllers;
            this.PreviousState = slots.PreviousStates;
            this.TempState = slots.TempStates;
            // TODO(技術的負債): 過渡期の防御コード。DI 経由の生成（ServiceRegistration）では profileSettings は常に非 null。
            // 旧シグネチャ（profileSettings = null）との互換のために残している。Phase6-Step12 で削除判断（Phase6-Step2-Plan.md 決定D2）。
            this._profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings)); // Phase6-Step10-4-5: Global のフォールバックを廃止
            this._outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            this._profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            // 適用のたびに、そのスロットのコントローラー（MAC）に、適用したプロファイルを記憶する（再接続で使う）
            (appliedProfileRecorder ?? throw new ArgumentNullException(nameof(appliedProfileRecorder))).Attach();
            this._stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            this._mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            this._virtualPad = virtualPadService ?? throw new ArgumentNullException(nameof(virtualPadService));
            this._eventThread = eventThread ?? throw new ArgumentNullException(nameof(eventThread));

            Crc32Algorithm.InitializeTable(InputDeviceCore.DefaultPolynomial);

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
            (deviceRegistryHooks ?? throw new ArgumentNullException(nameof(deviceRegistryHooks))).Attach();
            (backendFailureHandler ?? throw new ArgumentNullException(nameof(backendFailureHandler))).Attach();

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

        public void ShutDown()
        {
            _virtualPad.ShutDownOutputSlots();
            // Phase7-Step5 W3（Q9＝A）: 契約（IOutputSlotService）経由に切り替えた。同じスロット管理と保存先で OutputSlotPersist.WriteConfig を呼ぶ
            _outputSlotService.SaveOutputSlots();

            _eventThread.Shutdown();
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

        // Phase9 9-E E-4: 旧 StartViGEm／StopViGEm／AssignInitialDevices／EstablishOutDevice／EstablishOutFeedback／RemoveOutFeedback／AttachNewUnboundOutDev／
        // AttachUnboundOutDev／DetachUnboundOutDev／PluginOutDev／UnplugOutDev の本体は VirtualPadService へ移した。
        // Phase13-Step10 10-b: PluginOutDev／UnplugOutDev の転送は削除した（外部の利用者は 0 件。BackingStore／LegacyProfileXmlSerializer は IProfileLoadTarget を使う）。

        // Phase9 9-E E-6f2: 本体は ServiceLifecycle へ移した（薄い委譲。呼び出し側は 9-F まで変えない）

        // Phase9 9-E E-3: 旧 PrepareDevUDPMotion（モーションの送出のハンドラー）は UdpMotionService.PrepareDeviceMotion へ移した。






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

        // Called when DS4 is disconnected or timed out


        // Phase9 9-E E-5: 旧 lag／inWarnMonitor／tempStrings（遅延の監視の状態）は LatencyMonitor へ移した。

        // Called every time a new input report has arrived
        // Phase13-Step5b: 旧 On_Report（入力レポートの処理。約 200 行）は InputReportProcessor へ移した

        // Phase9 9-E E-1: 旧 OSCPostMappingStep／OSCPreMappingStep／CompareAndSendChangesToOSC（OSC の入力への重ね合わせと変化の送信）は OscService へ移した。

        // Phase13-Step10 10-d: GetActiveInputControl の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。


        // Phase9 9-E E-5: 判定の本体は TouchpadToggleService へ移した（状態 touchreleased／touchslid も）
        // Phase13-Step10 10-b: CheckForTouchToggle／StartTPOff の転送は削除した（外部の利用者は 0 件。StartTPOff は IProfileLoadTarget が持つ）。

        // Phase13-Step10 10-c: TouchpadSlide の転送は削除した（利用者は IRuntimeFeatureSwitch を使う）。

        // Phase13-Step10 10-d: setRumble の転送は削除した（利用者は IRuntimeFeatureSwitch.SetRumble を使う）。

        // Phase13-Step3: 旧 SetDevRumble（ブースト・反転つきの振動）は IDeviceRumbleService へ移した

    }
}