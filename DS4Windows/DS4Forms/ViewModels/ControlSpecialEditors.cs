using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step4 W6: Controls のモードの割り当てエディターが、<c>ControlSettingsEditTarget</c> に渡す、SA 専用の種別ごとの入力の一式。
    /// 群ごとの縦通し（Phase10-Step4-Plan.md §5）で、群が増えるたびに入力が増える（W4: 押し続ける秒数、W5: Launch Program、W6: Load Profile、以降: Check Battery、Multi-action）ため、
    /// 省略可の引数を並べず、1 つの型にまとめた。持たない入力は null（その種別の読み書きは、既定の値になる）。
    /// </summary>
    public sealed class ControlSpecialEditors
    {
        /// <summary>押し続ける秒数だけを持つ種別（Disconnect BT、Calibrate gyro、Calibrate sixaxis wheel）の入力。</summary>
        public IReadOnlyDictionary<SpecialAction.ActionTypeId, SpecialActionViewModel> HoldIntervals { get; set; }

        /// <summary>Launch Program の入力（パス、押し続ける秒数、引数）。</summary>
        public LaunchProgramViewModel LaunchProgram { get; set; }

        /// <summary>Load Profile の入力（切り替え先のプロファイル、離したら戻る）。</summary>
        public LoadProfileViewModel LoadProfile { get; set; }
    }
}
