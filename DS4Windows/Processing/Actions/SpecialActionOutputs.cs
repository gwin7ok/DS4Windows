using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step4 W2（Phase10-Step4-Plan.md §3.3）: <see cref="ISpecialActionOutputFactory"/> が、特殊アクション（<see cref="SpecialAction"/>）1 つから作る、束縛の「出力側」の組。
    /// 出力アクション（<see cref="Outputs"/>）、実行方針（<see cref="Policy"/>）、その種別が必要とする入力の種類（<see cref="InvertOnRelease"/>、<see cref="IsMultiTap"/>）を持つ。
    /// 入力アクション自体は、束縛を作る側が作る（SA は複数ボタンの組み合わせ、Controls は単体ボタン）。不変。
    /// </summary>
    public sealed class SpecialActionOutputs
    {
        public SpecialActionOutputs(IOutputAction[] outputs, BindingPolicy policy, bool invertOnRelease = false, bool isMultiTap = false)
        {
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));
            Outputs = (IOutputAction[])outputs.Clone();
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            InvertOnRelease = invertOnRelease;
            IsMultiTap = isMultiTap;
        }

        /// <summary>出力アクション。<see cref="BindingPolicy.RoutesOutputsByGesture"/> が真のとき、ジェスチャーごとの出力が、添字順に並ぶ。</summary>
        public IOutputAction[] Outputs { get; }

        /// <summary>実行方針（SA の種別ごとの方針）。</summary>
        public BindingPolicy Policy { get; }

        /// <summary>入力を、トリガーを離したときに成立させるか（Macro の <c>pressRelease</c>）。</summary>
        public bool InvertOnRelease { get; }

        /// <summary>入力を、タップ・長押し・ダブルタップを判定する入力アクション（MultiAction）にするか。</summary>
        public bool IsMultiTap { get; }
    }
}
