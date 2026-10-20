using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W4（Phase10-Step2-Plan.md §3.3）: Controls のモードの編集の対象。ボタン 1 つの割り当て（<see cref="DS4ControlSettings"/>）の
    /// <b>編集用のコピー</b>を持ち、種別ごとの入力（Press Key の <see cref="PressKeyViewModel"/>、Macro の <see cref="MacroViewModel"/>）との間で、
    /// 値を読み込み・書き出しする。<see cref="Apply"/>（Save）で、コピーを元へ反映し、反映しなければ（Cancel）、元は変わらない
    /// （従来の選択ダイアログ <c>BindingWindow</c> は、閉じると、元の設定を直接書き換えていた）。
    /// <para>
    /// 対象は Default／Key／Button／Macro。<c>extras</c>（振動・ライトバー・マウス感度）は、<see cref="WriteFrom"/> では変えず、出力ピッカーの結果を
    /// <see cref="Extras"/> で取り込む（Phase10-Step3 W2。割り当てエディター自身には、Extras の欄を作らない）。
    /// Key と Button は、同じ入力（Press Key）で扱い、<b>Toggle は、Key と Button の両方に書く</b>（従来の <c>OutBinding.WriteBind</c> は、Key にしか書かず、
    /// Toggle つきの Button が、ダイアログを開いて閉じるだけで消えた。持ち越し C-5。<c>Phase10Step2W0ButtonToggleReproTests</c> で、その挙動を固定してある）。
    /// 保存形式（<c>keyType</c> の <c>Toggle</c> フラグ）は、Button でも、すでに読み書きでき、実行側も対応済みのため、変更は要らない。
    /// </para>
    /// <para>
    /// TODO(Phase10-Step3 W4): ProfileEditor からの呼び出しは、W4 で接続する（Phase10-Step3-Plan.md §3.4。順序の改訂で、旧 Step4 を繰り上げた）。
    /// </para>
    /// </summary>
    public sealed class ControlSettingsEditTarget : IActionEditTarget
    {
        private readonly DS4ControlSettings original;
        private readonly IProfileSettingsService profileSettings;
        private readonly ControlEditTarget nameTarget;
        private readonly ActionTypeCatalog typeCatalog = new ActionTypeCatalog(includeControlOnlyItems: true);
        private DS4ControlSettings working;

        public ControlSettingsEditTarget(DS4ControlSettings original, IProfileSettingsService profileSettings)
        {
            this.original = original ?? throw new ArgumentNullException(nameof(original));
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            nameTarget = new ControlEditTarget(original.control);
            working = CopyOf(original);
        }

        /// <summary>対象のボタン。</summary>
        public DS4Controls Control => original.control;

        /// <summary>編集用のコピー（Save までは、元と別物）。</summary>
        public DS4ControlSettings Working => working;

        /// <summary>編集の元。<see cref="Apply"/> のときだけ書き換わる。</summary>
        public DS4ControlSettings Original => original;

        /// <summary>
        /// Phase10-Step3 W2: 編集用のコピーの Extras（振動・ライトバー・マウス感度の文字列）。出力ピッカーで編集した結果を取り込む。
        /// <see cref="WriteFrom"/> は、変えない。<see cref="Apply"/> で、元へ反映される（Cancel なら、反映されない）。
        /// </summary>
        public string Extras
        {
            get => working.extras;
            set => working.extras = value ?? string.Empty;
        }

        // ---- IActionEditTarget（名前は、ボタンの表示名で固定。検査なし）----

        public string Name
        {
            get => nameTarget.Name;
            set { }
        }

        public bool NameEditable => false;

        public bool EditMode => true;

        public SpecialAction SavedAction => null;

        public IReadOnlyList<string> ValidateName(out bool alreadyExists) => nameTarget.ValidateName(out alreadyExists);

        // ---- 読み込み ----

        /// <summary>
        /// 編集画面の種別の一覧の、初期の添字。Key と Button は、Press Key の項目、Macro は Macro の項目、Default は「Default」の項目、
        /// Unbound の Button は「Unbound」の項目（Controls のモードは、添字 0 の「種別を選んでください」を選べない）。
        /// </summary>
        public int InitialTypeIndex
        {
            get
            {
                switch (working.actionType)
                {
                    case DS4ControlSettings.ActionType.Button:
                        // Phase10-Step3: Unbound は、種別の一覧の「Unbound」の項目で表す（Default は、添字 0）
                        if (working.action.actionBtn == X360Controls.Unbound) return typeCatalog.UnboundIndex;
                        return typeCatalog.IndexOf(SpecialAction.ActionTypeId.Key);
                    case DS4ControlSettings.ActionType.Key:
                        return typeCatalog.IndexOf(SpecialAction.ActionTypeId.Key);
                    case DS4ControlSettings.ActionType.Macro:
                        return typeCatalog.IndexOf(SpecialAction.ActionTypeId.Macro);
                    default:
                        return typeCatalog.DefaultIndex;
                }
            }
        }

        /// <summary>編集用のコピーの値を、種別ごとの入力へ読み込む（Default のときは、何も読み込まない）。</summary>
        public void LoadInto(PressKeyViewModel pressKey, MacroViewModel macro)
        {
            if (pressKey == null) throw new ArgumentNullException(nameof(pressKey));
            if (macro == null) throw new ArgumentNullException(nameof(macro));

            switch (working.actionType)
            {
                case DS4ControlSettings.ActionType.Default:
                    pressKey.ReadSettings(working, allowDefault: true);
                    break;

                case DS4ControlSettings.ActionType.Key:
                case DS4ControlSettings.ActionType.Button:
                    pressKey.ReadSettings(working);
                    // Button の読み込みは、SwitchMode を変えない（SA の編集では、ダイアログが Toggle を落とすため）。Controls は、保存された Toggle を使う
                    pressKey.SwitchMode = working.keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press";
                    break;

                case DS4ControlSettings.ActionType.Macro:
                    macro.Macro.Clear();
                    macro.Macro.AddRange(working.action.actionMacro ?? new int[0]);
                    macro.PlaybackOptions.LoadFrom(working.keyType);
                    macro.UpdateMacroString();
                    break;
            }
        }

        // ---- 書き出し ----

        /// <summary>
        /// 種別の添字（<see cref="ActionTypeCatalog"/>）に対応する入力から、編集用のコピーへ書き出す。<c>extras</c> は、変えない。
        /// 0 は、既定（割り当てなし）。Controls で、現在保存できない種別（Step3 まで）は、例外。
        /// </summary>
        public void WriteFrom(int typeIndex, PressKeyViewModel pressKey, MacroViewModel macro)
        {
            if (pressKey == null) throw new ArgumentNullException(nameof(pressKey));
            if (macro == null) throw new ArgumentNullException(nameof(macro));

            ActionTypeDescriptor descriptor = typeCatalog.At(typeIndex);
            if (typeIndex != 0 && !descriptor.IsSavableIn(ActionEditorModes.Control))
                throw new ArgumentException("この種別は、Controls には、まだ保存できない（Step3 の保存形式の拡張まで）: " + descriptor.TypeId, nameof(typeIndex));

            // Phase10-Step3: 種別の一覧の「Default」「Unbound」の項目（出力ピッカーの Default／Unbound ボタンと同じ。Unbound は Button の Unbound）
            if (typeIndex == typeCatalog.DefaultIndex)
            {
                WriteDefault();
                profileSettings.RefreshActionAlias(working);
                return;
            }

            if (typeIndex == typeCatalog.UnboundIndex)
            {
                WriteUnbound();
                profileSettings.RefreshActionAlias(working);
                return;
            }

            switch (descriptor.TypeId)
            {
                case SpecialAction.ActionTypeId.Key:
                    if (pressKey.SelectedActionType == DS4ControlSettings.ActionType.Default)
                        WriteDefault();
                    else if (pressKey.SelectedActionType == DS4ControlSettings.ActionType.Button && pressKey.SelectedButton >= 0)
                        WriteButton(pressKey);
                    else
                        WriteKey(pressKey);
                    break;

                case SpecialAction.ActionTypeId.Macro:
                    WriteMacro(macro);
                    break;

                default:   // 0（None）
                    WriteDefault();
                    break;
            }

            profileSettings.RefreshActionAlias(working);
        }

        private void WriteUnbound()
        {
            working.keyType = DS4KeyType.Unbound;   // 出力先がない（Toggle・Scan Code の対象も、ない）
            working.actionType = DS4ControlSettings.ActionType.Button;
            working.action.actionBtn = X360Controls.Unbound;
        }

        private void WriteDefault()
        {
            working.keyType = DS4KeyType.None;
            working.action.actionKey = 0;
            working.actionType = DS4ControlSettings.ActionType.Default;
        }

        private void WriteKey(PressKeyViewModel pressKey)
        {
            working.keyType = DS4KeyType.None;
            working.actionType = DS4ControlSettings.ActionType.Key;
            working.action.actionKey = pressKey.Value;
            if (pressKey.UseScanCode) working.keyType |= DS4KeyType.ScanCode;
            if (IsToggle(pressKey)) working.keyType |= DS4KeyType.Toggle;
        }

        private void WriteButton(PressKeyViewModel pressKey)
        {
            working.keyType = DS4KeyType.None;
            working.actionType = DS4ControlSettings.ActionType.Button;
            working.action.actionBtn = (X360Controls)pressKey.SelectedButton;
            if (working.action.actionBtn == X360Controls.Unbound)
            {
                working.keyType |= DS4KeyType.Unbound;   // 出力先がない（Toggle の対象も、ない）
                return;
            }

            // C-5 の是正: Toggle は、Button（マウスボタン、パッドのボタン）にも書く
            if (IsToggle(pressKey)) working.keyType |= DS4KeyType.Toggle;
        }

        private void WriteMacro(MacroViewModel macro)
        {
            working.actionType = DS4ControlSettings.ActionType.Macro;
            working.action.actionMacro = macro.Macro.ToArray();
            // Phase8-Fix-MacroRepeatUnify（D1）: 「押している間繰り返す」は RepeatMacro だけで表す。Macro の印は、常に立てる
            working.keyType = macro.PlaybackOptions.ApplyTo(DS4KeyType.Macro);
        }

        private static bool IsToggle(PressKeyViewModel pressKey)
            => string.Equals(pressKey.SwitchMode, "Toggle", StringComparison.OrdinalIgnoreCase);

        // ---- Save／Cancel ----

        /// <summary>Save: 編集用のコピーを、元の設定へ反映する。</summary>
        public void Apply()
        {
            CopyTo(working, original);
        }

        /// <summary>Cancel: 編集用のコピーを捨てて、元の内容に戻す（元は、変わっていない）。</summary>
        public void Discard()
        {
            working = CopyOf(original);
        }

        private static DS4ControlSettings CopyOf(DS4ControlSettings source)
        {
            var copy = new DS4ControlSettings(source.control);
            CopyTo(source, copy);
            return copy;
        }

        private static void CopyTo(DS4ControlSettings from, DS4ControlSettings to)
        {
            to.extras = from.extras;
            to.keyType = from.keyType;
            to.actionType = from.actionType;
            to.action = new ControlActionData
            {
                actionKey = from.action.actionKey,
                actionAlias = from.action.actionAlias,
                actionBtn = from.action.actionBtn,
                actionMacro = from.action.actionMacro != null ? (int[])from.action.actionMacro.Clone() : null,
            };
        }
    }
}
