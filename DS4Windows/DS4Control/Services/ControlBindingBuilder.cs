using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md §3.2）: <see cref="IControlBindingBuilder"/> の実装（DI の Singleton）。
    /// Controls タブの割り当て（<c>ControlSettingsGroup</c>）のうち、各区分の条件を満たす割り当てだけを、束縛にする。
    /// 条件を満たさない割り当ては束縛を作らない（Phase8-Step8: 旧経路は削除済みのため、束縛なし＝何もしない。Q2＝C）。Phase8-Step8 8d: 区分ごとの移行のスイッチ（<c>BindingMigrationOptions</c>）は削除した。
    /// <para>
    /// Key（7.8b）の条件: 通常の割り当てが Key、<c>actionKey</c> が <c>ushort</c> の範囲内（シフトは 7.8d、Lightbar マクロは 7.8e で廃止した）。
    /// Extras（7.8e）は、通常の割り当て（Default／Key／Macro）と直交する部品で、別の束縛にする（<c>Synthetic_Extras_{device}_{control}</c>。通常の割り当ての束縛の前に並べる）。
    /// dcs が Extras を持つとき、Extras の区分が移行済みで、値が読める（9 個以上）ときだけ、dcs 全体を新経路が処理する。そうでなければ、束縛を 1 つも作らず、旧経路が全体を処理する。
    /// </para>
    /// 「何をするか」の解釈（キー番号、Toggle、ScanCode、別名キー）はここで行い、出力（<c>KeyOutputAction</c>）には解釈済みの値を渡す（Q9＝A）。
    /// 識別用の <c>SpecialAction</c>（状態の鍵）は、旧経路の合成と同じ名前（<c>Synthetic_Key_{device}_{control}</c>）で作る。
    /// 旧経路の <c>FindSpecialActionForLogicalKey</c> による Key 特殊アクションとの状態の共有は、再現しない（Q4＝B。承認済みの動作変更）。
    /// </summary>
    public sealed class ControlBindingBuilder : IControlBindingBuilder
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMacroPlayer _macroPlayer;   // Macro（7.8c）。省略時（テスト）は、Macro の束縛を作らず、旧経路が処理する
        private readonly IControlExtrasPort _extrasPort;   // Extras（7.8e）。省略時（テスト）は、Extras を持つ割り当ての束縛を作らず、旧経路が処理する
        private readonly ISpecialActionOutputFactory _outputFactory;   // Phase10-Step4 W4: SA 専用の種別（Special）の出力と方針。省略時（テスト）は、Special の束縛を作らない
        private readonly AssignmentBindingBuilder _assignmentBuilder;   // Phase10-Step6 I4c: Key／Macro／Special の束縛の組み立て（SA タブ側と同じ部品）
        private readonly ISpecialActionOutputFactory _controlsFactory;   // Phase10-Step6 I4b: Key と Macro 用。_outputFactory がなければ、最小の部品（Key と Macro だけを作る）

        /// <summary>Program の引数に書くと、ウィンドウを隠して起動する語（SA の仕様。大文字小文字不問）。</summary>
        public const string HiddenKeyword = "$hidden";

        public ControlBindingBuilder(IProfileSettingsService profileSettings, IMacroPlayer macroPlayer = null,
            IControlExtrasPort extrasPort = null, ISpecialActionOutputFactory outputFactory = null)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _macroPlayer = macroPlayer;
            _extrasPort = extrasPort;
            _outputFactory = outputFactory;
            _controlsFactory = SpecialActionOutputFactory.ForControlsKeyAndMacro(_profileSettings, macroPlayer);
            _assignmentBuilder = new AssignmentBindingBuilder(_outputFactory ?? _controlsFactory, _profileSettings);   // Phase10-Step6 I4c: 束縛の組み立ては、SA タブ側と同じ部品
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

        /// <summary>
        /// Phase8-Step7 7.8c: Macro の新経路の条件（Key と同じ形。Macro、手順あり、Extras なし）を満たすか。満たすとき、手順を返す。
        /// </summary>
        public static bool TryInterpretMacro(DS4ControlSettings dcs, out int[] steps)
        {
            steps = null;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Macro) return false;

            int[] macro = dcs.action.actionMacro;
            // Phase10-Step3（ユーザー承認、2026-10-21）: 手順が空のマクロも、新経路が所有する（SA の Macro と同じ。トリガーが成立すると、空のマクロを実行し、何も出力しない。
            // 通常の出力は、他の割り当てと同じく毎フレーム消える）。以前は「旧経路が、手順がないと例外になる」ため所有しなかったが、旧経路の本体は
            // Phase8-Step8 8a で削除済みで、所有しないと、割り当てたのに通常の出力が出てしまっていた。手順そのものがない（null）ものは、従来どおり所有しない
            if (macro == null) return false;

            steps = macro;
            return true;
        }

        /// <summary>
        /// 割り当てが、Key の新経路の条件（クラスの説明）を満たすか。満たすときは、解釈済みの値を返す。
        /// </summary>
        public static bool TryInterpretKey(DS4ControlSettings dcs, out KeyBindingValues values)
        {
            values = default;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Key) return false;

            int key = dcs.action.actionKey;
            if (key < ushort.MinValue || key > ushort.MaxValue) return false;   // 旧経路は範囲外で例外になる。新経路は所有せず、旧経路の挙動を保つ

            DS4KeyType keyType = dcs.keyType;
            values = new KeyBindingValues((ushort)key, keyType.HasFlag(DS4KeyType.ScanCode), keyType.HasFlag(DS4KeyType.Toggle),
                dcs.action.actionAlias);
            return true;
        }

        /// <summary>
        /// Phase8-Step7 7.8e: Extras（カンマ区切りの 9 個の値）の解釈。空（<c>IsExtrasEmpty</c>）なら偽（Extras なし）。
        /// 9 個未満だと、旧経路は途中の欄の参照で例外になり、そこまでの効果だけが残る。その挙動を保つため、新経路は所有せず、偽を返す（旧経路が処理する）。
        /// 数値として読めない欄は 0（旧経路の <c>int.TryParse</c> の失敗と同じ）。
        /// </summary>
        public static bool TryInterpretExtras(string extras, out ExtrasBindingValues values)
        {
            values = default;
            if (string.IsNullOrEmpty(extras) || extras == "0,0,0,0,0,0,0,0,0") return false;

            string[] parts = extras.Split(',');
            if (parts.Length < 9) return false;

            var numbers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int n)) numbers[i] = n;
            }

            values = new ExtrasBindingValues(numbers[0], numbers[1], numbers[2] == 1, numbers[3], numbers[4], numbers[5], numbers[6], numbers[7] == 1, numbers[8]);
            return true;
        }

        private void Add(int device, List<DS4ControlSettings> settings, bool keys, bool macros, bool extras, bool pads, bool mice, bool analogMice, List<IActionBinding> result)
        {
            if (settings == null) return;
            for (int i = 0; i < settings.Count; i++)
                Add(device, settings[i], keys, macros, extras, pads, mice, analogMice, result);
        }

        /// <summary>
        /// Phase8-Step7 7.8f-1: Button のうち、パッド（X360 の軸・トリガー・ボタン）への割り当ての解釈。出力先が Unbound／None なら、消すだけ（<paramref name="eraseOnly"/>）。
        /// マウス・ホイール・タッチパッドクリックへの割り当て（7.8f-2・7.8f-3）と、Toggle つきは偽（Toggle つきは、<see cref="TryInterpretPadTarget"/> と Toggle の出力を組み合わせて扱う［7.8f-2］）。
        /// </summary>
        public static bool TryInterpretPad(DS4ControlSettings dcs, out PadBindingValues values, out bool eraseOnly)
        {
            values = default;
            eraseOnly = false;
            if (dcs == null) return false;
            if (dcs.keyType.HasFlag(DS4KeyType.Toggle)) return false;
            return TryInterpretPadTarget(dcs, out values, out eraseOnly);
        }

        /// <summary>Phase8-Step7 7.8f-2: <see cref="TryInterpretPad"/> と同じ解釈で、Toggle の有無を見ない（Toggle は別の出力が担う）。</summary>
        public static bool TryInterpretPadTarget(DS4ControlSettings dcs, out PadBindingValues values, out bool eraseOnly)
        {
            values = default;
            eraseOnly = false;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            if (xbox == X360Controls.None || xbox == X360Controls.Unbound)
            {
                eraseOnly = true;
                return true;
            }

            if (xbox < X360Controls.LXNeg || xbox > X360Controls.Start) return false;

            DS4Controls output = X360ControlTables.reverseX360ButtonMapping[(int)xbox];
            PadTargetKind kind = output >= DS4Controls.LXNeg && output <= DS4Controls.RYPos ? PadTargetKind.Axis
                : output == DS4Controls.L2 || output == DS4Controls.R2 ? PadTargetKind.Trigger
                : PadTargetKind.Button;
            values = new PadBindingValues(dcs.control, output, kind);
            return true;
        }

        /// <summary>
        /// Phase8-Step7 7.8f-2: Button のうち、マウスのボタン（左・右・中・4・5）、タッチパッドクリック、デジタルのホイール（元がボタン）への割り当ての解釈。
        /// Toggle の有無は見ない（Toggle は別の出力が担う）。元がスティック・トリガー・ジャイロのホイール（アナログ）と、マウスの移動は偽（7.8f-3。旧経路が処理する）。
        /// </summary>
        public static bool TryInterpretMouse(DS4ControlSettings dcs, out X360Controls target)
        {
            target = X360Controls.None;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            switch (xbox)
            {
                case X360Controls.TouchpadClick:
                case X360Controls.LeftMouse:
                case X360Controls.RightMouse:
                case X360Controls.MiddleMouse:
                case X360Controls.FourthMouse:
                case X360Controls.FifthMouse:
                    target = xbox;
                    return true;
                case X360Controls.WUP:
                case X360Controls.WDOWN:
                    // 旧経路の isAnalog と同じ: 元が軸・トリガー・ジャイロならアナログのホイール
                    DS4Controls source = dcs.control;
                    bool analog = (source >= DS4Controls.LXNeg && source <= DS4Controls.RYPos)
                        || source == DS4Controls.L2 || source == DS4Controls.R2
                        || (source >= DS4Controls.GyroXPos && source <= DS4Controls.GyroZNeg);
                    if (analog) return false;
                    target = xbox;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Phase8-Step7 7.8f-3: Button のうち、マウスの移動（MouseUp／Down／Left／Right）と、元がスティック・トリガー・ジャイロのホイール（アナログ）への割り当ての解釈。
        /// Toggle の有無は見ない。元がボタンのホイール（デジタル）は <see cref="TryInterpretMouse"/> が扱う。
        /// </summary>
        public static bool TryInterpretAnalogMouse(DS4ControlSettings dcs, out X360Controls target)
        {
            target = X360Controls.None;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            switch (xbox)
            {
                case X360Controls.MouseUp:
                case X360Controls.MouseDown:
                case X360Controls.MouseLeft:
                case X360Controls.MouseRight:
                    target = xbox;
                    return true;
                case X360Controls.WUP:
                case X360Controls.WDOWN:
                    // TryInterpretMouse が偽（元がアナログ）のものだけ
                    if (TryInterpretMouse(dcs, out _)) return false;
                    target = xbox;
                    return true;
                default:
                    return false;
            }
        }

        // 軸の Default で、反対側の向きが割り当て済み（Key／Macro／Button のどれでも、割り当てたボタンの消去が軸のペアの両方を中立にする）なら、素通しが要る
        private bool NeedsPassthrough(int device, DS4ControlSettings dcs)
        {
            int control = (int)dcs.control;
            if (control < 0 || control >= DS4StateFieldMapping.mappedType.Length) return false;
            if (DS4StateFieldMapping.mappedType[control] != DS4StateFieldMapping.ControlType.AxisDir) return false;

            int relation = control % 2 == 0 ? control - 1 : control + 1;
            DS4ControlSettings partner = _profileSettings.GetDS4CSetting(device, (DS4Controls)relation);
            return partner != null && partner.actionType != DS4ControlSettings.ActionType.Default;
        }

        // dcs の部品（Extras、通常の割り当て、Default の素通し）は、すべて新経路で扱えるときだけ束縛にする。一部でも扱えなければ、1 つも作らず、旧経路が dcs 全体を処理する
        private void Add(int device, DS4ControlSettings dcs, bool keys, bool macros, bool extras, bool pads, bool mice, bool analogMice, List<IActionBinding> result)
        {
            if (dcs == null) return;

            bool hasExtras = !dcs.IsExtrasEmpty(dcs.extras);
            ExtrasBindingValues extrasValues = default;
            if (hasExtras && (!extras || !TryInterpretExtras(dcs.extras, out extrasValues))) return;

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
                    if (keys && ControlsAssignmentReader.TryReadKey(device, dcs, out AssignmentDefinition keyDefinition, out SpecialAction keyIdentity))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddFromDefinition(device, keyDefinition, keyIdentity, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    if (macros && ControlsAssignmentReader.TryReadMacro(device, dcs, out AssignmentDefinition macroDefinition, out SpecialAction macroIdentity))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddFromDefinition(device, macroDefinition, macroIdentity, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Special:
                    // Phase10-Step6 I3: 定義と、実行時の状態の入れ物（合成の SpecialAction）は、読み込みの部品が作る
                    if (_outputFactory != null && ControlsAssignmentReader.TryReadSpecial(device, dcs, out AssignmentDefinition special, out SpecialAction identity))
                    {
                        // 出力を作れない種別（窓口が未接続のテスト）は、束縛を 1 つも作らない（Extras も含め、dcs 全体を作らない）
                        TriggerBinding binding = _assignmentBuilder.Build(device, special, identity, -1);
                        if (binding == null) break;

                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        result.Add(binding);
                    }
                    break;
                case DS4ControlSettings.ActionType.Button:
                    {
                        // Phase8-Step7 7.8f-2: Toggle つきは、ButtonMouse が有効なときだけ（Toggle の出力は ButtonMouse の区分）。出力先がパッドなら ButtonPad も要る
                        bool toggle = dcs.keyType.HasFlag(DS4KeyType.Toggle);
                        if (toggle && !mice) break;

                        if (TryInterpretMouse(dcs, out X360Controls mouseTarget))
                        {
                            if (!mice) break;
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                            AddMouse(device, dcs, mouseTarget, toggle, result);
                        }
                        else if (TryInterpretAnalogMouse(dcs, out X360Controls analogTarget))
                        {
                            if (!analogMice) break;
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                            AddAnalogMouse(device, dcs, analogTarget, toggle, result);
                        }
                        else if (pads && TryInterpretPadTarget(dcs, out PadBindingValues pad, out bool eraseOnly))
                        {
                            if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                            AddPad(device, dcs, pad, eraseOnly, toggle, result);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Phase10-Step4 W4: SA 専用の種別の割り当てが、新経路の条件（<see cref="ControlsSpecialTypeTable"/> の種別で、型のある項目を持つ）を満たすか。
        /// </summary>
        public static bool TryInterpretSpecial(DS4ControlSettings dcs, out ControlSpecialItem item)
        {
            item = null;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Special) return false;
            if (dcs.special == null || !ControlsSpecialTypeTable.IsSupported(dcs.special.Type)) return false;

            // Phase10-Step4 W6: 切り替え先のない Load Profile は、束縛を作らない（作ると、切り替えに失敗し続けながら、割り当てたボタンの元の出力だけが消える）
            if (dcs.special.Type == SpecialAction.ActionTypeId.Profile && string.IsNullOrWhiteSpace(dcs.special.ProfileName)) return false;

            item = dcs.special;
            return true;
        }

        // 設計: Phase8-Step7-8f-Plan.md §3.2。パッドへの割り当て。割り当てたボタンの通常の出力は、毎フレーム消す（Controls の規則）。値の出力は、評価されたフレームごとに、全 dcs のあと（Stage3 と同じ段階）で書く。
        // 出力先が Unbound／None のときは、出力を持たない束縛（消すだけ）
        private void AddPad(int device, DS4ControlSettings dcs, PadBindingValues values, bool eraseOnly, bool toggle, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Pad_{device}_{(int)dcs.control}", "Synthetic", "Pad", "Pad", 0, string.Empty);
            identity.type = "Pad";
            identity.details = dcs.action.actionBtn.ToString();   // ログの「maps to」に出る（出力の部品は values を使い、details は読まない）
            identity.controls = identity.name;

            // Phase10-Step4c W2: 立ち上がり・離したときのログはエンジンが出す。出力先が Unbound／None の消すだけの束縛は、ユーザーの操作ではないので出さない
            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonPad), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true, logsRisingEdge: !eraseOnly, logsReleaseEdge: !eraseOnly);
            var outputs = new List<IOutputAction>(2);
            if (!eraseOnly) outputs.Add(new ControlPadOutputAction(identity, values));
            if (toggle) outputs.Add(new ControlToggleOutputAction(identity));   // 7.8f-2（Q8＝A）。Toggle の出力は ButtonMouse の区分
            // Phase10-Step4c W4（ユーザー承認、2026-10-21）: パッドのボタンへの割り当て（デジタルな出力）は、SA と同じ閾値で判定する。軸・トリガーへの割り当て（アナログな出力）は、従来の判定のまま
            ControlOutputNature nature = ControlOutputNatureTable.ForPad(eraseOnly, values.Kind);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, toggle ? ControlBindingKind.ButtonMouse : ControlBindingKind.ButtonPad, nature),
                outputs.ToArray(), policy, identity, -1));
        }

        // 設計: Phase8-Step7-8f-Plan.md §3.2（7.8f-3）。マウスの移動とアナログのホイール。割り当てたボタンの通常の出力は、毎フレーム消す。
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

        // 設計: Phase8-Step7-8f-Plan.md §3.2（7.8f-2）。マウスのボタン・タッチパッドクリック・デジタルのホイール。割り当てたボタンの通常の出力は、毎フレーム消す。
        // マウスのボタンとホイールは Controls の判定（旧 GetBoolActionMapping）。タッチパッドクリックの押下は SA と同じ判定（旧 GetBoolMapping）で、出力が自分で見る。Toggle つきなら、Toggle の出力を足す（Q8＝A）
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
            // Phase10-Step4c W4: マウスのボタン・タッチパッドクリック・デジタルのホイール（デジタルな出力）は、SA と同じ閾値で判定する
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonMouse, ControlOutputNatureTable.ForKind(ControlBindingKind.ButtonMouse)),
                outputs.ToArray(), policy, identity, -1));
        }

        // 設計: Phase8-Step7-8f-Plan.md §3.2（Q2＝A）。Default の軸の素通し。Default は通常の出力を消さない（消すのは、反対側の向きの割り当て）
        private void AddPassthrough(int device, DS4ControlSettings dcs, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_PadPassthrough_{device}_{(int)dcs.control}", "Synthetic", "PadPassthrough", "PadPassthrough", 0, string.Empty);
            identity.type = "PadPassthrough";
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.ButtonPad), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false, logsReleaseEdge: false);   // Phase10-Step4c W2: 素通しは、ユーザーの操作ではないので、ログを出さない
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonPad, ControlOutputNatureTable.ForKind(ControlBindingKind.ButtonPad)),
                new IOutputAction[] { new ControlPassthroughOutputAction(identity, dcs.control) }, policy, identity, -1));
        }

        // 設計: Phase8-Step7-8e-Plan.md §3.2。Extras は Controls の判定（Assignment）で、通常の割り当てとは別の束縛にする。
        // Fresh-press ガードなし、トリガーのボタンを抑制せず、通常の出力も消さない（消すのは通常の割り当ての束縛）。離したとき Stop（元に戻す）
        private void AddExtras(int device, DS4ControlSettings dcs, ExtrasBindingValues values, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Extras_{device}_{(int)dcs.control}", "Synthetic", "Extras", "Extras", 0, string.Empty);
            identity.type = "Extras";
            identity.details = dcs.extras;
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.Extras), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false, logsRisingEdge: true);
            // Phase10-Step4c W4: Extras（振動・ライトバー・感度。オン／オフの出力）は、SA と同じ閾値で判定する
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Extras, ControlOutputNatureTable.ForKind(ControlBindingKind.Extras)),
                new IOutputAction[] { new ControlExtrasOutputAction(identity, values, _extrasPort) }, policy, identity, -1));
        }

        // Phase10-Step6 I4b: Key と Macro は、読み込みの部品（定義と、実行時の状態の入れ物）と、出力の部品（種別 × 由来の方針）を通して作る。
        // 設計書 §3.2: Controls タブの Key は、Fresh-press ガードを見ず、トリガーのボタンを抑制せず、押していなくても毎フレーム通常の出力を消す（旧 ResetToDefaultValue）。
        // Macro は SA と同じ仕組み（MacroAction、SA と同じ押下の判定、ActionInstanceState）に載せ、Fresh-press ガードは SA と同じく見る。割り当てたボタンの通常の出力は、毎フレーム消す
        private void AddFromDefinition(int device, AssignmentDefinition definition, SpecialAction identity, List<IActionBinding> result)
        {
            // Phase10-Step4c W4: Key・Macro（デジタルな出力）は、SA と同じ閾値で判定する。Macro の「Run on trigger release」は、離したときに成立する入力にする
            // Phase10-Step6 I4c: 入力の組み立て（区分、判定の性質、離したとき成立）は AssignmentBindingBuilder が行う
            TriggerBinding binding = _assignmentBuilder.Build(device, definition, identity, -1);
            if (binding != null) result.Add(binding);
        }
    }
}
