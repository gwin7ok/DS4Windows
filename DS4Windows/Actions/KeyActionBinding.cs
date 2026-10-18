using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step4〜5): 使うべき型だが、現状は SpecialAction に直結した実装（Input は未実装で null）。Controls タブ由来のデータも扱える TriggerBinding（Step3 W1 で新設）が
    // 統一ディスパッチの束縛になるため、Step5（ビルダー）で TriggerBinding に置き換えて削除する見込み。それまでは、コントローラー（IActionController）へ渡す引数として使われる。
    // 作られた経緯: 2025-12-19（5564c2d6）、系統 A の骨組みとして。KeyAction が生成する。
    // Phase8-Step3 W1: IActionBinding の改訂（OnTriggered／OnReleased を削除、Policy／Source／Index を追加）に追従した。
    public class KeyActionBinding : IActionBinding
    {
        public KeyActionBinding(SpecialAction sa)
        {
            Special = sa;
        }

        public SpecialAction Special { get; }

        public IInputAction Input => null;

        private readonly System.Collections.Generic.List<IOutputAction> outputs = new System.Collections.Generic.List<IOutputAction>();

        public KeyActionBinding(SpecialAction sa, bool populateOutputs = true) : this(sa)
        {
            if (populateOutputs && sa != null)
            {
                outputs.Add(new KeyOutputAction(sa));
            }
        }

        public IReadOnlyList<IOutputAction> Outputs => outputs.AsReadOnly();

        public BindingPolicy Policy => BindingPolicy.Default;

        public SpecialAction Source => Special;

        public int Index => -1;
    }
}
