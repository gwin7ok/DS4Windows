using System.Collections.Generic;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step2〜7): 使うべき型（Phase8 の D1＝A2 で本採用する統一抽象化の骨組み）。2025-12-19（d650c4fc）に作られたが、稼働中の系統 B（Actions.Action）には配線されず、
    // OnTriggered／OnReleased／Outputs の呼び出し元は本体に 0 件のまま。接続予定: Step5（読込時の TriggerBinding の構築）〜Step7（種別ごとの切替）。
    // 現状の呼び出し側との差分: トリガー成立の判定と実行は Mapping.MapCustomAction／Stage3 に手続きで書かれている（Phase8-Step1-Trigger-Spec-Ledger.md）。
    /// <summary>
    /// Binds an input action to one or more output actions and mediates lifecycle events.
    /// </summary>
    public interface IActionBinding
    {
        IInputAction Input { get; }
        IReadOnlyList<IOutputAction> Outputs { get; }
        void OnTriggered(ITriggerContext trigger);
        void OnReleased(ITriggerContext trigger);
    }
}
