using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 共通の編集画面の、編集の対象（名前、新規か編集か、保存済みの値、名前の検証）。
    /// SA のモード（<see cref="SpecialActionEditTarget"/>）は、名前を編集でき、名前の重複を検査する。Controls のモード（W3・W4 で追加）は、
    /// 名前が、対象のボタン名で固定（編集不可）で、重複の検査はない。
    /// </summary>
    public interface IActionEditTarget
    {
        /// <summary>名前（SA は、ユーザーが付けた名前。Controls は、対象のボタン名）。</summary>
        string Name { get; set; }

        /// <summary>名前を編集できるか（Controls のモードは偽）。</summary>
        bool NameEditable { get; }

        /// <summary>既存の項目の編集か（偽なら、新規）。</summary>
        bool EditMode { get; }

        /// <summary>編集前の、保存済みの SA（新規なら <c>null</c>）。</summary>
        SpecialAction SavedAction { get; }

        /// <summary>
        /// 名前の検証。エラーの文言を返す（空なら有効）。<paramref name="alreadyExists"/> は、重複で無効になったときだけ真
        /// （画面が、重複のメッセージを出す判断に使う）。
        /// </summary>
        IReadOnlyList<string> ValidateName(out bool alreadyExists);
    }
}
