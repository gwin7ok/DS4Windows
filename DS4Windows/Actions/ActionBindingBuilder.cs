using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step5 W1（Phase8-Step5-Plan.md §3.1。Phase8-Step2-Design.md §6・§8）: <see cref="IActionBindingBuilder"/> の実装（DI の Singleton）。
    /// 種別ごとの構成は設計書 §6 のとおり（Button／Key／Macro／Program／Profile／GyroCalibrate／DisconnectBT／BatteryCheck／SASteeringWheelEmulationCalibrate／MultiAction の 10 種）。それ以外の種別
    /// （<c>None</c>）は束縛を作らず、旧経路が処理する。
    /// <para>
    /// TODO（Phase8-Step7。規約 2.4）: 本クラスは Step5 で作り、本番の適用の経路（<c>ProfileApplicationService</c>）から呼ぶが、
    /// Phase8-Step8 8d: 種別ごとの移行のスイッチ（<c>BindingMigrationOptions</c>）は削除した。全種別の束縛を常に組み立てる。
    /// </para>
    /// </summary>
    public sealed class ActionBindingBuilder : IActionBindingBuilder
    {
        private readonly IProfileActionProvider _profileActions;
        private readonly IProfileSettingsService _profileSettings;
        private readonly ISpecialActionOutputFactory _outputFactory;     // Phase10-Step4 W2: 種別ごとの出力アクション・方針の作り方（Controls タブの束縛を作る側と共有）
        private readonly IControlBindingBuilder _controlBuilder;         // Controls タブの束縛（7.8b）。省略時（テスト）は連結しない

        /// <param name="outputFactory">
        /// 種別ごとの出力と方針の作り方。省略時（テスト）は、同じ引数（窓口）から、この場で作る。DI からは、登録済みの共有のものが渡る。
        /// 窓口の引数（<paramref name="actionRepository"/> 以降）は、<paramref name="outputFactory"/> を省略したときだけ使う。
        /// </param>
        public ActionBindingBuilder(IProfileActionProvider profileActions, IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository = null,
            Func<IMapping> mappingProvider = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null,
            IWheelCalibrationPort wheelCalibrationPort = null, IMacroStringPlayer macroStringPlayer = null,
            IControlBindingBuilder controlBuilder = null, ISpecialActionOutputFactory outputFactory = null)
        {
            _controlBuilder = controlBuilder;
            _profileActions = profileActions ?? throw new ArgumentNullException(nameof(profileActions));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (macroPlayer == null) throw new ArgumentNullException(nameof(macroPlayer));
            if (processLauncher == null) throw new ArgumentNullException(nameof(processLauncher));
            if (profileSwitcher == null) throw new ArgumentNullException(nameof(profileSwitcher));

            _outputFactory = outputFactory ?? new SpecialActionOutputFactory(profileSettings, manager, macroPlayer, processLauncher,
                profileSwitcher, actionRepository, mappingProvider, gyroCalibration, disconnectPort, batteryCheckPort,
                wheelCalibrationPort, macroStringPlayer);
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            IReadOnlyList<IActionBinding> special = BuildSpecialActions(device);

            // Phase8-Step7 7.8b: Controls タブの束縛を、同じ一覧に連結する（Q5＝A。入力アクションの型で見分ける）。空なら、連結しない
            IReadOnlyList<IActionBinding> controls = _controlBuilder?.Build(device);
            if (controls == null || controls.Count == 0) return special;

            var all = new List<IActionBinding>(special.Count + controls.Count);
            all.AddRange(special);
            all.AddRange(controls);
            return all.ToArray();
        }

        private IReadOnlyList<IActionBinding> BuildSpecialActions(int device)
        {
            // 名前の一覧は、読込側が Clear→Add するリストの複製を使う（適用の途中で変わっても、この呼び出しの中では一貫させる）
            IReadOnlyList<string> names = _profileActions.GetProfileActionNames(device);
            if (names == null || names.Count == 0) return Array.Empty<IActionBinding>();

            var result = new List<IActionBinding>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                SpecialAction sa = _profileActions.GetProfileAction(device, names[i]);
                if (sa == null) continue;   // 旧ループと同じ: 見つからない名前は飛ばす（警告ログは EmitMissingActionLogsForDevice が出す）

                IActionBinding binding = Create(device, sa, _profileActions.GetProfileActionIndexOf(device, names[i]));
                if (binding != null) result.Add(binding);
            }

            return result.Count == 0 ? (IReadOnlyList<IActionBinding>)Array.Empty<IActionBinding>() : result.ToArray();
        }

        private IActionBinding Create(int device, SpecialAction sa, int index)
        {
            // トリガーのボタンがない特殊アクションは、旧経路でも成立しない。束縛を作らない
            if (sa.trigger == null || sa.trigger.Count == 0) return null;

            // Phase10-Step4 W2: 種別ごとの出力アクションと方針は、共通の部品が作る（動作は変えない）。未対応の種別・窓口が未接続の種別は null（旧経路が処理する）
            SpecialActionOutputs outputs = _outputFactory.Create(device, sa, index);
            if (outputs == null) return null;

            // 入力は、SA 側が作る（複数ボタンの組み合わせ。MultiAction だけは、タップ・長押し・ダブルタップを判定する独立した入力）
            IInputAction input = outputs.IsMultiTap
                ? (IInputAction)new MultiTapInputAction(sa.name, sa.trigger, _profileSettings, sa.delayTime)
                : Input(sa, outputs.InvertOnRelease);
            return new TriggerBinding(input, outputs.Outputs, outputs.Policy, sa, index);
        }

        /// <summary>
        /// Phase8-Step7 7.8a（Q9＝A）: SA タブの Key の設定の解釈（<c>details</c> → 論理キー、<c>keyType</c> → ScanCode／Toggle）。変換層の責務。
        /// <c>details</c> が数値として読めなければ論理キーは 0（現行の <c>KeyOutputAction</c> と同じ）。
        /// </summary>
        public static KeyBindingValues InterpretKey(SpecialAction sa)
        {
            ushort logical = 0;
            if (sa != null && !string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
            bool scan = sa != null && sa.keyType.HasFlag(DS4KeyType.ScanCode);
            bool toggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggle);
            return new KeyBindingValues(logical, scan, toggle);
        }

        /// <summary>
        /// Phase8-Step7 7.8a（Q9＝A）: SA タブの Button の設定の解釈（<c>details</c> → X360 のボタン、<c>KeyButtonSwitchMode</c> → Toggle）。変換層の責務。
        /// <c>details</c> が整数として読めなければ、ボタンなし（現行の Stage3 と同じく、何も出力しない）。
        /// </summary>
        public static ButtonBindingValues InterpretButton(SpecialAction sa)
        {
            bool toggle = sa != null && sa.KeyButtonSwitchMode.HasValue &&
                sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            if (sa != null && int.TryParse(sa.details, out int value))
                return new ButtonBindingValues(true, (X360Controls)value, toggle);
            return new ButtonBindingValues(false, X360Controls.None, toggle);
        }

        private ComboTriggerInputAction Input(SpecialAction sa, bool invertOnRelease)
            => new ComboTriggerInputAction(sa.name, sa.trigger, _profileSettings, sa.delayTime, invertOnRelease);
    }
}
