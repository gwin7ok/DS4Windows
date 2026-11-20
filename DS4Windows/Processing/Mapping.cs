using DS4Windows.Processing.Touchpad;
using DS4Windows.Processing.Gyro;
using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using IVirtualKBM = DS4Windows.Processing.Abstractions.IVirtualKBM;
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
using System.Runtime.CompilerServices;
using System.Threading;
// 見送り。GetTransitionedColor（純粋計算、Step3対象外の除外51件の1つ、
// docs-forDIMG/MadeByAgent/Archive/Phase6-Step3-Plan.md §1）が非修飾で呼ばれており本ディレクティブに依存するため、現時点では削除できない。
// これ以外の Global 実利用箇所はすべて Global. 明示修飾（温存3件: ProfileSettingsServiceInstance／outputKBMHandler／ApplyProfile。
// いずれも上記のTODO参照）または各DIサービス経由への置換で解消済み（docs-forDIMG/MadeByAgent/Archive/Phase6-Step3-Plan.md §6 完了判定チェックリスト）。
// 【Phase7c W3 追記】上記は Phase6 時点の記録。Phase7-Step10d で Global 型は削除済み（Global への参照は存在しない）。
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing
{
    public partial class Mapping : DS4Windows.Processing.Abstractions.IMapping, DS4Windows.Processing.Actions.IMacroStringPlayer
    {
        // 旧 静的な束縛（AppHost／Global のフォールバックつき）。コンストラクターで受ける（フォールバックは廃止）。
        private readonly DS4Windows.Core.Abstractions.IProfileSettingsService profileSettings;
        private readonly DS4Windows.Processing.Abstractions.IDeviceSlotState _slots;   // デバイス・状態の窓口
        private readonly DS4Windows.Processing.Abstractions.IProfileActionProvider _profileActionProvider;   // 旧 ctrl.ProfileActionProvider
        private readonly IVirtualKBM VirtualKBM;
        // 旧 静的クラス SyntheticDispatcher の呼び出し
        // 旧 _deviceStateAccessor（マクロの振動イベントで使う遅延の取得）は MacroStepExecutor へ移した。マクロの再生は IMacroPlayback が担う
        private readonly DS4Windows.Processing.Abstractions.IMacroPlayback _macroPlayback;

        // 保持クラス（コンストラクターで受ける）。Step7 の転送プロパティは削除した。
        // 配列の参照は再代入されない（要素の読み書きのみ）ため、readonly で 1 度だけ複製して持ち、毎レポートの割り当てと間接参照を増やさない。
        // 差し替わる GlobalState と、更新される MacroCount だけは、保持クラスを直接読み書きする。
        private readonly SyntheticInputState _syntheticInput;
        private readonly MacroRuntimeState _macroRuntime;
        private readonly ReaderWriterLockSlim syncStateLock;
        private readonly SyntheticState[] deviceState;
        // 旧 macroControl／macroTaskQueue（MacroRuntimeState の配列の複製）は、マクロの再生の部品（MacroStepExecutor／MacroPlayback）と
        // MacroRuntimeState.ApplyHeldControls へ移したため、Mapping は持たない（_macroRuntime を直接使う）。
        private readonly byte[] gyroStickX;
        private readonly byte[] gyroStickY;
        private readonly PostMapStickData[] mapStickActionData;
        private readonly FlickStickMappingData[] flickMappingData;
        private readonly TwoStageTriggerMappingData[] l2TwoStageMappingData;
        private readonly TwoStageTriggerMappingData[] r2TwoStageMappingData;
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

        // DI の Singleton（IMapping）。旧: メンバーのほぼすべてが static で、サービスは AppHost／Global の静的な束縛だった。
        public Mapping(
            SyntheticInputState syntheticInput,
            MacroRuntimeState macroRuntime,
            StickMotionState stickMotion,
            DS4Windows.Core.Abstractions.IProfileSettingsService profileSettings,
            IVirtualKBM virtualKBM,
            DS4Windows.Processing.Abstractions.IDeviceInputCleanup inputCleanup,
            DS4Windows.Processing.Abstractions.IMacroPlayback macroPlayback,
            DS4Windows.Processing.Actions.ITriggerDispatchEngine triggerEngine,
            DS4Windows.Processing.Abstractions.IMouseConversionStage mouseConversion,
            DS4Windows.Processing.Abstractions.ICurveAndDeadzoneStage curveStage,
            DS4Windows.Processing.Abstractions.IControlActivityReader controlActivityReader,
            DS4Windows.Processing.Abstractions.ISyntheticInputCommitter syntheticCommitter,
            DS4Windows.Processing.Abstractions.ITwoStageTriggerProcessor twoStageTrigger,
            DS4Windows.Processing.Abstractions.IDeviceSlotState slots,
            DS4Windows.Processing.Abstractions.IProfileActionProvider profileActionProvider)
        {
            // 旧 MapCustom の ControlService 引数（逆依存）の代わりに、必要な 3 つの窓口をコンストラクターで受ける
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(profileActionProvider);
            _profileActionProvider = profileActionProvider;

            ArgumentNullException.ThrowIfNull(syntheticInput);
            _syntheticInput = syntheticInput;
            ArgumentNullException.ThrowIfNull(macroRuntime);
            _macroRuntime = macroRuntime;
            if (stickMotion == null) throw new ArgumentNullException(nameof(stickMotion));
            ArgumentNullException.ThrowIfNull(profileSettings);
            this.profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(virtualKBM);
            this.VirtualKBM = virtualKBM;
            ArgumentNullException.ThrowIfNull(macroPlayback);
            _macroPlayback = macroPlayback;
            ArgumentNullException.ThrowIfNull(inputCleanup);
            _inputCleanup = inputCleanup;
            ArgumentNullException.ThrowIfNull(triggerEngine);
            _engine = triggerEngine;
            ArgumentNullException.ThrowIfNull(mouseConversion);
            _mouseConversion = mouseConversion;
            ArgumentNullException.ThrowIfNull(curveStage);
            _curveStage = curveStage;
            ArgumentNullException.ThrowIfNull(controlActivityReader);
            _controlActivityReader = controlActivityReader;
            ArgumentNullException.ThrowIfNull(syntheticCommitter);
            _syntheticCommitter = syntheticCommitter;
            ArgumentNullException.ThrowIfNull(twoStageTrigger);
            _twoStageTrigger = twoStageTrigger;

            _mouseSampler = new ButtonMouseSampler(profileSettings, slots, mouseConversion, stickMotion.DeltaAccelProcessors);
            _triggerInputs = new DS4Windows.Processing.Actions.TriggerInput[ControllerLimits.MaxControllerCount];
            _frameOutputs = new DS4Windows.Processing.Actions.FrameOutput[ControllerLimits.MaxControllerCount];
            for (int device = 0; device < _triggerInputs.Length; device++)
            {
                _triggerInputs[device] = new DS4Windows.Processing.Actions.TriggerInput(device, profileSettings);
                _frameOutputs[device] = new DS4Windows.Processing.Actions.FrameOutput();

                // 新経路のマウスの移動が、生の量の計算（getMouseMapping）を借りる。デバイスごとに 1 回だけ作る（フレームごとの割り当てなし）
                int sampleDevice = device;
                _triggerInputs[device].MouseSampler = (control, direction) => _mouseSampler.SampleMouseMovement(sampleDevice, control, direction);
                _triggerInputs[device].FixedMouseSampler = (direction, fraction) => _mouseSampler.SampleFixedMouseMovement(sampleDevice, direction, fraction);   // SA のマウス移動（強さの固定値）
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
            for (int i = 0; i < _mapFrames.Length; i++) _mapFrames[i] = new MapFrame();
            _controlFrame = new ControlFrameProcessor(slots, triggerEngine, mouseConversion, twoStageTrigger, stickMotion.DeltaAccelProcessors,
                stickMotion.FlickMappingData, _triggerInputs, _frameOutputs);
            _specialActions = new SpecialActionFrameRunner(profileActionProvider, triggerEngine, _triggerInputs, _frameOutputs);
        }

        // デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: keyButtonControllers／
        // GetOrCreateKeyButtonController／ClearKeyButtonControllersForDevice）は KeyButtonControllerRegistry へ移した。

        // 外から触られるスティック・ジャイロ・トリガーの処理状態（gyroStickX／gyroStickY／mapStickActionData／
        // flickMappingData／l2TwoStageMappingData／r2TwoStageMappingData／deltaAccelProcessors［旧 wheelFilters は Phase9 Steering 削除 W2 で削除］）の実体は StickMotionState が持つ。
        // 転送プロパティは削除した。上のフィールド宣言（保持クラスの配列の readonly の複製）が、その代わりである。
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

        // 合成入力の状態（_syntheticInput.GlobalState／deviceState／syncStateLock）の実体は SyntheticInputState が持つ。
        // 転送プロパティは削除した。deviceState／syncStateLock は上のフィールド宣言（readonly の複製）、GlobalState は保持クラスを直接読む。

        public ControllerStateFieldMapping[] fieldMappings = new ControllerStateFieldMapping[ControllerLimits.MaxControllerCount] {
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
        };
        public ControllerStateFieldMapping[] outputFieldMappings = new ControllerStateFieldMapping[ControllerLimits.MaxControllerCount]
        {
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
        };
        public ControllerStateFieldMapping[] previousFieldMappings = new ControllerStateFieldMapping[ControllerLimits.MaxControllerCount]
        {
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
            new ControllerStateFieldMapping(), new ControllerStateFieldMapping(),
        };

        // TODO When we disconnect, process a null/dead state to release any keys or buttons.

        // Data needed to calculate Stick to Mouse Wheel conversion

        // マクロの実行時状態（macroControl／MacroCount／macroTaskQueue）の実体は MacroRuntimeState が持つ。
        // 旧 macrodone／macroDispatchInFlight（control 単位の二重実行防止の暫定対策。呼び出しが常に control == None のため、本番では到達しなかった）と、
        // その添字の表（ds4ControlMapping／DS4ControltoInt）は削除した。二重実行の防止は ActionInstanceState.IsMacroRunning が担う。

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

        // 抑制の集合（旧 suppressedTriggerButtons。Issue8-1(3)是正: トリガー成立時に抑制対象となった物理ボタンのうち、まだ「離される」ことを
        // 観測していないものの集合）の所有を、エンジン（ITriggerDispatchEngine）に一本化した。ここに含まれる間は、当該ボタン自身の通常出力を抑制し続ける
        // （トリガー全体の成立/解除とは独立に、ボタン単位で管理する）。
        // 新旧の経路が別々の集合を持つと、通常の割り当ての抑制が食い違うため。
        private readonly DS4Windows.Processing.Actions.ITriggerDispatchEngine _engine;
        private readonly DS4Windows.Processing.Abstractions.IDeviceInputCleanup _inputCleanup;   // 切断時の合成キーの解放・マクロのキュー・ボタンのコントローラーの後始末
        private readonly DS4Windows.Processing.Abstractions.IMouseConversionStage _mouseConversion;   // 入力をマウスの動きへ変える段（移動量の計算、フリックスティック、整数化、ジャイロのマウススティック）
        private readonly DS4Windows.Processing.Abstractions.ICurveAndDeadzoneStage _curveStage;   // 入力の補正の段（回転・ドリフト・デッドゾーン・出力カーブ・ジャイロ）
        private readonly DS4Windows.Processing.Abstractions.ITwoStageTriggerProcessor _twoStageTrigger;   // 2 段階トリガーの状態機械
        private readonly DS4Windows.Processing.Abstractions.ISyntheticInputCommitter _syntheticCommitter;   // 合成入力の確定
        private readonly DS4Windows.Processing.Abstractions.IControlActivityReader _controlActivityReader;   // コントロールが押されているかの判定

        // デバイスごとのトリガー判定の入力（毎フレーム MapCustom の先頭で更新する）と、フレームの出力への書き込み口。
        // 遅延の基準時刻は、_triggerInputs[device].Delay に置き換わった。
        private readonly DS4Windows.Processing.Actions.TriggerInput[] _triggerInputs;
        private readonly DS4Windows.Processing.Actions.FrameOutput[] _frameOutputs;

        private readonly MapFrame[] _mapFrames = new MapFrame[ControllerLimits.MaxControllerCount];   // デバイスごとの 1 フレームぶんの作業の表（使い回し）
        private readonly ControlFrameProcessor _controlFrame;   // スティック・トリガー・各ボタンの割り当ての処理
        private readonly ButtonMouseSampler _mouseSampler;   // 新経路のマウスの移動が借りる、ボタンからのマウス移動量の計算
        private readonly SpecialActionFrameRunner _specialActions;   // 1 フレームぶんのスペシャルアクションの評価
        // 旧 _sampleControl（MapCustom が毎フレーム受け取る ControlService の保持）は削除した。必要なものはコンストラクターで受ける（逆依存の解消）

        // IsToggledOn lifecycle is managed by Action implementations (e.g., KeyAction).
        // Mapping must not mutate per-action IsToggledOn state.

        //.Delay に置き換え済み）

        // Mouse
        // 旧 mcounter／mouseaccel／prevmouseaccel（マウス加速の状態）は ButtonMouseMovementCalculator へ、
        public const int MOUSESPEEDFACTOR = 48;
        //private const double MOUSESTICKMINVELOCITY = 40.0;

        // 合成入力の確定（約 170 行）は SyntheticInputCommitter へ移した（薄い委譲。呼び出し側は変えない）
        public void Commit(int device, DS4Windows.Core.Abstractions.IProfileSettingsService settings)
        {
            _syntheticCommitter.Commit(device, settings);
        }

        static double TValue(double value1, double value2, double percent)
        {
            percent /= 100f;
            return value1 * percent + value2 * (1 - percent);
        }

        /// <summary>
        /// Map DS4 Buttons/Axes to other DS4 Buttons/Axes (largely the same as Xinput ones) and to keyboard and mouse buttons.
        /// </summary>

        // プロファイル切替のスペシャルアクション（「Unload on trigger release」など）の復帰の合図を、
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

        // 切断（とプロファイルの適用）のときの後始末。合成キーの解放・マクロのキュー・ボタンのコントローラーは IDeviceInputCleanup が、
        // デバイスごとの実行時の状態（deviceRuntime）の作り直しは、この型が持つ状態のため、ここで行う
        public void HandleDeviceDisconnect(int device)
        {
            try
            {
                _inputCleanup.Clear(device);

                try { if (deviceRuntime != null && device >= 0 && device < deviceRuntime.Length) deviceRuntime[device] = new DeviceRuntimeState(); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }

        // 2 段階トリガーの状態機械（約 195 行）は TwoStageTriggerProcessor へ移した

        //private static OneEuroFilter flickFilter = new OneEuroFilter(0.4, 0.4);
        //private static double flickProgress = 0.0;
        //private static double flickSize = 0.0;
        //private static double flickAngleRemainder = 0.0;

        //private const double REAL_WORLD_CALIBRATION = 10;

        //private static double FlickThreshold = 0.9;
        //private static double FlickTime = 0.1;

        // InitializeActionDoneList removed — ActionManager owns per-action state initialization.

        // SA の種別ごとの旧枝（トリガーの判定、Key／Macro／Button／Program／Profile／DisconnectBT／BatteryCheck／ステアリング較正／
        // GyroCalibrate／MultiAction の実行、押し続けの解除、uTrigger の復帰）を削除した。移行済みの束縛をエンジンが処理する。束縛を持たない SA は、何もしない。
        // 以前は async void だったが、初期化待ちは完了済みの Task で、実際の await はタップ判定の遅延だけだったため、同期の void にした。
        // デバイスごとのアクション名の写し。そのデバイスの入力のスレッドだけが読み書きする

        /// <summary>
        /// MultiAction の新経路（汎用の出力 <c>MacroStringAction</c>）用。マクロを文字列で直接指定して実行する。
        /// <c>IMapping</c> のメンバーではない（<c>DefaultMacroStringPlayer</c> が委譲する）。
        /// </summary>
        public void PlayMacroString(int device, string macro)
        {
            if (device < 0 || device >= ControllerLimits.MaxControllerCount || string.IsNullOrEmpty(macro)) return;
            _macroPlayback.Play(device, macro, null, null, DS4KeyType.None);
        }

        // 旧 PlayMacro／PlayMacroTask／RunMacroOnce（マクロの再生の予約・繰り返し・後始末）は MacroPlayback へ、
        // 旧コメント・変更履歴は、移し先のファイルに残してある。

        // 旧 getMouseMapping（約 318 行）は ButtonMouseMovementCalculator（DS4Control/Services）へ、
        // 計算の変更履歴は git log を参照。

        public static double RemainderCutoff(double dividend, double divisor)
        {
            return dividend - (divisor * (int)(dividend / divisor));
        }

        // 旧 compare(byte, byte)（呼び出し元 0 件。上流 DS4Windows 由来の未使用コード）は削除した。

        public static bool GetBoolButtonMapping(bool stateButton)
        {
            return stateButton;
        }

        public static bool GetBoolAxisDirMapping(byte stateAxis, bool positive)
        {
            return positive ? stateAxis > 128 + 55 : stateAxis < 128 - 55;
        }

        public static bool GetBoolTriggerMapping(byte stateAxis)
        {
            return stateAxis > 100;
        }

        public static bool GetBoolTouchMapping(bool touchButton)
        {
            return touchButton;
        }

        // Phase9 Steering 削除 W2（2026-10-22。docs-forDIMG/MadeByAgent/Archive/Phase9-SteeringWheel-Removal-Plan.md）: 旧「SA steering wheel emulation mapping」（Scale360degreeGyroAxis／SAWheelEmulationCalibration／CalculateControllerAngle／CalcWheelFuzz ほか。
        // ジャイロでハンドル操作をする機能）は、機能ごと削除した（ユーザーの承認）。
    }
}