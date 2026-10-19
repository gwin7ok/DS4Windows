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
    /// Key（7.8b）の条件: 通常の割り当てが Key、シフトなし、Extras なし（通常・シフトとも）、Lightbar マクロが有効でない、<c>actionKey</c> が <c>ushort</c> の範囲内。
    /// ジャイロスワイプの割り当て（<c>GyroSwipe*</c>）は、前の方向を先に離す旧経路の順序に依存するため、対象にしない（旧経路が処理する）。
    /// </para>
    /// 「何をするか」の解釈（キー番号、Toggle、ScanCode、別名キー）はここで行い、出力（<c>KeyOutputAction</c>）には解釈済みの値を渡す（Q9＝A）。
    /// 識別用の <c>SpecialAction</c>（状態の鍵）は、旧経路の合成と同じ名前（<c>Synthetic_Key_{device}_{control}</c>）で作る。
    /// 旧経路の <c>FindSpecialActionForLogicalKey</c> による Key 特殊アクションとの状態の共有は、再現しない（Q4＝B。承認済みの動作変更）。
    /// </summary>
    // TODO(Phase8-Step7 7.8c〜f。規約 2.4): Macro（7.8c）、シフト（7.8d）、Extras／Lightbar マクロ（7.8e）、Button（7.8f）の区分は、それぞれの PR でここに足す。
    public sealed class ControlBindingBuilder : IControlBindingBuilder
    {
        private readonly BindingMigrationOptions _options;
        private readonly IProfileSettingsService _profileSettings;

        public ControlBindingBuilder(BindingMigrationOptions options, IProfileSettingsService profileSettings)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            if (_options.MigratedControlKinds.Count == 0) return Array.Empty<IActionBinding>();
            if (!_options.IsControlMigrated(ControlBindingKind.Key)) return Array.Empty<IActionBinding>();

            ControlSettingsGroup group = _profileSettings.GetControlSettingsGroup(device);
            if (group == null) return Array.Empty<IActionBinding>();

            var result = new List<IActionBinding>();
            AddKeys(device, group.LS, result);
            AddKeys(device, group.RS, result);
            AddKey(device, group.L2, result);
            AddKey(device, group.L2FullPull, result);
            AddKey(device, group.R2, result);
            AddKey(device, group.R2FullPull, result);
            AddKeys(device, group.ControlButtons, result);
            AddKeys(device, group.ExtraDeviceButtons, result);

            return result.Count == 0 ? (IReadOnlyList<IActionBinding>)Array.Empty<IActionBinding>() : result.ToArray();
        }

        /// <summary>
        /// 割り当てが、Key の新経路の条件（クラスの説明）を満たすか。満たすときは、解釈済みの値を返す。
        /// </summary>
        public static bool TryInterpretKey(DS4ControlSettings dcs, out KeyBindingValues values)
        {
            values = default;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Key) return false;
            if (dcs.shiftActionType != DS4ControlSettings.ActionType.Default) return false;
            if (!dcs.IsExtrasEmpty(dcs.extras) || !dcs.IsExtrasEmpty(dcs.shiftExtras)) return false;
            if (dcs.LightbarMacro != null && dcs.LightbarMacro.Active) return false;

            int key = dcs.action.actionKey;
            if (key < ushort.MinValue || key > ushort.MaxValue) return false;   // 旧経路は範囲外で例外になる。新経路は所有せず、旧経路の挙動を保つ

            DS4KeyType keyType = dcs.keyType;
            values = new KeyBindingValues((ushort)key, keyType.HasFlag(DS4KeyType.ScanCode), keyType.HasFlag(DS4KeyType.Toggle),
                dcs.action.actionAlias, suppressSpecialActionLogs: true);
            return true;
        }

        private void AddKeys(int device, List<DS4ControlSettings> settings, List<IActionBinding> result)
        {
            if (settings == null) return;
            for (int i = 0; i < settings.Count; i++)
                AddKey(device, settings[i], result);
        }

        private void AddKey(int device, DS4ControlSettings dcs, List<IActionBinding> result)
        {
            if (!TryInterpretKey(dcs, out KeyBindingValues values)) return;

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
