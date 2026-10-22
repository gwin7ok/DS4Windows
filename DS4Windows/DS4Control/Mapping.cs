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
        // Phase9 9-C: 旧 _deviceStateAccessor（マクロの振動イベントで使う遅延の取得）は MacroStepExecutor へ移した。マクロの再生は IMacroPlayback が担う
        private readonly DS4Windows.DI.IMacroPlayback _macroPlayback;

        // Phase7-Step8 W2（Q5＝A）: 保持クラス（コンストラクターで受ける）。Step7 の転送プロパティは削除した。
        // 配列の参照は再代入されない（要素の読み書きのみ）ため、readonly で 1 度だけ複製して持ち、毎レポートの割り当てと間接参照を増やさない。
        // 差し替わる GlobalState と、更新される MacroCount だけは、保持クラスを直接読み書きする。
        private readonly SyntheticInputState _syntheticInput;
        private readonly MacroRuntimeState _macroRuntime;
        private readonly StickMotionState _stickMotion;
        private readonly ReaderWriterLockSlim syncStateLock;
        private readonly SyntheticState[] deviceState;
        // Phase9 9-C: 旧 macroControl／macroTaskQueue（MacroRuntimeState の配列の複製）は、マクロの再生の部品（MacroStepExecutor／MacroPlayback）と
        // MacroRuntimeState.ApplyHeldControls へ移したため、Mapping は持たない（_macroRuntime を直接使う）。
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
            DS4Windows.DI.IMacroPlayback macroPlayback,
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
            DS4Windows.DI.IControlActivityReader controlActivityReader,
            DS4Windows.DI.ISyntheticInputCommitter syntheticCommitter,
            DS4Windows.DI.ITwoStageTriggerProcessor twoStageTrigger)
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
            _macroPlayback = macroPlayback ?? throw new ArgumentNullException(nameof(macroPlayback));
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
            _syntheticCommitter = syntheticCommitter ?? throw new ArgumentNullException(nameof(syntheticCommitter));
            _twoStageTrigger = twoStageTrigger ?? throw new ArgumentNullException(nameof(twoStageTrigger));

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
        // Phase9 MapCustom の分割 M1: 旧 oldnow／wheel（ホイールの繰り返しの状態）は SyntheticInputCommitter へ、常に false だった pressagain は削除した。

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
        private readonly DS4Windows.DI.ITwoStageTriggerProcessor _twoStageTrigger;   // Phase9 MapCustom の分割 M2: 2 段階トリガーの状態機械（旧 ProcessTwoStageTrigger）
        private readonly DS4Windows.DI.ISyntheticInputCommitter _syntheticCommitter;   // Phase9 MapCustom の分割 M1: 合成入力の確定（旧 Commit の本体）
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

        // Phase9 MapCustom の分割 M1: 合成入力の確定（約 170 行）は SyntheticInputCommitter へ移した（薄い委譲。呼び出し側は変えない）
        public void Commit(int device, DS4Windows.DI.IProfileSettingsService settings)
        {
            _syntheticCommitter.Commit(device, settings);
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

            SyntheticState deviceState = this.deviceState[device];
            if (ctrl.ProfileActionProvider.GetProfileActionCount(device) > 0 || profileSettings.GetUseTempProfile(device))
                MapCustomAction(device, ctrl);

            ControlSettingsGroup controlSetGroup = profileSettings.GetControlSettingsGroup(device);

            // Process LS／RS（Phase9 MapCustom の分割 M3c: 左右で似たコードが 2 回あった。LS → RS の順は変えない）
            ProcessStick(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY, ctrl,
                profileSettings.LSOutputSettings[device], controlSetGroup.LS, false);
            ProcessStick(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY, ctrl,
                profileSettings.RSOutputSettings[device], controlSetGroup.RS, true);

            // Process L2／R2（Phase9 MapCustom の分割 M3b: 左右で同じコードが 2 回あった［約 130 行］。L2 → R2 の順は変えない）
            ProcessTriggerWithTwoStage(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY, ctrl,
                controlSetGroup.L2, controlSetGroup.L2FullPull, DS4Controls.L2, DS4Controls.L2FullPull,
                cState.L2, cState.L2Raw, profileSettings.L2OutputSettings[device], l2TwoStageMappingData[device]);
            ProcessTriggerWithTwoStage(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY, ctrl,
                controlSetGroup.R2, controlSetGroup.R2FullPull, DS4Controls.R2, DS4Controls.R2FullPull,
                cState.R2, cState.R2Raw, profileSettings.R2OutputSettings[device], r2TwoStageMappingData[device]);

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

            // Phase9 9-C: マクロが押しているゲームパッドのボタン等を出力へ重ねる処理は、MacroRuntimeState.ApplyHeldControls へ移した（デバイスごと。ユーザー判断 Q2）
            _macroRuntime.ApplyHeldControls(device, MappedState);

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

        // Phase9 MapCustom の分割 M3c: LS／RS の 1 スティック分の処理（旧 MapCustom の「Process LS」「Process RS」）。
        // 左右の違い: 軸の添字、フリックスティックの読む軸、RS だけの「デルタ加速」（Controls モードのとき、割り当ての処理の前に実行）。ホットパスのため割り当てを増やさない
        private void ProcessStick(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState,
            Mouse tp, DS4StateFieldMapping fieldMapping, DS4StateFieldMapping outputfieldMapping, SyntheticState deviceState,
            ref double tempMouseDeltaX, ref double tempMouseDeltaY, ControlService ctrl,
            StickOutputSetting stickSettings, List<DS4ControlSettings> stickControls, bool isRightStick)
        {
            if (stickSettings.mode == StickMode.Controls)
            {
                if (isRightStick && stickSettings.outputSettings.controlSettings.deltaAccelSettings.enabled)
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

                for (var settingEnum = stickControls.GetEnumerator(); settingEnum.MoveNext();)
                {
                    DS4ControlSettings dcs = settingEnum.Current;
                    ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
            }
            else
            {
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RXNeg : DS4Controls.LXNeg)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RXPos : DS4Controls.LXPos)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RYNeg : DS4Controls.LYNeg)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RYPos : DS4Controls.LYPos)] = 128;

                switch (stickSettings.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.FlickStick:
                        DS4Device d = ctrl.DS4Controllers[device];
                        DS4State cRawState = d.getCurrentStateRef();
                        DS4State pState = d.getPreviousStateRef();

                        if (isRightStick)
                        {
                            _flickStick.Process(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.RX, cRawState.RY, pState.RX, pState.RY, ref tempMouseDeltaX);
                        }
                        else
                        {
                            _flickStick.Process(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.LX, cRawState.LY, pState.LX, pState.LY, ref tempMouseDeltaX);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        // Phase9 MapCustom の分割 M3b: L2／R2 の 1 トリガー分の処理（旧 MapCustom の「Process L2」「Process R2」。値は Phase9MapCustomTwoStageCharacterizationTests が守る）。
        // ホットパスのため、ラムダや割り当てを使わない
        private void ProcessTriggerWithTwoStage(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState,
            Mouse tp, DS4StateFieldMapping fieldMapping, DS4StateFieldMapping outputfieldMapping, SyntheticState deviceState,
            ref double tempMouseDeltaX, ref double tempMouseDeltaY, ControlService ctrl,
            DS4ControlSettings dcsTemp, DS4ControlSettings fullPullSettings, DS4Controls softControl, DS4Controls fullControl,
            byte triggerValue, byte triggerRawValue, TriggerOutputSettings triggerSettings, TwoStageTriggerMappingData twoStageData)
        {
            if (triggerSettings.twoStageMode == TwoStageTriggerMode.Disabled)
            {
                ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY, ctrl);
            }
            else
            {
                DS4ControlSettings fullPull = fullPullSettings;
                _twoStageTrigger.Process(device, triggerValue, triggerRawValue, ref dcsTemp, ref fullPull,
                    triggerSettings, twoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull);

                TwoStageTriggerMappingData.ActiveZoneButtons tempButtons = TwoStageTriggerMappingData.ActiveZoneButtons.None;
                // Check for Soft Pull activation
                if (outputSoftPull != null ||
                    (twoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull) != 0)
                {
                    if (outputSoftPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.triggers[(int)softControl] = 0;
                        outputfieldMapping.triggers[(int)softControl] = 0;
                    }

                    ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }
                else
                {
                    // Soft Pull binding not engaged
                    outputfieldMapping.triggers[(int)softControl] = 0;
                }

                // Check for Full Pull activation
                if (outputFullPull != null ||
                    (twoStageData.previousActiveButtons & TwoStageTriggerMappingData.ActiveZoneButtons.FullPull) != 0)
                {
                    if (outputFullPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.FullPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.buttons[(int)fullControl] = false;
                    }

                    ProcessControlSettingAction(fullPull, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY, ctrl);
                }

                // Store active buttons state
                twoStageData.previousActiveButtons = tempButtons;
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
                // Phase9 9-C: キューのクリアは IMacroPlayback.ClearDevice へ移した（同じ処理）
                try { _macroPlayback.ClearDevice(device); }
                catch { }

                // Reset any simple runtime structures that could hold device state
                try { if (deviceRuntime != null && device >= 0 && device < deviceRuntime.Length) deviceRuntime[device] = new DeviceRuntimeState(); } catch { }

                // Finally ensure any per-device controllers are removed (defensive)
                try { _keyButtonControllers.ClearForDevice(device); } catch { }
            }
            catch { }
        }

        // Phase9 MapCustom の分割 M2: 2 段階トリガーの状態機械（約 195 行）は TwoStageTriggerProcessor へ移した

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
            _macroPlayback.Play(device, String.Empty, action.macro, null, action.keyType, action, null);
        }

        /// <summary>
        /// Phase8-Step7 7.7b（Q4＝A）: MultiAction の新経路（汎用の出力 <c>MacroStringAction</c>）用。マクロを文字列で直接指定して実行する
        /// （旧 <c>MapCustomAction</c> の MultiAction の枝と同じ呼び出し）。<c>IMapping</c> のメンバーではない（<c>DefaultMacroStringPlayer</c> が委譲する）。
        /// </summary>
        public void PlayMacroString(int device, string macro)
        {
            if (device < 0 || device >= ControllerLimits.MaxControllerCount || string.IsNullOrEmpty(macro)) return;
            _macroPlayback.Play(device, macro, null, null, DS4KeyType.None);
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




        // Phase9 9-C: 旧 PlayMacro／PlayMacroTask／RunMacroOnce（マクロの再生の予約・繰り返し・後始末）は MacroPlayback へ、
        // 旧 PlayMacroCodeValue（1 つのマクロ値の実行）は MacroStepExecutor（どちらも DS4Control/Services）へ、そのまま移した。
        // 旧コメント・変更履歴は、移し先のファイルに残してある（Phase9-C-Plan.md）。

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