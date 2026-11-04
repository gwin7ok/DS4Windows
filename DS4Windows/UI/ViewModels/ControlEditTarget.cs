using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3）: Controls のモードの編集の対象。名前は、対象のボタンの表示名で固定（編集不可）で、
    /// 名前の重複の検査はない。既存の割り当ての編集のため、<see cref="EditMode"/> は真。
    /// <para>
    /// TODO(Phase10-Step4): W3 では、画面（名前・トリガー・種別の一覧）の骨格だけ。<c>DS4ControlSettings</c> との変換と、編集用のコピー
    /// （Save で元へ反映、Cancel で破棄）は W4（<c>ControlSettingsEditTarget</c>）で、この対象に足す。画面の呼び出しは、Step4 まで接続しない。
    /// </para>
    /// </summary>
    public sealed class ControlEditTarget : IActionEditTarget
    {
        private static readonly string[] NoErrors = new string[0];

        private readonly string name;

        public ControlEditTarget(DS4Controls control)
        {
            Control = control;
            name = DS4ControlNames.ds4inputNames.TryGetValue(control, out string displayName) ? displayName : control.ToString();
        }

        /// <summary>対象のボタン。</summary>
        public DS4Controls Control { get; }

        /// <summary>対象のボタンの表示名（例: <c>Circle</c>）。書き込みは無視する。</summary>
        public string Name
        {
            get => name;
            set { }   // 名前は固定（書き込みは無視する）
        }

        public bool NameEditable => false;

        public bool EditMode => true;

        public SpecialAction SavedAction => null;

        public IReadOnlyList<string> ValidateName(out bool alreadyExists)
        {
            alreadyExists = false;
            return NoErrors;
        }
    }
}
