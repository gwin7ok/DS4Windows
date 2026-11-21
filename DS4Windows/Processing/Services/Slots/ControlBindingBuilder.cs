using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// <see cref="IControlBindingBuilder"/> の実装（DI の Singleton）。
    /// Controls タブの割り当て（<c>ControlSettingsGroup</c>）のうち、各区分の条件を満たす割り当てだけを、束縛にする。
    /// 条件を満たさない割り当ては束縛を作らない（Phase8-Step8: 旧経路は削除済みのため、束縛なし＝何もしない。Q2＝C）。Phase8-Step8 8d: 区分ごとの移行のスイッチ（<c>BindingMigrationOptions</c>）は削除した。
    /// <para>
    /// Key（7.8b）の条件: 通常の割り当てが Key、<c>actionKey</c> が <c>ushort</c> の範囲内（シフトは 7.8d、Lightbar マクロは 7.8e で廃止した）。
    /// Extras（7.8e）は、通常の割り当て（Default／Key／Macro）と直交する部品で、別の束縛にする（<c>Synthetic_Extras_{device}_{control}</c>。通常の割り当ての束縛の前に並べる）。
    /// dcs が Extras を持つとき、Extras の区分が移行済みで、値が読める（9 個以上）ときだけ、dcs 全体を新経路が処理する。そうでなければ、束縛を 1 つも作らず、旧経路が全体を処理する。
    /// </para>
    /// 「何をするか」の解釈（キー番号、Toggle、ScanCode、別名キー）はここで行い、出力（<c>KeyOutputAction</c>）には解釈済みの値を渡す（Q9＝A）。
    /// 識別用の <c>SpecialAction</c>（状態の鍵）は、旧経路の合成と同じ名前（<c>Synthetic_Key_{device}_{control}</c>）で作る。
    /// 旧経路の <c>FindSpecialActionForLogicalKey</c> による Key スペシャルアクションとの状態の共有は、再現しない（Q4＝B。承認済みの動作変更）。
    /// </summary>
    public sealed partial class ControlBindingBuilder : IControlBindingBuilder
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMacroPlayer? _macroPlayer;   // Macro（7.8c）。省略時（テスト）は、Macro の束縛を作らず、旧経路が処理する
        private readonly IControlExtrasPort? _extrasPort;   // Extras（7.8e）。省略時（テスト）は、Extras を持つ割り当ての束縛を作らず、旧経路が処理する
        private readonly ISpecialActionOutputFactory? _outputFactory;   // SA 専用の種別（Special）の出力と方針。省略時（テスト）は、Special の束縛を作らない
        private readonly AssignmentBindingBuilder _assignmentBuilder;   // Key／Macro／Special の束縛の組み立て（SA タブ側と同じ部品）
        private readonly ISpecialActionOutputFactory? _controlsFactory;   // Key と Macro 用。_outputFactory がなければ、最小の部品（Key と Macro だけを作る）

        /// <summary>Program の引数に書くと、ウィンドウを隠して起動する語（SA の仕様。大文字小文字不問）。</summary>
        public const string HiddenKeyword = "$hidden";

        public ControlBindingBuilder(IProfileSettingsService profileSettings, IMacroPlayer? macroPlayer = null,
            IControlExtrasPort? extrasPort = null, ISpecialActionOutputFactory? outputFactory = null,
            IManagedActionManager? manager = null, IKeyButtonControllerRegistry? controllers = null)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            _macroPlayer = macroPlayer;
            _extrasPort = extrasPort;
            _outputFactory = outputFactory;
            // 最小の部品は _outputFactory がないとき（テスト）だけ使う。Key の出力が使う管理役と登録簿は、ここで受ける
            _controlsFactory = outputFactory == null
                ? SpecialActionOutputFactory.ForControlsKeyAndMacro(_profileSettings, manager, controllers, macroPlayer)
                : null;
            _assignmentBuilder = new AssignmentBindingBuilder(_outputFactory ?? _controlsFactory!);   // _outputFactory がなければ _controlsFactory を作ってある。束縛の組み立ては、SA タブ側と同じ部品
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            bool keys = true;
            bool macros = _macroPlayer != null;
            bool extras = _extrasPort != null;
            bool pads = true;
            bool mice = true;
            bool analogMice = true;

            ControlSettingsGroup group = _profileSettings.GetControlSettingsGroup(device);
            if (group == null) return Array.Empty<IActionBinding>();

            var result = new List<IActionBinding>();
            Add(device, group.LS, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.RS, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.L2, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.L2FullPull, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.R2, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.R2FullPull, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.ControlButtons, keys, macros, extras, pads, mice, analogMice, result);
            Add(device, group.ExtraDeviceButtons, keys, macros, extras, pads, mice, analogMice, result);

            return result.Count == 0 ? (IReadOnlyList<IActionBinding>)Array.Empty<IActionBinding>() : result.ToArray();
        }

        private void Add(int device, List<DS4ControlSettings> settings, bool keys, bool macros, bool extras, bool pads, bool mice, bool analogMice, List<IActionBinding> result)
        {
            if (settings == null) return;
            for (int i = 0; i < settings.Count; i++)
                Add(device, settings[i], keys, macros, extras, pads, mice, analogMice, result);
        }

        // 軸の Default で、反対側の向きが割り当て済み（Key／Macro／Button のどれでも、割り当てたボタンの消去が軸のペアの両方を中立にする）なら、素通しが要る
        private bool NeedsPassthrough(int device, DS4ControlSettings dcs)
        {
            int control = (int)dcs.control;
            if (control < 0 || control >= ControllerStateFieldMapping.mappedType.Length) return false;
            if (ControllerStateFieldMapping.mappedType[control] != ControllerStateFieldMapping.ControlType.AxisDir) return false;

            int relation = control % 2 == 0 ? control - 1 : control + 1;
            DS4ControlSettings? partner = _profileSettings.GetDS4CSetting(device, (DS4Controls)relation);
            return partner != null && partner.actionType != DS4ControlSettings.ActionType.Default;
        }

        // dcs の部品（Extras、通常の割り当て、Default の素通し）は、すべて新経路で扱えるときだけ束縛にする。一部でも扱えなければ、1 つも作らず、旧経路が dcs 全体を処理する
        private void Add(int device, DS4ControlSettings dcs, bool keys, bool macros, bool extras, bool pads, bool mice, bool analogMice, List<IActionBinding> result)
        {
            if (dcs == null) return;

            bool hasExtras = !dcs.IsExtrasEmpty(dcs.extras);
            ExtrasBindingValues extrasValues = default;
            if (hasExtras && (!extras || !ControlAssignmentInterpreter.TryInterpretExtras(dcs.extras, out extrasValues))) return;

            switch (dcs.actionType)
            {
                case DS4ControlSettings.ActionType.Default:
                    {
                        bool passthrough = NeedsPassthrough(device, dcs);
                        if (passthrough && !pads) return;   // 素通しは旧経路が行う（Extras も含め、dcs 全体を旧経路が処理する）
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        if (passthrough) AddPassthrough(device, dcs, result);
                        break;
                    }
                case DS4ControlSettings.ActionType.Key:
                    if (keys && ControlsAssignmentReader.TryReadKey(device, dcs, out AssignmentDefinition? keyDefinition, out SpecialAction? keyIdentity))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddFromDefinition(device, keyDefinition, keyIdentity, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    if (macros && ControlsAssignmentReader.TryReadMacro(device, dcs, out AssignmentDefinition? macroDefinition, out SpecialAction? macroIdentity))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddFromDefinition(device, macroDefinition, macroIdentity, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Special:
                    // 定義と、実行時の状態の入れ物（合成の SpecialAction）は、読み込みの部品が作る
                    if (_outputFactory != null && ControlsAssignmentReader.TryReadSpecial(device, dcs, out AssignmentDefinition? special, out SpecialAction? identity))
                    {
                        // 出力を作れない種別（窓口が未接続のテスト）は、束縛を 1 つも作らない（Extras も含め、dcs 全体を作らない）
                        TriggerBinding? binding = _assignmentBuilder.Build(device, special, identity, -1);
                        if (binding == null) break;

                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        result.Add(binding);
                    }
                    break;
                case DS4ControlSettings.ActionType.Button:
                    {
                        // Toggle つきは、ButtonMouse が有効なときだけ（Toggle の出力は ButtonMouse の区分）。出力先がパッドなら ButtonPad も要る
                        bool toggle = dcs.keyType.HasFlag(DS4KeyType.Toggle);
                        if (toggle && !mice) break;

                        if (ControlAssignmentInterpreter.TryInterpretMouse(dcs, out X360Controls mouseTarget))
                        {
                            if (!mice) break;
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);

                            // マウスのボタン等の Toggle も、SA の Button と同じ部品（ボタンごとの状態）で処理する。作れないときは、従来の束縛
                            if (toggle && ControlsAssignmentReader.TryReadMouseToggle(device, dcs, out AssignmentDefinition? mouseToggle, out SpecialAction? mouseToggleIdentity))
                            {
                                TriggerBinding? mouseToggleBinding = _assignmentBuilder.Build(device, mouseToggle, mouseToggleIdentity, -1);
                                if (mouseToggleBinding != null)
                                {
                                    result.Add(mouseToggleBinding);
                                    break;
                                }
                            }

                            AddMouse(device, dcs, mouseTarget, toggle, result);
                        }
                        else if (ControlAssignmentInterpreter.TryInterpretAnalogMouse(dcs, out X360Controls analogTarget))
                        {
                            if (!analogMice) break;
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);

                            // マウスの移動・アナログのホイールの Toggle も、SA の Button と同じ部品で処理する（以前は、Toggle が移動に効かなかった）。作れないときは、従来の束縛
                            if (toggle && ControlsAssignmentReader.TryReadAnalogMouseToggle(device, dcs, out AssignmentDefinition? analogToggle, out SpecialAction? analogToggleIdentity))
                            {
                                TriggerBinding? analogToggleBinding = _assignmentBuilder.Build(device, analogToggle, analogToggleIdentity, -1);
                                if (analogToggleBinding != null)
                                {
                                    result.Add(analogToggleBinding);
                                    break;
                                }
                            }

                            AddAnalogMouse(device, dcs, analogTarget, toggle, result);
                        }
                        else if (pads && ControlAssignmentInterpreter.TryInterpretPadTarget(dcs, out PadBindingValues pad, out bool eraseOnly))
                        {
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);

                            // パッドへの Toggle は、SA の Button と同じ部品（ButtonOutputAction）で処理する。
                            // 以前は ControlToggleOutputAction（マウスのボタンの Toggle の仕組み）しかなく、仮想パッドのボタンには Toggle が効かなかった。
                            // 部品を作れない（窓口が未接続のテスト）ときは、従来の束縛にする
                            if (toggle && ControlsAssignmentReader.TryReadPadToggle(device, dcs, out AssignmentDefinition? padToggle, out SpecialAction? padToggleIdentity))
                            {
                                TriggerBinding? padToggleBinding = _assignmentBuilder.Build(device, padToggle, padToggleIdentity, -1);
                                if (padToggleBinding != null)
                                {
                                    result.Add(padToggleBinding);
                                    break;
                                }
                            }

                            AddPad(device, dcs, pad, eraseOnly, toggle, result);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // 設計: docs-forDIMG/MadeByAgent/Archive/Phase8-Step7-8f-Plan.md §3.2。パッドへの割り当て。割り当てたボタンの通常の出力は、毎フレーム消す（Controls の規則）。値の出力は、評価されたフレームごとに、全 dcs のあと（Stage3 と同じ段階）で書く。
        // 出力先が Unbound／None のときは、出力を持たない束縛（消すだけ）
        private void AddPad(int device, DS4ControlSettings dcs, PadBindingValues values, bool eraseOnly, bool toggle, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Pad_{device}_{(int)dcs.control}", "Synthetic", "Pad", "Pad", 0, string.Empty);
            identity.type = "Pad";
            identity.details = dcs.action.actionBtn.ToString();   // ログの「maps to」に出る（出力の部品は values を使い、details は読まない）
            identity.controls = identity.name;

            // 立ち上がり・離したときのログはエンジンが出す。出力先が Unbound／None の消すだけの束縛は、ユーザーの操作ではないので出さない
            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonPad), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true, logsRisingEdge: !eraseOnly, logsReleaseEdge: !eraseOnly);
            var outputs = new List<IOutputAction>(2);
            if (!eraseOnly) outputs.Add(new ControlPadOutputAction(identity, values));
            if (toggle) outputs.Add(new ControlToggleOutputAction(identity));   // 7.8f-2（Q8＝A）。Toggle の出力は ButtonMouse の区分
            // パッドのボタンへの割り当て（デジタルな出力）は、SA と同じ閾値で判定する。軸・トリガーへの割り当て（アナログな出力）は、従来の判定のまま
            ControlOutputNature nature = ControlOutputNatureTable.ForPad(eraseOnly, values.Kind);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, toggle ? ControlBindingKind.ButtonMouse : ControlBindingKind.ButtonPad, nature),
                outputs.ToArray(), policy, identity, -1));
        }

        // 設計: docs-forDIMG/MadeByAgent/Archive/Phase8-Step7-8f-Plan.md §3.2（7.8f-3）。マウスの移動とアナログのホイール。割り当てたボタンの通常の出力は、毎フレーム消す。
        // 移動は、評価されたフレームごとに量を借りて加算器へ書く（Q6＝A）。ホイールは、成立中に束縛ごとの状態で進む（Q5＝B）。Toggle つきなら、Toggle の出力を足す
        private void AddAnalogMouse(int device, DS4ControlSettings dcs, X360Controls target, bool toggle, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_AnalogMouse_{device}_{(int)dcs.control}", "Synthetic", "AnalogMouse", "AnalogMouse", 0, string.Empty);
            identity.type = "AnalogMouse";
            identity.details = target.ToString();
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonAnalogMouse), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true, logsRisingEdge: true);
            var outputs = new List<IOutputAction>(2);
            switch (target)
            {
                case X360Controls.MouseUp: outputs.Add(new ControlMouseMoveOutputAction(identity, dcs.control, 0)); break;
                case X360Controls.MouseDown: outputs.Add(new ControlMouseMoveOutputAction(identity, dcs.control, 1)); break;
                case X360Controls.MouseLeft: outputs.Add(new ControlMouseMoveOutputAction(identity, dcs.control, 2)); break;
                case X360Controls.MouseRight: outputs.Add(new ControlMouseMoveOutputAction(identity, dcs.control, 3)); break;
                default: outputs.Add(new ControlAnalogWheelOutputAction(identity, dcs.control, target == X360Controls.WDOWN, _profileSettings)); break;
            }

            if (toggle) outputs.Add(new ControlToggleOutputAction(identity));
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonAnalogMouse, ControlOutputNatureTable.ForKind(ControlBindingKind.ButtonAnalogMouse)),
                outputs.ToArray(), policy, identity, -1));
        }

        // 設計: docs-forDIMG/MadeByAgent/Archive/Phase8-Step7-8f-Plan.md §3.2（7.8f-2）。マウスのボタン・タッチパッドクリック・デジタルのホイール。割り当てたボタンの通常の出力は、毎フレーム消す。
        // マウスのボタンとホイールは Controls の判定。タッチパッドクリックの押下は SA と同じ判定で、出力が自分で見る。Toggle つきなら、Toggle の出力を足す（Q8＝A）
        private void AddMouse(int device, DS4ControlSettings dcs, X360Controls target, bool toggle, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Mouse_{device}_{(int)dcs.control}", "Synthetic", "Mouse", "Mouse", 0, string.Empty);
            identity.type = "Mouse";
            identity.details = target.ToString();
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonMouse), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true, logsRisingEdge: true);
            var outputs = new List<IOutputAction>(2);
            if (target == X360Controls.TouchpadClick) outputs.Add(new ControlTouchpadClickOutputAction(identity, dcs.control));
            else outputs.Add(new ControlMouseOutputAction(identity, target));
            if (toggle) outputs.Add(new ControlToggleOutputAction(identity));
            // マウスのボタン・タッチパッドクリック・デジタルのホイール（デジタルな出力）は、SA と同じ閾値で判定する
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonMouse, ControlOutputNatureTable.ForKind(ControlBindingKind.ButtonMouse)),
                outputs.ToArray(), policy, identity, -1));
        }

        // 設計: docs-forDIMG/MadeByAgent/Archive/Phase8-Step7-8f-Plan.md §3.2（Q2＝A）。Default の軸の素通し。Default は通常の出力を消さない（消すのは、反対側の向きの割り当て）
        private void AddPassthrough(int device, DS4ControlSettings dcs, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_PadPassthrough_{device}_{(int)dcs.control}", "Synthetic", "PadPassthrough", "PadPassthrough", 0, string.Empty);
            identity.type = "PadPassthrough";
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonPad), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false, logsReleaseEdge: false);   // 素通しは、ユーザーの操作ではないので、ログを出さない
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonPad, ControlOutputNatureTable.ForKind(ControlBindingKind.ButtonPad)),
                new IOutputAction[] { new ControlPassthroughOutputAction(identity, dcs.control) }, policy, identity, -1));
        }

        // 設計: docs-forDIMG/MadeByAgent/Archive/Phase8-Step7-8e-Plan.md §3.2。Extras は Controls の判定（Assignment）で、通常の割り当てとは別の束縛にする。
        // Fresh-press ガードなし、トリガーのボタンを抑制せず、通常の出力も消さない（消すのは通常の割り当ての束縛）。離したとき Stop（元に戻す）
        private void AddExtras(int device, DS4ControlSettings dcs, ExtrasBindingValues values, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Extras_{device}_{(int)dcs.control}", "Synthetic", "Extras", "Extras", 0, string.Empty);
            identity.type = "Extras";
            identity.details = dcs.extras;
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.Extras), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false, logsRisingEdge: true);
            // Extras（振動・ライトバー・感度。オン／オフの出力）は、SA と同じ閾値で判定する
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Extras, ControlOutputNatureTable.ForKind(ControlBindingKind.Extras)),
                new IOutputAction[] { new ControlExtrasOutputAction(identity, values, _extrasPort!) }, policy, identity, -1));   // Extras の束縛を作るのは、窓口を渡された生成だけ
        }

        // Key と Macro は、読み込みの部品（定義と、実行時の状態の入れ物）と、出力の部品（種別 × 由来の方針）を通して作る。
        // 設計書 §3.2: Controls タブの Key は、Fresh-press ガードを見ず、トリガーのボタンを抑制せず、押していなくても毎フレーム通常の出力を消す。
        // Macro は SA と同じ仕組み（MacroAction、SA と同じ押下の判定、ActionInstanceState）に載せ、Fresh-press ガードは SA と同じく見る。割り当てたボタンの通常の出力は、毎フレーム消す
        private void AddFromDefinition(int device, AssignmentDefinition definition, SpecialAction identity, List<IActionBinding> result)
        {
            // Key・Macro（デジタルな出力）は、SA と同じ閾値で判定する。Macro の「Run on trigger release」は、離したときに成立する入力にする
            // 入力の組み立て（区分、判定の性質、離したとき成立）は AssignmentBindingBuilder が行う
            TriggerBinding? binding = _assignmentBuilder.Build(device, definition, identity, -1);
            if (binding != null) result.Add(binding);
        }
    }
}
