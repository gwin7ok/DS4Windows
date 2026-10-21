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
        private readonly IMacroStringPlayer _macroStringPlayer;          // MultiAction（7.7b）。同上

        public SpecialActionOutputFactory(IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository = null,
            Func<IMapping> mappingProvider = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null,
            IMacroStringPlayer macroStringPlayer = null)
            : this(profileSettings, manager ?? throw new ArgumentNullException(nameof(manager)),
                macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer)),
                processLauncher ?? throw new ArgumentNullException(nameof(processLauncher)),
                profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher)),
                actionRepository, mappingProvider, gyroCalibration, disconnectPort, batteryCheckPort, macroStringPlayer, true)
        {
        }

        /// <summary>
        /// Phase10-Step6 I4b: Controls の Key と Macro だけを作る最小の部品（<see cref="ControlBindingBuilder"/> が、出力の部品を渡されなかったとき［テスト］に使う）。
        /// 他の種別（窓口が必要なもの）は、窓口が未接続として null を返す。
        /// </summary>
        public static SpecialActionOutputFactory ForControlsKeyAndMacro(IProfileSettingsService profileSettings, IMacroPlayer macroPlayer)
            => new SpecialActionOutputFactory(profileSettings, null, macroPlayer, null, null, null, null, null, null, null, null, true);

        // 最小の窓口だけで作る版も、この 1 本の本体を通す（flag は、公開のコンストラクターと見分けるためだけの印）
        private SpecialActionOutputFactory(IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository,
            Func<IMapping> mappingProvider, IGyroCalibrationPort gyroCalibration,
            IControllerDisconnectPort disconnectPort, IBatteryCheckPort batteryCheckPort,
            IMacroStringPlayer macroStringPlayer, bool bypassesRequiredChecks)
        {
            _actionRepository = actionRepository;
            _mappingProvider = mappingProvider;
            _gyroCalibration = gyroCalibration;
            _disconnectPort = disconnectPort;
            _batteryCheckPort = batteryCheckPort;
            _macroStringPlayer = macroStringPlayer;
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _manager = manager;
            _macroPlayer = macroPlayer;
            _processLauncher = processLauncher;
            _profileSwitcher = profileSwitcher;
        }

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (source == null) throw new ArgumentNullException(nameof(source));

            // 設定の内容は定義から、実行時の状態の入れ物（出力アクションが受け取る SpecialAction）は source から。由来が Controls のときは、方針を Controls の形にする
            AssignmentOrigin origin = definition.Origin;
            // Controls 由来の Key／Macro は、ログの index と ActionEntries の数（SA タブの設定の位置）を持たない（従来の Controls の束縛と同じ）
            ISpecialActionRepository logRepository = origin == AssignmentOrigin.Controls ? null : _actionRepository;
            SpecialAction sa = source;
            ControlSpecialItem item = definition.Item;

            switch (definition.Kind)
            {
                case SpecialAction.ActionTypeId.Button:
                {
                    // 設計書 §6: Press は Fresh-press ガードを見ない／Toggle は見る（規則は FreshPressGuardTable。Stage3 の ButtonSpecialActionOutput と同じ）
                    if (!definition.Button.HasValue || _manager == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new ButtonOutputAction(sa, definition.Button.Value, _manager) },
                        Policy(origin, definition));
                }

                case SpecialAction.ActionTypeId.Key:
                    if (!definition.Key.HasValue) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new KeyOutputAction(sa, definition.Key.Value, _profileSettings, index, logRepository) },
                        Policy(origin, definition));

                case SpecialAction.ActionTypeId.Macro:
                    if (_macroPlayer == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new MacroAction(sa, device, _macroPlayer, index, logRepository) },
                        Policy(origin, definition), invertOnRelease: item != null && item.RunOnRelease);

                case SpecialAction.ActionTypeId.Program:
                    if (_processLauncher == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new LaunchProcessAction(sa, _processLauncher, index, _actionRepository) },
                        Policy(origin, definition, blocksWhileExecuting: true));

                case SpecialAction.ActionTypeId.Profile:
                {
                    if (_profileSwitcher == null) return null;
                    // Phase8-Step8 8b（ユーザー承認、2026-10-20）: 解除トリガー（uTrigger）のボタンによる復帰は、機能廃止（規約 2.2）。UI から作れず
                    // （a7277da9 で「Set Unload Trigger」を削除）、旧データ（Actions.xml）にだけ残っていた。uTrigger を持つ Profile も束縛を作り、
                    // 切替は行う。復帰は、自動（AutomaticUntrigger）のときだけ。uTrigger の値は読み込みも保存もそのまま（ファイルは変えない）。
                    UntriggerPolicy untrigger = item != null && item.AutomaticUntrigger ? UntriggerPolicy.Automatic : UntriggerPolicy.None;
                    return new SpecialActionOutputs(new IOutputAction[] { new ProfileSwitchAction(sa, device, _profileSwitcher, index, _actionRepository, _profileSettings, _mappingProvider) },
                        Policy(origin, definition, blocksWhileExecuting: true, untrigger: untrigger,
                            stopOnRelease: untrigger == UntriggerPolicy.None));   // 復帰を持つ Profile は、離したエッジの Stop を呼ばない。復帰は ProcessUntrigger だけが担う（二重の復帰を防ぐ。Q4）
                }

                case SpecialAction.ActionTypeId.GyroCalibrate:
                    // 立ち上がりの 1 回だけ。Fresh-press ガードは既定のまま（旧経路の独自の判定と同じ効果）。実行中のブロックなし
                    if (_gyroCalibration == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new GyroCalibrateAction(sa, device, _gyroCalibration, index, _actionRepository) },
                        Policy(origin, definition));

                case SpecialAction.ActionTypeId.DisconnectBT:
                    // 切断の要求が成功するまで、押している間は再試行する（Q3＝B）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_disconnectPort == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new DisconnectBTAction(sa, device, _disconnectPort, _profileSettings) },
                        Policy(origin, definition));

                case SpecialAction.ActionTypeId.BatteryCheck:
                    // 押している間、毎フレームの色の遷移。離したときに戻す（既定の StopOnRelease）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_batteryCheckPort == null) return null;
                    return new SpecialActionOutputs(new IOutputAction[] { new BatteryCheckAction(sa, device, _batteryCheckPort, index, _actionRepository) },
                        Policy(origin, definition));

                case SpecialAction.ActionTypeId.MultiAction:
                {
                    // 独立した入力アクション（タップ・長押し・ダブルタップを Gesture で返す）。Fresh-press ガードを見ない。出力の Stop は不要（旧経路になし）。
                    // 「どのジェスチャーでどのマクロか」の対応は、ここ（変換層）で決める: details を「,」で分けた 3 つ（タップ、長押し、ダブルタップの順。足りなければ空）から、
                    // ジェスチャーごとの出力を 3 つそろえて作る（添字を固定するため、空でも作る）。エンジンが Outputs[Gesture - 1] を実行する（BindingPolicy.RoutesOutputsByGesture）
                    if (_macroStringPlayer == null) return null;

                    // 手順は、型のある項目の 3 つのマクロ（タップ、長押し、ダブルタップ）を「/」でつないだ文字列にして渡す（再生側が受け取る形）
                    var macros = new[]
                    {
                        item == null ? string.Empty : string.Join("/", item.TapMacro),
                        item == null ? string.Empty : string.Join("/", item.HoldMacro),
                        item == null ? string.Empty : string.Join("/", item.DoubleTapMacro),
                    };
                    var gestureOutputs = new IOutputAction[3];
                    for (int g = 0; g < gestureOutputs.Length; g++)
                        gestureOutputs[g] = new MacroStringAction(sa, device, macros[g], _macroStringPlayer);

                    return new SpecialActionOutputs(gestureOutputs,
                        Policy(origin, definition, stopOnRelease: false, routesOutputsByGesture: true),
                        isMultiTap: true);
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// 実行方針。SA 用（由来が SA）は、種別ごとの値のまま（すべて既定なら共有の <see cref="BindingPolicy.Default"/>）。
        /// Controls 用（由来が Controls）は、Controls の既存の束縛（Key／Macro／Pad など）と同じく、トリガーのボタンを抑制せず・出力も消さず（<c>suppressesTriggerButtons: false</c>、
        /// <c>resetsTriggerOutput: false</c>）、代わりに割り当てたボタンの通常の出力を毎フレーム消す（<c>erasesOutputEveryFrame: true</c>）。
        /// </summary>
        private static BindingPolicy Policy(AssignmentOrigin origin, AssignmentDefinition definition, bool blocksWhileExecuting = false,
            UntriggerPolicy untrigger = UntriggerPolicy.None, bool stopOnRelease = true, bool routesOutputsByGesture = false)
        {
            // Fresh-press ガードを見るかは、規則の表（FreshPressGuardTable）が決める（Phase10-Step4c W1）。Button の Toggle だけ、SA の設定に依存する
            bool toggle = definition.Button?.Toggle ?? definition.Key?.Toggle ?? false;   // Button の Toggle（SA の KeyButtonSwitchMode）。Key では表が見ない
            bool respectsFreshPressGuard = FreshPressGuardTable.For(origin, definition.Kind, toggle);   // Phase10-Step6 I4b: 種別 × 由来の表
            if (origin != AssignmentOrigin.Controls)
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
