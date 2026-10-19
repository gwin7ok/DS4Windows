using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step5 W1（Phase8-Step5-Plan.md §3.1。Phase8-Step2-Design.md §6・§8）: <see cref="IActionBindingBuilder"/> の実装（DI の Singleton）。
    /// 種別ごとの構成は設計書 §6 のとおり（Button／Key／Macro／Program／Profile／GyroCalibrate／DisconnectBT／BatteryCheck の 8 種）。それ以外の種別
    /// （<c>MultiAction</c>、<c>XboxGameDVR</c>、<c>SASteeringWheelEmulationCalibrate</c>、<c>None</c>）は束縛を作らず、旧経路が処理する。
    /// <para>
    /// TODO（Phase8-Step7。規約 2.4）: 本クラスは Step5 で作り、本番の適用の経路（<c>ProfileApplicationService</c>）から呼ぶが、
    /// <see cref="BindingMigrationOptions.MigratedTypes"/> が空の間は常に空のリストを返す。各種別の構成（方針の細部を含む）は、
    /// Step7 の種別ごとの PR で、特性化テストを新旧の両経路で走らせて確定する。Key の <c>uTrigger</c>（解除用のボタン）、
    /// Macro の同期実行（<c>synchronized</c>）・RunOnRelease の細部は、Step7.2・7.3 で扱う。
    /// </para>
    /// </summary>
    public sealed class ActionBindingBuilder : IActionBindingBuilder
    {
        private readonly IProfileActionProvider _profileActions;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IManagedActionManager _manager;
        private readonly IMacroPlayer _macroPlayer;
        private readonly IProcessLauncher _processLauncher;
        private readonly IProfileSwitcher _profileSwitcher;
        private readonly BindingMigrationOptions _options;
        private readonly ISpecialActionRepository _actionRepository;   // Key のログ（ActionEntries の数）用。省略可（テスト）
        private readonly Func<IMapping> _mappingProvider;               // Profile の連鎖の防止（復帰の合図の参照）用。省略可（テスト）
        private readonly IGyroCalibrationPort _gyroCalibration;         // GyroCalibrate（7.6a）。省略時（テスト）は束縛を作らず、旧経路が処理する
        private readonly IControllerDisconnectPort _disconnectPort;     // DisconnectBT（7.6b）。省略時（テスト）は束縛を作らず、旧経路が処理する
        private readonly IBatteryCheckPort _batteryCheckPort;           // BatteryCheck（7.6c）。省略時（テスト）は束縛を作らず、旧経路が処理する

        public ActionBindingBuilder(IProfileActionProvider profileActions, IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, BindingMigrationOptions options, ISpecialActionRepository actionRepository = null,
            Func<IMapping> mappingProvider = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null)
        {
            _actionRepository = actionRepository;
            _mappingProvider = mappingProvider;
            _gyroCalibration = gyroCalibration;
            _disconnectPort = disconnectPort;
            _batteryCheckPort = batteryCheckPort;
            _profileActions = profileActions ?? throw new ArgumentNullException(nameof(profileActions));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            // 移行対象が空の間（本番の既定）は、何も読まずに空のリストを返す
            if (_options.MigratedTypes.Count == 0) return Array.Empty<IActionBinding>();

            // 名前の一覧は、読込側が Clear→Add するリストの複製を使う（適用の途中で変わっても、この呼び出しの中では一貫させる）
            IReadOnlyList<string> names = _profileActions.GetProfileActionNames(device);
            if (names == null || names.Count == 0) return Array.Empty<IActionBinding>();

            var result = new List<IActionBinding>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                SpecialAction sa = _profileActions.GetProfileAction(device, names[i]);
                if (sa == null) continue;   // 旧ループと同じ: 見つからない名前は飛ばす（警告ログは EmitMissingActionLogsForDevice が出す）
                if (!_options.IsMigrated(sa.typeID)) continue;

                IActionBinding binding = Create(device, sa, _profileActions.GetProfileActionIndexOf(device, names[i]));
                if (binding != null) result.Add(binding);
            }

            return result.Count == 0 ? (IReadOnlyList<IActionBinding>)Array.Empty<IActionBinding>() : result.ToArray();
        }

        private IActionBinding Create(int device, SpecialAction sa, int index)
        {
            // トリガーのボタンがない特殊アクションは、旧経路でも成立しない。束縛を作らない
            if (sa.trigger == null || sa.trigger.Count == 0) return null;

            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Button:
                {
                    // 設計書 §6: Press は Fresh-press ガードを見ない／Toggle は見る（Stage3 の ButtonSpecialActionOutput と同じ規則）
                    bool toggle = sa.KeyButtonSwitchMode.HasValue && sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new ButtonOutputAction(sa, _manager) },
                        new BindingPolicy(respectsFreshPressGuard: toggle), sa, index);
                }

                case SpecialAction.ActionTypeId.Key:
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new KeyOutputAction(sa, _profileSettings, index, _actionRepository) },
                        BindingPolicy.Default, sa, index);

                case SpecialAction.ActionTypeId.Macro:
                    return new TriggerBinding(Input(sa, sa.pressRelease), new IOutputAction[] { new MacroAction(sa, device, _macroPlayer, index, _actionRepository) },
                        BindingPolicy.Default, sa, index);

                case SpecialAction.ActionTypeId.Program:
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new LaunchProcessAction(sa, _processLauncher, index, _actionRepository) },
                        new BindingPolicy(blocksWhileExecuting: true), sa, index);

                case SpecialAction.ActionTypeId.Profile:
                {
                    // TODO(Phase8-Step8。規約 2.4。Phase8-Step7-5b-Plan.md Q2＝A): 解除トリガー（uTrigger）を持つ Profile は、UI から作れない（a7277da9 で「Set Unload Trigger」を削除）ため、
                    // 束縛を作らず、旧経路が処理する。Step8 の旧コード削除のときに、廃止（機能廃止のため、ユーザーの承認が要る）か、エンジンへの移行かを決める。
                    if (sa.uTrigger != null && sa.uTrigger.Count > 0)
                        return null;

                    // 復帰: 自動（トリガーを離したとき）が優先。そうでなく解除用のボタンがあれば、そのボタン（旧 Mapping の uTrigger と同じ）
                    UntriggerPolicy untrigger = sa.automaticUntrigger
                        ? UntriggerPolicy.Automatic
                        : (sa.uTrigger != null && sa.uTrigger.Count > 0 ? UntriggerPolicy.Buttons : UntriggerPolicy.None);
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new ProfileSwitchAction(sa, device, _profileSwitcher, index, _actionRepository, _profileSettings, _mappingProvider) },
                        new BindingPolicy(blocksWhileExecuting: true, untrigger: untrigger,
                            untriggerButtons: untrigger == UntriggerPolicy.Buttons ? sa.uTrigger : null,
                            stopOnRelease: untrigger == UntriggerPolicy.None), sa, index);   // 復帰を持つ Profile は、離したエッジの Stop を呼ばない。復帰は ProcessUntrigger だけが担う（二重の復帰を防ぐ。Q4）
                }

                case SpecialAction.ActionTypeId.GyroCalibrate:
                    // 立ち上がりの 1 回だけ。Fresh-press ガードは既定のまま（旧経路の独自の判定と同じ効果）。実行中のブロックなし
                    if (_gyroCalibration == null) return null;
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new GyroCalibrateAction(sa, device, _gyroCalibration, index, _actionRepository) },
                        BindingPolicy.Default, sa, index);

                case SpecialAction.ActionTypeId.DisconnectBT:
                    // 切断の要求が成功するまで、押している間は再試行する（Q3＝B）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_disconnectPort == null) return null;
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new DisconnectBTAction(sa, device, _disconnectPort, _profileSettings) },
                        BindingPolicy.Default, sa, index);

                case SpecialAction.ActionTypeId.BatteryCheck:
                    // 押している間、毎フレームの色の遷移。離したときに戻す（既定の StopOnRelease）。Fresh-press ガードは既定のまま。実行中のブロックなし
                    if (_batteryCheckPort == null) return null;
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new BatteryCheckAction(sa, device, _batteryCheckPort, index, _actionRepository) },
                        BindingPolicy.Default, sa, index);

                default:
                    return null;
            }
        }

        private ComboTriggerInputAction Input(SpecialAction sa, bool invertOnRelease)
            => new ComboTriggerInputAction(sa.name, sa.trigger, _profileSettings, sa.delayTime, invertOnRelease);
    }
}
