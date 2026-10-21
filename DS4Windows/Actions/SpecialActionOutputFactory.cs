using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step4 W2（Phase10-Step4-Plan.md §3.3）: <see cref="ISpecialActionOutputFactory"/> の実装（DI の Singleton）。
    /// <see cref="ActionBindingBuilder"/> の <c>Create</c> にあった、種別ごとの出力アクション・実行方針の組み立てを、そのまま移したもの（動作は変えない。
    /// 種別ごとの構成は Phase8-Step2-Design.md §6）。依存する窓口（ポート）もここが持つ。
    /// </summary>
    public sealed class SpecialActionOutputFactory : ISpecialActionOutputFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IManagedActionManager _manager;
        private readonly IMacroPlayer _macroPlayer;
        private readonly IProcessLauncher _processLauncher;
        private readonly IProfileSwitcher _profileSwitcher;
        private readonly ISpecialActionRepository _actionRepository;   // Key のログ（ActionEntries の数）用。省略可（テスト）
        private readonly Func<IMapping> _mappingProvider;               // Profile の連鎖の防止（復帰の合図の参照）用。省略可（テスト）
        private readonly IGyroCalibrationPort _gyroCalibration;         // GyroCalibrate（7.6a）。省略時（テスト）は束縛を作らない
        private readonly IControllerDisconnectPort _disconnectPort;     // DisconnectBT（7.6b）。同上
        private readonly IBatteryCheckPort _batteryCheckPort;           // BatteryCheck（7.6c）。同上
        private readonly IWheelCalibrationPort _wheelCalibrationPort;   // ステアリング較正（7.6d）。同上
        private readonly IMacroStringPlayer _macroStringPlayer;          // MultiAction（7.7b）。同上

        public SpecialActionOutputFactory(IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository = null,
            Func<IMapping> mappingProvider = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null,
            IWheelCalibrationPort wheelCalibrationPort = null, IMacroStringPlayer macroStringPlayer = null)
        {
            _actionRepository = actionRepository;
            _mappingProvider = mappingProvider;
            _gyroCalibration = gyroCalibration;
            _disconnectPort = disconnectPort;
            _batteryCheckPort = batteryCheckPort;
            _wheelCalibrationPort = wheelCalibrationPort;
            _macroStringPlayer = macroStringPlayer;
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public SpecialActionOutputs Create(int device, SpecialAction sa, int index, bool forControls = false)
        {
            if (sa == null) throw new ArgumentNullException(nameof(sa));

            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Button:
                {
                    // 設計書 §6: Press は Fresh-press ガードを見ない／Toggle は見る（規則は FreshPressGuardTable。Stage3 の ButtonSpecialActionOutput と同じ）
                    return new SpecialActionOutputs(new IOutputAction[] { new ButtonOutputAction(sa, ActionBindingBuilder.InterpretButton(sa), _manager) },
                        Policy(forControls, sa));
                }

                case SpecialAction.ActionTypeId.Key:
                    return new SpecialActionOutputs(new IOutputAction[] { new KeyOutputAction(sa, ActionBindingBuilder.InterpretKey(sa), _profileSettings, index, _actionRepository) },
                        Policy(forControls, sa));

                case SpecialAction.ActionTypeId.Macro:
                    return new SpecialActionOutputs(new IOutputAction[] { new MacroAction(sa, device, _macroPlayer, index, _actionRepository) },
                        Policy(forControls, sa), invertOnRelease: sa.pressRelease);

                case SpecialAction.ActionTypeId.Program:
                    return new SpecialActionOutputs(new IOutputAction[] { new LaunchProcessAction(sa, _processLauncher, index, _actionRepository) },
                        Policy(forControls, sa, blocksWhileExecuting: true));

                case SpecialAction.ActionTypeId.Profile:
                {
                    // Phase8-Step8 8b（ユーザー承認、2026-10-20）: 解除トリガー（uTrigger）のボタンによる復帰は、機能廃止（規約 2.2）。UI から作れず
                    // （a7277da9 で「Set Unload Trigger」を削除）、旧データ（Actions.xml）にだけ残っていた。uTrigger を持つ Profile も束縛を作り、
                    // 切替は行う。復帰は、自動（AutomaticUntrigger）のときだけ。uTrigger の値は読み込みも保存もそのまま（ファイルは変えない）。
                    UntriggerPolicy untrigger = sa.automaticUntrigger ? UntriggerPolicy.Automatic : UntriggerPolicy.None;
                    return new SpecialActionOutputs(new IOutputAction[] { new ProfileSwitchAction(sa, device, _profileSwitcher, index, _actionRepository, _profileSettings, _mappingProvider) },
                        Policy(forControls, sa, blocksWhileExecuting: true, untrigger: untrigger,
                            stopOnRelease: untrigger == UntriggerPolicy.None));   // 復帰を持つ Profile は、離したエッジの Stop を呼ばない。復帰は ProcessUntrigger だけが担う（二重の復帰を防ぐ。Q4）
                }

                case SpecialAction.ActionTypeId.GyroCalibrate:
                    // 立ち上がりの 1 回だけ。Fresh-press ガードは既定のまま（旧経路の独自の判定と同じ効果）。実行中のブロックなし
                    if (_gyroCalibration == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new GyroCalibrateAction(sa, device, _gyroCalibration, index, _actionRepository) },
                        Policy(forControls, sa));

                case SpecialAction.ActionTypeId.DisconnectBT:
                    // 切断の要求が成功するまで、押している間は再試行する（Q3＝B）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_disconnectPort == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new DisconnectBTAction(sa, device, _disconnectPort, _profileSettings) },
                        Policy(forControls, sa));

                case SpecialAction.ActionTypeId.BatteryCheck:
                    // 押している間、毎フレームの色の遷移。離したときに戻す（既定の StopOnRelease）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_batteryCheckPort == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new BatteryCheckAction(sa, device, _batteryCheckPort, index, _actionRepository) },
                        Policy(forControls, sa));

                case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                    // 押している間、毎フレーム判定する（立ち上がりも含む）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_wheelCalibrationPort == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new SteeringWheelCalibrateAction(sa, device, _wheelCalibrationPort) },
                        Policy(forControls, sa));

                case SpecialAction.ActionTypeId.MultiAction:
                {
                    // 独立した入力アクション（タップ・長押し・ダブルタップを Gesture で返す）。Fresh-press ガードを見ない。出力の Stop は不要（旧経路になし）。
                    // 「どのジェスチャーでどのマクロか」の対応は、ここ（変換層）で決める: details を「,」で分けた 3 つ（タップ、長押し、ダブルタップの順。足りなければ空）から、
                    // ジェスチャーごとの出力を 3 つそろえて作る（添字を固定するため、空でも作る）。エンジンが Outputs[Gesture - 1] を実行する（BindingPolicy.RoutesOutputsByGesture）
                    if (_macroStringPlayer == null) return null;

                    string[] macros = (sa.details ?? string.Empty).Split(',');
                    var gestureOutputs = new IOutputAction[3];
                    for (int g = 0; g < gestureOutputs.Length; g++)
                        gestureOutputs[g] = new MacroStringAction(sa, device, g < macros.Length ? macros[g] : string.Empty, _macroStringPlayer);

                    return new SpecialActionOutputs(gestureOutputs,
                        Policy(forControls, sa, stopOnRelease: false, routesOutputsByGesture: true),
                        isMultiTap: true);
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// 実行方針。SA 用（<paramref name="forControls"/> が偽）は、種別ごとの値のまま（すべて既定なら共有の <see cref="BindingPolicy.Default"/>）。
        /// Controls 用は、Controls の既存の束縛（Key／Macro／Pad など）と同じく、トリガーのボタンを抑制せず・出力も消さず（<c>suppressesTriggerButtons: false</c>、
        /// <c>resetsTriggerOutput: false</c>）、代わりに割り当てたボタンの通常の出力を毎フレーム消す（<c>erasesOutputEveryFrame: true</c>）。
        /// </summary>
        private static BindingPolicy Policy(bool forControls, SpecialAction sa, bool blocksWhileExecuting = false,
            UntriggerPolicy untrigger = UntriggerPolicy.None, bool stopOnRelease = true, bool routesOutputsByGesture = false)
        {
            // Fresh-press ガードを見るかは、規則の表（FreshPressGuardTable）が決める（Phase10-Step4c W1）。Button の Toggle だけ、SA の設定に依存する
            bool toggle = sa.KeyButtonSwitchMode.HasValue && sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            bool respectsFreshPressGuard = FreshPressGuardTable.ForSpecialAction(sa.typeID, toggle);
            if (!forControls)
            {
                bool isDefault = respectsFreshPressGuard && !blocksWhileExecuting && untrigger == UntriggerPolicy.None && stopOnRelease && !routesOutputsByGesture;
                return isDefault
                    ? BindingPolicy.Default
                    : new BindingPolicy(respectsFreshPressGuard: respectsFreshPressGuard, blocksWhileExecuting: blocksWhileExecuting, untrigger: untrigger,
                        stopOnRelease: stopOnRelease, routesOutputsByGesture: routesOutputsByGesture);
            }

            return new BindingPolicy(respectsFreshPressGuard: respectsFreshPressGuard, blocksWhileExecuting: blocksWhileExecuting, untrigger: untrigger,
                suppressesTriggerButtons: false, resetsTriggerOutput: false, stopOnRelease: stopOnRelease,
                routesOutputsByGesture: routesOutputsByGesture, erasesOutputEveryFrame: true);
        }
    }
}
