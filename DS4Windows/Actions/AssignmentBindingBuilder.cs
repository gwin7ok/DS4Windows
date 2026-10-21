using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I4c（Phase10-Step6-Plan.md §4）: 共通の割り当て定義（<see cref="AssignmentDefinition"/>）から、束縛（<see cref="TriggerBinding"/>）を組み立てる、1 つの部品。
    /// 出力と方針は <see cref="ISpecialActionOutputFactory"/>（種別 × 由来の表）、入力は定義の由来と種別から決める:
    /// Controls 由来は <see cref="ControlButtonInputAction"/>（押下の判定は出力の種類から決まる性質、押し続ける秒数、離したとき成立）、
    /// SA 由来は <see cref="MultiTapInputAction"/>（MultiAction）か <see cref="ComboTriggerInputAction"/>（複数ボタンの組み合わせ）。
    /// 以前は <c>ActionBindingBuilder</c> と <c>ControlBindingBuilder</c> に、似た組み立てが 3 か所あった。一覧を歩く部分（SA のリスト、Controls の dcs と Extras の順序・門番）は、それぞれの builder に残る。
    /// </summary>
    public sealed class AssignmentBindingBuilder
    {
        private readonly ISpecialActionOutputFactory _outputFactory;
        private readonly IProfileSettingsService _profileSettings;

        private readonly IControlExtrasPort _extrasPort;   // Phase10-Step6 I5b: SA に付ける Extras の実行。省略時（テスト）は、Extras の束縛を作らない

        public AssignmentBindingBuilder(ISpecialActionOutputFactory outputFactory, IProfileSettingsService profileSettings,
            IControlExtrasPort extrasPort = null)
        {
            _outputFactory = outputFactory ?? throw new ArgumentNullException(nameof(outputFactory));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _extrasPort = extrasPort;
        }

        /// <summary>
        /// Phase10-Step6 I5b: SA に付けた Extras（<see cref="AssignmentDefinition.Extras"/>）の束縛を組み立てる。Extras がないとき、窓口が未接続のとき（テスト）は null。
        /// 動作は Controls の Extras と同じ出力（<see cref="ControlExtrasOutputAction"/>。押している間、毎フレーム適用し、離したときに元へ戻す）。
        /// 「その SA が成立している間だけ」有効にするため、入力は SA のトリガー（複数ボタンの組み合わせ）と押し続ける秒数を同じにした、押している間の入力
        /// （離したとき成立の反転は使わない。MultiAction は遅延なし）。Fresh-press ガードは、SA 本体の方針と同じ（プロファイル適用直後の押しっぱなしで、SA より先に効かないため）。
        /// トリガーのボタンの抑制・通常の出力の消去はしない（それは SA 本体の束縛が行う）。SA の束縛の<b>前</b>に並べる（Controls の Extras と同じ）。
        /// </summary>
        /// <param name="mainPolicy">同じ SA の本体の束縛の方針（Fresh-press ガードの規則を合わせる）。</param>
        public TriggerBinding BuildExtras(int device, AssignmentDefinition definition, SpecialAction source, BindingPolicy mainPolicy)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (mainPolicy == null) throw new ArgumentNullException(nameof(mainPolicy));
            if (!definition.Extras.HasValue || _extrasPort == null) return null;

            // 状態の鍵は、SA の固定 ID から（名前の変更に依らず、Controls の Extras の名前［ボタン番号］とも重ならない）
            string name = $"Synthetic_SaExtras_{device}_{definition.Id}";
            var identity = new SpecialAction(name, "Synthetic", "Extras", "Extras", 0, string.Empty);
            identity.type = "Extras";
            identity.details = source.outputExtras;
            identity.controls = name;

            double hold = definition.Kind == SpecialAction.ActionTypeId.MultiAction ? 0.0 : definition.HoldSeconds;
            var policy = new BindingPolicy(respectsFreshPressGuard: mainPolicy.RespectsFreshPressGuard, suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false, logsRisingEdge: true);
            return new TriggerBinding(new ComboTriggerInputAction(name, definition.Triggers, _profileSettings, hold),
                new IOutputAction[] { new ControlExtrasOutputAction(identity, definition.Extras.Value, _extrasPort) }, policy, identity, -1);
        }

        /// <summary>
        /// 束縛を組み立てる。種別が未対応のとき、種別に必要な窓口が未接続のとき（テスト）は null。
        /// </summary>
        /// <param name="source">実行時の状態の入れ物（SA 由来はプロファイルの実物、Controls 由来は合成のもの）。</param>
        /// <param name="index">SA の一覧での位置（Controls 由来は -1）。</param>
        public TriggerBinding Build(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (source == null) throw new ArgumentNullException(nameof(source));

            SpecialActionOutputs outputs = _outputFactory.Create(device, definition, source, index);
            if (outputs == null) return null;

            return new TriggerBinding(CreateInput(definition, source, outputs), outputs.Outputs, outputs.Policy, source, index);
        }

        private IInputAction CreateInput(AssignmentDefinition definition, SpecialAction source, SpecialActionOutputs outputs)
        {
            if (definition.Origin == AssignmentOrigin.Controls)
            {
                // Controls は、1 ボタン。区分は種別から（Key、Macro、それ以外は SA 専用の種別）。押下の判定は、出力の種類（デジタル）から決まる性質
                ControlBindingKind kind = definition.Kind == SpecialAction.ActionTypeId.Key ? ControlBindingKind.Key
                    : definition.Kind == SpecialAction.ActionTypeId.Macro ? ControlBindingKind.Macro
                    : ControlBindingKind.Special;
                return new ControlButtonInputAction(source.name, definition.Triggers[0], kind, ControlOutputNatureTable.ForKind(kind),
                    invertOnRelease: outputs.InvertOnRelease, delaySeconds: definition.HoldSeconds, profileSettings: _profileSettings);
            }

            // SA は、複数ボタンの組み合わせ。MultiAction だけは、タップ・長押し・ダブルタップを判定する独立した入力
            return outputs.IsMultiTap
                ? (IInputAction)new MultiTapInputAction(source.name, definition.Triggers, _profileSettings, definition.HoldSeconds)
                : new ComboTriggerInputAction(source.name, definition.Triggers, _profileSettings, definition.HoldSeconds, outputs.InvertOnRelease);
        }
    }
}
