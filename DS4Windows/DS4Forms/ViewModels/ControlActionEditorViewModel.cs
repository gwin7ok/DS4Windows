using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3）: 共通の編集画面の、Controls のモードの ViewModel。割り当てを編集する対象のボタン 1 つについて、
    /// 名前（ボタンの表示名。編集不可）、トリガー（そのボタンだけ。変更不可）、種別の一覧（Controls で現在保存できるものだけ有効）を持つ。
    /// SA のモードは <see cref="SpecialActEditorViewModel"/>。
    /// <para>
    /// TODO(Phase10-Step4): 呼び出しは、Step4 まで接続しない（Phase10-Step2-Plan.md §7 Q2＝接続しない）。<c>DS4ControlSettings</c> との変換は W4。
    /// </para>
    /// </summary>
    public class ControlActionEditorViewModel : ActionEditorViewModel
    {
        private readonly DS4Controls control;

        /// <summary>名前・トリガー・種別の骨格だけ（設定との変換なし）。</summary>
        public ControlActionEditorViewModel(int deviceNum, DS4Controls control)
            : this(deviceNum, control, new ControlEditTarget(control))
        {
        }

        /// <summary>Phase10-Step2 W4: 対象のボタンの設定（編集用のコピー）との変換つき。</summary>
        public ControlActionEditorViewModel(int deviceNum, ControlSettingsEditTarget target)
            : this(deviceNum, (target ?? throw new System.ArgumentNullException(nameof(target))).Control, target)
        {
        }

        private ControlActionEditorViewModel(int deviceNum, DS4Controls control, IActionEditTarget target)
            : base(deviceNum, new ControlTriggerSource(control.ToString()), target, new ActionTypeCatalog(),
                new TriggerCatalog(new[] { new TriggerItem(control.ToString(), target.Name) }), ActionEditorModes.Control)
        {
            this.control = control;
        }

        /// <summary>編集する対象のボタン。</summary>
        public DS4Controls Control => control;
    }
}
