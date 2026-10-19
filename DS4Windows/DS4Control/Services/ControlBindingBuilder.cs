using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md §3.2）: <see cref="IControlBindingBuilder"/> の実装（DI の Singleton）。
    /// Controls タブの割り当て（<c>ControlSettingsGroup</c>）のうち、移行済みの区分（<see cref="BindingMigrationOptions.MigratedControlKinds"/>）の条件を満たす割り当てだけを、束縛にする。
    /// 条件を満たさない割り当ては束縛を作らず、旧経路が全体を処理する（Q2＝A: ボタン単位の所有）。移行済みの区分が空の間は、何も読まずに空のリストを返す。
    /// <para>
    /// Key（7.8b）の条件: 通常の割り当てが Key、<c>actionKey</c> が <c>ushort</c> の範囲内（シフトは 7.8d、Lightbar マクロは 7.8e で廃止した）。
    /// Extras（7.8e）は、通常の割り当て（Default／Key／Macro）と直交する部品で、別の束縛にする（<c>Synthetic_Extras_{device}_{control}</c>。通常の割り当ての束縛の前に並べる）。
    /// dcs が Extras を持つとき、Extras の区分が移行済みで、値が読める（9 個以上）ときだけ、dcs 全体を新経路が処理する。そうでなければ、束縛を 1 つも作らず、旧経路が全体を処理する。
    /// ジャイロスワイプの割り当て（<c>GyroSwipe*</c>）は、前の方向を先に離す旧経路の順序に依存するため、対象にしない（旧経路が処理する）。
    /// </para>
    /// 「何をするか」の解釈（キー番号、Toggle、ScanCode、別名キー）はここで行い、出力（<c>KeyOutputAction</c>）には解釈済みの値を渡す（Q9＝A）。
    /// 識別用の <c>SpecialAction</c>（状態の鍵）は、旧経路の合成と同じ名前（<c>Synthetic_Key_{device}_{control}</c>）で作る。
    /// 旧経路の <c>FindSpecialActionForLogicalKey</c> による Key 特殊アクションとの状態の共有は、再現しない（Q4＝B。承認済みの動作変更）。
    /// </summary>
    // TODO(Phase8-Step7 7.8f。規約 2.4): Button の区分は、7.8f の PR でここに足す。
    public sealed class ControlBindingBuilder : IControlBindingBuilder
    {
        private readonly BindingMigrationOptions _options;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMacroPlayer _macroPlayer;   // Macro（7.8c）。省略時（テスト）は、Macro の束縛を作らず、旧経路が処理する
        private readonly IControlExtrasPort _extrasPort;   // Extras（7.8e）。省略時（テスト）は、Extras を持つ割り当ての束縛を作らず、旧経路が処理する

        public ControlBindingBuilder(BindingMigrationOptions options, IProfileSettingsService profileSettings, IMacroPlayer macroPlayer = null,
            IControlExtrasPort extrasPort = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _macroPlayer = macroPlayer;
            _extrasPort = extrasPort;
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            if (_options.MigratedControlKinds.Count == 0) return Array.Empty<IActionBinding>();

            bool keys = _options.IsControlMigrated(ControlBindingKind.Key);
            bool macros = _options.IsControlMigrated(ControlBindingKind.Macro) && _macroPlayer != null;
            bool extras = _options.IsControlMigrated(ControlBindingKind.Extras) && _extrasPort != null;
            if (!keys && !macros && !extras) return Array.Empty<IActionBinding>();

            ControlSettingsGroup group = _profileSettings.GetControlSettingsGroup(device);
            if (group == null) return Array.Empty<IActionBinding>();

            var result = new List<IActionBinding>();
            Add(device, group.LS, keys, macros, extras, result);
            Add(device, group.RS, keys, macros, extras, result);
            Add(device, group.L2, keys, macros, extras, result);
            Add(device, group.L2FullPull, keys, macros, extras, result);
            Add(device, group.R2, keys, macros, extras, result);
            Add(device, group.R2FullPull, keys, macros, extras, result);
            Add(device, group.ControlButtons, keys, macros, extras, result);
            Add(device, group.ExtraDeviceButtons, keys, macros, extras, result);

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
            if (macro == null || macro.Length == 0) return false;   // 旧経路は、手順がないと例外になる。新経路は所有せず、旧経路の挙動を保つ

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
                dcs.action.actionAlias, suppressSpecialActionLogs: true);
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

        private void Add(int device, List<DS4ControlSettings> settings, bool keys, bool macros, bool extras, List<IActionBinding> result)
        {
            if (settings == null) return;
            for (int i = 0; i < settings.Count; i++)
                Add(device, settings[i], keys, macros, extras, result);
        }

        // dcs の部品（Extras、通常の割り当て）は、すべて新経路で扱えるときだけ束縛にする。一部でも扱えなければ、1 つも作らず、旧経路が dcs 全体を処理する
        private void Add(int device, DS4ControlSettings dcs, bool keys, bool macros, bool extras, List<IActionBinding> result)
        {
            if (dcs == null) return;

            bool hasExtras = !dcs.IsExtrasEmpty(dcs.extras);
            ExtrasBindingValues extrasValues = default;
            if (hasExtras && (!extras || !TryInterpretExtras(dcs.extras, out extrasValues))) return;

            switch (dcs.actionType)
            {
                case DS4ControlSettings.ActionType.Default:
                    if (hasExtras) AddExtras(device, dcs, extrasValues, result);
                    break;
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
                default:
                    break;   // Button は 7.8f まで旧経路
            }
        }

        // 設計: Phase8-Step7-8e-Plan.md §3.2。Extras は Controls の判定（Assignment）で、通常の割り当てとは別の束縛にする。
        // Fresh-press ガードなし、トリガーのボタンを抑制せず、通常の出力も消さない（消すのは通常の割り当ての束縛）。離したとき Stop（元に戻す）
        private void AddExtras(int device, DS4ControlSettings dcs, ExtrasBindingValues values, List<IActionBinding> result)
        {
            var identity = new SpecialAction($"Synthetic_Extras_{device}_{(int)dcs.control}", "Synthetic", "Extras", "Extras", 0, string.Empty);
            identity.type = "Extras";
            identity.controls = identity.name;

            var policy = new BindingPolicy(respectsFreshPressGuard: false, suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: false);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Extras, ControlJudgement.Assignment),
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

            var policy = new BindingPolicy(respectsFreshPressGuard: true, suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control, ControlBindingKind.Macro, ControlJudgement.Trigger),
                new IOutputAction[] { new MacroAction(identity, device, _macroPlayer) }, policy, identity, -1));
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
            var policy = new BindingPolicy(respectsFreshPressGuard: false, suppressesTriggerButtons: false, resetsTriggerOutput: false,
                erasesOutputEveryFrame: true);
            result.Add(new TriggerBinding(new ControlButtonInputAction(identity.name, dcs.control),
                new IOutputAction[] { new KeyOutputAction(identity, values, _profileSettings) }, policy, identity, -1));
        }
    }
}
