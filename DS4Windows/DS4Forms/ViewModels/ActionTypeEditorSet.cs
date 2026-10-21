using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 種別ごとの入力（<see cref="IActionTypeEditor"/>）の一式。種別（<c>ActionTypeId</c>）から、
    /// 対応する入力を選んで、読み込み・検証・保存を中継する。従来は、<c>AssignmentEditor</c> のコードビハインドの、3 つの <c>switch</c>
    /// （<c>LoadAction</c>、<c>CheckActionValid</c>、<c>SaveBtn_Click</c>）が、同じ対応を 3 回書いていた。
    /// <para>
    /// Button は、Key と同じ入力（Press Key のタブの <c>PressKeyViewModel</c>）で扱う（読み込みだけ。保存の種別は、画面の添字から Key になる）。
    /// 一覧にない種別は、読み込み・保存は何もせず、検証は偽（従来の <c>switch</c> の既定値と同じ）。
    /// </para>
    /// </summary>
    public sealed class ActionTypeEditorSet
    {
        private readonly IReadOnlyDictionary<SpecialAction.ActionTypeId, IActionTypeEditor> editors;

        public ActionTypeEditorSet(IReadOnlyDictionary<SpecialAction.ActionTypeId, IActionTypeEditor> editors)
        {
            this.editors = editors ?? throw new ArgumentNullException(nameof(editors));
        }

        /// <summary>保存済みの SA を、種別に対応する入力へ読み込む。</summary>
        public void Load(SpecialAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (TryGet(action.typeID, out IActionTypeEditor editor))
                editor.LoadAction(action);
        }

        /// <summary>種別に対応する入力を検証する。対応する入力がなければ、偽。</summary>
        public bool IsValid(SpecialAction.ActionTypeId typeId, SpecialAction action)
        {
            return TryGet(typeId, out IActionTypeEditor editor) && editor.IsValid(action);
        }

        /// <summary>種別に対応する入力を保存する。対応する入力がなければ、何もしない。</summary>
        public void Save(SpecialAction.ActionTypeId typeId, SpecialAction action, bool edit)
        {
            if (TryGet(typeId, out IActionTypeEditor editor))
                editor.SaveAction(action, edit);
        }

        /// <summary>
        /// Phase10-Step6 I6b: Controls のモードで、保存済みの割り当ての型のある項目を、項目の種別に対応する入力へ読み込む（<c>ControlSpecialEditors</c> の後継）。
        /// 対応する入力がなければ、何もしない。
        /// </summary>
        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            if (TryGet(item.Type, out IActionTypeEditor editor))
                editor.LoadControlItem(item);
        }

        /// <summary>
        /// Phase10-Step6 I6b: Controls のモードで、種別に対応する入力から、型のある項目を作る。対応する入力がなければ、種別だけを持つ項目（秒数 0）。
        /// </summary>
        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId typeId)
        {
            return TryGet(typeId, out IActionTypeEditor editor)
                ? editor.ToControlItem(typeId)
                : new ControlSpecialItem { Type = typeId };
        }

        private bool TryGet(SpecialAction.ActionTypeId typeId, out IActionTypeEditor editor)
        {
            if (typeId == SpecialAction.ActionTypeId.Button) typeId = SpecialAction.ActionTypeId.Key;
            return editors.TryGetValue(typeId, out editor);
        }
    }
}
