using IVirtualKBM = DS4Windows.Services.IVirtualKBM;
using DS4Windows.Services;
using DS4WinWPF;
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
// Phase6-Step3-7 で削除可否を判定: 見送り。getTransitionedColor（純粋計算、Step3対象外の除外51件の1つ、
// Phase6-Step3-Plan.md §1）が非修飾で呼ばれており本ディレクティブに依存するため、現時点では削除できない。
// これ以外の Global 実利用箇所はすべて Global. 明示修飾（温存3件: ProfileSettingsServiceInstance／outputKBMHandler／ApplyProfile。
// いずれも上記のTODO参照）または各DIサービス経由への置換で解消済み（Phase6-Step3-Plan.md §6 完了判定チェックリスト）。
// 【Phase7c W3 追記】上記は Phase6 時点の記録。Phase7-Step10d で Global 型は削除済み（Global への参照は存在しない）。
using System.Drawing; // Point struct
using Sensorit.Base;
using DS4WinWPF.DS4Control;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using ThreadState = System.Threading.ThreadState;

namespace DS4Windows
{
    public class Mapping : DS4Windows.DI.IMapping, DS4Windows.Actions.IMacroStringPlayer
    {
        // Phase7-Step8 W2: 旧 静的な束縛（AppHost／Global のフォールバックつき）。コンストラクターで受ける（フォールバックは廃止）。
        private readonly DS4Windows.DI.IProfileSettingsService profileSettings;
        private readonly DS4Windows.DI.IProfileApplicationService profileApplication;
        private readonly IVirtualKBM VirtualKBM;
        private readonly DS4Windows.DI.IKeyButtonControllerRegistry _keyButtonControllers;
        private readonly DS4Windows.Actions.IProcessLauncher _processLauncher;
        private readonly DS4Windows.Actions.IProfileSwitcher _profileSwitcher;
        // Phase7-Step8 W3（Q6＝A）: 旧 静的クラス SyntheticDispatcher の呼び出し
        private readonly DS4Windows.DI.ISyntheticDispatcher _syntheticDispatcher;
        // ControlService が IDeviceStateAccessor の実装者のため、循環を避けて遅延で取得する（マクロの振動イベントで使う）
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;

        // Phase7-Step8 W2（Q5＝A）: 保持クラス（コンストラクターで受ける）。Step7 の転送プロパティは削除した。
        // 配列の参照は再代入されない（要素の読み書きのみ）ため、readonly で 1 度だけ複製して持ち、毎レポートの割り当てと間接参照を増やさない。
        // 差し替わる GlobalState と、更新される MacroCount だけは、保持クラスを直接読み書きする。
        private readonly SyntheticInputState _syntheticInput;
        private readonly MacroRuntimeState _macroRuntime;
        private readonly StickMotionState _stickMotion;
        private readonly ReaderWriterLockSlim syncStateLock;
        private readonly SyntheticState[] deviceState;
        private readonly bool[] macroControl;
        private readonly Dictionary<string, Task>[] macroTaskQueue;
        private readonly byte[] gyroStickX;
        private readonly byte[] gyroStickY;
        private readonly PostMapStickData[] mapStickActionData;
        private readonly FlickStickMappingData[] flickMappingData;
        private readonly TwoStageTriggerMappingData[] l2TwoStageMappingData;
        private readonly TwoStageTriggerMappingData[] r2TwoStageMappingData;
        private readonly Queue<DS4TimedStickAxisValue>[][] stickValueHistory;   // Phase9 9-A(2)-B: StickMotionState.StickValueHistory の複製（旧: Mapping の public フィールド）
        private readonly byte[][] lastStickAxisValues;   // Phase9 9-A(2)-B: StickMotionState.LastStickAxisValues の複製（[デバイス][LX, LY, RX, RY]）
        private readonly DeltaSettingsProcessorGroup[] deltaAccelProcessors;

        // SpecialAction名ごとのエラーログ抑制用（1プロファイル切り替えごとに1回だけ）
        // private static HashSet<string> loggedInvalidActions = new HashSet<string>();

        // プロファイル切り替え時に呼び出すことでエラーログ抑制をリセット
        // public static void ResetLoggedInvalidActions()
        // {
        //     loggedInvalidActions.Clear();
        // }
        // ToggleRepeatController removed — replaced by ToggleActionController and per-device controllers.

        // Legacy ActionState removed; per-action state lives in ActionManager/ActionInstanceState.

        // SpecialAction名ごとのエラーログ抑制用（1プロファイル切り替えごとに1回だけ）
        // private static HashSet<string> loggedInvalidActions = new HashSet<string>();

        // プロファイル切り替え時に呼び出すことでエラーログ抑制をリセット
        // public static void ResetLoggedInvalidActions()
        // {
        //     loggedInvalidActions.Clear();
        // }

        // Phase7-Step8 W2: 静的コンストラクターは、ログ出力だけの購読を 1 度だけ行う（Mapping の instance ごとに購読を増やさない）。
        static Mapping()
        {
            try
            {
                ActionManager.ToggledOnChanged += (sa, dev, oldv, newv) =>
                {
                    try { AppLogger.LogTrace($"Mapping: ToggledOnChanged name={sa?.name} device={dev} old={oldv} new={newv}"); } catch { }
                };
            }
            catch { }
        }

        // Phase7-Step8 W2（Q1＝A）: DI の Singleton（IMapping）。旧: メンバーのほぼすべてが static で、サービスは AppHost／Global の静的な束縛だった。
        public Mapping(
            SyntheticInputState syntheticInput,
            MacroRuntimeState macroRuntime,
            StickMotionState stickMotion,
            DS4Windows.DI.IProfileSettingsService profileSettings,
            DS4Windows.DI.IProfileApplicationService profileApplication,
            IVirtualKBM virtualKBM,
            DS4Windows.DI.IKeyButtonControllerRegistry keyButtonControllers,
            DS4Windows.Actions.IProcessLauncher processLauncher,
            DS4Windows.Actions.IProfileSwitcher profileSwitcher,
            DS4Windows.DI.ISyntheticDispatcher syntheticDispatcher,
            Func<IDeviceStateAccessor> deviceStateAccessor,
            DS4Windows.Actions.ITriggerDispatchEngine triggerEngine,
            DS4Windows.DI.IStickDeadZoneProcessor stickDeadZone,
            DS4Windows.DI.IOutputCurveProcessor outputCurve,
            DS4Windows.DI.ITriggerDeadZoneProcessor triggerDeadZone,
            DS4Windows.DI.IGyroControlsProcessor gyroControls,
            DS4Windows.DI.IStickInputConditioner stickConditioner,
            DS4Windows.DI.IFlickStickProcessor flickStick,
            DS4Windows.DI.IButtonMouseMovementCalculator buttonMouseMovement,
            DS4Windows.DI.IMouseMotionQuantizer mouseMotionQuantizer,
            DS4Windows.DI.IGyroMouseStickBlender gyroMouseStickBlender,
            DS4Windows.DI.IControlActivityReader controlActivityReader)
        {
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _macroRuntime = macroRuntime ?? throw new ArgumentNullException(nameof(macroRuntime));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            this.profileApplication = profileApplication ?? throw new ArgumentNullException(nameof(profileApplication));
            this.VirtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _keyButtonControllers = keyButtonControllers ?? throw new ArgumentNullException(nameof(keyButtonControllers));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
            _syntheticDispatcher = syntheticDispatcher ?? throw new ArgumentNullException(nameof(syntheticDispatcher));
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
            _engine = triggerEngine ?? throw new ArgumentNullException(nameof(triggerEngine));
            _stickDeadZone = stickDeadZone ?? throw new ArgumentNullException(nameof(stickDeadZone));
            _outputCurve = outputCurve ?? throw new ArgumentNullException(nameof(outputCurve));
            _triggerDeadZone = triggerDeadZone ?? throw new ArgumentNullException(nameof(triggerDeadZone));
            _gyroControls = gyroControls ?? throw new ArgumentNullException(nameof(gyroControls));
            _stickConditioner = stickConditioner ?? throw new ArgumentNullException(nameof(stickConditioner));
            _flickStick = flickStick ?? throw new ArgumentNullException(nameof(flickStick));
            _buttonMouseMovement = buttonMouseMovement ?? throw new ArgumentNullException(nameof(buttonMouseMovement));
            _mouseMotionQuantizer = mouseMotionQuantizer ?? throw new ArgumentNullException(nameof(mouseMotionQuantizer));
            _gyroMouseStickBlender = gyroMouseStickBlender ?? throw new ArgumentNullException(nameof(gyroMouseStickBlender));
            _controlActivityReader = controlActivityReader ?? throw new ArgumentNullException(nameof(controlActivityReader));

            _triggerInputs = new DS4Windows.Actions.TriggerInput[ControllerLimits.MaxControllerCount];
            _frameOutputs = new DS4Windows.Actions.FrameOutput[ControllerLimits.MaxControllerCount];
            for (int device = 0; device < _triggerInputs.Length; device++)
            {
                _triggerInputs[device] = new DS4Windows.Actions.TriggerInput(device, profileSettings);
                _frameOutputs[device] = new DS4Windows.Actions.FrameOutput();

                // Phase8-Step7 7.8f-3: 新経路のマウスの移動が、生の量の計算（getMouseMapping）を借りる。デバイスごとに 1 回だけ作る（フレームごとの割り当てなし）
                int sampleDevice = device;
                _triggerInputs[device].MouseSampler = (control, direction) => SampleMouseMovement(sampleDevice, control, direction);
                _triggerInputs[device].FixedMouseSampler = (direction, fraction) => SampleFixedMouseMovement(sampleDevice, direction, fraction);   // Phase10-Step6 後の Step5: SA のマウス移動（強さの固定値）
            }

            syncStateLock = syntheticInput.SyncStateLock;
            deviceState = syntheticInput.DeviceState;
            macroControl = macroRuntime.MacroControl;
            macroTaskQueue = macroRuntime.TaskQueue;
            gyroStickX = stickMotion.GyroStickX;
            gyroStickY = stickMotion.GyroStickY;
            mapStickActionData = stickMotion.MapStickActionData;
            flickMappingData = stickMotion.FlickMappingData;
            l2TwoStageMappingData = stickMotion.L2TwoStageMappingData;
            r2TwoStageMappingData = stickMotion.R2TwoStageMappingData;
            deltaAccelProcessors = stickMotion.DeltaAccelProcessors;
            stickValueHistory = stickMotion.StickValueHistory;
            lastStickAxisValues = stickMotion.LastStickAxisValues;
        }

        // Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: keyButtonControllers／
        // GetOrCreateKeyButtonController／ClearKeyButtonControllersForDevice）は KeyButtonControllerRegistry へ移した。


        // Phase9 9-A(1) (c): 旧 DS4Vector2／DS4SquareStick（スクエアスティックの作業用）は、OutputCurveProcessor（DS4Control/Services）へ移した（ローカル変数で計算する）。

        // Phase9 9-A(2)-B: 旧 DS4TimedStickAxisValue（入れ子の構造体）は DS4Control/DS4TimedStickAxisValue.cs へ、旧 stickValueHistory／lastStickAxisValues は StickMotionState へ移した。

        // Phase7-Step7 W4（Q2＝A・Q3＝A）: 外から触られるスティック・ジャイロ・トリガーの処理状態（gyroStickX／gyroStickY／mapStickActionData／
        // flickMappingData／l2TwoStageMappingData／r2TwoStageMappingData／deltaAccelProcessors［旧 wheelFilters は Phase9 Steering 削除 W2 で削除］）の実体は StickMotionState が持つ。
        // Phase7-Step8 W2: 転送プロパティは削除した。上のフィールド宣言（保持クラスの配列の readonly の複製）が、その代わりである。
        //public static byte[] touchStickX = new byte[Global.MAX_DS4_CONTROLLER_COUNT] { 128, 128, 128, 128, 128, 128, 128, 128 };
        //public static byte[] touchStickY = new byte[Global.MAX_DS4_CONTROLLER_COUNT] { 128, 128, 128, 128, 128, 128, 128, 128 };

        //static int lastGyroX = 0;
        //static int lastGyroZ = 0;

        //private static OneEuroFilter filterX = new OneEuroFilter(minCutoff: 1, beta: 0);
        //private static OneEuroFilter filterZ = new OneEuroFilter(minCutoff: 1, beta: 0);
        //private static OneEuroFilter filterX = new OneEuroFilter(minCutoff: 0.0001, beta: 0.001);
        //private static OneEuroFilter filterZ = new OneEuroFilter(minCutoff: 0.0001, beta: 0.001);
        //private static OneEuroFilter wheel360FilterX = new OneEuroFilter(minCutoff: 0.1, beta: 0.02);
        //private static OneEuroFilter wheel360FilterZ = new OneEuroFilter(minCutoff: 0.1, beta: 0.02);






        // Phase7-Step7 W2（Q2＝A・Q3＝A）: 合成入力の状態（_syntheticInput.GlobalState／deviceState／syncStateLock）の実体は SyntheticInputState が持つ。
        // Phase7-Step8 W2: 転送プロパティは削除した。deviceState／syncStateLock は上のフィールド宣言（readonly の複製）、GlobalState は保持クラスを直接読む。

        public DS4StateFieldMapping[] fieldMappings = new DS4StateFieldMapping[ControllerLimits.MaxControllerCount] {
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(),
        };
        public DS4StateFieldMapping[] outputFieldMappings = new DS4StateFieldMapping[ControllerLimits.MaxControllerCount]
        {
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(),
        };
        public DS4StateFieldMapping[] previousFieldMappings = new DS4StateFieldMapping[ControllerLimits.MaxControllerCount]
        {
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(), new DS4StateFieldMapping(),
            new DS4StateFieldMapping(), new DS4StateFieldMapping(),
        };

        // TODO When we disconnect, process a null/dead state to release any keys or buttons.
        public DateTime oldnow = DateTime.UtcNow;
        private bool pressagain = false;
        private int wheel = 0;

        // Data needed to calculate Stick to Mouse Wheel conversion

        // Phase7-Step7 W3（Q2＝A・Q3＝A）: マクロの実行時状態（macroControl／MacroCount／macroTaskQueue）の実体は MacroRuntimeState が持つ。
        // Phase8-Fix-MacroRepeatUnify（D5）: 旧 macrodone／macroDispatchInFlight（control 単位の二重実行防止の暫定対策。呼び出しが常に control == None のため、本番では到達しなかった）と、
        // その添字の表（ds4ControlMapping／DS4ControltoInt）は削除した。二重実行の防止は ActionInstanceState.IsMacroRunning が担う。
        // Phase7-Step8 W2: 転送プロパティは削除した（配列は上のフィールド宣言の readonly の複製、MacroCount は _macroRuntime を直接読み書きする）。

        //mapcustom

        //actions
        public int[] fadetimer = new int[ControllerLimits.MaxControllerCount] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] prevFadetimer = new int[ControllerLimits.MaxControllerCount] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public DS4Color[] lastColor = new DS4Color[ControllerLimits.MaxControllerCount];
        // Legacy `actionDone` list removed — per-action state now lives in ActionManager/ActionInstanceState.
        // Per-device runtime state replacing legacy `untriggeraction`/`untriggerindex` globals
        public DeviceRuntimeState[] deviceRuntime = new DeviceRuntimeState[ControllerLimits.MaxControllerCount]
        {
            new DeviceRuntimeState(), new DeviceRuntimeState(), new DeviceRuntimeState(), new DeviceRuntimeState(),
            new DeviceRuntimeState(), new DeviceRuntimeState(), new DeviceRuntimeState(), new DeviceRuntimeState()
        };

        // Phase8-Step4 W4: 抑制の集合（旧 suppressedTriggerButtons。Issue8-1(3)是正: トリガー成立時に抑制対象となった物理ボタンのうち、まだ「離される」ことを
        // 観測していないものの集合）の所有を、エンジン（ITriggerDispatchEngine）に一本化した。ここに含まれる間は、当該ボタン自身の通常出力を抑制し続ける
        // （トリガー全体の成立/解除とは独立に、ボタン単位で管理する。詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-3-Fix-Plan.md §2.1）。
        // 新旧の経路が別々の集合を持つと、通常の割り当ての抑制が食い違うため（Phase8-Step2-Design.md §23.3）。
        private readonly DS4Windows.Actions.ITriggerDispatchEngine _engine;
        private readonly DS4Windows.DI.IStickDeadZoneProcessor _stickDeadZone;   // Phase9 9-A(1) (b): 左右のスティックのデッドゾーン（共通の処理）
        private readonly DS4Windows.DI.IOutputCurveProcessor _outputCurve;   // Phase9 9-A(1) (c): スクエアスティックと出力カーブ（スティック、トリガー。共通の処理）
        private readonly DS4Windows.DI.ITriggerDeadZoneProcessor _triggerDeadZone;   // Phase9 9-A(1) (d): L2／R2 のデッドゾーン（共通の処理）
        private readonly DS4Windows.DI.IGyroControlsProcessor _gyroControls;   // Phase9 9-A(2)-A: ジャイロ → コントロールの補正（X／Z 共通の処理）
        private readonly DS4Windows.DI.IStickInputConditioner _stickConditioner;   // Phase9 9-A(2)-B: ドリフト補正・アンチスナップバック・ファズ（共通の処理）
        private readonly DS4Windows.DI.IFlickStickProcessor _flickStick;   // Phase9 9-A(2)-C: フリックスティック（共通の処理）
        private readonly DS4Windows.DI.IButtonMouseMovementCalculator _buttonMouseMovement;   // Phase9 9-B: ボタン・スティック・トリガー・ジャイロ方向 → マウスの生の移動量（旧 getMouseMapping。加速の状態はデバイスごと）
        private readonly DS4Windows.DI.IMouseMotionQuantizer _mouseMotionQuantizer;   // Phase9 9-B: マウスの移動量の整数化と端数の持ち越し（旧 calculateFinalMouseMovement。端数はデバイスごと）
        private readonly DS4Windows.DI.IGyroMouseStickBlender _gyroMouseStickBlender;   // Phase9 9-B: ジャイロのマウススティック出力の合成（旧 TempMouseJoystick の内側）
        private readonly DS4Windows.DI.IControlActivityReader _controlActivityReader;   // Phase9 9-B: コントロールが押されているかの判定（旧 GetBoolMappingExternal の本体）

        // Phase8-Step4 W4: デバイスごとのトリガー判定の入力（毎フレーム MapCustom の先頭で更新する）と、フレームの出力への書き込み口。
        // 遅延の基準時刻（旧 nowAction／oldnowAction）は、_triggerInputs[device].Delay に置き換わった。
        private readonly DS4Windows.Actions.TriggerInput[] _triggerInputs;
        private readonly DS4Windows.Actions.FrameOutput[] _frameOutputs;

        // Phase8-Step7 7.8f-3: 今のフレームの入力（getMouseMapping を新経路から借りるために、MapCustom が設定する）
        private readonly DS4State[] _sampleState = new DS4State[ControllerLimits.MaxControllerCount];
        private readonly DS4StateExposed[] _sampleExposed = new DS4StateExposed[ControllerLimits.MaxControllerCount];
        private readonly DS4StateFieldMapping[] _sampleFields = new DS4StateFieldMapping[ControllerLimits.MaxControllerCount];
        private readonly ControlService[] _sampleControl = new ControlService[ControllerLimits.MaxControllerCount];

        // 新経路のマウスの移動（ControlMouseMoveOutputAction）が、TriggerInput 経由で呼ぶ。旧 getMouseMapping と同じ計算
        private double SampleMouseMovement(int device, DS4Controls control, int direction)
        {
            DS4State state = _sampleState[device];
            DS4StateFieldMapping fields = _sampleFields[device];
            ControlService ctrl = _sampleControl[device];
            if (state == null || fields == null || ctrl == null) return 0.0;

            // Phase9 9-B: 旧 getMouseMapping（ctrl から設定と経過時間を読んでいた）は ButtonMouseMovementCalculator へ移した。設定と経過時間は、ここで読んで渡す
            return _buttonMouseMovement.Calculate(device, control, direction, state, fields,
                ctrl.ProfileSettingsService.ButtonMouseInfos[device],
                ctrl.ProfileSettingsService.LSModInfo[device].deadZone,
                ctrl.ProfileSettingsService.RSModInfo[device].deadZone,
                deltaAccelProcessors[device],
                ctrl.DS4Controllers[device].lastTimeElapsedDouble);
        }

        // Phase10-Step6 後の Step5: SA のマウス移動（強さの固定値）が、TriggerInput 経由で呼ぶ。getMouseMapping の「ボタンが元のとき」（押している）の量
        // （感度 activeButtonSensitivity と、縦方向の倍率 buttonVerticalScale）に、強さ（0〜1）を掛ける。1 は、Controls のボタンからのマウス移動と同じ量
        private double SampleFixedMouseMovement(int device, int direction, double fraction)
        {
            ControlService ctrl = _sampleControl[device];
            if (ctrl == null) return 0.0;

            ButtonMouseInfo buttonMouseInfo = ctrl.ProfileSettingsService.ButtonMouseInfos[device];
            const double root = 1.002;
            const double divide = 10000d;
            double value = Math.Pow(root + buttonMouseInfo.activeButtonSensitivity / divide, 100) - 1;
            if (direction == 0 || direction == 1) value *= buttonMouseInfo.buttonVerticalScale;
            return value * fraction;
        }


        // IsToggledOn lifecycle is managed by Action implementations (e.g., KeyAction).
        // Mapping must not mutate per-action IsToggledOn state.

        // Phase8-Step8 8b: 旧 oldnowKeyAct（タップ判定の特殊アクションの基準時刻）は、旧枝と一緒に削除した。
        // （Phase8-Step4 W4: 旧 nowAction／oldnowAction は _triggerInputs[device].Delay に置き換え済み）

        // Mouse
        // Phase9 9-B: 旧 mcounter／mouseaccel／prevmouseaccel（マウス加速の状態）は ButtonMouseMovementCalculator へ、
        // 旧 horizontalRemainder／verticalRemainder（移動の端数）は MouseMotionQuantizer へ移した（どちらもデバイスごと。ユーザー判断 Q2）。
        public const int MOUSESPEEDFACTOR = 48;
        // Phase9 9-B（Q4）: 旧 MOUSESTICKANTIOFFSET／MOUSESTICKMINVELOCITY（使用箇所がコメントの中だけの定数）は削除した（上流 DS4Windows 由来の未使用コード）。
        //private const double MOUSESTICKMINVELOCITY = 40.0;

        public void Commit(int device, DS4Windows.DI.IProfileSettingsService settings)
        {
            SyntheticState state = deviceState[device];
            syncStateLock.EnterWriteLock();

            _syntheticInput.GlobalState.currentClicks.leftCount += state.currentClicks.leftCount - state.previousClicks.leftCount;
            _syntheticInput.GlobalState.currentClicks.middleCount += state.currentClicks.middleCount - state.previousClicks.middleCount;
            _syntheticInput.GlobalState.currentClicks.rightCount += state.currentClicks.rightCount - state.previousClicks.rightCount;
            _syntheticInput.GlobalState.currentClicks.fourthCount += state.currentClicks.fourthCount - state.previousClicks.fourthCount;
            _syntheticInput.GlobalState.currentClicks.fifthCount += state.currentClicks.fifthCount - state.previousClicks.fifthCount;
            _syntheticInput.GlobalState.currentClicks.wUpCount += state.currentClicks.wUpCount - state.previousClicks.wUpCount;
            _syntheticInput.GlobalState.currentClicks.wDownCount += state.currentClicks.wDownCount - state.previousClicks.wDownCount;
            _syntheticInput.GlobalState.currentClicks.toggleCount += state.currentClicks.toggleCount - state.previousClicks.toggleCount;
            _syntheticInput.GlobalState.currentClicks.toggle = state.currentClicks.toggle;

            if (_syntheticInput.GlobalState.currentClicks.toggleCount != 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0 && _syntheticInput.GlobalState.currentClicks.toggle)
            {
                AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftDown");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftDown");
                // Phase7-Step8 W2（Q8＝A）: 旧 MouseOutputAction の検索（未登録のため常に null で、下の出力が実際の経路だった）を削除した。
                VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);

                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightDown");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                }
                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleDown");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                }
                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=1");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                }
                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=2");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                }
            }
            else if (_syntheticInput.GlobalState.currentClicks.toggleCount != 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0 && !_syntheticInput.GlobalState.currentClicks.toggle)
            {
                if (_syntheticInput.GlobalState.currentClicks.leftCount != 0 && _syntheticInput.GlobalState.previousClicks.leftCount == 0)
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
            }

            if (_syntheticInput.GlobalState.currentClicks.toggleCount == 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0)
            {
                if (_syntheticInput.GlobalState.currentClicks.leftCount != 0 && _syntheticInput.GlobalState.previousClicks.leftCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftDown");
                    // Phase7-Step8 W2（Q8＝A）: 旧 MouseOutputAction の検索（未登録のため常に null で、下の出力が実際の経路だった）を削除した。
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.leftCount == 0 && _syntheticInput.GlobalState.previousClicks.leftCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftUp");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleDown");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.middleCount == 0 && _syntheticInput.GlobalState.previousClicks.middleCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleUp");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightDown");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.rightCount == 0 && _syntheticInput.GlobalState.previousClicks.rightCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightUp");
                    VirtualKBM.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=1");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                }
                else if (_syntheticInput.GlobalState.currentClicks.fourthCount == 0 && _syntheticInput.GlobalState.previousClicks.fourthCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonUp btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonUp btn=1");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                }

                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=2");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                }
                else if (_syntheticInput.GlobalState.currentClicks.fifthCount == 0 && _syntheticInput.GlobalState.previousClicks.fifthCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonUp btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonUp btn=2");
                    VirtualKBM.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
                }

                if (_syntheticInput.GlobalState.currentClicks.wUpCount != 0 && _syntheticInput.GlobalState.previousClicks.wUpCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseWheelUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseWheelUp");
                    VirtualKBM.PerformMouseWheelEvent(settings.OutputKBMMapping.WHEEL_TICK_UP, 0);
                    oldnow = DateTime.UtcNow;
                    wheel = settings.OutputKBMMapping.WHEEL_TICK_UP;
                }
                else if (_syntheticInput.GlobalState.currentClicks.wUpCount == 0 && _syntheticInput.GlobalState.previousClicks.wUpCount != 0)
                    wheel = 0;

                if (_syntheticInput.GlobalState.currentClicks.wDownCount != 0 && _syntheticInput.GlobalState.previousClicks.wDownCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseWheelDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseWheelDown");
                    VirtualKBM.PerformMouseWheelEvent(settings.OutputKBMMapping.WHEEL_TICK_DOWN, 0);
                    oldnow = DateTime.UtcNow;
                    wheel = settings.OutputKBMMapping.WHEEL_TICK_DOWN;
                }
                if (_syntheticInput.GlobalState.currentClicks.wDownCount == 0 && _syntheticInput.GlobalState.previousClicks.wDownCount != 0)
                    wheel = 0;
            }


            if (wheel != 0) //Continue mouse wheel movement
            {
                DateTime now = DateTime.UtcNow;
                if (now >= oldnow + TimeSpan.FromMilliseconds(150) && !pressagain)
                {
                    oldnow = now;
                    VirtualKBM.PerformMouseWheelEvent(wheel, 0);
                }
            }

            _syntheticInput.GlobalState.SaveToPrevious(false);

            syncStateLock.ExitWriteLock();
            state.SaveToPrevious(true);

            // Send possible virtual events to system. Only used for FakerInput atm.
            // SendInput version does nothing
            // Update toggle-driven repeat controller so it can emit repeats using stored handlers
            // ToggleActionController removed from per-path usage — per-device controllers handle updates.
            VirtualKBM.Sync();
        }


        static double TValue(double value1, double value2, double percent)
        {
            percent /= 100f;
            return value1 * percent + value2 * (1 - percent);
        }

        private static int ClampInt(int min, int value, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public DS4State SetCurveAndDeadzone(int device, DS4State cState, DS4State dState,
                DS4Windows.DI.IProfileSettingsService settings)
        {
            double rotation = /*tempDoubleArray[device] =*/  settings.LSRotation[device];
            if (rotation > 0.0 || rotation < 0.0)
                cState.rotateLSCoordinates(rotation);

            double rotationRS = /*tempDoubleArray[device] =*/ settings.RSRotation[device];
            if (rotationRS > 0.0 || rotationRS < 0.0)
                cState.rotateRSCoordinates(rotationRS);

            StickAntiSnapbackInfo lsAntiSnapback = settings.LSAntiSnapbackInfo[device];
            StickAntiSnapbackInfo rsAntiSnapback = settings.RSAntiSnapbackInfo[device];

            if (lsAntiSnapback.enabled)
            {
                _stickConditioner.AntiSnapback(stickValueHistory[device][0], lsAntiSnapback.delta, lsAntiSnapback.timeout, DateTimeOffset.Now.ToUnixTimeMilliseconds(), ref cState.LX, ref cState.LY);
            }

            if (rsAntiSnapback.enabled)
            {
                _stickConditioner.AntiSnapback(stickValueHistory[device][1], rsAntiSnapback.delta, rsAntiSnapback.timeout, DateTimeOffset.Now.ToUnixTimeMilliseconds(), ref cState.RX, ref cState.RY);
            }

            StickDeadZoneInfo lsMod = settings.LSModInfo[device];
            StickDeadZoneInfo rsMod = settings.RSModInfo[device];

            if (lsMod.fuzz > 0)
            {
                _stickConditioner.Fuzz(lastStickAxisValues[device], 0, lsMod.fuzz, ref cState.LX, ref cState.LY);
            }

            if (rsMod.fuzz > 0)
            {
                _stickConditioner.Fuzz(lastStickAxisValues[device], 1, rsMod.fuzz, ref cState.RX, ref cState.RY);
            }

            cState.CopyTo(dState);
            //DS4State dState = new DS4State(cState);

            // Phase9 9-A(2)-B: ドリフト補正・アンチスナップバック・ファズは、共通の処理（IStickInputConditioner）が行う。状態（履歴、前回の値）は StickMotionState が持つ。
            cState = _stickConditioner.ApplyDrift(device, dState, settings);


            // Phase9 9-A(1) (b): 左右のスティックのデッドゾーン（Radial・Axial、Radial の外側の出力）は、共通の処理（IStickDeadZoneProcessor）が行う。
            // 以前は、ここに、左右でほぼ同じコードが 2 回あった（約 470 行）。値は Phase9A1SetCurveAndDeadzoneGoldenTests が守る
            _stickDeadZone.Process(lsMod, cState.LX, cState.LY, ref dState.LX, ref dState.LY, ref dState.OutputLSOuter);
            _stickDeadZone.Process(rsMod, cState.RX, cState.RY, ref dState.RX, ref dState.RY, ref dState.OutputRSOuter);

            /*byte l2Deadzone = getL2Deadzone(device);
            int l2AntiDeadzone = getL2AntiDeadzone(device);
            int l2Maxzone = getL2Maxzone(device);
            */

            // Phase9 9-A(1) (d): トリガーのデッドゾーン（デッドゾーン、アンチデッドゾーン、最大ゾーン、最大出力）は、L2／R2 とも共通の処理（ITriggerDeadZoneProcessor）が行う。
            // 以前は、ここに、L2 と R2 で同じコードが 2 回あった（約 100 行）。値は Phase9A1SetCurveAndDeadzoneGoldenTests と Phase7bTriggerDeadZoneRoundingTests が守る
            TriggerDeadZoneZInfo l2ModInfo = settings.L2ModInfo[device];
            if (l2ModInfo.deadZone > 0 || l2ModInfo.antiDeadZone > 0 || l2ModInfo.maxZone != 100 || l2ModInfo.maxOutput != 100.0)
                _triggerDeadZone.Process(l2ModInfo, cState.L2, ref dState.L2);

            /*byte r2Deadzone = getR2Deadzone(device);
            int r2AntiDeadzone = getR2AntiDeadzone(device);
            int r2Maxzone = getR2Maxzone(device);
            */
            TriggerDeadZoneZInfo r2ModInfo = settings.R2ModInfo[device];
            if (r2ModInfo.deadZone > 0 || r2ModInfo.antiDeadZone > 0 || r2ModInfo.maxZone != 100 || r2ModInfo.maxOutput != 100.0)
                _triggerDeadZone.Process(r2ModInfo, cState.R2, ref dState.R2);

            // Only apply deprecated Sensitivity modifier for Radial DZ
            if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                double lsSens = settings.LSSens[device];
                if (lsSens != 1.0)
                {
                    dState.LX = (byte)NumericUtil.Clamp(0, lsSens * (dState.LX - 128.0) + 128.0, 255);
                    dState.LY = (byte)NumericUtil.Clamp(0, lsSens * (dState.LY - 128.0) + 128.0, 255);
                }
            }

            // Only apply deprecated Sensitivity modifier for Radial DZ
            if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                double rsSens = settings.RSSens[device];
                if (rsSens != 1.0)
                {
                    dState.RX = (byte)NumericUtil.Clamp(0, rsSens * (dState.RX - 128.0) + 128.0, 255);
                    dState.RY = (byte)NumericUtil.Clamp(0, rsSens * (dState.RY - 128.0) + 128.0, 255);
                }
            }

            double l2Sens = settings.L2Sens[device];
            if (l2Sens != 1.0)
                dState.L2 = (byte)NumericUtil.Clamp(0, l2Sens * dState.L2, 255);

            double r2Sens = settings.R2Sens[device];
            if (r2Sens != 1.0)
                dState.R2 = (byte)NumericUtil.Clamp(0, r2Sens * dState.R2, 255);

            SquareStickInfo squStk = settings.SquStickInfo[device];
            // Phase9 9-A(1) (c): スクエアスティックと出力カーブは IOutputCurveProcessor へ切り出した（左右・L2／R2 は同じ処理）。条件の判定は、ここに残す
            if (squStk.lsMode && (dState.LX != 128 || dState.LY != 128))
                _outputCurve.ProcessSquareStick(squStk.lsRoundness, ref dState.LX, ref dState.LY);

            int lsOutCurveMode = settings.GetLsOutCurveMode(device);
            if (lsOutCurveMode > 0 && (dState.LX != 128 || dState.LY != 128))
                _outputCurve.ProcessStickCurve(lsOutCurveMode, lsMod.deadzoneType, settings.LsOutBezierCurveObj[device], ref dState.LX, ref dState.LY);

            if (squStk.rsMode && (dState.RX != 128 || dState.RY != 128))
                _outputCurve.ProcessSquareStick(squStk.rsRoundness, ref dState.RX, ref dState.RY);

            int rsOutCurveMode = settings.GetRsOutCurveMode(device);
            if (rsOutCurveMode > 0 && (dState.RX != 128 || dState.RY != 128))
                _outputCurve.ProcessStickCurve(rsOutCurveMode, rsMod.deadzoneType, settings.RsOutBezierCurveObj[device], ref dState.RX, ref dState.RY);

            int l2OutCurveMode = settings.GetL2OutCurveMode(device);
            if (l2OutCurveMode > 0 && dState.L2 != 0)
                _outputCurve.ProcessTriggerCurve(l2OutCurveMode, settings.L2OutBezierCurveObj[device], ref dState.L2);

            int r2OutCurveMode = settings.GetR2OutCurveMode(device);
            if (r2OutCurveMode > 0 && dState.R2 != 0)
                _outputCurve.ProcessTriggerCurve(r2OutCurveMode, settings.R2OutBezierCurveObj[device], ref dState.R2);


            bool saControls = settings.GyroOutputMode[device] == GyroOutMode.Controls;
            if (saControls && dState.Motion.outputGyroControls)
            {
                int SXD = (int)(128d * settings.SXDeadzone[device]);
                int SZD = (int)(128d * settings.SZDeadzone[device]);
                double SXMax = settings.SXMaxzone[device];
                double SZMax = settings.SZMaxzone[device];
                double sxAntiDead = settings.SXAntiDeadzone[device];
                double szAntiDead = settings.SZAntiDeadzone[device];
                double sxsens = settings.SXSens[device];
                double szsens = settings.SZSens[device];

                int gyroX = cState.Motion.accelX, gyroZ = cState.Motion.accelZ;

                // Phase9 9-A(2)-A: ジャイロの補正（デッドゾーン、最大ゾーン、アンチデッドゾーン、感度、出力カーブ）は、X／Z とも共通の処理（IGyroControlsProcessor）が行う。
                // 以前は、ここに、X と Z で同じコードが 2 回あった（約 150 行）。値は Phase9A2GyroControlsGoldenTests が守る。出力カーブを適用する条件は、X と Z で違うため、ここに残す
                dState.Motion.outputAccelX = _gyroControls.Correct(gyroX, SXD, SXMax, sxAntiDead, sxsens);
                dState.Motion.outputAccelZ = _gyroControls.Correct(gyroZ, SZD, SZMax, szAntiDead, szsens);

                int sxOutCurveMode = settings.GetSxOutCurveMode(device);
                if (sxOutCurveMode > 0)
                    dState.Motion.outputAccelX = _gyroControls.ApplyCurve(sxOutCurveMode, settings.SxOutBezierCurveObj[device], dState.Motion.outputAccelX);

                int szOutCurveMode = settings.GetSzOutCurveMode(device);
                if (szOutCurveMode > 0 && dState.Motion.outputAccelZ != 0)
                    dState.Motion.outputAccelZ = _gyroControls.ApplyCurve(szOutCurveMode, settings.SzOutBezierCurveObj[device], dState.Motion.outputAccelZ);
            }

            return dState;
        }

        /// <summary>
        /// Map DS4 Buttons/Axes to other DS4 Buttons/Axes (largely the same as Xinput ones) and to keyboard and mouse buttons.
        /// </summary>
        /*static double previousPointerX = 0.0;
        //double accelHelperX = 0.0;
        //double accelTravelX = 0.0;
        //Stopwatch deltaEasingTimeX = new Stopwatch();

        double previousPointerY = 0.0;
        //double accelHelperY = 0.0;
        //double accelTravelY = 0.0;
        //Stopwatch deltaEasingTimeY = new Stopwatch();

        double previousPointerRadial = 0.0;
        double accelCurrentMultiRadial = 0.0;
        double accelEasingMultiRadial = 0.0;
        double accelTravelRadial = 0.0;
        Stopwatch deltaEasingTimeRadial = new Stopwatch();
        double totalTravelRadial = 0.0;

        double tempOutXNorm = 0.0;
        double tempOutYNorm = 0.0;
        bool usingDeltaAccel = false;
        */

        public void MapCustom(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState,
            Mouse tp, ControlService ctrl)
        {
            /* TODO: This method is slow sauce. Find ways to speed up action execution */
            double tempMouseDeltaX = 0.0;
            double tempMouseDeltaY = 0.0;
            int mouseDeltaX = 0;
            int mouseDeltaY = 0;

            cState.calculateStickAngles();
            DS4StateFieldMapping fieldMapping = fieldMappings[device];
            fieldMapping.PopulateFieldMapping(cState, eState, tp);
            DS4StateFieldMapping outputfieldMapping = outputFieldMappings[device];
            outputfieldMapping.PopulateFieldMapping(cState, eState, tp);
            // Phase8-Step4 W4: トリガー判定の入力と、フレームの出力の書き込み口を、このフレームの表に合わせる（割り当て 0）
            // Phase8-Step7 7.7b: 前フレームの状態（MultiAction のエッジ判定用）は、デバイスの前回のレポート（旧経路の getPreviousStateRef と同じ）を渡す。
            // 前フレームの表は、WasActive が最初に呼ばれたときにだけ作る（遅延）ため、MultiAction がなければ費用は増えない。デバイスがなければ null（従来どおり）
            DS4State previousState = null;
            try { previousState = ctrl?.DS4Controllers[device]?.getPreviousStateRef(); } catch { }
            _triggerInputs[device].Update(cState, previousState, eState, tp, fieldMapping);
            _frameOutputs[device].Bind(outputfieldMapping, this.deviceState[device]);
            _sampleState[device] = cState;
            _sampleExposed[device] = eState;
            _sampleFields[device] = fieldMapping;
            _sampleControl[device] = ctrl;
            //DS4StateFieldMapping fieldMapping = new DS4StateFieldMapping(cState, eState, tp);
            //DS4StateFieldMapping outputfieldMapping = new DS4StateFieldMapping(cState, eState, tp);

            SyntheticState deviceState = this.deviceState[device];
            if (ctrl.ProfileActionProvider.GetProfileActionCount(device) > 0 || profileSettings.GetUseTempProfile(device))
                MapCustomAction(device, ctrl);
            //if (ctrl.DS4Controllers[device] == null) return;

            //cState.CopyTo(MappedState);

            //Dictionary<DS4Controls, DS4Controls> tempControlDict = new Dictionary<DS4Controls, DS4Controls>();
            //MultiValueDict<DS4Controls, DS4Controls> tempControlDict = new MultiValueDict<DS4Controls, DS4Controls>();

            //List<DS4ControlSettings> tempSettingsList = getDS4CSettings(device);
            //foreach (DS4ControlSettings dcs in getDS4CSettings(device))
            //for (int settingIndex = 0, arlen = tempSettingsList.Count; settingIndex < arlen; settingIndex++)

            // Process LS
            ControlSettingsGroup controlSetGroup = profileSettings.GetControlSettingsGroup(device);
            StickOutputSetting stickSettings = profileSettings.LSOutputSettings[device];
            if (stickSettings.mode == StickMode.Controls)
            {
                for (var settingEnum = controlSetGroup.LS.GetEnumerator(); settingEnum.MoveNext();)
                {
                    DS4ControlSettings dcs = settingEnum.Current;
                    ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
            }
            else
            {
                outputfieldMapping.axisdirs[(int)DS4Controls.LXNeg] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.LXPos] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.LYNeg] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.LYPos] = 128;

                switch (stickSettings.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.FlickStick:
                        DS4Device d = ctrl.DS4Controllers[device];
                        DS4State cRawState = d.getCurrentStateRef();
                        DS4State pState = d.getPreviousStateRef();

                        _flickStick.Process(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                            cRawState.LX, cRawState.LY, pState.LX, pState.LY, ref tempMouseDeltaX);
                        break;
                    default:
                        break;
                }
            }

            // Process RS
            stickSettings = profileSettings.RSOutputSettings[device];
            if (stickSettings.mode == StickMode.Controls)
            {
                if (stickSettings.outputSettings.controlSettings.deltaAccelSettings.enabled)
                {
                    DS4Device d = ctrl.DS4Controllers[device];
                    DS4State cRawState = d.getCurrentStateRef();
                    cRawState.calculateStickAngles();

                    double maxXValue = cState.RX >= 128.0 ? 255.0 : 0.0;
                    double maxYValue = cState.RY >= 128.0 ? 255.0 : 0.0;
                    double maxXDir = maxXValue - 128.0;
                    double maxYDir = maxYValue - 128.0;
                    double axisDirX = cState.RX - 128.0;
                    double axisDirY = cState.RY - 128.0;
                    double axisRawDirX = cRawState.RX - 128.0;
                    double axisRawDirY = cRawState.RY - 128.0;

                    deltaAccelProcessors[device].RSProcessor.Process(device, axisDirX, axisDirY,
                        axisRawDirX, axisRawDirY,
                        maxXDir, maxYDir,
                        stickSettings.outputSettings.controlSettings.deltaAccelSettings);
                }

                for (var settingEnum = controlSetGroup.RS.GetEnumerator(); settingEnum.MoveNext();)
                {
                    DS4ControlSettings dcs = settingEnum.Current;
                    ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
            }
            else
            {
                outputfieldMapping.axisdirs[(int)DS4Controls.RXNeg] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.RXPos] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.RYNeg] = 128;
                outputfieldMapping.axisdirs[(int)DS4Controls.RYPos] = 128;

                switch (stickSettings.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.FlickStick:
                        DS4Device d = ctrl.DS4Controllers[device];
                        DS4State cRawState = d.getCurrentStateRef();
                        DS4State pState = d.getPreviousStateRef();

                        _flickStick.Process(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                            cRawState.RX, cRawState.RY, pState.RX, pState.RY, ref tempMouseDeltaX);
                        break;
                    default:
                        break;
                }
            }

            // Process L2
            TriggerOutputSettings l2TriggerSettings = profileSettings.L2OutputSettings[device];
            DS4ControlSettings dcsTemp = controlSetGroup.L2;
            if (l2TriggerSettings.twoStageMode == TwoStageTriggerMode.Disabled)
            {
                ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY, ctrl);
            }
            else
            {
                DS4ControlSettings l2FullPull = controlSetGroup.L2FullPull;
                TwoStageTriggerMappingData l2TwoStageData = l2TwoStageMappingData[device];
                ProcessTwoStageTrigger(device, cState, cState.L2, cState.L2Raw, ref dcsTemp, ref l2FullPull,
                    l2TriggerSettings, l2TwoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull);

                TwoStageTriggerMappingData.ActiveZoneButtons tempButtons = TwoStageTriggerMappingData.ActiveZoneButtons.None;
                // Check for Soft Pull activation
                if (outputSoftPull != null ||
                    (l2TwoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull) != 0)
                {
                    if (outputSoftPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.triggers[(int)DS4Controls.L2] = 0;
                        outputfieldMapping.triggers[(int)DS4Controls.L2] = 0;
                    }

                    ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
                else
                {
                    // Soft Pull binding not engaged
                    outputfieldMapping.triggers[(int)DS4Controls.L2] = 0;
                }

                // Check for Full Pull activation
                if (outputFullPull != null ||
                    (l2TwoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.FullPull) != 0)
                {
                    if (outputFullPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.FullPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.buttons[(int)DS4Controls.L2FullPull] = false;
                    }

                    ProcessControlSettingAction(l2FullPull, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }

                // Store active buttons state
                l2TwoStageData.previousActiveButtons = tempButtons;
            }

            // Process R2
            TriggerOutputSettings r2TriggerSettings = profileSettings.R2OutputSettings[device];
            dcsTemp = controlSetGroup.R2;
            if (r2TriggerSettings.twoStageMode == TwoStageTriggerMode.Disabled)
            {
                ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY, ctrl);
            }
            else
            {
                DS4ControlSettings r2FullPull = controlSetGroup.R2FullPull;
                TwoStageTriggerMappingData r2TwoStageData = r2TwoStageMappingData[device];
                ProcessTwoStageTrigger(device, cState, cState.R2, cState.R2Raw, ref dcsTemp, ref r2FullPull,
                    r2TriggerSettings, r2TwoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull);

                TwoStageTriggerMappingData.ActiveZoneButtons tempButtons = TwoStageTriggerMappingData.ActiveZoneButtons.None;
                // Check for Soft Pull activation
                if (outputSoftPull != null ||
                    (r2TwoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull) != 0)
                {
                    if (outputSoftPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.triggers[(int)DS4Controls.R2] = 0;
                        outputfieldMapping.triggers[(int)DS4Controls.R2] = 0;
                    }

                    ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
                else
                {
                    // Soft Pull binding not engaged
                    outputfieldMapping.triggers[(int)DS4Controls.R2] = 0;
                }

                // Check for Full Pull activation
                if (outputFullPull != null ||
                    (r2TwoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.FullPull) != 0)
                {
                    if (outputFullPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.FullPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.buttons[(int)DS4Controls.R2FullPull] = false;
                    }

                    ProcessControlSettingAction(r2FullPull, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }

                // Store active buttons state
                r2TwoStageData.previousActiveButtons = tempButtons;
            }

            // Process Standard buttons
            for (var settingEnum = controlSetGroup.ControlButtons.GetEnumerator(); settingEnum.MoveNext();)
            {
                DS4ControlSettings dcs = settingEnum.Current;
                ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY, ctrl);
            }

            // Process Extra Device specific buttons
            for (var settingEnum = controlSetGroup.ExtraDeviceButtons.GetEnumerator(); settingEnum.MoveNext();)
            {
                DS4ControlSettings dcs = settingEnum.Current;
                ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY, ctrl);
            }

            // Phase8-Step4 W4: 移行済みの種別（既定は空）の毎フレームの出力。Stage3 の直後（通常の割り当ての後）の位置を保つ
            try { _engine.ApplyFrameOutputs(device, _triggerInputs[device], _frameOutputs[device]); }
            catch (Exception ex) { AppLogger.LogToGui($"Synchronous Button SA handling error: {ex}", true); }

            outputfieldMapping.PopulateState(MappedState);

            // MappedState summary TRACE removed as requested.

            if (_macroRuntime.MacroCount > 0)
            {
                if (macroControl[00]) MappedState.Cross = true;
                if (macroControl[01]) MappedState.Circle = true;
                if (macroControl[02]) MappedState.Square = true;
                if (macroControl[03]) MappedState.Triangle = true;
                if (macroControl[04]) MappedState.Options = true;
                if (macroControl[05]) MappedState.Share = true;
                if (macroControl[06]) MappedState.DpadUp = true;
                if (macroControl[07]) MappedState.DpadDown = true;
                if (macroControl[08]) MappedState.DpadLeft = true;
                if (macroControl[09]) MappedState.DpadRight = true;
                if (macroControl[10]) MappedState.PS = true;
                if (macroControl[11]) MappedState.L1 = true;
                if (macroControl[12]) MappedState.R1 = true;
                if (macroControl[13]) MappedState.L2 = 255;
                if (macroControl[14]) MappedState.R2 = 255;
                if (macroControl[15]) MappedState.L3 = true;
                if (macroControl[16]) MappedState.R3 = true;
                if (macroControl[17]) MappedState.LX = 255;
                if (macroControl[18]) MappedState.LX = 0;
                if (macroControl[19]) MappedState.LY = 255;
                if (macroControl[20]) MappedState.LY = 0;
                if (macroControl[21]) MappedState.RX = 255;
                if (macroControl[22]) MappedState.RX = 0;
                if (macroControl[23]) MappedState.RY = 255;
                if (macroControl[24]) MappedState.RY = 0;
                if (macroControl[25]) MappedState.OutputTouchButton = true;
            }


            PostMapStickData mapStickData = mapStickActionData[device];
            if (mapStickData.dirty)
            {
                if (mapStickData.LX != 128 &&
                    Math.Abs(mapStickData.LX - 128) > Math.Abs(MappedState.LX - 128))
                {
                    MappedState.LX = mapStickData.LX;
                }

                if (mapStickData.LY != 128 &&
                    Math.Abs(mapStickData.LY - 128) > Math.Abs(MappedState.LY - 128))
                {
                    MappedState.LY = mapStickData.LY;
                }

                if (mapStickData.RX != 128 &&
                    Math.Abs(mapStickData.RX - 128) > Math.Abs(MappedState.RX - 128))
                {
                    MappedState.RX = mapStickData.RX;
                }

                if (mapStickData.RY != 128 &&
                    Math.Abs(mapStickData.RY - 128) > Math.Abs(MappedState.RY - 128))
                {
                    MappedState.RY = mapStickData.RY;
                }

                mapStickData.Reset();
                mapStickData.dirty = false;
            }

            //if (imuOutMode == GyroOutMode.MouseJoystick)
            //{
            //    GyroMouseStickInfo msinfo = Global.GetGyroMouseStickInfo(device);
            //    if (msinfo.outputStick != GyroMouseStickInfo.OutputStick.None)
            //    {
            //        ref byte gyroTempX = ref gyroStickX[device];
            //        if (msinfo.OutputHorizontal() && gyroTempX != 128)
            //        {
            //            byte outputStickXVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
            //                MappedState.RX : MappedState.LX;
            //            byte tempAxisVal = 128;

            //            if (outputStickXVal != 128)
            //            {
            //                tempAxisVal = Math.Abs(gyroTempX - 128) > Math.Abs(outputStickXVal - 128) ?
            //                    gyroTempX : outputStickXVal;
            //            }
            //            else
            //            {
            //                tempAxisVal = gyroTempX;
            //            }

            //            if (msinfo.outputStick ==
            //                GyroMouseStickInfo.OutputStick.RightStick)
            //            {
            //                MappedState.RX = tempAxisVal;
            //            }
            //            else if (msinfo.outputStick ==
            //                GyroMouseStickInfo.OutputStick.LeftStick)
            //            {
            //                MappedState.LX = tempAxisVal;
            //            }
            //        }

            //        ref byte gyroTempY = ref gyroStickY[device];
            //        if (msinfo.OutputVertical() && gyroTempY != 128)
            //        {
            //            byte outputStickYVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
            //                MappedState.RY : MappedState.LY;
            //            byte tempAxisVal = 128;

            //            if (outputStickYVal != 128)
            //                tempAxisVal = Math.Abs(gyroTempY - 128) > Math.Abs(outputStickYVal - 128) ?
            //                    gyroTempY : outputStickYVal;
            //            else
            //                tempAxisVal = gyroTempY;

            //            if (msinfo.outputStick ==
            //                GyroMouseStickInfo.OutputStick.RightStick)
            //            {
            //                MappedState.RY = tempAxisVal;
            //            }
            //            else if (msinfo.outputStick ==
            //                GyroMouseStickInfo.OutputStick.LeftStick)
            //            {
            //                MappedState.LY = tempAxisVal;
            //            }
            //        }

            //        gyroTempX = gyroTempY = 128;
            //    }
            //}

            //TouchpadOutMode tempMode = Global.TouchOutMode[device];
            //if (tempMode == TouchpadOutMode.MouseJoystick)
            //{
            //    TouchMouseStickInfo msinfo = Global.GetTouchMouseStickInfo(device);
            //    if (msinfo.outputStick != TouchMouseStickInfo.OutputStick.None)
            //    {
            //        ref byte touchTempX = ref touchStickX[device];
            //        if (msinfo.OutputHorizontal() && touchTempX != 128)
            //        //if (touchTempX != 128)
            //        {
            //            byte outputStickXVal = msinfo.outputStick == TouchMouseStickInfo.OutputStick.RightStick ?
            //                MappedState.RX : MappedState.LX;
            //            byte tempAxisVal = 128;

            //            if (outputStickXVal != 128)
            //            {
            //                tempAxisVal = Math.Abs(touchTempX - 128) > Math.Abs(outputStickXVal - 128) ?
            //                    touchTempX : outputStickXVal;
            //            }
            //            else
            //            {
            //                tempAxisVal = touchTempX;
            //            }

            //            if (msinfo.outputStick ==
            //                TouchMouseStickInfo.OutputStick.RightStick)
            //            {
            //                MappedState.RX = tempAxisVal;
            //            }
            //            else if (msinfo.outputStick ==
            //                TouchMouseStickInfo.OutputStick.LeftStick)
            //            {
            //                MappedState.LX = tempAxisVal;
            //            }
            //        }

            //        ref byte touchTempY = ref touchStickY[device];
            //        if (msinfo.OutputVertical() && touchTempY != 128)
            //        //if (touchTempY != 128)
            //        {
            //            byte outputStickYVal = msinfo.outputStick == TouchMouseStickInfo.OutputStick.RightStick ?
            //                MappedState.RY : MappedState.LY;
            //            //byte outputStickYVal = MappedState.RY;
            //            byte tempAxisVal = 128;

            //            if (outputStickYVal != 128)
            //                tempAxisVal = Math.Abs(touchTempY - 128) > Math.Abs(outputStickYVal - 128) ?
            //                    touchTempY : outputStickYVal;
            //            else
            //                tempAxisVal = touchTempY;

            //            if (msinfo.outputStick ==
            //                TouchMouseStickInfo.OutputStick.RightStick)
            //            {
            //                MappedState.RY = tempAxisVal;
            //            }
            //            else if (msinfo.outputStick ==
            //                TouchMouseStickInfo.OutputStick.LeftStick)
            //            {
            //                MappedState.LY = tempAxisVal;
            //            }
            //        }

            //        touchTempX = touchTempY = 128;
            //    }
            //}

            // Phase8-Step7 7.8f-3（Q6＝A）: 新経路が書いたマウスの移動量。旧経路（局所変数）が何も書いていないときだけ使う（先に書いた方が勝つ、の規則のまま）
            DS4Windows.Actions.FrameOutput frameOutput = _frameOutputs[device];
            if (tempMouseDeltaX == 0.0) tempMouseDeltaX = frameOutput.MouseDeltaX;
            if (tempMouseDeltaY == 0.0) tempMouseDeltaY = frameOutput.MouseDeltaY;

            _mouseMotionQuantizer.Quantize(device, ref tempMouseDeltaX, ref tempMouseDeltaY,
                out mouseDeltaX, out mouseDeltaY);
            if (mouseDeltaX != 0 || mouseDeltaY != 0)
            {
                VirtualKBM.MoveRelativeMouse(mouseDeltaX, mouseDeltaY);
            }
        }

        public void TempMouseJoystick(int device, DS4State MappedState)
        {
            GyroOutMode imuOutMode = profileSettings.GetGyroOutMode(device);
            if (imuOutMode == GyroOutMode.MouseJoystick)
            {
                GyroMouseStickInfo msinfo = profileSettings.GetGyroMouseStickInfo(device);
                if (msinfo.outputStick != GyroMouseStickInfo.OutputStick.None)
                {
                    // Phase9 9-B: X／Y の合成（約 55 行）は GyroMouseStickBlender へ移した
                    _gyroMouseStickBlender.Blend(msinfo, gyroStickX[device], gyroStickY[device],
                        MappedState, mapStickActionData[device]);
                }
            }
        }

        // Phase7b W1: プロファイル切替の特殊アクション（「Unload on trigger release」など）の復帰の合図を、
        // ActionManager.ClearDeviceState（内部の HandleDeviceDisconnect が deviceRuntime を作り直す）の前後で保つための退避・復元。
        // 旧: プロファイルの適用のたびに合図が消え、トリガーを離しても元のプロファイルへ戻らなかった。
        public ProfileUntriggerSnapshot CaptureProfileUntrigger(int device)
        {
            if (deviceRuntime == null || device < 0 || device >= deviceRuntime.Length || deviceRuntime[device] == null)
                return default;

            return new ProfileUntriggerSnapshot(deviceRuntime[device].UntriggerAction, deviceRuntime[device].UntriggerIndex);
        }

        public void RestoreProfileUntrigger(int device, ProfileUntriggerSnapshot snapshot)
        {
            if (snapshot.Action == null || deviceRuntime == null || device < 0 || device >= deviceRuntime.Length || deviceRuntime[device] == null)
                return;

            deviceRuntime[device].UntriggerAction = snapshot.Action;
            deviceRuntime[device].UntriggerIndex = snapshot.Index;
        }

        // Handle full device disconnect cleanup: release synthetic keys, clear queues and tasks,
        // and remove any per-device runtime state. This consolidates the shutdown-like cleanup
        // performed when the application exits so it can be used on disconnect as well.
        public void HandleDeviceDisconnect(int device)
        {
            try
            {
                // Do not call back into ActionManager.ClearDeviceState here (ActionManager
                // now calls this method). Perform per-mapping cleanup locally below.

                // Clear per-device synthetic state: send release for any remaining synthetic key presses
                try
                {
                    if (deviceState != null && device >= 0 && device < deviceState.Length)
                    {
                        var ds = deviceState[device];
                        if (ds?.keyPresses != null)
                        {
                            var keys = new List<ushort>(ds.keyPresses.Keys);
                            foreach (var k in keys)
                            {
                                try
                                {
                                    uint native = 0;
                                    ds.nativeKeyAlias?.TryGetValue(k, out native);
                                    _syntheticDispatcher.SendRelease(device, k, native, false, null);
                                }
                                catch { }
                            }
                            try { ds.keyPresses.Clear(); } catch { }
                            try { ds.nativeKeyAlias.Clear(); } catch { }
                        }
                    }
                }
                catch { }

                // Clear macro task queue entries for device
                try
                {
                    if (macroTaskQueue != null && device >= 0 && device < macroTaskQueue.Length)
                    {
                        var dict = macroTaskQueue[device];
                        if (dict != null)
                        {
                            try { dict.Clear(); } catch { }
                        }
                    }
                }
                catch { }

                // Reset any simple runtime structures that could hold device state
                try { if (deviceRuntime != null && device >= 0 && device < deviceRuntime.Length) deviceRuntime[device] = new DeviceRuntimeState(); } catch { }

                // Finally ensure any per-device controllers are removed (defensive)
                try { _keyButtonControllers.ClearForDevice(device); } catch { }
            }
            catch { }
        }

        private void ProcessTwoStageTrigger(int device, DS4State cState, byte triggerValue, byte triggerRawValue,
            ref DS4ControlSettings inputSoftPull, ref DS4ControlSettings inputFullPull, TriggerOutputSettings outputSettings,
            TwoStageTriggerMappingData twoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull)
        {
            DS4ControlSettings dcsTemp = inputSoftPull;
            DS4ControlSettings dcsFullPull = null;
            TwoStageTriggerMappingData triggerData = twoStageData;

            switch (outputSettings.twoStageMode)
            {
                case TwoStageTriggerMode.Normal:
                    if (triggerRawValue == 255)
                    {
                        dcsFullPull = inputFullPull;
                    }

                    break;
                case TwoStageTriggerMode.ExclusiveButtons:
                    if (triggerRawValue == 255)
                    {
                        dcsFullPull = inputFullPull;
                        dcsTemp = null;
                        triggerData.actionStateMode =
                            TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                        triggerData.outputActive = true;
                    }
                    else if (triggerValue != 0 && triggerData.actionStateMode !=
                        TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly)
                    {
                        triggerData.actionStateMode =
                            TwoStageTriggerMappingData.EngageButtonsMode.Both;
                        triggerData.outputActive = true;
                    }
                    else if (triggerValue == 0 && triggerData.outputActive)
                    {
                        triggerData.Reset();
                    }

                    break;
                case TwoStageTriggerMode.HairTrigger:
                    dcsTemp = null;

                    triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.Both;

                    if (triggerRawValue == 255)
                    {
                        // Full pull now activates both. Soft pull action
                        // no longer engaged with threshold
                        dcsTemp = inputSoftPull;
                        dcsFullPull = inputFullPull;
                        triggerData.softPullActActive = true;
                        triggerData.fullPullActActive = true;
                        triggerData.outputActive = true;
                    }
                    else if (triggerValue != 0 && !triggerData.fullPullActActive)
                    {
                        // Full pull not engaged yet. Activate Soft pull action.
                        dcsTemp = inputSoftPull;
                        triggerData.softPullActActive = true;
                        triggerData.outputActive = true;
                    }
                    else if (triggerValue == 0 && triggerData.outputActive)
                    {
                        triggerData.Reset();
                    }
                    //else if (triggerData.outputActive)
                    //{
                    //    Console.WriteLine(triggerValue);
                    //}

                    break;

                case TwoStageTriggerMode.HipFire:
                    dcsTemp = null;

                    if (triggerValue != 0 && !triggerData.startCheck)
                    {
                        triggerData.StartProcessing();
                        dcsTemp = null;
                    }
                    else if (triggerValue != 0 && !triggerData.outputActive)
                    {
                        bool outputActive = triggerData.checkTime +
                            TimeSpan.FromMilliseconds(outputSettings.hipFireMS) + TimeSpan.FromMilliseconds(profileSettings.DebouncingMs[device]) < DateTime.Now;
                        if (outputActive)
                        {
                            triggerData.outputActive = true;

                            if (triggerRawValue == 255)
                            {
                                dcsFullPull = inputFullPull;
                                triggerData.fullPullActActive = true;
                                triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                            }
                            else if (triggerValue != 0)
                            {
                                dcsTemp = inputSoftPull;
                                triggerData.softPullActActive = true;
                                triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.Both;
                            }
                        }
                    }
                    else if (triggerData.outputActive)
                    {
                        //DS4State pState = d.getPreviousStateRef();
                        if (triggerRawValue == 255)
                        {
                            dcsFullPull = inputFullPull;
                            triggerData.fullPullActActive = true;
                            if (triggerData.actionStateMode == TwoStageTriggerMappingData.EngageButtonsMode.Both)
                            {
                                dcsTemp = inputSoftPull;
                            }
                        }
                        else if (triggerValue != 0 && triggerData.actionStateMode ==
                            TwoStageTriggerMappingData.EngageButtonsMode.Both)
                        {
                            triggerData.fullPullActActive = false;

                            dcsTemp = inputSoftPull;
                            triggerData.softPullActActive = true;
                        }
                        else if (triggerValue == 0)
                        {
                            triggerData.Reset();
                        }
                    }
                    else if (triggerData.startCheck)
                    {
                        triggerData.Reset();
                    }

                    break;
                case TwoStageTriggerMode.HipFireExclusiveButtons:
                    dcsTemp = null;

                    if (triggerValue != 0 && !triggerData.startCheck)
                    {
                        triggerData.StartProcessing();
                        dcsTemp = null;
                    }
                    else if (triggerValue != 0 && !triggerData.outputActive)
                    {
                        bool outputActive = triggerData.checkTime + TimeSpan.FromMilliseconds(outputSettings.hipFireMS) + TimeSpan.FromMilliseconds(profileSettings.DebouncingMs[device]) < DateTime.Now;
                        if (outputActive)
                        {
                            triggerData.outputActive = true;

                            if (triggerRawValue == 255)
                            {
                                dcsFullPull = inputFullPull;
                                triggerData.fullPullActActive = true;
                                triggerData.actionStateMode =
                                    TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                            }
                            else if (triggerValue != 0)
                            {
                                dcsTemp = inputSoftPull;
                                triggerData.softPullActActive = true;
                                triggerData.actionStateMode =
                                    TwoStageTriggerMappingData.EngageButtonsMode.SoftPullOnly;
                            }
                        }
                    }
                    else if (triggerData.outputActive)
                    {
                        //DS4State pState = d.getPreviousStateRef();
                        if (triggerRawValue == 255 &&
                            triggerData.actionStateMode == TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly)
                        {
                            dcsFullPull = inputFullPull;
                        }
                        else if (triggerValue != 0 && triggerData.actionStateMode ==
                            TwoStageTriggerMappingData.EngageButtonsMode.SoftPullOnly)
                        {
                            dcsTemp = inputSoftPull;
                        }
                        else if (triggerValue == 0)
                        {
                            triggerData.Reset();
                        }
                    }
                    else if (triggerData.startCheck)
                    {
                        triggerData.Reset();
                    }

                    break;
                default:
                    break;
            }

            outputSoftPull = dcsTemp;
            outputFullPull = dcsFullPull;
        }

        // Phase9 9-A(2)-C: 旧 ProcessFlickStick／HandleFlickStickAngle／WarpEaseOut（フリックスティック）は、FlickStickProcessor（DS4Control/Services）へ移した。

        //private static OneEuroFilter flickFilter = new OneEuroFilter(0.4, 0.4);
        //private static double flickProgress = 0.0;
        //private static double flickSize = 0.0;
        //private static double flickAngleRemainder = 0.0;

        //private const double REAL_WORLD_CALIBRATION = 10;

        //private static double FlickThreshold = 0.9;
        //private static double FlickTime = 0.1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessControlSettingAction(DS4ControlSettings dcs, int device, DS4State cState, DS4State MappedState, DS4StateExposed eState,
            Mouse tp, DS4StateFieldMapping fieldMapping, DS4StateFieldMapping outputfieldMapping, SyntheticState deviceState, ref double tempMouseDeltaX, ref double tempMouseDeltaY,
            ControlService ctrl)
        {
            // Check if this control press would trigger a Special Action
            // If so, suppress the normal mapping to prevent conflict
            if (CheckForSpecialActionSuppression(device, dcs.control, cState, eState, tp, fieldMapping))
            {
                // Special Action would be triggered - reset the control to default and return without processing
                ResetToDefaultValue(dcs.control, MappedState, outputfieldMapping);
                return;
            }

            // Phase8-Step7 7.8a／7.8b: 新経路（エンジン）が所有する割り当て（7.8b では Key の条件を満たすもの）は、エンジンが処理し、旧経路の本体は素通りする。
            // ロールバックは git のみ（Phase8-Step8 8d で移行のスイッチを削除）
            if (_engine.OwnsControl(device, dcs.control))
            {
                _engine.ProcessControl(device, dcs.control, _triggerInputs[device], _frameOutputs[device]);
                return;
            }

            // Phase8-Step8 8a: 旧経路の本体（Controls タブの Key／Macro／Button／Extras／Default の素通し）を削除した。
            // 新経路が所有しない割り当て（読めない Extras ほか）は、束縛を作らない＝何もしない（Phase8-Step8-Plan.md Q2）。
        }

        // InitializeActionDoneList removed — ActionManager owns per-action state initialization.

        // Phase8-Step8 8b: SA の種別ごとの旧枝（トリガーの判定、Key／Macro／Button／Program／Profile／DisconnectBT／BatteryCheck／ステアリング較正／
        // GyroCalibrate／MultiAction の実行、押し続けの解除、uTrigger の復帰）を削除した。移行済みの束縛をエンジンが処理する。束縛を持たない SA は、何もしない
        // （Phase8-Step8-Plan.md Q2）。以前は async void だったが、初期化待ちは完了済みの Task で、実際の await はタップ判定の遅延だけだったため、同期の void にした。
        private void MapCustomAction(int device, ControlService ctrl)
        {
            try
            {
                // 例外（プロファイルの編集との競合など）はここで記録して、フレームを続ける。
                int totalActionCount = ctrl.SpecialActionRepository.ActionList.Count;

                // プロファイルのアクション名のスナップショット（UI スレッドが一覧を変更しても、列挙中に壊れないように）
                string[] profileActions = ctrl.ProfileActionProvider.GetProfileActionsRaw(device)?.ToArray() ?? Array.Empty<string>();

                // Issue8-1(3)是正: 抑制中のボタンのうち、実際に離されたものを解除する（デバイスにつき 1 回）。
                // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-3-Fix-Plan.md §2.3
                try
                {
                    _engine.ReleaseSuppressed(device, _triggerInputs[device]);
                }
                catch { }

                for (int actionIndex = 0, profileListLen = profileActions.Length;
                     actionIndex < profileListLen; actionIndex++)
                {
                    string actionname = profileActions[actionIndex];
                    SpecialAction action = ctrl.ProfileActionProvider.GetProfileAction(device, actionname);
                    int index = ctrl.ProfileActionProvider.GetProfileActionIndexOf(device, actionname);

                    if (index < 0 || index >= totalActionCount)
                        continue;

                    if (action == null)
                        continue;

                    // 束縛が登録されているアクションは、エンジンが処理する。プロファイル内のアクションの順序は変わらない。
                    if (_engine.Owns(device, action))
                    {
                        if (_engine.ProcessOwned(device, action, _triggerInputs[device], _frameOutputs[device]) == DS4Windows.Actions.FrameResult.EndFrame)
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AppLogger.LogToGui($"MapCustomAction exception: {ex}", true);
                }
                catch { }

                return;
            }

            // 自動の復帰（Unload on trigger release）は、エンジンが持つ合図で確認する
            _engine.ProcessUntrigger(device, _triggerInputs[device]);
        }

        /// <summary>
        /// DI/IMacroPlayer 用のマクロ実行エントリーポイント
        /// </summary>
        /// <summary>
        /// DI/IMacroPlayer 用のマクロ実行エントリーポイント
        /// </summary>
        public void PlayMacroDirect(int device, SpecialAction action)
        {
            // Phase6-Step9b-3（F05）: 4 スロット固定だと、拡張スロット数が 8 のときデバイス 4〜7 のマクロが再生されなかった。
            if (device < 0 || device >= ControllerLimits.MaxControllerCount || action == null) return;
            PlayMacro(device, String.Empty, action.macro, null, action.keyType, action, null);
        }

        /// <summary>
        /// Phase8-Step7 7.7b（Q4＝A）: MultiAction の新経路（汎用の出力 <c>MacroStringAction</c>）用。マクロを文字列で直接指定して実行する
        /// （旧 <c>MapCustomAction</c> の MultiAction の枝と同じ呼び出し）。<c>IMapping</c> のメンバーではない（<c>DefaultMacroStringPlayer</c> が委譲する）。
        /// </summary>
        public void PlayMacroString(int device, string macro)
        {
            if (device < 0 || device >= ControllerLimits.MaxControllerCount || string.IsNullOrEmpty(macro)) return;
            PlayMacro(device, macro, null, null, DS4KeyType.None);
        }

        /// <summary>
        /// DI/IMacroPlayer 用のマクロ停止・キー解放エントリーポイント
        /// </summary>
        public void EndMacroDirect(int device)
        {
            if (device < 0 || device >= ControllerLimits.MaxControllerCount) return;
            // TODO(技術的負債・Phase9 候補): IMacroPlayer.Stop の実体。旧 EndMacro（control == None のときは何もしない空処理）を削除した（Phase8-Fix-MacroRepeatUnify D5）ため、
            // 現状も何もしない（マクロの停止は、RepeatMacro のループが BeingTriggered を見て止まる）。インターフェース（IMapping の窓口）を保つために、空実装で残している。
            // 離したときにマクロを中断してキーを解放する必要が出たら、ここで実装する。
        }




        // Play macro as a background task. Optionally the new macro play waits for completion of a previous macro execution (synchronized macro special action).
        // Macro steps are defined either as macrostr string value, macroLst list<int> object or as macroArr integer array. Only one of these should have a valid macro definition when this method is called.
        // If the macro definition is a macroStr string value then it will be converted as integer array on the fl. If steps are already defined as list or array of integers then there is no need to do type cast conversion.
        // Phase8-Fix-MacroRepeatUnify（D5）: 呼び出しは常に control == DS4Controls.None（SA・Controls タブ・MultiAction のすべて）だったため、control 単位の経路
        // （macrodone、macroDispatchInFlight／TryBeginDispatch）と、未使用の引数 control／macrocontrol を削除した。二重起動の防止は ActionInstanceState.IsMacroRunning が担う。
        private void PlayMacro(int device, string macroStr, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action = null, ActionInstanceState actionDoneState = null)
        {
            // If caller didn't provide per-action state, obtain it so we can check for running macro.
            ActionInstanceState st = actionDoneState;
            if (st == null && action != null)
            {
                try { st = ActionManager.GetStateFor(action, device); } catch { st = null; }
            }

            // Startup guard: if a macro iteration is already running for this action/device,
            // skip starting another macro and log the decision (DECISION=RUN/NORUN).
            try
            {
                bool actionRunning = st != null && st.IsMacroRunning;
                string decision = actionRunning ? "NORUN" : "RUN";
                AppLogger.LogTrace($"PlayMacro START GUARD: action={action?.name} device={device} IsMacroRunning={actionRunning} DECISION={decision}");
                if (actionRunning) return;
            }
            catch { }

            // Note: do not use a task-wide "macro running" guard here. Per-iteration
            // running state is represented by `IsMacroRunning` on the ActionInstanceState
            // and `RepeatMacro` semantics rely on `BeingTriggered`. Allow PlayMacro to
            // start; synchronization for repeat macros is handled via `macroTaskQueue`.
            // For "Repeat while held" macros we must avoid overlapping executions.
            // Treat repeat-macros as synchronized so a new run waits for previous to finish.
            bool shouldSync = action != null && (action.synchronized || keyType.HasFlag(DS4KeyType.RepeatMacro));
            if (shouldSync)
            {
                // Run special action macros in synchronized order (ie. FirstIn-First-Out). The trigger control name string is the execution queue identifier (ie. each unique trigger combination has an own synchronization queue).
                if (!macroTaskQueue[device].TryGetValue(action.controls, out Task prevTask))
                    macroTaskQueue[device].Add(action.controls, (Task.Factory.StartNew(() => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState))));
                else
                    macroTaskQueue[device][action.controls] = prevTask.ContinueWith((x) => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState));
            }
            else
                // Run macro as "fire and forget" background task. No need to wait for completion of any of the other macros.
                // If the same trigger macro is re-launched while previous macro is still running then the order of parallel macros is not guaranteed.
                Task.Factory.StartNew(() => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState));
        }

        /// <summary>Phase10-Step3: 手順が空のマクロを「押している間繰り返す」ときの、1 回ごとの間隔（ミリ秒）。手順のあるマクロの 1 回の再生時間（約 50ms）に合わせた。</summary>
        private const int EmptyMacroRepeatIntervalMs = 50;

        // Play through a macro. The macro steps are defined either as string, List or Array object (always only one of those parameters is set to a valid value)
        private void PlayMacroTask(int device, string macroStr, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action, ActionInstanceState actionDoneState)
        {
            // Ensure we have the per-action state from ActionManager if not provided by caller
            if (actionDoneState == null && action != null)
            {
                try { actionDoneState = ActionManager.GetStateFor(action, device); } catch { }
            }
            if (!String.IsNullOrEmpty(macroStr))
            {
                string[] skeys;

                skeys = macroStr.Split('/');
                macroArr = new int[skeys.Length];
                for (int i = 0; i < macroArr.Length; i++)
                    macroArr[i] = int.Parse(skeys[i]);
            }

            // Do not mark a task-wide "macro running" flag here. `IsMacroRunning` will
            // be set/cleared per single macro iteration below.

            try
            {
                try { AppLogger.LogTrace($"PlayMacroTask START: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)} keyType={keyType}"); } catch { }

                // Phase8-Step7-3a（機能廃止。ユーザー承認済み）: Alt-Tab の並び（Alt 押す・Tab 押す・Tab 離す・Alt 離す）で始まるマクロの特別扱いを廃止した。
                // この並びのマクロも、他のマクロと同じく、手順どおりに再生する（Phase8-Step7-3a-AltTab-Removal-Plan.md）。
                // 最初の 1 回は、IsMacroRunning を立てずに再生する（従来どおり）。
                RunMacroOnce(device, macroLst, macroArr, keyType, action, actionDoneState, markRunning: false);

                // Repeat-while-held handling: instead of re-dispatching a released edge (which may enqueue
                // additional macro runs), perform repeat-in-place. After a macro run completes, check the
                // per-action BeingTriggered flag; if it remains true (trigger still held), run the macro again.
                // This avoids accumulating a queue of runs that would execute after physical release.
                // Phase8-Fix-MacroRepeatUnify（D1）: 「押している間繰り返す」は RepeatMacro に統合した（SA と Controls タブで共通。旧 HoldMacro は読込時に RepeatMacro になる）。
                if (action != null && keyType.HasFlag(DS4KeyType.RepeatMacro) && actionDoneState != null)
                {
                    bool keepRunning = true;
                    while (keepRunning)
                    {
                        bool curBeing = false;
                        try { curBeing = ActionManager.IsBeingTriggered(action, device); keepRunning = curBeing; } catch { keepRunning = false; curBeing = false; }

                        // Log the recheck result and the decision whether we'll rerun this macro
                        try
                        {
                            string decision = keepRunning ? "RERUN" : "NORUN";
                            AppLogger.LogTrace($"PlayMacroTask ITERATION CHECK: action={action?.name} device={device} IsMacroRunning={actionDoneState?.IsMacroRunning ?? false} IsBeingTriggered={curBeing} DECISION={decision}");
                        }
                        catch { }

                        if (!keepRunning) break;

                        // Phase10-Step3: 手順が空のマクロも、押している間は繰り返す。ただし、1 回の再生に時間がかからず、待ちなしの空回りになるため、
                        // 繰り返しの間に、短い間隔（EmptyMacroRepeatIntervalMs）を置く（手順のあるマクロは、手順そのものの時間が間隔になる）
                        if ((macroLst == null || macroLst.Count == 0) && (macroArr == null || macroArr.Length == 0))
                            Task.Delay(EmptyMacroRepeatIntervalMs).Wait();

                        // Execute the macro sequence again inline (the iteration is marked as running)
                        RunMacroOnce(device, macroLst, macroArr, keyType, action, actionDoneState, markRunning: true);

                        // loop will check BeingTriggered again
                    }
                }
            }
            finally
            {
                if (actionDoneState != null)
                {
                    try { lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } } catch { }
                }
            }
            try { AppLogger.LogTrace($"PlayMacroTask END: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)}"); } catch { }
        }

        // Phase8-Fix-MacroRepeatUnify（D5）: 旧 PlayMacroTask の「最初の 1 回」と「繰り返しの 1 回」は、ほぼ同じコードが 2 つあったため、1 つにまとめた（挙動は同じ）。
        // markRunning: 繰り返しの 1 回は、実行中に IsMacroRunning を立てる（最初の 1 回は、従来どおり立てない）。
        private void RunMacroOnce(int device, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action, ActionInstanceState actionDoneState, bool markRunning)
        {
            IList<int> steps = macroLst != null ? (IList<int>)macroLst : macroArr;

            // Mark iteration running for this inline repeat
            if (markRunning && actionDoneState != null)
            {
                try { lock (actionDoneState) { actionDoneState.IsMacroRunning = true; } } catch { }
            }

            bool[] keydown = new bool[512];

            // Play macro codes and simulate key down/up events (note! The same key may go through several up and down events during the same macro).
            // If the return value is TRUE then this method should do a asynchronized delay (the usual Thread.Sleep doesnt work here because it would block the main gamepad reading thread).
            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    int macroCodeValue = steps[i];
                    if (PlayMacroCodeValue(device, keyType, macroCodeValue, keydown))
                        Task.Delay(macroCodeValue - 300).Wait();
                }
            }

            // The macro is finished. If any of the keys is still in down state then release a key state (ie. simulate key up event) unless special action specified to keep the last state as it is left in a macro
            if (action == null || !action.keepKeyState)
            {
                for (int i = 0, arlength = keydown.Length; i < arlength; i++)
                {
                    if (keydown[i])
                        PlayMacroCodeValue(device, keyType, i, keydown);
                }

                // Reset lightbar back to a default value (if the macro modified the color) because keepKeyState macro option was not set
                DS4LightBar.forcedFlash[device] = 0;
                DS4LightBar.forcelight[device] = false;
            }

            // Commented out rumble reset. No need to zero out rumble after a macro because it may conflict with a game generated rumble events (ie. macro would stop a game generated rumble effect).
            // If macro generates rumble effects then the macro can stop the rumble as a last step or wait for rumble watchdog timer to do it after few seconds.
            //Program.rootHub.DS4Controllers[device].setRumble(0, 0);

            // Clear iteration-running so the log shows the iteration completed
            if (actionDoneState != null)
            {
                try { lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } } catch { }
            }
            try { AppLogger.LogTrace($"PlayMacroTask MACRO EXECUTE END: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)}"); } catch { }
        }

        private bool PlayMacroCodeValue(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown)
        {
            // Phase6-Step3-6c（決定P2）: 非同期マクロ経路（別スレッド、ctrl を持たない連鎖）のため、Mapping が保持する profileSettings から読む。
            // Phase7-Step8 W2 で instance 化（コンストラクター注入）済み。この経路も instance の profileSettings を読む（Phase7c W3 でコメントを訂正）。
            bool doDelayOnCaller = false;
            if (macroCodeValue >= 261 && macroCodeValue <= DS4ControlSettings.MAX_MACRO_VALUE)
            {
                // Gamepad button up or down macro event. macroCodeValue index value is the button identifier (codeValue-261 = idx in 0..24 range)
                if (!keydown[macroCodeValue])
                {
                    macroControl[macroCodeValue - 261] = keydown[macroCodeValue] = true;
                    _macroRuntime.MacroCount++;
                }
                else
                {
                    macroControl[macroCodeValue - 261] = keydown[macroCodeValue] = false;
                    if (_macroRuntime.MacroCount > 0) _macroRuntime.MacroCount--;
                }
            }
            else if (macroCodeValue < 300)
            {
                // Keyboard key or mouse button macro event
                if (!keydown[macroCodeValue])
                {
                    switch (macroCodeValue)
                    {
                        //anything above 255 is not a keyvalue
                        case 256:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseLeftDown");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);
                            break;
                        case 257:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseRightDown");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                            break;
                        case 258:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseMiddleDown");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                            break;
                        case 259:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonDown btn=1");
                            VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                            break;
                        case 260:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonDown btn=2");
                            VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                            break;

                        default:
                            uint eventMacroCode = !profileSettings.OutputKBMMapping.macroKeyTranslate ? (uint)macroCodeValue :
                                profileSettings.OutputKBMMapping.GetRealEventKey((uint)macroCodeValue);

                            if (keyType.HasFlag(DS4KeyType.ScanCode))
                            {
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyPressAlt native={eventMacroCode}");
                                VirtualKBM.PerformKeyPressAlt(eventMacroCode);
                            }
                            else
                            {
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyPress native={eventMacroCode}");
                                VirtualKBM.PerformKeyPress(eventMacroCode);
                            }
                            break;
                    }
                    keydown[macroCodeValue] = true;
                }
                else
                {
                    switch (macroCodeValue)
                    {
                        //anything above 255 is not a keyvalue
                        case 256:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseLeftUp");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                            break;
                        case 257:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseRightUp");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                            break;
                        case 258:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseMiddleUp");
                            VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                            break;
                        case 259:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonUp btn=1");
                            VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                            break;
                        case 260:
                            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonUp btn=2");
                            VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
                            break;

                        default:
                            uint eventMacroCode = !profileSettings.OutputKBMMapping.macroKeyTranslate ? (uint)macroCodeValue :
                                profileSettings.OutputKBMMapping.GetRealEventKey((uint)macroCodeValue);

                            if (keyType.HasFlag(DS4KeyType.ScanCode))
                            {
                                long _nowTicksDbg = DateTime.UtcNow.Ticks;
                                long _deltaSendDbg = 0; // inline macro release: lastSyntheticSend not tracked here
                                AppLogger.LogTrace($"INLINE RELEASE TRACE device={device} macroCode={macroCodeValue} native={eventMacroCode} lastSendDeltaMs={_deltaSendDbg}");
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyReleaseAlt native={eventMacroCode}");
                                VirtualKBM.PerformKeyReleaseAlt(eventMacroCode);
                            }
                            else
                            {
                                long _nowTicksDbg = DateTime.UtcNow.Ticks;
                                long _deltaSendDbg = 0;
                                AppLogger.LogTrace($"INLINE RELEASE TRACE device={device} macroCode={macroCodeValue} native={eventMacroCode} lastSendDeltaMs={_deltaSendDbg}");
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyRelease native={eventMacroCode}");
                                VirtualKBM.PerformKeyRelease(eventMacroCode);
                            }
                            break;
                    }
                    keydown[macroCodeValue] = false;
                }
            }
            else if (macroCodeValue >= 1000000000)
            {
                // Lightbar color event
                if (macroCodeValue > 1000000000)
                {
                    string lb = macroCodeValue.ToString().Substring(1);
                    byte r = (byte)(int.Parse(lb[0].ToString()) * 100 + int.Parse(lb[1].ToString()) * 10 + int.Parse(lb[2].ToString()));
                    byte g = (byte)(int.Parse(lb[3].ToString()) * 100 + int.Parse(lb[4].ToString()) * 10 + int.Parse(lb[5].ToString()));
                    byte b = (byte)(int.Parse(lb[6].ToString()) * 100 + int.Parse(lb[7].ToString()) * 10 + int.Parse(lb[8].ToString()));
                    DS4LightBar.forcelight[device] = true;
                    DS4LightBar.forcedFlash[device] = 0;
                    DS4LightBar.forcedColor[device] = new DS4Color(r, g, b);
                }
                else
                {
                    DS4LightBar.forcedFlash[device] = 0;
                    DS4LightBar.forcelight[device] = false;
                }
            }
            else if (macroCodeValue >= 1000000)
            {
                // Rumble event
                DS4Device d = null;
                try
                {
                    var accessor = _deviceStateAccessor();
                    if (accessor != null) d = accessor.GetController(device);
                }
                catch { }
                // Phase7-Step5 W2: IDeviceStateAccessor は ControlService 自身の登録のため、旧 Program.rootHub のフォールバックは冗長だった
                string r = macroCodeValue.ToString().Substring(1);
                byte heavy = (byte)(int.Parse(r[0].ToString()) * 100 + int.Parse(r[1].ToString()) * 10 + int.Parse(r[2].ToString()));
                byte light = (byte)(int.Parse(r[3].ToString()) * 100 + int.Parse(r[4].ToString()) * 10 + int.Parse(r[5].ToString()));
                if (profileSettings.InverseRumbleMotors[device])
                    d.setRumble(heavy, light);
                else
                    d.setRumble(light, heavy);
            }
            else
            {
                // Delay specification. Indicate to caller that it should do a delay of macroCodeValue-300 msecs
                doDelayOnCaller = true;
            }

            return doDelayOnCaller;
        }

        // Phase9 9-B: 旧 getMouseMapping（約 318 行）は ButtonMouseMovementCalculator（DS4Control/Services）へ、
        // 旧 calculateFinalMouseMovement は MouseMotionQuantizer（同）へ、そのまま移した。
        // 計算の変更履歴は git log を参照（Phase9-B-Plan.md）。

        public static double remainderCutoff(double dividend, double divisor)
        {
            return dividend - (divisor * (int)(dividend / divisor));
        }

        // Phase9 9-B（Q4）: 旧 compare(byte, byte)（呼び出し元 0 件。上流 DS4Windows 由来の未使用コード）は削除した。

        /// <summary>
        /// Method to check the currently active bool state for an input control.
        /// Slower version that does not take advantage of a generated DS4StateFieldMapping instance.
        /// Meant to be used outside of the Mapping class
        /// </summary>
        /// <param name="device">Input slot number for DS4Device</param>
        /// <param name="control">Current control mapped</param>
        /// <param name="cState">Current input state</param>
        /// <param name="eState">Exposed input state helper</param>
        /// <param name="tp">Mouse object</param>
        /// <returns>Is the specified input considered active</returns>
        public bool GetBoolMappingExternal(int device, DS4Controls control,
            DS4State cState, DS4StateExposed eState, Mouse tp)
        {
            // Phase9 9-B: 判定の本体（約 105 行）は ControlActivityReader へ移した。設定は、Mapping が保持する profileSettings を渡す
            return _controlActivityReader.IsActive(profileSettings, device, control, cState, eState, tp);
        }

        // Check if the current button press would trigger any Special Action
        // Returns true if a Special Action would be triggered, indicating normal mapping should be suppressed
        private bool CheckForSpecialActionSuppression(int device, DS4Controls control,
            DS4State cState, DS4StateExposed eState, Mouse tp, DS4StateFieldMapping fieldMap)
        {
            // Issue8-1(3)是正: 「現在トリガーが成立しているか」（IsSpecialActionTriggeredの都度再計算）ではなく、
            // 「このボタンが、いずれかのトリガー成立によりまだ抑制継続中か」を判定する。
            // 判定に必要な状態は、エンジンが所有する抑制の集合（MapCustomAction内で更新。Phase8-Step4 W4 で Mapping から移した）で管理される。
            // これにより、トリガー成立中にPS等の一部ボタンだけ先に離されても、
            // まだ押され続けているボタン（例: L2）自身の通常出力は、L2が実際に離されるまで抑制され続ける
            // （旧実装は triggeractivated、すなわちいずれか1つでも離れた瞬間に抑制ごと解除されてしまっていた）。
            // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-3-Fix-Plan.md §2.4
            return _engine.IsSuppressed(device, control);
        }

        public static bool getBoolButtonMapping(bool stateButton)
        {
            return stateButton;
        }

        public static bool getBoolAxisDirMapping(byte stateAxis, bool positive)
        {
            return positive ? stateAxis > 128 + 55 : stateAxis < 128 - 55;
        }

        public static bool getBoolTriggerMapping(byte stateAxis)
        {
            return stateAxis > 100;
        }

        public static bool getBoolTouchMapping(bool touchButton)
        {
            return touchButton;
        }

        private void ResetToDefaultValue(DS4Controls control, DS4State cState,
            DS4StateFieldMapping fieldMap)
        {
            int controlNum = (int)control;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                fieldMap.buttons[controlNum] = false;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                fieldMap.axisdirs[controlNum] = 128;
                int controlRelation = (controlNum % 2 == 0 ? controlNum - 1 : controlNum + 1);
                fieldMap.axisdirs[controlRelation] = 128;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                fieldMap.triggers[controlNum] = 0;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Touch)
            {
                fieldMap.buttons[controlNum] = false;
            }
        }

        // Phase9 Steering 削除 W2（2026-10-22。Phase9-SteeringWheel-Removal-Plan.md）: 旧「SA steering wheel emulation mapping」（Scale360degreeGyroAxis／SAWheelEmulationCalibration／CalculateControllerAngle／CalcWheelFuzz ほか。
        // ジャイロでハンドル操作をする機能）は、機能ごと削除した（ユーザーの承認）。
    }
}