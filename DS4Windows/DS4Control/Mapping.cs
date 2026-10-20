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
        private readonly OneEuroFilter[] wheelFilters;
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
            DS4Windows.Actions.ITriggerDispatchEngine triggerEngine)
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

            _triggerInputs = new DS4Windows.Actions.TriggerInput[ControllerLimits.MaxControllerCount];
            _frameOutputs = new DS4Windows.Actions.FrameOutput[ControllerLimits.MaxControllerCount];
            for (int device = 0; device < _triggerInputs.Length; device++)
            {
                _triggerInputs[device] = new DS4Windows.Actions.TriggerInput(device, profileSettings);
                _frameOutputs[device] = new DS4Windows.Actions.FrameOutput();

                // Phase8-Step7 7.8f-3: 新経路のマウスの移動が、生の量の計算（getMouseMapping）を借りる。デバイスごとに 1 回だけ作る（フレームごとの割り当てなし）
                int sampleDevice = device;
                _triggerInputs[device].MouseSampler = (control, direction) => SampleMouseMovement(sampleDevice, control, direction);
            }

            syncStateLock = syntheticInput.SyncStateLock;
            deviceState = syntheticInput.DeviceState;
            macroControl = macroRuntime.MacroControl;
            macroTaskQueue = macroRuntime.TaskQueue;
            gyroStickX = stickMotion.GyroStickX;
            gyroStickY = stickMotion.GyroStickY;
            mapStickActionData = stickMotion.MapStickActionData;
            wheelFilters = stickMotion.WheelFilters;
            flickMappingData = stickMotion.FlickMappingData;
            l2TwoStageMappingData = stickMotion.L2TwoStageMappingData;
            r2TwoStageMappingData = stickMotion.R2TwoStageMappingData;
            deltaAccelProcessors = stickMotion.DeltaAccelProcessors;
        }

        // Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: keyButtonControllers／
        // GetOrCreateKeyButtonController／ClearKeyButtonControllersForDevice）は KeyButtonControllerRegistry へ移した。


        struct DS4Vector2
        {
            public double x;
            public double y;

            public DS4Vector2(double x, double y)
            {
                this.x = x;
                this.y = y;
            }
        }

        class DS4SquareStick
        {
            public DS4Vector2 current;
            public DS4Vector2 squared;

            public DS4SquareStick()
            {
                current = new DS4Vector2(0.0, 0.0);
                squared = new DS4Vector2(0.0, 0.0);
            }

            // Modification of squared stick routine documented
            // at http://theinstructionlimit.com/squaring-the-thumbsticks
            public void CircleToSquare(double roundness)
            {
                const double PiOverFour = Math.PI / 4.0;

                // Determine the theta angle
                double angle = Math.Atan2(current.y, -current.x);
                angle += Math.PI;
                double cosAng = Math.Cos(angle);
                // Scale according to which wall we're clamping to
                // X+ wall
                if (angle <= PiOverFour || angle > 7.0 * PiOverFour)
                {
                    double tempVal = 1.0 / cosAng;
                    //Console.WriteLine("1 ANG: {0} | TEMP: {1}", angle, tempVal);
                    squared.x = current.x * tempVal;
                    squared.y = current.y * tempVal;
                }
                // Y+ wall
                else if (angle > PiOverFour && angle <= 3.0 * PiOverFour)
                {
                    double tempVal = 1.0 / Math.Sin(angle);
                    //Console.WriteLine("2 ANG: {0} | TEMP: {1}", angle, tempVal);
                    squared.x = current.x * tempVal;
                    squared.y = current.y * tempVal;
                }
                // X- wall
                else if (angle > 3.0 * PiOverFour && angle <= 5.0 * PiOverFour)
                {
                    double tempVal = -1.0 / cosAng;
                    //Console.WriteLine("3 ANG: {0} | TEMP: {1}", angle, tempVal);
                    squared.x = current.x * tempVal;
                    squared.y = current.y * tempVal;
                }
                // Y- wall
                else if (angle > 5.0 * PiOverFour && angle <= 7.0 * PiOverFour)
                {
                    double tempVal = -1.0 / Math.Sin(angle);
                    //Console.WriteLine("4 ANG: {0} | TEMP: {1}", angle, tempVal);
                    squared.x = current.x * tempVal;
                    squared.y = current.y * tempVal;
                }
                else return;

                //double lengthOld = Math.Sqrt((x * x) + (y * y));
                double length = current.x / cosAng;
                //Console.WriteLine("LENGTH TEST ({0}) ({1}) {2}", lengthOld, length, (lengthOld == length).ToString());
                double factor = Math.Pow(length, roundness);
                //double ogX = current.x, ogY = current.y;
                current.x += (squared.x - current.x) * factor;
                current.y += (squared.y - current.y) * factor;
                //Console.WriteLine("INPUT: {0} {1} | {2} {3} | {4} {5} | {6} {7}",
                //    ogX, ogY, current.x, current.y, squared.x, squared.y, length, factor);
            }
        }

        public struct DS4TimedStickAxisValue
        {
            public long timestamp;
            public double x;
            public double y;
            public DS4TimedStickAxisValue(byte x, byte y, long timestamp)
            {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
            }
        }

        public Queue<DS4TimedStickAxisValue>[][] stickValueHistory = new Queue<DS4TimedStickAxisValue>[ControllerLimits.TestProfileItemCount][]
        {
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() },
            new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() }
        };

        private DS4SquareStick[] outSqrStk = new DS4SquareStick[ControllerLimits.TestProfileItemCount]
        {
            new DS4SquareStick(), new DS4SquareStick(), new DS4SquareStick(), new DS4SquareStick(),
            new DS4SquareStick(), new DS4SquareStick(), new DS4SquareStick(), new DS4SquareStick(),
            new DS4SquareStick(),
        };

        // Phase7-Step7 W4（Q2＝A・Q3＝A）: 外から触られるスティック・ジャイロ・トリガーの処理状態（gyroStickX／gyroStickY／mapStickActionData／
        // flickMappingData／l2TwoStageMappingData／r2TwoStageMappingData／wheelFilters／deltaAccelProcessors）の実体は StickMotionState が持つ。
        // Phase7-Step8 W2: 転送プロパティは削除した。上のフィールド宣言（保持クラスの配列の readonly の複製）が、その代わりである。
        //public static byte[] touchStickX = new byte[Global.MAX_DS4_CONTROLLER_COUNT] { 128, 128, 128, 128, 128, 128, 128, 128 };
        //public static byte[] touchStickY = new byte[Global.MAX_DS4_CONTROLLER_COUNT] { 128, 128, 128, 128, 128, 128, 128, 128 };

        // [<Device>][<AxisId>]. LX = 0, LY = 1, RX = 2, RY = 3
        public byte[][] lastStickAxisValues = new byte[ControllerLimits.TestProfileItemCount][]
        {
            new byte[4] {128, 128, 128, 128}, new byte[4] {128, 128, 128, 128},
            new byte[4] {128, 128, 128, 128}, new byte[4] {128, 128, 128, 128},
            new byte[4] {128, 128, 128, 128}, new byte[4] {128, 128, 128, 128},
            new byte[4] {128, 128, 128, 128}, new byte[4] {128, 128, 128, 128},
            new byte[4] {128, 128, 128, 128}
        };

        private class LastWheelGyroCoord
        {
            public int gyroX;
            public int gyroZ;
        }

        private LastWheelGyroCoord[] lastWheelGyroValues = new LastWheelGyroCoord[ControllerLimits.MaxControllerCount]
        {
            new LastWheelGyroCoord(), new LastWheelGyroCoord(), new LastWheelGyroCoord(), new LastWheelGyroCoord(),
            new LastWheelGyroCoord(), new LastWheelGyroCoord(), new LastWheelGyroCoord(), new LastWheelGyroCoord()
        };
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

            return getMouseMapping(device, control, state, _sampleExposed[device], fields, direction, ctrl);
        }


        // IsToggledOn lifecycle is managed by Action implementations (e.g., KeyAction).
        // Mapping must not mutate per-action IsToggledOn state.

        // Phase8-Step8 8b: 旧 oldnowKeyAct（タップ判定の特殊アクションの基準時刻）は、旧枝と一緒に削除した。
        // （Phase8-Step4 W4: 旧 nowAction／oldnowAction は _triggerInputs[device].Delay に置き換え済み）

        // Mouse
        public int mcounter = 34;
        public int mouseaccel = 0;
        public int prevmouseaccel = 0;
        private double horizontalRemainder = 0.0, verticalRemainder = 0.0;
        public const int MOUSESPEEDFACTOR = 48;
        private const double MOUSESTICKANTIOFFSET = 0.0128;
        private const double MOUSESTICKMINVELOCITY = 67.5;
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
                CalcAntiSnapbackStick(device, 0, lsAntiSnapback.delta, lsAntiSnapback.timeout, cState.LX, cState.LY, out cState.LX, out cState.LY);
            }

            if (rsAntiSnapback.enabled)
            {
                CalcAntiSnapbackStick(device, 1, rsAntiSnapback.delta, rsAntiSnapback.timeout, cState.RX, cState.RY, out cState.RX, out cState.RY);
            }

            StickDeadZoneInfo lsMod = settings.LSModInfo[device];
            StickDeadZoneInfo rsMod = settings.RSModInfo[device];

            if (lsMod.fuzz > 0)
            {
                CalcStickAxisFuzz(device, 0, lsMod.fuzz, cState.LX, cState.LY, out cState.LX, out cState.LY);
            }

            if (rsMod.fuzz > 0)
            {
                CalcStickAxisFuzz(device, 1, rsMod.fuzz, cState.RX, cState.RY, out cState.RX, out cState.RY);
            }

            cState.CopyTo(dState);
            //DS4State dState = new DS4State(cState);

            cState = ApplyStickCalibration(device, dState, settings);


            if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                int lsDeadzone = lsMod.deadZone;
                int lsAntiDead = lsMod.antiDeadZone;
                int lsMaxZone = lsMod.maxZone;
                double lsMaxOutput = lsMod.maxOutput;
                double lsVerticalScale = lsMod.verticalScale;
                bool interpret = lsAntiDead > 0 || lsMaxZone != 100 || lsMaxOutput != 100.0 || lsMod.maxOutputForce || lsVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;

                if (lsDeadzone > 0 || interpret)
                {
                    double lsSquared = Math.Pow(cState.LX - 128f, 2) + Math.Pow(cState.LY - 128f, 2);
                    double lsDeadzoneSquared = Math.Pow(lsDeadzone, 2);
                    if (lsDeadzone > 0 && lsSquared <= lsDeadzoneSquared)
                    {
                        dState.LX = 128;
                        dState.LY = 128;
                    }
                    else if ((lsDeadzone > 0 && lsSquared > lsDeadzoneSquared) || interpret)
                    {
                        double r = Math.Atan2(-(dState.LY - 128.0), (dState.LX - 128.0));
                        double maxXValue = dState.LX >= 128.0 ? 127.0 : -128;
                        double maxYValue = dState.LY >= 128.0 ? 127.0 : -128;
                        double ratio = lsMaxZone / 100.0;
                        double maxOutRatio = lsMaxOutput / 100.0;
                        double verticalScale = lsVerticalScale / 100.0;

                        double maxZoneXNegValue = (ratio * -128) + 128;
                        double maxZoneXPosValue = (ratio * 127) + 128;
                        double maxZoneYNegValue = maxZoneXNegValue;
                        double maxZoneYPosValue = maxZoneXPosValue;
                        double maxZoneX = dState.LX >= 128.0 ? (maxZoneXPosValue - 128.0) : (maxZoneXNegValue - 128.0);
                        double maxZoneY = dState.LY >= 128.0 ? (maxZoneYPosValue - 128.0) : (maxZoneYNegValue - 128.0);

                        double tempLsXDead = 0.0, tempLsYDead = 0.0;
                        double tempOutputX = 0.0, tempOutputY = 0.0;
                        if (lsDeadzone > 0)
                        {
                            tempLsXDead = Math.Abs(Math.Cos(r)) * (lsDeadzone / 127.0) * maxXValue;
                            tempLsYDead = Math.Abs(Math.Sin(r)) * (lsDeadzone / 127.0) * maxYValue;

                            if (lsSquared > lsDeadzoneSquared)
                            {
                                double currentX = NumericUtil.Clamp(maxZoneXNegValue, dState.LX, maxZoneXPosValue);
                                double currentY = NumericUtil.Clamp(maxZoneYNegValue, dState.LY, maxZoneYPosValue);
                                tempOutputX = ((currentX - 128.0 - tempLsXDead) / (maxZoneX - tempLsXDead));
                                tempOutputY = ((currentY - 128.0 - tempLsYDead) / (maxZoneY - tempLsYDead));
                            }
                        }
                        else
                        {
                            double currentX = NumericUtil.Clamp(maxZoneXNegValue, dState.LX, maxZoneXPosValue);
                            double currentY = NumericUtil.Clamp(maxZoneYNegValue, dState.LY, maxZoneYPosValue);
                            tempOutputX = (currentX - 128.0) / maxZoneX;
                            tempOutputY = (currentY - 128.0) / maxZoneY;
                        }

                        if (lsVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE)
                        {
                            tempOutputY = Math.Min(Math.Max(tempOutputY * verticalScale, 0.0), 1.0);
                        }

                        if (lsMaxOutput != 100.0 || lsMod.maxOutputForce)
                        {
                            double maxOutXRatio = Math.Abs(Math.Cos(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutXRatio = Math.Min(maxOutXRatio / 0.99, 1.0);

                            double maxOutYRatio = Math.Abs(Math.Sin(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutYRatio = Math.Min(maxOutYRatio / 0.99, 1.0);

                            tempOutputX = Math.Min(Math.Max(tempOutputX, 0.0), maxOutXRatio);
                            tempOutputY = Math.Min(Math.Max(tempOutputY, 0.0), maxOutYRatio);
                        }

                        double tempLsXAntiDeadPercent = 0.0, tempLsYAntiDeadPercent = 0.0;
                        if (lsAntiDead > 0)
                        {
                            tempLsXAntiDeadPercent = (lsAntiDead * 0.01) * Math.Abs(Math.Cos(r));
                            tempLsYAntiDeadPercent = (lsAntiDead * 0.01) * Math.Abs(Math.Sin(r));
                        }

                        if (tempOutputX > 0.0)
                        {
                            dState.LX = (byte)((((1.0 - tempLsXAntiDeadPercent) * tempOutputX + tempLsXAntiDeadPercent)) * maxXValue + 128.0);
                        }
                        else
                        {
                            dState.LX = 128;
                        }

                        if (tempOutputY > 0.0)
                        {
                            dState.LY = (byte)((((1.0 - tempLsYAntiDeadPercent) * tempOutputY + tempLsYAntiDeadPercent)) * maxYValue + 128.0);
                        }
                        else
                        {
                            dState.LY = 128;
                        }
                    }
                }

                // Process LS Outer Binding
                dState.OutputLSOuter = 0;
                if (dState.LX != 128 || dState.LY != 128)
                {
                    int adjustX = dState.LX - 128;
                    int adjustY = dState.LY - 128;
                    double r = Math.Atan2(-adjustY, adjustX);
                    //double r = Math.Atan2(-(dState.RY - 128.0), (dState.RX - 128.0));
                    //double maxXValue = dState.RX >= 128.0 ? 127.0 : -128;
                    //double maxYValue = dState.RY >= 128.0 ? 127.0 : -128;
                    double hyp = Math.Sqrt((adjustX * adjustX) + (adjustY * adjustY));

                    if (hyp != 0.0)
                    {
                        int tempX = (int)(Math.Abs(Math.Cos(r)) * (dState.LX >= 128 ? 127 : 128));
                        int tempY = (int)(Math.Abs(Math.Sin(r)) * (dState.LY >= 128 ? 127 : 128));
                        double maxValue = Math.Sqrt((tempX * tempX) + (tempY * tempY));
                        double ratio = hyp / maxValue;
                        if (ratio > 1.0) ratio = 1.0;
                        double currentValue = ratio * 255.0;
                        double deadValue = lsMod.outerBindDeadZone * 0.01 * 255.0;
                        if (!lsMod.outerBindInvert && currentValue > deadValue)
                        {
                            double outputRatio = (currentValue - deadValue) / (double)(255.0 - deadValue);
                            dState.OutputLSOuter = (byte)(outputRatio * 255);
                        }
                        else if (lsMod.outerBindInvert && currentValue < deadValue)
                        {
                            double outputRatio = (deadValue - currentValue) / (double)deadValue;
                            dState.OutputLSOuter = (byte)(outputRatio * 255);
                        }
                    }
                }
            }
            else if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                ref StickDeadZoneInfo.AxisDeadZoneInfo xAxisDeadInfo = ref lsMod.xAxisDeadInfo;
                if (xAxisDeadInfo.deadZone > 0 || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(cState.LX - 128);
                    if (xAxisDeadInfo.deadZone > 0 && distVal <= xAxisDeadInfo.deadZone)
                    {
                        dState.LX = 128;
                    }
                    else if ((xAxisDeadInfo.deadZone > 0 && distVal > xAxisDeadInfo.deadZone) || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = dState.LX >= 128.0 ? 127.0 : -128.0;
                        double ratio = xAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = xAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = dState.LX >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (xAxisDeadInfo.deadZone > 0) ? ((xAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, dState.LX, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (xAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (xAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = xAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            dState.LX = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            dState.LX = 128;
                        }
                    }
                }

                ref StickDeadZoneInfo.AxisDeadZoneInfo yAxisDeadInfo = ref lsMod.yAxisDeadInfo;
                if (yAxisDeadInfo.deadZone > 0 || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(cState.LY - 128);
                    if (yAxisDeadInfo.deadZone > 0 && distVal <= yAxisDeadInfo.deadZone)
                    {
                        dState.LY = 128;
                    }
                    else if ((yAxisDeadInfo.deadZone > 0 && distVal > yAxisDeadInfo.deadZone) || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = dState.LY >= 128.0 ? 127.0 : -128.0;
                        double ratio = yAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = yAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = dState.LY >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (yAxisDeadInfo.deadZone > 0) ? ((yAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, dState.LY, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (yAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (yAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = yAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            dState.LY = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            dState.LY = 128;
                        }
                    }
                }
            }


            if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                int rsDeadzone = rsMod.deadZone;
                int rsAntiDead = rsMod.antiDeadZone;
                int rsMaxZone = rsMod.maxZone;
                double rsMaxOutput = rsMod.maxOutput;
                double rsVerticalScale = rsMod.verticalScale;
                bool interpret = rsAntiDead > 0 || rsMaxZone != 100 || rsMaxOutput != 100.0 || rsMod.maxOutputForce || rsVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;

                if (rsDeadzone > 0 || interpret)
                {
                    double rsSquared = Math.Pow(cState.RX - 128.0, 2) + Math.Pow(cState.RY - 128.0, 2);
                    double rsDeadzoneSquared = Math.Pow(rsDeadzone, 2);
                    if (rsDeadzone > 0 && rsSquared <= rsDeadzoneSquared)
                    {
                        dState.RX = 128;
                        dState.RY = 128;
                    }
                    else if ((rsDeadzone > 0 && rsSquared > rsDeadzoneSquared) || interpret)
                    {
                        double r = Math.Atan2(-(dState.RY - 128.0), (dState.RX - 128.0));
                        double maxXValue = dState.RX >= 128.0 ? 127 : -128;
                        double maxYValue = dState.RY >= 128.0 ? 127 : -128;
                        double ratio = rsMaxZone / 100.0;
                        double maxOutRatio = rsMaxOutput / 100.0;
                        double verticalScale = rsVerticalScale / 100.0;

                        double maxZoneXNegValue = (ratio * -128.0) + 128.0;
                        double maxZoneXPosValue = (ratio * 127.0) + 128.0;
                        double maxZoneYNegValue = maxZoneXNegValue;
                        double maxZoneYPosValue = maxZoneXPosValue;
                        double maxZoneX = dState.RX >= 128.0 ? (maxZoneXPosValue - 128.0) : (maxZoneXNegValue - 128.0);
                        double maxZoneY = dState.RY >= 128.0 ? (maxZoneYPosValue - 128.0) : (maxZoneYNegValue - 128.0);

                        double tempRsXDead = 0.0, tempRsYDead = 0.0;
                        double tempOutputX = 0.0, tempOutputY = 0.0;
                        if (rsDeadzone > 0)
                        {
                            tempRsXDead = Math.Abs(Math.Cos(r)) * (rsDeadzone / 127.0) * maxXValue;
                            tempRsYDead = Math.Abs(Math.Sin(r)) * (rsDeadzone / 127.0) * maxYValue;

                            if (rsSquared > rsDeadzoneSquared)
                            {
                                double currentX = NumericUtil.Clamp(maxZoneXNegValue, dState.RX, maxZoneXPosValue);
                                double currentY = NumericUtil.Clamp(maxZoneYNegValue, dState.RY, maxZoneYPosValue);

                                tempOutputX = ((currentX - 128.0 - tempRsXDead) / (maxZoneX - tempRsXDead));
                                tempOutputY = ((currentY - 128.0 - tempRsYDead) / (maxZoneY - tempRsYDead));
                            }
                        }
                        else
                        {
                            double currentX = NumericUtil.Clamp(maxZoneXNegValue, dState.RX, maxZoneXPosValue);
                            double currentY = NumericUtil.Clamp(maxZoneYNegValue, dState.RY, maxZoneYPosValue);

                            tempOutputX = (currentX - 128.0) / maxZoneX;
                            tempOutputY = (currentY - 128.0) / maxZoneY;
                        }

                        if (rsVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE)
                        {
                            tempOutputY = Math.Min(Math.Max(tempOutputY * verticalScale, 0.0), 1.0);
                        }

                        if (rsMaxOutput != 100.0 || rsMod.maxOutputForce)
                        {
                            double maxOutXRatio = Math.Abs(Math.Cos(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutXRatio = Math.Min(maxOutXRatio / 0.99, 1.0);

                            double maxOutYRatio = Math.Abs(Math.Sin(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutYRatio = Math.Min(maxOutYRatio / 0.99, 1.0);

                            tempOutputX = Math.Min(Math.Max(tempOutputX, 0.0), maxOutXRatio);
                            tempOutputY = Math.Min(Math.Max(tempOutputY, 0.0), maxOutYRatio);
                        }

                        double tempRsXAntiDeadPercent = 0.0, tempRsYAntiDeadPercent = 0.0;
                        if (rsAntiDead > 0)
                        {
                            tempRsXAntiDeadPercent = (rsAntiDead * 0.01) * Math.Abs(Math.Cos(r));
                            tempRsYAntiDeadPercent = (rsAntiDead * 0.01) * Math.Abs(Math.Sin(r));
                        }

                        if (tempOutputX > 0.0)
                        {
                            dState.RX = (byte)((((1.0 - tempRsXAntiDeadPercent) * tempOutputX + tempRsXAntiDeadPercent)) * maxXValue + 128.0);
                        }
                        else
                        {
                            dState.RX = 128;
                        }

                        if (tempOutputY > 0.0)
                        {
                            dState.RY = (byte)((((1.0 - tempRsYAntiDeadPercent) * tempOutputY + tempRsYAntiDeadPercent)) * maxYValue + 128.0);
                        }
                        else
                        {
                            dState.RY = 128;
                        }
                    }
                }

                // Process RS Outer Binding
                dState.OutputRSOuter = 0;
                if (dState.RX != 128 || dState.RY != 128)
                {
                    int adjustX = dState.RX - 128;
                    int adjustY = dState.RY - 128;
                    double r = Math.Atan2(-adjustY, adjustX);
                    //double r = Math.Atan2(-(dState.RY - 128.0), (dState.RX - 128.0));
                    //double maxXValue = dState.RX >= 128.0 ? 127.0 : -128;
                    //double maxYValue = dState.RY >= 128.0 ? 127.0 : -128;
                    double hyp = Math.Sqrt((adjustX * adjustX) + (adjustY * adjustY));

                    if (hyp != 0.0)
                    {
                        int tempX = (int)(Math.Abs(Math.Cos(r)) * (dState.RX >= 128 ? 127 : 128));
                        int tempY = (int)(Math.Abs(Math.Sin(r)) * (dState.RY >= 128 ? 127 : 128));
                        double maxValue = Math.Sqrt((tempX * tempX) + (tempY * tempY));
                        double ratio = hyp / maxValue;
                        if (ratio > 1.0) ratio = 1.0;
                        double currentValue = ratio * 255;
                        double deadValue = rsMod.outerBindDeadZone * 0.01 * 255.0;
                        if (!rsMod.outerBindInvert && currentValue > deadValue)
                        {
                            double outputRatio = (currentValue - deadValue) / (double)(255.0 - deadValue);
                            dState.OutputRSOuter = (byte)(outputRatio * 255);
                        }
                        else if (rsMod.outerBindInvert && currentValue < deadValue)
                        {
                            double outputRatio = (deadValue - currentValue) / (double)deadValue;
                            dState.OutputRSOuter = (byte)(outputRatio * 255);
                        }
                    }
                }
            }
            else if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                ref StickDeadZoneInfo.AxisDeadZoneInfo xAxisDeadInfo = ref rsMod.xAxisDeadInfo;
                if (xAxisDeadInfo.deadZone > 0 || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(cState.RX - 128);
                    if (xAxisDeadInfo.deadZone > 0 && distVal <= xAxisDeadInfo.deadZone)
                    {
                        dState.RX = 128;
                    }
                    else if ((xAxisDeadInfo.deadZone > 0 && distVal > xAxisDeadInfo.deadZone) || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = dState.RX >= 128.0 ? 127.0 : -128.0;
                        double ratio = xAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = xAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = dState.RX >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (xAxisDeadInfo.deadZone > 0) ? ((xAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, dState.RX, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (xAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (xAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = xAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            dState.RX = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            dState.RX = 128;
                        }
                    }
                }

                ref StickDeadZoneInfo.AxisDeadZoneInfo yAxisDeadInfo = ref rsMod.yAxisDeadInfo;
                if (yAxisDeadInfo.deadZone > 0 || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(cState.RY - 128);
                    if (yAxisDeadInfo.deadZone > 0 && distVal <= yAxisDeadInfo.deadZone)
                    {
                        dState.RY = 128;
                    }
                    else if ((yAxisDeadInfo.deadZone > 0 && distVal > yAxisDeadInfo.deadZone) || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = dState.RY >= 128.0 ? 127.0 : -128.0;
                        double ratio = yAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = yAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = dState.RY >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (yAxisDeadInfo.deadZone > 0) ? ((yAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, dState.RY, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (yAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (yAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = yAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            dState.RY = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            dState.RY = 128;
                        }
                    }
                }
            }

            /*byte l2Deadzone = getL2Deadzone(device);
            int l2AntiDeadzone = getL2AntiDeadzone(device);
            int l2Maxzone = getL2Maxzone(device);
            */

            TriggerDeadZoneZInfo l2ModInfo = settings.L2ModInfo[device];
            byte l2Deadzone = l2ModInfo.deadZone;
            int l2AntiDeadzone = l2ModInfo.antiDeadZone;
            int l2Maxzone = l2ModInfo.maxZone;
            double l2MaxOutput = l2ModInfo.maxOutput;
            if (l2Deadzone > 0 || l2AntiDeadzone > 0 || l2Maxzone != 100 || l2MaxOutput != 100.0)
            {
                double tempL2Output = cState.L2 / 255.0;
                double tempL2AntiDead = 0.0;
                double ratio = l2Maxzone / 100.0;
                double maxValue = 255.0 * ratio;

                if (l2Deadzone > 0)
                {
                    if (cState.L2 > l2Deadzone)
                    {
                        double current = NumericUtil.Clamp(0, dState.L2, maxValue);
                        tempL2Output = (current - l2Deadzone) / (maxValue - l2Deadzone);
                    }
                    else
                    {
                        tempL2Output = 0.0;
                    }
                }
                else
                {
                    double current = NumericUtil.Clamp(0, dState.L2, maxValue);
                    tempL2Output = current / maxValue;
                }

                if (l2MaxOutput != 100.0)
                {
                    double maxOutRatio = l2MaxOutput / 100.0;
                    tempL2Output = Math.Min(Math.Max(tempL2Output, 0.0), maxOutRatio);
                }

                if (l2AntiDeadzone > 0)
                {
                    tempL2AntiDead = l2AntiDeadzone * 0.01;
                }

                if (tempL2Output > 0.0)
                {
                    dState.L2 = (byte)(((1.0 - tempL2AntiDead) * tempL2Output + tempL2AntiDead) * 255.0);
                }
                else
                {
                    dState.L2 = 0;
                }
            }

            /*byte r2Deadzone = getR2Deadzone(device);
            int r2AntiDeadzone = getR2AntiDeadzone(device);
            int r2Maxzone = getR2Maxzone(device);
            */
            TriggerDeadZoneZInfo r2ModInfo = settings.R2ModInfo[device];
            byte r2Deadzone = r2ModInfo.deadZone;
            int r2AntiDeadzone = r2ModInfo.antiDeadZone;
            int r2Maxzone = r2ModInfo.maxZone;
            double r2MaxOutput = r2ModInfo.maxOutput;
            if (r2Deadzone > 0 || r2AntiDeadzone > 0 || r2Maxzone != 100 || r2MaxOutput != 100.0)
            {
                double tempR2Output = cState.R2 / 255.0;
                double tempR2AntiDead = 0.0;
                double ratio = r2Maxzone / 100.0;
                double maxValue = 255 * ratio;

                if (r2Deadzone > 0)
                {
                    if (cState.R2 > r2Deadzone)
                    {
                        double current = NumericUtil.Clamp(0, dState.R2, maxValue);
                        tempR2Output = (current - r2Deadzone) / (maxValue - r2Deadzone);
                    }
                    else
                    {
                        tempR2Output = 0.0;
                    }
                }
                else
                {
                    double current = NumericUtil.Clamp(0, dState.R2, maxValue);
                    tempR2Output = current / maxValue;
                }

                if (r2MaxOutput != 100.0)
                {
                    double maxOutRatio = r2MaxOutput / 100.0;
                    tempR2Output = Math.Min(Math.Max(tempR2Output, 0.0), maxOutRatio);
                }

                if (r2AntiDeadzone > 0)
                {
                    tempR2AntiDead = r2AntiDeadzone * 0.01;
                }

                if (tempR2Output > 0.0)
                {
                    dState.R2 = (byte)(((1.0 - tempR2AntiDead) * tempR2Output + tempR2AntiDead) * 255.0);
                }
                else
                {
                    dState.R2 = 0;
                }
            }

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
            if (squStk.lsMode && (dState.LX != 128 || dState.LY != 128))
            {
                double capX = dState.LX >= 128 ? 127.0 : 128.0;
                double capY = dState.LY >= 128 ? 127.0 : 128.0;
                double tempX = (dState.LX - 128.0) / capX;
                double tempY = (dState.LY - 128.0) / capY;
                DS4SquareStick sqstick = outSqrStk[device];
                sqstick.current.x = tempX; sqstick.current.y = tempY;
                sqstick.CircleToSquare(squStk.lsRoundness);
                //Console.WriteLine("Input ({0}) | Output ({1})", tempY, sqstick.current.y);
                tempX = sqstick.current.x < -1.0 ? -1.0 : sqstick.current.x > 1.0
                    ? 1.0 : sqstick.current.x;
                tempY = sqstick.current.y < -1.0 ? -1.0 : sqstick.current.y > 1.0
                    ? 1.0 : sqstick.current.y;
                dState.LX = (byte)(tempX * capX + 128.0);
                dState.LY = (byte)(tempY * capY + 128.0);
            }

            int lsOutCurveMode = settings.GetLsOutCurveMode(device);
            if (lsOutCurveMode > 0 && (dState.LX != 128 || dState.LY != 128))
            {
                double tempRatioX = 0.0, tempRatioY = 0.0;
                double capX = 0.0, capY = 0.0;
                if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
                {
                    double r = Math.Atan2(-(dState.LY - 128.0), (dState.LX - 128.0));
                    double maxOutXRatio = Math.Abs(Math.Cos(r));
                    double maxOutYRatio = Math.Abs(Math.Sin(r));
                    double sideX = dState.LX - 128; double sideY = dState.LY - 128.0;
                    capX = dState.LX >= 128 ? maxOutXRatio * 127.0 : maxOutXRatio * 128.0;
                    capY = dState.LY >= 128 ? maxOutYRatio * 127.0 : maxOutYRatio * 128.0;
                    double absSideX = Math.Abs(sideX); double absSideY = Math.Abs(sideY);
                    if (absSideX > capX) capX = absSideX;
                    if (absSideY > capY) capY = absSideY;
                    tempRatioX = capX > 0 ? (dState.LX - 128.0) / capX : 0;
                    tempRatioY = capY > 0 ? (dState.LY - 128.0) / capY : 0;
                }
                else if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
                {
                    capX = dState.LX >= 128 ? 127.0 : 128.0;
                    capY = dState.LY >= 128 ? 127.0 : 128.0;
                    tempRatioX = (dState.LX - 128.0) / capX;
                    tempRatioY = (dState.LY - 128.0) / capY;
                }

                double signX = tempRatioX >= 0.0 ? 1.0 : -1.0;
                double signY = tempRatioY >= 0.0 ? 1.0 : -1.0;

                if (lsOutCurveMode == 1)
                {
                    double absX = Math.Abs(tempRatioX);
                    double absY = Math.Abs(tempRatioY);
                    double outputX = 0.0;
                    double outputY = 0.0;

                    if (absX <= 0.4)
                    {
                        outputX = 0.8 * absX;
                    }
                    else if (absX <= 0.75)
                    {
                        outputX = absX - 0.08;
                    }
                    else if (absX > 0.75)
                    {
                        outputX = (absX * 1.32) - 0.32;
                    }

                    if (absY <= 0.4)
                    {
                        outputY = 0.8 * absY;
                    }
                    else if (absY <= 0.75)
                    {
                        outputY = absY - 0.08;
                    }
                    else if (absY > 0.75)
                    {
                        outputY = (absY * 1.32) - 0.32;
                    }

                    dState.LX = (byte)(outputX * signX * capX + 128.0);
                    dState.LY = (byte)(outputY * signY * capY + 128.0);
                }
                else if (lsOutCurveMode == 2)
                {
                    double outputX = tempRatioX * tempRatioX;
                    double outputY = tempRatioY * tempRatioY;
                    dState.LX = (byte)(outputX * signX * capX + 128.0);
                    dState.LY = (byte)(outputY * signY * capY + 128.0);
                }
                else if (lsOutCurveMode == 3)
                {
                    double outputX = tempRatioX * tempRatioX * tempRatioX;
                    double outputY = tempRatioY * tempRatioY * tempRatioY;
                    dState.LX = (byte)(outputX * capX + 128.0);
                    dState.LY = (byte)(outputY * capY + 128.0);
                }
                else if (lsOutCurveMode == 4)
                {
                    double absX = Math.Abs(tempRatioX);
                    double absY = Math.Abs(tempRatioY);
                    double outputX = absX * (absX - 2.0);
                    double outputY = absY * (absY - 2.0);
                    dState.LX = (byte)(-1.0 * outputX * signX * capX + 128.0);
                    dState.LY = (byte)(-1.0 * outputY * signY * capY + 128.0);
                }
                else if (lsOutCurveMode == 5)
                {
                    double innerX = Math.Abs(tempRatioX) - 1.0;
                    double innerY = Math.Abs(tempRatioY) - 1.0;
                    double outputX = innerX * innerX * innerX + 1.0;
                    double outputY = innerY * innerY * innerY + 1.0;
                    dState.LX = (byte)(1.0 * outputX * signX * capX + 128.0);
                    dState.LY = (byte)(1.0 * outputY * signY * capY + 128.0);
                }
                else if (lsOutCurveMode == 6)
                {
                    if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
                    {
                        // Get max values and circular distance of axes
                        double maxX = (dState.LX >= 128 ? 127 : 128);
                        double maxY = (dState.LY >= 128 ? 127 : 128);
                        byte tempOutX = (byte)(tempRatioX * maxX + 128.0);
                        byte tempOutY = (byte)(tempRatioY * maxY + 128.0);

                        // Perform curve based on byte values from vector
                        byte tempX = settings.LsOutBezierCurveObj[device].arrayBezierLUT[tempOutX];
                        byte tempY = settings.LsOutBezierCurveObj[device].arrayBezierLUT[tempOutY];

                        // Calculate new ratio
                        double tempRatioOutX = (tempX - 128.0) / maxX;
                        double tempRatioOutY = (tempY - 128.0) / maxY;

                        // Map back to stick coordinates
                        dState.LX = (byte)(tempRatioOutX * capX + 128);
                        dState.LY = (byte)(tempRatioOutY * capY + 128);
                        //Console.WriteLine("X(I){0} X(O){1} {2} {3}", tempOutX, dState.LX, tempOutY, dState.LY);
                    }
                    else if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
                    {
                        dState.LX = settings.LsOutBezierCurveObj[device].arrayBezierLUT[dState.LX];
                        dState.LY = settings.LsOutBezierCurveObj[device].arrayBezierLUT[dState.LY];
                    }
                }
            }

            if (squStk.rsMode && (dState.RX != 128 || dState.RY != 128))
            {
                double capX = dState.RX >= 128 ? 127.0 : 128.0;
                double capY = dState.RY >= 128 ? 127.0 : 128.0;
                double tempX = (dState.RX - 128.0) / capX;
                double tempY = (dState.RY - 128.0) / capY;
                DS4SquareStick sqstick = outSqrStk[device];
                sqstick.current.x = tempX; sqstick.current.y = tempY;
                sqstick.CircleToSquare(squStk.rsRoundness);
                tempX = sqstick.current.x < -1.0 ? -1.0 : sqstick.current.x > 1.0
                    ? 1.0 : sqstick.current.x;
                tempY = sqstick.current.y < -1.0 ? -1.0 : sqstick.current.y > 1.0
                    ? 1.0 : sqstick.current.y;
                //Console.WriteLine("Input ({0}) | Output ({1})", tempY, sqstick.current.y);
                dState.RX = (byte)(tempX * capX + 128.0);
                dState.RY = (byte)(tempY * capY + 128.0);
            }

            int rsOutCurveMode = settings.GetRsOutCurveMode(device);
            if (rsOutCurveMode > 0 && (dState.RX != 128 || dState.RY != 128))
            {
                double tempRatioX = 0.0, tempRatioY = 0.0;
                double capX = 0.0, capY = 0.0;
                if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
                {
                    double r = Math.Atan2(-(dState.RY - 128.0), (dState.RX - 128.0));
                    double maxOutXRatio = Math.Abs(Math.Cos(r));
                    double maxOutYRatio = Math.Abs(Math.Sin(r));
                    double sideX = dState.RX - 128; double sideY = dState.RY - 128.0;
                    capX = dState.RX >= 128 ? maxOutXRatio * 127.0 : maxOutXRatio * 128.0;
                    capY = dState.RY >= 128 ? maxOutYRatio * 127.0 : maxOutYRatio * 128.0;
                    double absSideX = Math.Abs(sideX); double absSideY = Math.Abs(sideY);
                    if (absSideX > capX) capX = absSideX;
                    if (absSideY > capY) capY = absSideY;
                    tempRatioX = capX > 0 ? (dState.RX - 128.0) / capX : 0;
                    tempRatioY = capY > 0 ? (dState.RY - 128.0) / capY : 0;
                }
                else if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
                {
                    capX = dState.RX >= 128 ? 127.0 : 128.0;
                    capY = dState.RY >= 128 ? 127.0 : 128.0;
                    tempRatioX = (dState.RX - 128.0) / capX;
                    tempRatioY = (dState.RY - 128.0) / capY;
                }

                double signX = tempRatioX >= 0.0 ? 1.0 : -1.0;
                double signY = tempRatioY >= 0.0 ? 1.0 : -1.0;

                if (rsOutCurveMode == 1)
                {
                    double absX = Math.Abs(tempRatioX);
                    double absY = Math.Abs(tempRatioY);
                    double outputX = 0.0;
                    double outputY = 0.0;

                    if (absX <= 0.4)
                    {
                        outputX = 0.8 * absX;
                    }
                    else if (absX <= 0.75)
                    {
                        outputX = absX - 0.08;
                    }
                    else if (absX > 0.75)
                    {
                        outputX = (absX * 1.32) - 0.32;
                    }

                    if (absY <= 0.4)
                    {
                        outputY = 0.8 * absY;
                    }
                    else if (absY <= 0.75)
                    {
                        outputY = absY - 0.08;
                    }
                    else if (absY > 0.75)
                    {
                        outputY = (absY * 1.32) - 0.32;
                    }

                    dState.RX = (byte)(outputX * signX * capX + 128.0);
                    dState.RY = (byte)(outputY * signY * capY + 128.0);
                }
                else if (rsOutCurveMode == 2)
                {
                    double outputX = tempRatioX * tempRatioX;
                    double outputY = tempRatioY * tempRatioY;
                    dState.RX = (byte)(outputX * signX * capX + 128.0);
                    dState.RY = (byte)(outputY * signY * capY + 128.0);
                }
                else if (rsOutCurveMode == 3)
                {
                    double outputX = tempRatioX * tempRatioX * tempRatioX;
                    double outputY = tempRatioY * tempRatioY * tempRatioY;
                    dState.RX = (byte)(outputX * capX + 128.0);
                    dState.RY = (byte)(outputY * capY + 128.0);
                }
                else if (rsOutCurveMode == 4)
                {
                    double absX = Math.Abs(tempRatioX);
                    double absY = Math.Abs(tempRatioY);
                    double outputX = absX * (absX - 2.0);
                    double outputY = absY * (absY - 2.0);
                    dState.RX = (byte)(-1.0 * outputX * signX * capX + 128.0);
                    dState.RY = (byte)(-1.0 * outputY * signY * capY + 128.0);
                }
                else if (rsOutCurveMode == 5)
                {
                    double innerX = Math.Abs(tempRatioX) - 1.0;
                    double innerY = Math.Abs(tempRatioY) - 1.0;
                    double outputX = innerX * innerX * innerX + 1.0;
                    double outputY = innerY * innerY * innerY + 1.0;
                    dState.RX = (byte)(1.0 * outputX * signX * capX + 128.0);
                    dState.RY = (byte)(1.0 * outputY * signY * capY + 128.0);
                }
                else if (rsOutCurveMode == 6)
                {
                    if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
                    {
                        // Get max values and circular distance of axes
                        double maxX = (dState.RX >= 128 ? 127 : 128);
                        double maxY = (dState.RY >= 128 ? 127 : 128);
                        byte tempOutX = (byte)(tempRatioX * maxX + 128.0);
                        byte tempOutY = (byte)(tempRatioY * maxY + 128.0);

                        // Perform curve based on byte values from vector
                        byte tempX = settings.RsOutBezierCurveObj[device].arrayBezierLUT[tempOutX];
                        byte tempY = settings.RsOutBezierCurveObj[device].arrayBezierLUT[tempOutY];

                        // Calculate new ratio
                        double tempRatioOutX = (tempX - 128.0) / maxX;
                        double tempRatioOutY = (tempY - 128.0) / maxY;

                        // Map back to stick coordinates
                        dState.RX = (byte)(tempRatioOutX * capX + 128);
                        dState.RY = (byte)(tempRatioOutY * capY + 128);
                    }
                    else if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
                    {
                        dState.RX = settings.RsOutBezierCurveObj[device].arrayBezierLUT[dState.RX];
                        dState.RY = settings.RsOutBezierCurveObj[device].arrayBezierLUT[dState.RY];
                    }
                }
            }

            int l2OutCurveMode = settings.GetL2OutCurveMode(device);
            if (l2OutCurveMode > 0 && dState.L2 != 0)
            {
                double temp = dState.L2 / 255.0;
                if (l2OutCurveMode == 1)
                {
                    double output;

                    if (temp <= 0.4)
                        output = 0.55 * temp;
                    else if (temp <= 0.75)
                        output = temp - 0.18;
                    else // if (temp > 0.75)
                        output = (temp * 1.72) - 0.72;
                    dState.L2 = (byte)(output * 255.0);
                }
                else if (l2OutCurveMode == 2)
                {
                    double output = temp * temp;
                    dState.L2 = (byte)(output * 255.0);
                }
                else if (l2OutCurveMode == 3)
                {
                    double output = temp * temp * temp;
                    dState.L2 = (byte)(output * 255.0);
                }
                else if (l2OutCurveMode == 4)
                {
                    double output = temp * (temp - 2.0);
                    dState.L2 = (byte)(-1.0 * output * 255.0);
                }
                else if (l2OutCurveMode == 5)
                {
                    double inner = Math.Abs(temp) - 1.0;
                    double output = inner * inner * inner + 1.0;
                    dState.L2 = (byte)(-1.0 * output * 255.0);
                }
                else if (l2OutCurveMode == 6)
                {
                    dState.L2 = settings.L2OutBezierCurveObj[device].arrayBezierLUT[dState.L2];
                }
            }

            int r2OutCurveMode = settings.GetR2OutCurveMode(device);
            if (r2OutCurveMode > 0 && dState.R2 != 0)
            {
                double temp = dState.R2 / 255.0;
                if (r2OutCurveMode == 1)
                {
                    double output;

                    if (temp <= 0.4)
                        output = 0.55 * temp;
                    else if (temp <= 0.75)
                        output = temp - 0.18;
                    else // if (temp > 0.75)
                        output = (temp * 1.72) - 0.72;
                    dState.R2 = (byte)(output * 255.0);
                }
                else if (r2OutCurveMode == 2)
                {
                    double output = temp * temp;
                    dState.R2 = (byte)(output * 255.0);
                }
                else if (r2OutCurveMode == 3)
                {
                    double output = temp * temp * temp;
                    dState.R2 = (byte)(output * 255.0);
                }
                else if (r2OutCurveMode == 4)
                {
                    double output = temp * (temp - 2.0);
                    dState.R2 = (byte)(-1.0 * output * 255.0);
                }
                else if (r2OutCurveMode == 5)
                {
                    double inner = Math.Abs(temp) - 1.0;
                    double output = inner * inner * inner + 1.0;
                    dState.R2 = (byte)(-1.0 * output * 255.0);
                }
                else if (r2OutCurveMode == 6)
                {
                    dState.R2 = settings.R2OutBezierCurveObj[device].arrayBezierLUT[dState.R2];
                }
            }


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
                int result = 0;

                int gyroX = cState.Motion.accelX, gyroZ = cState.Motion.accelZ;
                int absx = Math.Abs(gyroX), absz = Math.Abs(gyroZ);

                if (SXD > 0 || SXMax < 1.0 || sxAntiDead > 0)
                {
                    int maxValue = (int)(SXMax * 128d);
                    if (absx > SXD)
                    {
                        double ratioX = absx < maxValue ? (absx - SXD) / (double)(maxValue - SXD) : 1.0;
                        dState.Motion.outputAccelX = Math.Sign(gyroX) *
                            (int)Math.Min(128d, sxsens * 128d * ((1.0 - sxAntiDead) * ratioX + sxAntiDead));
                    }
                    else
                    {
                        dState.Motion.outputAccelX = 0;
                    }
                }
                else
                {
                    dState.Motion.outputAccelX = Math.Sign(gyroX) *
                        (int)Math.Min(128d, sxsens * 128d * (absx / 128d));
                }

                if (SZD > 0 || SZMax < 1.0 || szAntiDead > 0)
                {
                    int maxValue = (int)(SZMax * 128d);
                    if (absz > SZD)
                    {
                        double ratioZ = absz < maxValue ? (absz - SZD) / (double)(maxValue - SZD) : 1.0;
                        dState.Motion.outputAccelZ = Math.Sign(gyroZ) *
                            (int)Math.Min(128d, szsens * 128d * ((1.0 - szAntiDead) * ratioZ + szAntiDead));
                    }
                    else
                    {
                        dState.Motion.outputAccelZ = 0;
                    }
                }
                else
                {
                    dState.Motion.outputAccelZ = Math.Sign(gyroZ) *
                        (int)Math.Min(128d, szsens * 128d * (absz / 128d));
                }

                int sxOutCurveMode = settings.GetSxOutCurveMode(device);
                if (sxOutCurveMode > 0)
                {
                    double temp = dState.Motion.outputAccelX / 128.0;
                    double sign = Math.Sign(temp);
                    if (sxOutCurveMode == 1)
                    {
                        double output;
                        double abs = Math.Abs(temp);

                        if (abs <= 0.4)
                            output = 0.55 * abs;
                        else if (abs <= 0.75)
                            output = abs - 0.18;
                        else // if (abs > 0.75)
                            output = (abs * 1.72) - 0.72;
                        dState.Motion.outputAccelX = (int)(output * sign * 128.0);
                    }
                    else if (sxOutCurveMode == 2)
                    {
                        double output = temp * temp;
                        result = (int)(output * sign * 128.0);
                        dState.Motion.outputAccelX = result;
                    }
                    else if (sxOutCurveMode == 3)
                    {
                        double output = temp * temp * temp;
                        result = (int)(output * 128.0);
                        dState.Motion.outputAccelX = result;
                    }
                    else if (sxOutCurveMode == 4)
                    {
                        double abs = Math.Abs(temp);
                        double output = abs * (abs - 2.0);
                        dState.Motion.outputAccelX = (int)(-1.0 * output *
                            sign * 128.0);
                    }
                    else if (sxOutCurveMode == 5)
                    {
                        double inner = Math.Abs(temp) - 1.0;
                        double output = inner * inner * inner + 1.0;
                        dState.Motion.outputAccelX = (int)(output *
                            sign * 128.0);
                    }
                    else if (sxOutCurveMode == 6)
                    {
                        int signSA = Math.Sign(dState.Motion.outputAccelX);
                        dState.Motion.outputAccelX = settings.SxOutBezierCurveObj[device].arrayBezierLUT[Math.Min(Math.Abs(dState.Motion.outputAccelX), 128)] * signSA;
                    }
                }

                int szOutCurveMode = settings.GetSzOutCurveMode(device);
                if (szOutCurveMode > 0 && dState.Motion.outputAccelZ != 0)
                {
                    double temp = dState.Motion.outputAccelZ / 128.0;
                    double sign = Math.Sign(temp);
                    if (szOutCurveMode == 1)
                    {
                        double output;
                        double abs = Math.Abs(temp);

                        if (abs <= 0.4)
                            output = 0.55 * abs;
                        else if (abs <= 0.75)
                            output = abs - 0.18;
                        else // if (abs > 0.75)
                            output = (abs * 1.72) - 0.72;
                        dState.Motion.outputAccelZ = (int)(output * sign * 128.0);
                    }
                    else if (szOutCurveMode == 2)
                    {
                        double output = temp * temp;
                        result = (int)(output * sign * 128.0);
                        dState.Motion.outputAccelZ = result;
                    }
                    else if (szOutCurveMode == 3)
                    {
                        double output = temp * temp * temp;
                        result = (int)(output * 128.0);
                        dState.Motion.outputAccelZ = result;
                    }
                    else if (szOutCurveMode == 4)
                    {
                        double abs = Math.Abs(temp);
                        double output = abs * (abs - 2.0);
                        dState.Motion.outputAccelZ = (int)(-1.0 * output *
                            sign * 128.0);
                    }
                    else if (szOutCurveMode == 5)
                    {
                        double inner = Math.Abs(temp) - 1.0;
                        double output = inner * inner * inner + 1.0;
                        dState.Motion.outputAccelZ = (int)(output *
                            sign * 128.0);
                    }
                    else if (szOutCurveMode == 6)
                    {
                        int signSA = Math.Sign(dState.Motion.outputAccelZ);
                        dState.Motion.outputAccelZ = settings.SzOutBezierCurveObj[device].arrayBezierLUT[Math.Min(Math.Abs(dState.Motion.outputAccelZ), 128)] * signSA;
                    }
                }
            }

            return dState;
        }

        public DS4State ApplyStickCalibration(int device, DS4State state, DS4Windows.DI.IProfileSettingsService settings)
        {
            if (settings.RightStickDriftXAxis[device] != 0)
            {
                var translated = state.RX - settings.RightStickDriftXAxis[device];
                state.RX = (byte)Math.Clamp(translated, 0, 255);
            }
            if (settings.RightStickDriftYAxis[device] != 0)
            {
                var translated = state.RY - settings.RightStickDriftYAxis[device];
                state.RY = (byte)Math.Clamp(translated, 0, 255);
            }

            if (settings.LeftStickDriftXAxis[device] != 0)
            {
                var translated = state.LX - settings.LeftStickDriftXAxis[device];
                state.LX = (byte)Math.Clamp(translated, 0, 255);
            }

            if (settings.LeftStickDriftYAxis[device] != 0)
            {
                var translated = state.LY - settings.LeftStickDriftYAxis[device];
                state.LY = (byte)Math.Clamp(translated, 0, 255);
            }

            return state;
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

                        ProcessFlickStick(device, cRawState, cRawState.LX, cRawState.LY, pState.LX, pState.LY, ctrl,
                            stickSettings.outputSettings.flickSettings, ref tempMouseDeltaX);
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

                        ProcessFlickStick(device, cRawState, cRawState.RX, cRawState.RY, pState.RX, pState.RY, ctrl,
                            stickSettings.outputSettings.flickSettings, ref tempMouseDeltaX);
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

            if (ctrl.ProfileSettingsService.GetSASteeringWheelEmulationAxis(device) != SASteeringWheelEmulationAxisType.None)
            {
                MappedState.SASteeringWheelEmulationUnit = this.Scale360degreeGyroAxis(device, eState, ctrl);
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

            calculateFinalMouseMovement(ref tempMouseDeltaX, ref tempMouseDeltaY,
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
                    PostMapStickData mapStickData = mapStickActionData[device];
                    ref byte gyroTempX = ref gyroStickX[device];
                    if (msinfo.OutputHorizontal() && gyroTempX != 128)
                    {
                        byte outputStickXVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
                            MappedState.RX : MappedState.LX;

                        byte tempAxisVal = 128;

                        if (outputStickXVal != 128)
                        {
                            tempAxisVal = Math.Abs(gyroTempX - 128) > Math.Abs(outputStickXVal - 128) ?
                                gyroTempX : outputStickXVal;
                        }
                        else
                        {
                            tempAxisVal = gyroTempX;
                        }

                        if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick)
                        {
                            MappedState.RX = tempAxisVal;
                            mapStickData.RX = tempAxisVal;
                            mapStickData.dirty = true;
                        }
                        else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick)
                        {
                            MappedState.LX = tempAxisVal;
                            mapStickData.LX = tempAxisVal;
                            mapStickData.dirty = true;
                        }
                    }

                    ref byte gyroTempY = ref gyroStickY[device];
                    if (msinfo.OutputVertical() && gyroTempY != 128)
                    {
                        byte outputStickYVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
                        MappedState.RY : MappedState.LY;
                        byte tempAxisVal = 128;

                        if (outputStickYVal != 128)
                            tempAxisVal = Math.Abs(gyroTempY - 128) > Math.Abs(outputStickYVal - 128) ?
                                gyroTempY : outputStickYVal;
                        else
                            tempAxisVal = gyroTempY;

                        if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick)
                        {
                            MappedState.RY = tempAxisVal;
                            mapStickData.RY = tempAxisVal;
                            mapStickData.dirty = true;
                        }
                        else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick)
                        {
                            MappedState.LY = tempAxisVal;
                            mapStickData.LY = tempAxisVal;
                            mapStickData.dirty = true;
                        }
                    }

                    // Don't reset Mouse Joystick output coords here
                    //gyroTempX = gyroTempY = 128;
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

        private void ProcessFlickStick(int device, DS4State cRawState, byte stickX, byte stickY, byte prevStickX, byte prevStickY, ControlService ctrl, FlickStickSettings flickSettings, ref double tempMouseDeltaX)
        {
            FlickStickMappingData tempFlickData = flickMappingData[device];
            double angleChange = HandleFlickStickAngle(cRawState, stickX, stickY, prevStickX, prevStickY,
                tempFlickData, flickSettings);
            //angleChange = flickFilter.Filter(angleChange, cState.elapsedTime);
            //Console.WriteLine(angleChange);
            //if (angleChange != 0.0)
            double lsangle = angleChange * 180.0 / Math.PI;
            if (lsangle == 0.0)
            {
                tempFlickData.flickAngleRemainder = 0.0;
            }
            else if (lsangle >= 0.0 && tempFlickData.flickAngleRemainder >= 0.0)
            {
                lsangle += tempFlickData.flickAngleRemainder;
            }

            tempFlickData.flickAngleRemainder = 0.0;
            //Console.WriteLine(lsangle);
            //if (angleChange != 0.0)
            if (flickSettings.minAngleThreshold == 0.0 && lsangle != 0.0)
            //if (Math.Abs(lsangle) >= 0.5)
            {
                tempFlickData.flickAngleRemainder = 0.0;
                //flickAngleRemainder = lsangle - (int)lsangle;
                //lsangle = (int)lsangle;
                tempMouseDeltaX += lsangle * flickSettings.realWorldCalibration;
            }
            else if (Math.Abs(lsangle) >= flickSettings.minAngleThreshold)
            {
                tempFlickData.flickAngleRemainder = 0.0;
                //flickAngleRemainder = lsangle - (int)lsangle;
                //lsangle = (int)lsangle;
                tempMouseDeltaX += lsangle * flickSettings.realWorldCalibration;
            }
            else
            {
                tempFlickData.flickAngleRemainder = lsangle;
            }
        }

        private double HandleFlickStickAngle(DS4State cState, byte stickX, byte stickY, byte prevStickX, byte prevStickY,
            FlickStickMappingData flickData, FlickStickSettings flickSettings)
        {
            double result = 0.0;

            double lastXMax = prevStickX >= 128 ? 127.0 : -128.0;
            double lastTestX = (prevStickX - 128) / lastXMax;
            double lastYMax = prevStickY >= 128 ? 127.0 : -128.0;
            double lastTestY = (prevStickY - 128) / lastYMax;

            double currentXMax = stickX >= 128 ? 127.0 : -128.0;
            double currentTestX = (stickX - 128) / currentXMax;
            double currentYMax = stickY >= 128 ? 127.0 : -128.0;
            double currentTestY = (stickY - 128) / currentYMax;

            double lastLength = (lastTestX * lastTestX) + (lastTestY * lastTestY);
            double length = (currentTestX * currentTestX) + (currentTestY * currentTestY);
            double testLength = flickSettings.flickThreshold * flickSettings.flickThreshold;

            if (length >= testLength)
            {
                if (lastLength < testLength)
                {
                    // Start new Flick
                    flickData.flickProgress = 0.0; // Reset Flick progress
                    flickData.flickSize = Math.Atan2((stickX - 128), -(stickY - 128));
                    //flickData.flickFilter.Filter(0.0, cState.elapsedTime);
                }
                else
                {
                    // Turn camera
                    double stickAngle = Math.Atan2((stickX - 128), -(stickY - 128));
                    double lastStickAngle = Math.Atan2((prevStickX - 128), -(prevStickY - 128));
                    double angleChange = (stickAngle - lastStickAngle);
                    double rawAngleChange = angleChange;
                    angleChange = (angleChange + Math.PI) % (2 * Math.PI);
                    if (angleChange < 0)
                    {
                        angleChange += 2 * Math.PI;
                    }
                    angleChange -= Math.PI;
                    //Console.WriteLine("ANGLE CHANGE: {0} {1} {2}", stickAngle, lastStickAngle, rawAngleChange);
                    //Console.WriteLine("{0} {1} | {2} {3}", cState.RX, pState.RX, cState.RY, pState.RY);
                    //angleChange = flickData.flickFilter.Filter(angleChange, cState.elapsedTime);
                    result += angleChange;
                }
            }
            else
            {
                // Cleanup
                //flickData.flickFilter.Filter(0.0, cState.elapsedTime);
                result = 0.0;
            }

            // Continue Flick motion
            double lastFlickProgress = flickData.flickProgress;
            double flickTime = flickSettings.flickTime;
            if (lastFlickProgress < flickTime)
            {
                flickData.flickProgress = Math.Min(flickData.flickProgress + cState.elapsedTime, flickTime);

                double lastPerOne = lastFlickProgress / flickTime;
                double thisPerOne = flickData.flickProgress / flickTime;

                double warpedLastPerOne = WarpEaseOut(lastPerOne);
                double warpedThisPerone = WarpEaseOut(thisPerOne);
                //Console.WriteLine("{0} {1}", warpedThisPerone, warpedLastPerOne);

                result += (warpedThisPerone - warpedLastPerOne) * flickData.flickSize;
            }

            return result;
        }

        private static double WarpEaseOut(double input)
        {
            double flipped = 1.0 - input;
            return 1.0 - flipped * flipped;
        }

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

        private double getMouseMapping(int device, DS4Controls control, DS4State cState, DS4StateExposed eState,
            DS4StateFieldMapping fieldMapping, int mnum, ControlService ctrl)
        {
            int deadzoneL = 0;
            int deadzoneR = 0;
            if (ctrl.ProfileSettingsService.LSModInfo[device].deadZone == 0)
                deadzoneL = 3;
            if (ctrl.ProfileSettingsService.RSModInfo[device].deadZone == 0)
                deadzoneR = 3;

            double value = 0.0;
            ButtonMouseInfo buttonMouseInfo = ctrl.ProfileSettingsService.ButtonMouseInfos[device];
            DeltaSettingsProcessorGroup deltaAccelProcessorGroup = deltaAccelProcessors[device];
            int speed = buttonMouseInfo.activeButtonSensitivity;
            const double root = 1.002;
            const double divide = 10000d;

            int controlNum = (int)control;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
            //long timeElapsed = ctrl.DS4Controllers[device].getLastTimeElapsed();
            double timeElapsed = ctrl.DS4Controllers[device].lastTimeElapsedDouble;
            //double mouseOffset = 0.025;
            double tempMouseOffsetX = 0.0, tempMouseOffsetY = 0.0;
            double mouseVerticalScale = 1.0;
            bool verticalDir = false;
            // 0 = MouseUp, 1 = MouseDown
            if (mnum == 0 || mnum == 1)
            {
                verticalDir = true;
                mouseVerticalScale = buttonMouseInfo.buttonVerticalScale;
            }

            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                bool active = fieldMapping.buttons[controlNum];
                value = (active ? Math.Pow(root + speed / divide, 100) - 1 : 0);
                if (verticalDir) value *= mouseVerticalScale;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                double timeDelta = timeElapsed * 0.001;
                int mouseVelocity = speed * MOUSESPEEDFACTOR;
                double mouseOffset = buttonMouseInfo.mouseVelocityOffset * mouseVelocity;
                if (verticalDir) mouseVelocity = (int)(mouseVelocity * mouseVerticalScale);

                //double mouseOffset = MOUSESTICKANTIOFFSET * mouseVelocity;
                // Cap mouse offset to final mouse velocity
                //double mouseOffset = mouseVelocity >= MOUSESTICKMINVELOCITY ? MOUSESTICKMINVELOCITY : mouseVelocity;

                switch (control)
                {
                    case DS4Controls.LXNeg:
                        {
                            if (cState.LX < 128 - deadzoneL)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                {
                                    diff = -(cState.LX - 128 - deadzoneL) / (double)(0 - 128 - deadzoneL);
                                }
                                else
                                {
                                    diff = -deltaAccelProcessorGroup.LSProcessor.AccelOutXNorm;
                                }

                                //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                //tempMouseOffsetX = MOUSESTICKOFFSET;
                                tempMouseOffsetX = cState.LXUnit * mouseOffset;
                                value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * -1.0 * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = -(cState.LX - 127 - deadzoneL) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.LXPos:
                        {
                            if (cState.LX > 128 + deadzoneL)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                {
                                    diff = (cState.LX - 128 + deadzoneL) / (double)(255 - 128 + deadzoneL);
                                }
                                else
                                {
                                    diff = deltaAccelProcessorGroup.LSProcessor.AccelOutXNorm;
                                }

                                tempMouseOffsetX = cState.LXUnit * mouseOffset;
                                //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                //tempMouseOffsetX = MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = (cState.LX - 127 + deadzoneL) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.RXNeg:
                        {
                            if (cState.RX < 128 - deadzoneR)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                {
                                    diff = -(cState.RX - 128 - deadzoneR) / (double)(0 - 128 - deadzoneR);
                                }
                                else
                                {
                                    diff = -deltaAccelProcessorGroup.RSProcessor.AccelOutXNorm;
                                }

                                tempMouseOffsetX = cState.RXUnit * mouseOffset;
                                //tempMouseOffsetX = MOUSESTICKOFFSET;
                                //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * -1.0 * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = -(cState.RX - 127 - deadzoneR) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.RXPos:
                        {
                            if (cState.RX > 128 + deadzoneR)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                {
                                    diff = (cState.RX - 128 + deadzoneR) / (double)(255 - 128 + deadzoneR);
                                }
                                else
                                {
                                    diff = deltaAccelProcessorGroup.RSProcessor.AccelOutXNorm;
                                }

                                tempMouseOffsetX = cState.RXUnit * mouseOffset;
                                //tempMouseOffsetX = MOUSESTICKOFFSET;
                                //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = (cState.RX - 127 + deadzoneR) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.LYNeg:
                        {
                            if (cState.LY < 128 - deadzoneL)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                {
                                    diff = -(cState.LY - 128 - deadzoneL) / (double)(0 - 128 - deadzoneL);
                                }
                                else
                                {
                                    diff = -deltaAccelProcessorGroup.LSProcessor.AccelOutYNorm;
                                }

                                tempMouseOffsetY = cState.LYUnit * mouseOffset;
                                //tempMouseOffsetY = MOUSESTICKOFFSET;
                                //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * -1.0 * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = -(cState.LY - 127 - deadzoneL) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.LYPos:
                        {
                            if (cState.LY > 128 + deadzoneL)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                {
                                    diff = (cState.LY - 128 + deadzoneL) / (double)(255 - 128 + deadzoneL);
                                }
                                else
                                {
                                    diff = deltaAccelProcessorGroup.LSProcessor.AccelOutYNorm;
                                }

                                tempMouseOffsetY = cState.LYUnit * mouseOffset;
                                //tempMouseOffsetY = MOUSESTICKOFFSET;
                                //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = (cState.LY - 127 + deadzoneL) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.RYNeg:
                        {
                            if (cState.RY < 128 - deadzoneR)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                {
                                    diff = -(cState.RY - 128 - deadzoneR) / (double)(0 - 128 - deadzoneR);
                                }
                                else
                                {
                                    diff = -deltaAccelProcessorGroup.RSProcessor.AccelOutYNorm;
                                }

                                tempMouseOffsetY = cState.RYUnit * mouseOffset;
                                //tempMouseOffsetY = MOUSESTICKOFFSET;
                                //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * -1.0 * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = -(cState.RY - 127 - deadzoneR) / 2550d * speed;
                            }

                            break;
                        }
                    case DS4Controls.RYPos:
                        {
                            if (cState.RY > 128 + deadzoneR)
                            {
                                double diff;
                                if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                {
                                    diff = (cState.RY - 128 + deadzoneR) / (double)(255 - 128 + deadzoneR);
                                }
                                else
                                {
                                    diff = deltaAccelProcessorGroup.RSProcessor.AccelOutYNorm;
                                }

                                tempMouseOffsetY = cState.RYUnit * mouseOffset;
                                //tempMouseOffsetY = MOUSESTICKOFFSET;
                                //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * timeDelta);
                                //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                //value = (cState.RY - 127 + deadzoneR) / 2550d * speed;
                            }

                            break;
                        }

                    default: break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                byte trigger = fieldMapping.triggers[controlNum];
                value = Math.Pow(root + speed / divide, trigger / 2d) - 1;
                if (verticalDir) value *= mouseVerticalScale;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.GyroDir)
            {
                //double SXD = getSXDeadzone(device);
                //double SZD = getSZDeadzone(device);

                switch (control)
                {
                    case DS4Controls.GyroXPos:
                        {
                            int gyroX = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyroX > 0 ? Math.Pow(root + speed / divide, gyroX) : 0);
                            if (verticalDir) value *= mouseVerticalScale;
                            break;
                        }
                    case DS4Controls.GyroXNeg:
                        {
                            int gyroX = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyroX < 0 ? Math.Pow(root + speed / divide, -gyroX) : 0);
                            if (verticalDir) value *= mouseVerticalScale;
                            break;
                        }
                    case DS4Controls.GyroZPos:
                        {
                            int gyroZ = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyroZ > 0 ? Math.Pow(root + speed / divide, gyroZ) : 0);
                            if (verticalDir) value *= mouseVerticalScale;
                            break;
                        }
                    case DS4Controls.GyroZNeg:
                        {
                            int gyroZ = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyroZ < 0 ? Math.Pow(root + speed / divide, -gyroZ) : 0);
                            if (verticalDir) value *= mouseVerticalScale;
                            break;
                        }
                    default: break;
                }
            }

            if (buttonMouseInfo.mouseAccel)
            {
                if (value > 0)
                {
                    mcounter = 34;
                    mouseaccel++;
                }

                if (mouseaccel == prevmouseaccel)
                {
                    mcounter--;
                }

                if (mcounter <= 0)
                {
                    mouseaccel = 0;
                    mcounter = 34;
                }

                value *= 1 + Math.Min(20000, (mouseaccel)) / 10000d;
                prevmouseaccel = mouseaccel;
            }

            return value;
        }

        private void calculateFinalMouseMovement(ref double rawMouseX, ref double rawMouseY,
            out int mouseX, out int mouseY)
        {
            if ((rawMouseX > 0.0 && horizontalRemainder > 0.0) || (rawMouseX < 0.0 && horizontalRemainder < 0.0))
            {
                rawMouseX += horizontalRemainder;
            }
            else
            {
                horizontalRemainder = 0.0;
            }

            //double mouseXTemp = rawMouseX - (Math.IEEERemainder(rawMouseX * 1000.0, 1.0) / 1000.0);
            double mouseXTemp = rawMouseX - (remainderCutoff(rawMouseX * 100.0, 1.0) / 100.0);
            //double mouseXTemp = rawMouseX - (rawMouseX * 1000.0 - (1.0 * (int)(rawMouseX * 1000.0 / 1.0)));
            mouseX = (int)mouseXTemp;
            horizontalRemainder = mouseXTemp - mouseX;
            //mouseX = (int)rawMouseX;
            //horizontalRemainder = rawMouseX - mouseX;

            if ((rawMouseY > 0.0 && verticalRemainder > 0.0) || (rawMouseY < 0.0 && verticalRemainder < 0.0))
            {
                rawMouseY += verticalRemainder;
            }
            else
            {
                verticalRemainder = 0.0;
            }

            //double mouseYTemp = rawMouseY - (Math.IEEERemainder(rawMouseY * 1000.0, 1.0) / 1000.0);
            double mouseYTemp = rawMouseY - (remainderCutoff(rawMouseY * 100.0, 1.0) / 100.0);
            mouseY = (int)mouseYTemp;
            verticalRemainder = mouseYTemp - mouseY;
            //mouseY = (int)rawMouseY;
            //verticalRemainder = rawMouseY - mouseY;
        }

        public static double remainderCutoff(double dividend, double divisor)
        {
            return dividend - (divisor * (int)(dividend / divisor));
        }

        public static bool compare(byte b1, byte b2)
        {
            bool result = true;
            if (Math.Abs(b1 - b2) > 10)
            {
                result = false;
            }

            return result;
        }

        // GetBoolMappingExternal から呼ばれる。Mapping の profileSettings（IProfileSettingsService.GyroOutputMode。同じ BackingStore の同じ配列）から読む。
        private bool IsUsingGyroForControls(int device)
        {
            return profileSettings.GyroOutputMode[device] == GyroOutMode.Controls;
        }

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
            bool result = false;

            if (control >= DS4Controls.Square && control <= DS4Controls.Cross)
            {
                switch (control)
                {
                    case DS4Controls.Cross: result = cState.Cross; break;
                    case DS4Controls.Square: result = cState.Square; break;
                    case DS4Controls.Triangle: result = cState.Triangle; break;
                    case DS4Controls.Circle: result = cState.Circle; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.L1 && control <= DS4Controls.R3)
            {
                switch (control)
                {
                    case DS4Controls.L1: result = cState.L1; break;
                    case DS4Controls.R1: result = cState.R1; break;
                    case DS4Controls.L2: result = cState.L2 > 100; break;
                    case DS4Controls.R2: result = cState.R2 > 100; break;
                    case DS4Controls.L3: result = cState.L3; break;
                    case DS4Controls.R3: result = cState.R3; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.DpadUp && control <= DS4Controls.DpadLeft)
            {
                switch (control)
                {
                    case DS4Controls.DpadUp: result = cState.DpadUp; break;
                    case DS4Controls.DpadDown: result = cState.DpadDown; break;
                    case DS4Controls.DpadLeft: result = cState.DpadLeft; break;
                    case DS4Controls.DpadRight: result = cState.DpadRight; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.LXNeg && control <= DS4Controls.RYPos)
            {
                switch (control)
                {
                    case DS4Controls.LXNeg: result = cState.LX < 128 - 55; break;
                    case DS4Controls.LYNeg: result = cState.LY < 128 - 55; break;
                    case DS4Controls.RXNeg: result = cState.RX < 128 - 55; break;
                    case DS4Controls.RYNeg: result = cState.RY < 128 - 55; break;
                    case DS4Controls.LXPos: result = cState.LX > 128 + 55; break;
                    case DS4Controls.LYPos: result = cState.LY > 128 + 55; break;
                    case DS4Controls.RXPos: result = cState.RX > 128 + 55; break;
                    case DS4Controls.RYPos: result = cState.RY > 128 + 55; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.TouchLeft && control <= DS4Controls.TouchRight)
            {
                switch (control)
                {
                    case DS4Controls.TouchLeft: result = (tp != null ? tp.leftDown : false); break;
                    case DS4Controls.TouchRight: result = (tp != null ? tp.rightDown : false); break;
                    case DS4Controls.TouchMulti: result = (tp != null ? tp.multiDown : false); break;
                    case DS4Controls.TouchUpper: result = (tp != null ? tp.upperDown : false); break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.SwipeLeft && control <= DS4Controls.SwipeDown)
            {
                switch (control)
                {
                    case DS4Controls.SwipeUp: result = (tp != null && tp.swipeUp); break;
                    case DS4Controls.SwipeDown: result = (tp != null && tp.swipeDown); break;
                    case DS4Controls.SwipeLeft: result = (tp != null && tp.swipeLeft); break;
                    case DS4Controls.SwipeRight: result = (tp != null && tp.swipeRight); break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.GyroXPos && control <= DS4Controls.GyroZNeg)
            {
                bool saControls = IsUsingGyroForControls(device);

                switch (control)
                {
                    // Phase6-Step3-6b（決定P2）: 呼び出し元が ctrl を持たないため、Mapping が保持する profileSettings から読む。Phase7-Step8 W2 で instance 化済み（Phase7c W3 でコメントを訂正）。
                    case DS4Controls.GyroXPos: result = saControls ? profileSettings.SXSens[device] * -eState.AccelX > 67 : false; break;
                    case DS4Controls.GyroXNeg: result = saControls ? profileSettings.SXSens[device] * -eState.AccelX < -67 : false; break;
                    case DS4Controls.GyroZPos: result = saControls ? profileSettings.SZSens[device] * eState.AccelZ > 67 : false; break;
                    case DS4Controls.GyroZNeg: result = saControls ? profileSettings.SZSens[device] * eState.AccelZ < -67 : false; break;
                    default: break;
                }
            }
            else
            {
                switch (control)
                {
                    case DS4Controls.PS: result = cState.PS; break;
                    case DS4Controls.Share: result = cState.Share; break;
                    case DS4Controls.Options: result = cState.Options; break;
                    default: break;
                }
            }

            return result;
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


        // SA steering wheel emulation mapping

        private const int C_WHEEL_ANGLE_PRECISION = 10; // Precision of SA angle in 1/10 of degrees

        private static readonly DS4Color calibrationColor_0 = new DS4Color { red = 0xA0, green = 0x00, blue = 0x00 };
        private static readonly DS4Color calibrationColor_1 = new DS4Color { red = 0xFF, green = 0xFF, blue = 0x00 };
        private static readonly DS4Color calibrationColor_2 = new DS4Color { red = 0x00, green = 0x50, blue = 0x50 };
        private static readonly DS4Color calibrationColor_3 = new DS4Color { red = 0x00, green = 0xC0, blue = 0x00 };

        private DateTime latestDebugMsgTime;
        private string latestDebugData;
        private void LogToGuiSACalibrationDebugMsg(string data, bool forceOutput = false)
        {
            // Print debug calibration log messages only once per 2 secs to avoid flooding the log receiver
            DateTime curTime = DateTime.Now;
            if (forceOutput || ((TimeSpan)(curTime - latestDebugMsgTime)).TotalSeconds > 2)
            {
                latestDebugMsgTime = curTime;
                if (data != latestDebugData)
                {
                    AppLogger.LogToGui(data, false);
                    latestDebugData = data;
                }
            }
        }

        // Return number of bits set in a value
        protected int CountNumOfSetBits(int bitValue)
        {
            int count = 0;
            while (bitValue != 0)
            {
                count++;
                bitValue &= (bitValue - 1);
            }
            return count;
        }

        // Calculate and return the angle of the controller as -180...0...+180 value.
        private Int32 CalculateControllerAngle(int gyroAccelX, int gyroAccelZ, DS4Device controller)
        {
            Int32 result;

            if (gyroAccelX == controller.wheelCenterPoint.X && Math.Abs(gyroAccelZ - controller.wheelCenterPoint.Y) <= 1)
            {
                // When the current gyro position is "close enough" the wheel center point then no need to go through the hassle of calculating an angle
                result = 0;
            }
            else
            {
                // Calculate two vectors based on "circle center" (ie. circle represents the 360 degree wheel turn and wheelCenterPoint and currentPosition vectors both start from circle center).
                // To improve accuracy both left and right turns use a decicated calibration "circle" because DS4 gyro and DoItYourselfWheelRig may return slightly different SA sensor values depending on the tilt direction (well, only one or two degree difference so nothing major).
                Point vectorAB;
                Point vectorCD;

                if (gyroAccelX >= controller.wheelCenterPoint.X)
                {
                    // "DS4 gyro wheel" tilted to right
                    vectorAB = new Point(controller.wheelCenterPoint.X - controller.wheelCircleCenterPointRight.X, controller.wheelCenterPoint.Y - controller.wheelCircleCenterPointRight.Y);
                    vectorCD = new Point(gyroAccelX - controller.wheelCircleCenterPointRight.X, gyroAccelZ - controller.wheelCircleCenterPointRight.Y);
                }
                else
                {
                    // "DS4 gyro wheel" tilted to left
                    vectorAB = new Point(controller.wheelCenterPoint.X - controller.wheelCircleCenterPointLeft.X, controller.wheelCenterPoint.Y - controller.wheelCircleCenterPointLeft.Y);
                    vectorCD = new Point(gyroAccelX - controller.wheelCircleCenterPointLeft.X, gyroAccelZ - controller.wheelCircleCenterPointLeft.Y);
                }

                // Calculate dot product and magnitude of vectors (center vector and the current tilt vector)
                double dotProduct = vectorAB.X * vectorCD.X + vectorAB.Y * vectorCD.Y;
                double magAB = Math.Sqrt(vectorAB.X * vectorAB.X + vectorAB.Y * vectorAB.Y);
                double magCD = Math.Sqrt(vectorCD.X * vectorCD.X + vectorCD.Y * vectorCD.Y);

                // Calculate angle between vectors and convert radian to degrees
                if (magAB == 0 || magCD == 0)
                {
                    result = 0;
                }
                else
                {
                    double angle = Math.Acos(dotProduct / (magAB * magCD));
                    result = Convert.ToInt32(NumericUtil.Clamp(
                            -180.0 * C_WHEEL_ANGLE_PRECISION,
                            Math.Round((angle * (180.0 / Math.PI)), 1) * C_WHEEL_ANGLE_PRECISION,
                            180.0 * C_WHEEL_ANGLE_PRECISION)
                         );
                }

                // Left turn is -180..0 and right turn 0..180 degrees
                if (gyroAccelX < controller.wheelCenterPoint.X) result = -result;
            }

            return result;
        }

        // Calibrate sixaxis steering wheel emulation. Use DS4Windows configuration screen to start a calibration or press a special action key (if defined)
        private void SAWheelEmulationCalibration(int device, DS4StateExposed exposedState, ControlService ctrl, DS4State currentDeviceState, DS4Device controller)
        {
            int gyroAccelX, gyroAccelZ;
            int result;

            gyroAccelX = exposedState.getAccelX();
            gyroAccelZ = exposedState.getAccelZ();

            // State 0=Normal mode (ie. calibration process is not running), 1=Activating calibration, 2=Calibration process running, 3=Completing calibration, 4=Cancelling calibration
            if (controller.WheelRecalibrateActiveState == 1)
            {
                AppLogger.LogToGui($"Controller {1 + device} activated re-calibration of SA steering wheel emulation", false);

                controller.WheelRecalibrateActiveState = 2;

                controller.wheelPrevPhysicalAngle = 0;
                controller.wheelPrevFullAngle = 0;
                controller.wheelFullTurnCount = 0;

                // Clear existing calibration value and use current position as "center" point.
                // This initial center value may be off-center because of shaking the controller while button was pressed. The value will be overriden with correct value once controller is stabilized and hold still few secs at the center point
                controller.wheelCenterPoint.X = gyroAccelX;
                controller.wheelCenterPoint.Y = gyroAccelZ;
                controller.wheel90DegPointRight.X = gyroAccelX + 20;
                controller.wheel90DegPointLeft.X = gyroAccelX - 20;

                // Clear bitmask for calibration points. All three calibration points need to be set before re-calibration process is valid
                controller.wheelCalibratedAxisBitmask = DS4Device.WheelCalibrationPoint.None;

                controller.wheelPrevRecalibrateTime = new DateTime(2500, 1, 1);
            }
            else if (controller.WheelRecalibrateActiveState == 3)
            {
                AppLogger.LogToGui($"Controller {1 + device} completed the calibration of SA steering wheel emulation. center=({controller.wheelCenterPoint.X}, {controller.wheelCenterPoint.Y})  90L=({controller.wheel90DegPointLeft.X}, {controller.wheel90DegPointLeft.Y})  90R=({controller.wheel90DegPointRight.X}, {controller.wheel90DegPointRight.Y})", false);

                // If any of the calibration points (center, left 90deg, right 90deg) are missing then reset back to default calibration values
                if (((controller.wheelCalibratedAxisBitmask & DS4Device.WheelCalibrationPoint.All) == DS4Device.WheelCalibrationPoint.All))
                    ctrl.ProfileXmlStore.SaveControllerConfigsForDevice(controller);
                else
                    controller.wheelCenterPoint.X = controller.wheelCenterPoint.Y = 0;

                controller.WheelRecalibrateActiveState = 0;
                controller.wheelPrevRecalibrateTime = DateTime.Now;
            }
            else if (controller.WheelRecalibrateActiveState == 4)
            {
                AppLogger.LogToGui($"Controller {1 + device} cancelled the calibration of SA steering wheel emulation.", false);

                controller.WheelRecalibrateActiveState = 0;
                controller.wheelPrevRecalibrateTime = DateTime.Now;
            }

            if (controller.WheelRecalibrateActiveState > 0)
            {
                // Cross "X" key pressed. Set calibration point when the key is released and controller hold steady for a few seconds
                if (currentDeviceState.Cross == true) controller.wheelPrevRecalibrateTime = DateTime.Now;

                // Make sure controller is hold steady (velocity of gyro axis) to avoid misaligments and set calibration few secs after the "X" key was released
                if (Math.Abs(currentDeviceState.Motion.angVelPitch) < 0.5 && Math.Abs(currentDeviceState.Motion.angVelYaw) < 0.5 && Math.Abs(currentDeviceState.Motion.angVelRoll) < 0.5
                    && ((TimeSpan)(DateTime.Now - controller.wheelPrevRecalibrateTime)).TotalSeconds > 1)
                {
                    controller.wheelPrevRecalibrateTime = new DateTime(2500, 1, 1);

                    if (controller.wheelCalibratedAxisBitmask == DS4Device.WheelCalibrationPoint.None)
                    {
                        controller.wheelCenterPoint.X = gyroAccelX;
                        controller.wheelCenterPoint.Y = gyroAccelZ;

                        controller.wheelCalibratedAxisBitmask |= DS4Device.WheelCalibrationPoint.Center;
                    }
                    else if (controller.wheel90DegPointRight.X < gyroAccelX)
                    {
                        controller.wheel90DegPointRight.X = gyroAccelX;
                        controller.wheel90DegPointRight.Y = gyroAccelZ;
                        controller.wheelCircleCenterPointRight.X = controller.wheelCenterPoint.X;
                        controller.wheelCircleCenterPointRight.Y = controller.wheel90DegPointRight.Y;

                        controller.wheelCalibratedAxisBitmask |= DS4Device.WheelCalibrationPoint.Right90;
                    }
                    else if (controller.wheel90DegPointLeft.X > gyroAccelX)
                    {
                        controller.wheel90DegPointLeft.X = gyroAccelX;
                        controller.wheel90DegPointLeft.Y = gyroAccelZ;
                        controller.wheelCircleCenterPointLeft.X = controller.wheelCenterPoint.X;
                        controller.wheelCircleCenterPointLeft.Y = controller.wheel90DegPointLeft.Y;

                        controller.wheelCalibratedAxisBitmask |= DS4Device.WheelCalibrationPoint.Left90;
                    }
                }

                // Show lightbar color feedback how the calibration process is proceeding.
                //  red / yellow / blue / green = No calibration anchors/one anchor/two anchors/all three anchors calibrated when color turns to green (center, 90DegLeft, 90DegRight).
                int bitsSet = CountNumOfSetBits((int)controller.wheelCalibratedAxisBitmask);
                if (bitsSet >= 3) DS4LightBar.forcedColor[device] = calibrationColor_3;
                else if (bitsSet == 2) DS4LightBar.forcedColor[device] = calibrationColor_2;
                else if (bitsSet == 1) DS4LightBar.forcedColor[device] = calibrationColor_1;
                else DS4LightBar.forcedColor[device] = calibrationColor_0;

                result = CalculateControllerAngle(gyroAccelX, gyroAccelZ, controller);

                // Force lightbar flashing when controller is currently at calibration point (user can verify the calibration before accepting it by looking at flashing lightbar)
                if (((controller.wheelCalibratedAxisBitmask & DS4Device.WheelCalibrationPoint.Center) != 0 && Math.Abs(result) <= 1 * C_WHEEL_ANGLE_PRECISION)
                 || ((controller.wheelCalibratedAxisBitmask & DS4Device.WheelCalibrationPoint.Left90) != 0 && result <= -89 * C_WHEEL_ANGLE_PRECISION && result >= -91 * C_WHEEL_ANGLE_PRECISION)
                 || ((controller.wheelCalibratedAxisBitmask & DS4Device.WheelCalibrationPoint.Right90) != 0 && result >= 89 * C_WHEEL_ANGLE_PRECISION && result <= 91 * C_WHEEL_ANGLE_PRECISION)
                 || ((controller.wheelCalibratedAxisBitmask & DS4Device.WheelCalibrationPoint.Left90) != 0 && Math.Abs(result) >= 179 * C_WHEEL_ANGLE_PRECISION))
                    DS4LightBar.forcedFlash[device] = 2;
                else
                    DS4LightBar.forcedFlash[device] = 0;

                DS4LightBar.forcelight[device] = true;

                LogToGuiSACalibrationDebugMsg($"Calibration values ({gyroAccelX}, {gyroAccelZ})  angle={result / (1.0 * C_WHEEL_ANGLE_PRECISION)}\n");
            }
            else
            {
                // Re-calibration completed or cancelled. Set lightbar color back to normal color
                DS4LightBar.forcedFlash[device] = 0;
                DS4LightBar.forcedColor[device] = profileSettings.GetMainColor(device);
                DS4LightBar.forcelight[device] = false;
                DS4LightBar.updateLightBar(controller, device, profileSettings);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalcStickAxisFuzz(int device,
            int stickId, int delta, byte axisXValue, byte axisYValue,
            out byte useAxisX, out byte useAxisY)
        {
            if (stickId < 0 || stickId > 2)
            {
                throw new ArgumentOutOfRangeException("Stick ID has to be either 0 or 1");
            }

            int xIdX = stickId == 0 ? 0 : 2;
            int yIdX = stickId == 1 ? 1 : 3;
            ref byte lastXVal = ref lastStickAxisValues[device][xIdX];
            ref byte lastYVal = ref lastStickAxisValues[device][yIdX];
            useAxisX = lastXVal;
            useAxisY = lastYVal;

            int deltaX = axisXValue - lastXVal;
            int deltaY = axisYValue - lastYVal;
            int magSqu = (deltaX * deltaX) + (deltaY * deltaY);
            int deltaSqu = delta * delta;
            //if (stickId == 0)
            //    Console.WriteLine("DELTA MAG SQU: {0} {1}", magSqu, deltaSqu);

            if (axisXValue == 0 || axisXValue == 255 || magSqu > deltaSqu)
            {
                useAxisX = axisXValue;
                lastXVal = axisXValue;
            }

            if (axisYValue == 0 || axisYValue == 255 || magSqu > deltaSqu)
            {
                useAxisY = axisYValue;
                lastYVal = axisYValue;
            }
        }

        private void CalcWheelFuzz(int gyroX, int gyroZ, int lastGyroX, int lastGyroZ,
            int delta, out int useGyroX, out int useGyroZ)
        {
            useGyroX = lastGyroX;
            if (gyroX == 0 || gyroX == 128 || gyroX == -128 || Math.Abs(gyroX - lastGyroX) > delta)
            {
                useGyroX = gyroX;
            }

            useGyroZ = lastGyroZ;
            if (gyroZ == 0 || gyroZ == 128 || gyroZ == -128 || Math.Abs(gyroZ - lastGyroZ) > delta)
            {
                useGyroZ = gyroZ;
            }
        }

        protected Int32 Scale360degreeGyroAxis(int device, DS4StateExposed exposedState, ControlService ctrl)
        {
            unchecked
            {
                DS4Device controller;
                DS4State currentDeviceState;

                int gyroAccelX, gyroAccelZ;
                int result;

                controller = ctrl.DS4Controllers[device];
                if (controller == null) return 0;

                currentDeviceState = controller.getCurrentStateRef();

                // If calibration is active then do the calibration process instead of the normal "angle calculation"
                if (controller.WheelRecalibrateActiveState > 0)
                {
                    SAWheelEmulationCalibration(device, exposedState, ctrl, currentDeviceState, controller);

                    // Return center wheel position while SA wheel emuation is being calibrated
                    return 0;
                }

                // Do nothing if connection is active but the actual DS4 controller is still missing or not yet synchronized
                if (!controller.Synced)
                    return 0;

                gyroAccelX = exposedState.getAccelX();
                gyroAccelZ = exposedState.getAccelZ();

                // If calibration values are missing then use "educated guesses" about good starting values
                if (controller.wheelCenterPoint.IsEmpty)
                {
                    // Run if no controller config exists or if an empty wheelCenterPoint is still being used
                    if (!ctrl.ProfileXmlStore.LoadControllerConfigsForDevice(controller) || controller.wheelCenterPoint.IsEmpty)
                    {
                        AppLogger.LogToGui($"Controller {1 + device} sixaxis steering wheel calibration data missing. It is recommended to run steering wheel calibration process by pressing SASteeringWheelEmulationCalibration special action key. Using estimated values until the controller is calibrated at least once.", false);

                        // Use current controller position as "center point". Assume DS4Windows was started while controller was hold in center position (yes, dangerous assumption but can't do much until controller is calibrated)
                        controller.wheelCenterPoint.X = gyroAccelX;
                        controller.wheelCenterPoint.Y = gyroAccelZ;

                        controller.wheel90DegPointRight.X = controller.wheelCenterPoint.X + 113;
                        controller.wheel90DegPointRight.Y = controller.wheelCenterPoint.Y + 110;

                        controller.wheel90DegPointLeft.X = controller.wheelCenterPoint.X - 127;
                        controller.wheel90DegPointLeft.Y = controller.wheel90DegPointRight.Y;
                    }

                    controller.wheelCircleCenterPointRight.X = controller.wheelCenterPoint.X;
                    controller.wheelCircleCenterPointRight.Y = controller.wheel90DegPointRight.Y;
                    controller.wheelCircleCenterPointLeft.X = controller.wheelCenterPoint.X;
                    controller.wheelCircleCenterPointLeft.Y = controller.wheel90DegPointLeft.Y;

                    AppLogger.LogToGui($"Controller {1 + device} steering wheel emulation calibration values. Center=({controller.wheelCenterPoint.X}, {controller.wheelCenterPoint.Y})  90L=({controller.wheel90DegPointLeft.X}, {controller.wheel90DegPointLeft.Y})  90R=({controller.wheel90DegPointRight.X}, {controller.wheel90DegPointRight.Y})  Range={profileSettings.GetSASteeringWheelEmulationRange(device)}", false);
                    controller.wheelPrevRecalibrateTime = DateTime.Now;
                }


                int maxRangeRight = profileSettings.GetSASteeringWheelEmulationRange(device) / 2 * C_WHEEL_ANGLE_PRECISION;
                int maxRangeLeft = -maxRangeRight;

                //Console.WriteLine("Values {0} {1}", gyroAccelX, gyroAccelZ);

                //gyroAccelX = (int)(wheel360FilterX.Filter(gyroAccelX, currentRate));
                //gyroAccelZ = (int)(wheel360FilterZ.Filter(gyroAccelZ, currentRate));

                int wheelFuzz = ctrl.ProfileSettingsService.SAWheelFuzzValues[device];
                if (wheelFuzz != 0)
                {
                    //int currentValueX = gyroAccelX;
                    LastWheelGyroCoord lastWheelGyro = lastWheelGyroValues[device];
                    CalcWheelFuzz(gyroAccelX, gyroAccelZ, lastWheelGyro.gyroX, lastWheelGyro.gyroZ,
                        wheelFuzz, out gyroAccelX, out gyroAccelZ);
                    lastWheelGyro.gyroX = gyroAccelX; lastWheelGyro.gyroZ = gyroAccelZ;
                    //lastGyroX = gyroAccelX; lastGyroZ = gyroAccelZ;
                }

                result = CalculateControllerAngle(gyroAccelX, gyroAccelZ, controller);

                // Apply deadzone (SA X-deadzone value). This code assumes that 20deg is the max deadzone anyone ever might wanna use (in practice effective deadzone
                // is probably just few degrees by using SXDeadZone values 0.01...0.05)
                double sxDead = ctrl.ProfileSettingsService.SXDeadzone[device];
                if (sxDead > 0)
                {
                    int sxDeadInt = Convert.ToInt32(20.0 * C_WHEEL_ANGLE_PRECISION * sxDead);
                    if (Math.Abs(result) <= sxDeadInt)
                    {
                        result = 0;
                    }
                    else
                    {
                        // Smooth steering angle based on deadzone range instead of just clipping the deadzone gap
                        result -= (result < 0 ? -sxDeadInt : sxDeadInt);
                    }
                }

                // If wrapped around from +180 to -180 side (or vice versa) then SA steering wheel keeps on turning beyond 360 degrees (if range is >360).
                // Keep track of how many times the steering wheel has been turned beyond the full 360 circle and clip the result to max range.
                int wheelFullTurnCount = controller.wheelFullTurnCount;
                if (controller.wheelPrevPhysicalAngle < 0 && result > 0)
                {
                    if ((result - controller.wheelPrevPhysicalAngle) > 180 * C_WHEEL_ANGLE_PRECISION)
                    {
                        if (maxRangeRight > 360 / 2 * C_WHEEL_ANGLE_PRECISION)
                            wheelFullTurnCount--;
                        else
                            result = maxRangeLeft;
                    }
                }
                else if (controller.wheelPrevPhysicalAngle > 0 && result < 0)
                {
                    if ((controller.wheelPrevPhysicalAngle - result) > 180 * C_WHEEL_ANGLE_PRECISION)
                    {
                        if (maxRangeRight > 360 / 2 * C_WHEEL_ANGLE_PRECISION)
                            wheelFullTurnCount++;
                        else
                            result = maxRangeRight;
                    }
                }
                controller.wheelPrevPhysicalAngle = result;

                if (wheelFullTurnCount != 0)
                {
                    // Adjust value of result (steering wheel angle) based on num of full 360 turn counts
                    result += (wheelFullTurnCount * 180 * C_WHEEL_ANGLE_PRECISION * 2);
                }

                // If the new angle is more than 180 degrees further away then this is probably bogus value (controller shaking too much and gyro and velocity sensors went crazy).
                // Accept the new angle only when the new angle is within a "stability threshold", otherwise use the previous full angle value and wait for controller to be stabilized.
                if (Math.Abs(result - controller.wheelPrevFullAngle) <= 180 * C_WHEEL_ANGLE_PRECISION)
                {
                    controller.wheelPrevFullAngle = result;
                    controller.wheelFullTurnCount = wheelFullTurnCount;
                }
                else
                {
                    result = controller.wheelPrevFullAngle;
                }

                result = Mapping.ClampInt(maxRangeLeft, result, maxRangeRight);
                if (ctrl.ProfileSettingsService.WheelSmoothInfo[device].enabled)
                {
                    double currentRate = 1.0 / currentDeviceState.elapsedTime; // Need to express poll time in Hz
                    OneEuroFilter wheelFilter = wheelFilters[device];
                    result = (int)(wheelFilter.Filter(result * 1.0005, currentRate));
                    // Perform clamp again
                    result = Mapping.ClampInt(maxRangeLeft, result, maxRangeRight);
                }

                // Debug log output of SA sensor values
                //LogToGuiSACalibrationDebugMsg($"DBG gyro=({gyroAccelX}, {gyroAccelZ})  output=({exposedState.OutputAccelX}, {exposedState.OutputAccelZ})  PitRolYaw=({currentDeviceState.Motion.gyroPitch}, {currentDeviceState.Motion.gyroRoll}, {currentDeviceState.Motion.gyroYaw})  VelPitRolYaw=({currentDeviceState.Motion.angVelPitch}, {currentDeviceState.Motion.angVelRoll}, {currentDeviceState.Motion.angVelYaw})  angle={result / (1.0 * C_WHEEL_ANGLE_PRECISION)}  fullTurns={controller.wheelFullTurnCount}", false);

                // Apply anti-deadzone (SA X-antideadzone value)
                double sxAntiDead = ctrl.ProfileSettingsService.SXAntiDeadzone[device];

                int outputAxisMax, outputAxisMin, outputAxisZero;
                if (profileSettings.OutContType[device] == OutContType.DS4)
                {
                    // DS4 analog stick axis supports only 0...255 output value range (not the best one for steering wheel usage)
                    outputAxisMax = 255;
                    outputAxisMin = 0;
                    outputAxisZero = 128;
                }
                else
                {
                    // x360 (xinput) analog stick axis supports -32768...32767 output value range (more than enough for steering wheel usage)
                    outputAxisMax = 32767;
                    outputAxisMin = -32768;
                    outputAxisZero = 0;
                }

                switch (profileSettings.GetSASteeringWheelEmulationAxis(device))
                {
                    case SASteeringWheelEmulationAxisType.LX:
                    case SASteeringWheelEmulationAxisType.LY:
                    case SASteeringWheelEmulationAxisType.RX:
                    case SASteeringWheelEmulationAxisType.RY:
                        // DS4 thumbstick axis output (-32768..32767 raw value range)
                        //return (((result - maxRangeLeft) * (32767 - (-32768))) / (maxRangeRight - maxRangeLeft)) + (-32768);
                        if (result == 0) return outputAxisZero;

                        if (sxAntiDead > 0)
                        {
                            sxAntiDead *= ((result < 0 ? outputAxisMin : outputAxisMax) - outputAxisZero);
                            int outputResult = 0;
                            if (result < 0)
                            {
                                //(((result - maxRangeLeft) * (outputAxisZero - Convert.ToInt32(sxAntiDead) - (outputAxisMin))) / (0 - maxRangeLeft)) + (outputAxisMin);
                                outputResult = (int)((outputAxisMin - (int)sxAntiDead) * ((result - 0) / (double)(maxRangeLeft - 0)) + (int)sxAntiDead);
                            }
                            else
                            {
                                //(((result - 0) * (outputAxisMax - (outputAxisZero + Convert.ToInt32(sxAntiDead)))) / (maxRangeRight - 0)) + (outputAxisZero + Convert.ToInt32(sxAntiDead));
                                outputResult = (int)((outputAxisMax - (int)sxAntiDead) * ((result - 0) / (double)(maxRangeRight - 0)) + (int)sxAntiDead);
                            }

                            return outputResult;
                            //if (result < 0) return (((result - maxRangeLeft) * (outputAxisZero - Convert.ToInt32(sxAntiDead) - (outputAxisMin))) / (0 - maxRangeLeft)) + (outputAxisMin);
                            //else return (((result - 0) * (outputAxisMax - (outputAxisZero + Convert.ToInt32(sxAntiDead)))) / (maxRangeRight - 0)) + (outputAxisZero + Convert.ToInt32(sxAntiDead));
                        }
                        else
                        {
                            return (((result - maxRangeLeft) * (outputAxisMax - (outputAxisMin))) / (maxRangeRight - maxRangeLeft)) + (outputAxisMin);
                        }

                    case SASteeringWheelEmulationAxisType.L2R2:
                        // DS4 Trigger axis output. L2+R2 triggers share the same axis in x360 xInput/DInput controller,
                        // so L2+R2 steering output supports only 360 turn range (-255..255 raw value range in the shared trigger axis)
                        if (result == 0) return 0;

                        result = Convert.ToInt32(Math.Round(result / (1.0 * C_WHEEL_ANGLE_PRECISION)));
                        if (result < 0) result = -181 - result;

                        if (sxAntiDead > 0)
                        {
                            sxAntiDead *= 255;
                            if (result < 0) return (((result - (-180)) * (-Convert.ToInt32(sxAntiDead) - (-255))) / (0 - (-180))) + (-255);
                            else return (((result - (0)) * (255 - (Convert.ToInt32(sxAntiDead)))) / (180 - (0))) + (Convert.ToInt32(sxAntiDead));
                        }
                        else
                        {
                            return (((result - (-180)) * (255 - (-255))) / (180 - (-180))) + (-255);
                        }

                    case SASteeringWheelEmulationAxisType.VJoy1X:
                    case SASteeringWheelEmulationAxisType.VJoy1Y:
                    case SASteeringWheelEmulationAxisType.VJoy1Z:
                    case SASteeringWheelEmulationAxisType.VJoy2X:
                    case SASteeringWheelEmulationAxisType.VJoy2Y:
                    case SASteeringWheelEmulationAxisType.VJoy2Z:
                        // SASteeringWheelEmulationAxisType.VJoy1X/VJoy1Y/VJoy1Z/VJoy2X/VJoy2Y/VJoy2Z VJoy axis output (0..32767 raw value range by default)
                        if (result == 0) return 16384;

                        if (sxAntiDead > 0)
                        {
                            sxAntiDead *= 16384;
                            if (result < 0) return (((result - maxRangeLeft) * (16384 - Convert.ToInt32(sxAntiDead) - (-0))) / (0 - maxRangeLeft)) + (-0);
                            else return (((result - 0) * (32767 - (16384 + Convert.ToInt32(sxAntiDead)))) / (maxRangeRight - 0)) + (16384 + Convert.ToInt32(sxAntiDead));
                        }
                        else
                        {
                            return (((result - maxRangeLeft) * (32767 - (-0))) / (maxRangeRight - maxRangeLeft)) + (-0);
                        }

                    default:
                        // Should never come here, but C# case statement syntax requires DEFAULT handler
                        return 0;
                }
            }
        }

        private void CalcAntiSnapbackStick(int device,
            int stickId, double delta, long timeout, byte axisXValue, byte axisYValue,
            out byte useAxisX, out byte useAxisY)
        {
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            ref Queue<DS4TimedStickAxisValue> queue = ref stickValueHistory[device][stickId];
            while (queue.Count > 0 && queue.Peek().timestamp < timestamp - timeout)
            {
                queue.Dequeue();
            }
            if (queue.Any(oldValues =>
            {
                double distanceSquared = Math.Pow(axisXValue - oldValues.x, 2) + Math.Pow(axisYValue - oldValues.y, 2);
                if (distanceSquared >= (delta * delta))
                {
                    //Checks if the line between two points touches a 15 unit circle in the middle
                    double t = ((128 - axisXValue) * (oldValues.x - axisXValue) + (128 - axisYValue) * (oldValues.y - axisYValue)) / distanceSquared;
                    t = Math.Max(0, Math.Min(1, t));
                    double distanceToMiddleSquared = Math.Pow(128 - (axisXValue + t * (oldValues.x - axisXValue)), 2) + Math.Pow(128 - (axisYValue + t * (oldValues.y - axisYValue)), 2);
                    return distanceToMiddleSquared <= 15 * 15;
                }
                else
                {
                    return false;
                }
            }))
            {
                useAxisX = useAxisY = 128;
            }
            else
            {
                useAxisX = axisXValue;
                useAxisY = axisYValue;
            }
            queue.Enqueue(new DS4TimedStickAxisValue(axisXValue, axisYValue, timestamp));
        }
    }
}