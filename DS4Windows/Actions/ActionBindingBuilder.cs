using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step5 W1（Phase8-Step5-Plan.md §3.1。Phase8-Step2-Design.md §6・§8）: <see cref="IActionBindingBuilder"/> の実装（DI の Singleton）。
    /// 種別ごとの構成は設計書 §6 のとおり（Button／Key／Macro／Program／Profile の 5 種）。それ以外の種別
    /// （<c>DisconnectBT</c>、<c>BatteryCheck</c>、<c>MultiAction</c>、<c>XboxGameDVR</c>、<c>SASteeringWheelEmulationCalibrate</c>、<c>GyroCalibrate</c>、<c>None</c>）は束縛を作らず、旧経路が処理する。
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

        // TODO(Phase8-Step7 7.5b。規約 2.4): 復帰（uTrigger／AutomaticUntrigger）を持つ Profile は、復帰の合図と毎フレームの確認をエンジンへ移す 7.5b まで、
        // 束縛を作らず、旧経路が処理する（7.5a は、復帰を持たない「切替のみ」の Profile だけを新経路へ移す）。7.5b で、この定数と分岐を削除する。
        private const bool ProfileUntriggerMigrated = false;

        public ActionBindingBuilder(IProfileActionProvider profileActions, IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, BindingMigrationOptions options, ISpecialActionRepository actionRepository = null,
            Func<IMapping> mappingProvider = null)
        {
            _actionRepository = actionRepository;
            _mappingProvider = mappingProvider;
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
                    if (!ProfileUntriggerMigrated && (sa.automaticUntrigger || (sa.uTrigger != null && sa.uTrigger.Count > 0)))
                        return null;   // 復帰を持つ Profile は、7.5b まで旧経路が処理する

                    // 復帰: 自動（トリガーを離したとき）が優先。そうでなく解除用のボタンがあれば、そのボタン（旧 Mapping の uTrigger と同じ）
                    UntriggerPolicy untrigger = sa.automaticUntrigger
                        ? UntriggerPolicy.Automatic
                        : (sa.uTrigger != null && sa.uTrigger.Count > 0 ? UntriggerPolicy.Buttons : UntriggerPolicy.None);
                    return new TriggerBinding(Input(sa, false), new IOutputAction[] { new ProfileSwitchAction(sa, device, _profileSwitcher, index, _actionRepository, _profileSettings, _mappingProvider) },
                        new BindingPolicy(blocksWhileExecuting: true, untrigger: untrigger,
                            untriggerButtons: untrigger == UntriggerPolicy.Buttons ? sa.uTrigger : null), sa, index);
                }

                default:
                    return null;
            }
        }

        private ComboTriggerInputAction Input(SpecialAction sa, bool invertOnRelease)
            => new ComboTriggerInputAction(sa.name, sa.trigger, _profileSettings, sa.delayTime, invertOnRelease);
    }
}
