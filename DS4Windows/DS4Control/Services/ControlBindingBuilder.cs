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

        /// <summary>
        /// Phase10-Step4 W4: Controls に割り当てられる、SA 専用の種別（<see cref="DS4ControlSettings.ActionType.Special"/>）のうち、束縛を作る種別。
        /// 群ごとの縦通し（Phase10-Step4-Plan.md §5）で、保存・実行・画面が通ったものから足す。Macro は Macro の割り当て（3 項目の入れ物）で扱うため含めない。
        /// </summary>
        private static readonly SpecialAction.ActionTypeId[] BuiltSpecialTypes =
        {
            SpecialAction.ActionTypeId.Program,
            SpecialAction.ActionTypeId.Profile,
            SpecialAction.ActionTypeId.DisconnectBT,
            SpecialAction.ActionTypeId.GyroCalibrate,
            SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate,
        };

        /// <summary>Program の引数に書くと、ウィンドウを隠して起動する語（SA の仕様。大文字小文字不問）。</summary>
        public const string HiddenKeyword = "$hidden";

        public ControlBindingBuilder(IProfileSettingsService profileSettings, IMacroPlayer macroPlayer = null,
            IControlExtrasPort extrasPort = null, ISpecialActionOutputFactory outputFactory = null)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _macroPlayer = macroPlayer;
            _extrasPort = extrasPort;
            _outputFactory = outputFactory;
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
                    if (keys && TryInterpretKey(dcs, out KeyBindingValues values))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddKey(device, dcs, values, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    if (macros && TryInterpretMacro(dcs, out int[] steps))
                    {
                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddMacro(device, dcs, steps, result);
                    }
                    break;
                case DS4ControlSettings.ActionType.Special:
                    if (TryInterpretSpecial(dcs, out ControlSpecialItem special) && _outputFactory != null)
                    {
                        // 出力を作れない種別（窓口が未接続のテスト）は、束縛を 1 つも作らない（Extras も含め、dcs 全体を作らない）
                        var identity = CreateSpecialIdentity(device, dcs, special);
                        SpecialActionOutputs outputs = _outputFactory.Create(device, identity, -1, forControls: true);
                        if (outputs == null) break;

                        if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                        AddSpecial(dcs, identity, special, outputs, result);
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
        /// Phase10-Step4 W4: SA 専用の種別の割り当てが、新経路の条件（<see cref="BuiltSpecialTypes"/> の種別で、型のある項目を持つ）を満たすか。
        /// </summary>
        public static bool TryInterpretSpecial(DS4ControlSettings dcs, out ControlSpecialItem item)
        {
            item = null;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Special) return false;
            if (dcs.special == null || Array.IndexOf(BuiltSpecialTypes, dcs.special.Type) < 0) return false;

            // Phase10-Step4 W6: 切り替え先のない Load Profile は、束縛を作らない（作ると、切り替えに失敗し続けながら、割り当てたボタンの元の出力だけが消える）
            if (dcs.special.Type == SpecialAction.ActionTypeId.Profile && string.IsNullOrWhiteSpace(dcs.special.ProfileName)) return false;

            item = dcs.special;
            return true;
        }

        // 設計: Phase10-Step4-Plan.md §3.3。SA 専用の割り当てを、合成の SpecialAction（SA と同じ出力アクションが受け取る形）で表す。
        // トリガーは、そのボタン 1 つ。押し続ける秒数は delayTime（入力が SA と同じ規則で見る）
        private static SpecialAction CreateSpecialIdentity(int device, DS4ControlSettings dcs, ControlSpecialItem item)
        {
            var identity = new SpecialAction($"Synthetic_Special_{device}_{(int)dcs.control}", "Synthetic", "Special", string.Empty, 0, string.Empty);
            identity.typeID = item.Type;
            identity.type = item.Type.ToString();
            identity.controls = identity.name;
            identity.delayTime = item.HoldSeconds;
            identity.trigger = new List<DS4Controls> { dcs.control };

            if (item.Type == SpecialAction.ActionTypeId.Profile)
            {
                // Phase10-Step4 W6: 切り替え先を details、「離したら戻る」を automaticUntrigger に入れる（SA の Profile と同じ形）。
                // 復帰は、エンジンが、この SpecialAction の trigger（そのボタン）の離れを見て行う。IsTemporaryProfileAction は automaticUntrigger から決まる
                identity.details = item.ProfileName;
                identity.automaticUntrigger = item.AutomaticUntrigger;
            }

            if (item.Type == SpecialAction.ActionTypeId.Program)
            {
                // Phase10-Step4 W5: Program は、パスを details、引数を extra に入れる（SA の起動処理が読む形）。ウィンドウを隠すときは、extra の先頭に $hidden を付ける
                // （起動処理は、この語を 1 回取り除いて、残りを引数にする。結果の引数は、型のある項目の ProgramArguments と同じ）
                identity.details = item.ProgramPath ?? string.Empty;
                identity.extra = (item.HideWindow ? HiddenKeyword : string.Empty) + (item.ProgramArguments ?? string.Empty);
            }

            return identity;
        }

        // 入力は、Controls の 1 ボタン（SA と同じ閾値の判定。押し続ける秒数つき）。方針は、factory の Controls 用（抑制も消去もせず、割り当てたボタンの出力を毎フレーム消す）
        private void AddSpecial(DS4ControlSettings dcs, SpecialAction identity, ControlSpecialItem item, SpecialActionOutputs outputs, List<IActionBinding> result)
        {
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Special, ControlJudgement.Trigger,
                    delaySeconds: item.HoldSeconds, profileSettings: _profileSettings),
                outputs.Outputs, outputs.Policy, identity, -1));
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
            ControlJudgement judgement = !eraseOnly && values.Kind == PadTargetKind.Button ? ControlJudgement.Trigger : ControlJudgement.Assignment;
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, toggle ? ControlBindingKind.ButtonMouse : ControlBindingKind.ButtonPad, judgement),
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
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonAnalogMouse, ControlJudgement.Assignment),
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
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonMouse, ControlJudgement.Trigger),
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
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.ButtonPad, ControlJudgement.Assignment),
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
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Extras, ControlJudgement.Trigger),
                new IOutputAction[] { new ControlExtrasOutputAction(identity, values, _extrasPort) }, policy, identity, -1));
        }

        // 設計書 §3.2（Phase8-Step7-8c-Plan.md §3.2）: Controls タブの Macro は、SA タブの Macro と同じ仕組み（MacroAction、SA と同じ押下の判定、ActionInstanceState）に載せる。
        // 手順と keyType の解釈はここ（変換層）で行い、識別用の SpecialAction の項目として渡す。Fresh-press ガードは SA と同じく見る。割り当てたボタンの通常の出力は、毎フレーム消す（Controls の規則）
        private void AddMacro(int device, DS4ControlSettings dcs, int[] steps, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Macro_{device}_{(int)dcs.control}", "Synthetic", "Macro", string.Join("/", steps), 0, string.Empty);
            identity.typeID = SpecialAction.ActionTypeId.Macro;
            identity.type = "Macro";
            identity.keyType = dcs.keyType;
            identity.controls = identity.name;   // 同期実行のキューの識別子（RepeatMacro のマクロは、この名前ごとのキューで動く）。ボタンごとに別にする

            // Phase10-Step4 W3: Macro の SA 専用の 3 項目（型のある項目。Run on trigger release／Synchronized Run／Keep key state）を、SA と同じ項目へ写す。
            // 項目がなければ（従来の Macro）、すべて偽のまま。再生側（PlayMacro）は、SA と同じく synchronized／keepKeyState を見る
            ControlSpecialItem options = dcs.special != null && dcs.special.Type == SpecialAction.ActionTypeId.Macro ? dcs.special : null;
            identity.pressRelease = options != null && options.RunOnRelease;
            identity.synchronized = options != null && options.Synchronized;
            identity.keepKeyState = options != null && options.KeepKeyState;

            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.Macro), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Macro, ControlJudgement.Trigger,
                    invertOnRelease: identity.pressRelease),
                new IOutputAction[] { new MacroAction(identity, device, _macroPlayer, -1, null) }, policy, identity, -1));   // Phase10-Step4c W2: SA と同じ立ち上がりのログを出す版
        }

        private void AddKey(int device, DS4ControlSettings dcs, KeyBindingValues values, List<IActionBinding> result)
        {

            // 旧経路の合成 SpecialAction（Mapping.GetOrCreateSyntheticKeyAction）と同じ名前・種別。状態は名前で引かれる。解釈は値で渡すため、ここは識別用
            var identity = new SpecialAction($"Synthetic_Key_{device}_{(int)dcs.control}", "Synthetic", "Key", "Key", 0, string.Empty);
            identity.typeID = SpecialAction.ActionTypeId.Key;
            identity.type = "Key";
            identity.details = values.LogicalKey.ToString();
            DS4KeyType keyType = DS4KeyType.None;
            if (values.ScanCode) keyType |= DS4KeyType.ScanCode;
            if (values.Toggle) keyType |= DS4KeyType.Toggle;
            identity.keyType = keyType;

            // 設計書 §3.2: Controls タブの Key は、Fresh-press ガードを見ず、トリガーのボタンを抑制せず、押していなくても毎フレーム通常の出力を消す（旧 ResetToDefaultValue）
            var policy = new BindingPolicy(respectsFreshPressGuard: FreshPressGuardTable.ForControls(ControlBindingKind.Key), suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true);
            // Phase10-Step4c W4: Key（デジタルな出力）は、SA と同じ閾値で判定する（以前は Controls の判定）
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Key, ControlJudgement.Trigger),
                new IOutputAction[] { new KeyOutputAction(identity, values, _profileSettings) }, policy, identity, -1));
        }
    }
}
