using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// 判定（<see cref="IInputAction"/>）と実行（<see cref="IOutputAction"/>）の組と、実行方針（<see cref="BindingPolicy"/>）。不変のデータで、
    /// エッジ検出・ガード・抑制・復帰は、エンジン（<c>ITriggerDispatchEngine</c>。Step4）が一元的に担う。
    /// Phase8-Step3 W1 で改訂した（旧: <c>OnTriggered</c>／<c>OnReleased</c> を持っていたが、呼び出し元は 0 件で、ライフサイクルはエンジンが担うため削除。
    /// 新: <see cref="Policy"/>、<see cref="Source"/>、<see cref="Index"/>）。設計は Phase8-Step2-Design.md §6。
    /// </summary>
    public interface IActionBinding
    {
        IInputAction Input { get; }

        IReadOnlyList<IOutputAction> Outputs { get; }

        BindingPolicy Policy { get; }

        /// <summary>実行時の状態（<c>ActionInstanceState</c>）のキー。Controls タブ由来のときは合成の <c>SpecialAction</c>。</summary>
        SpecialAction Source { get; }

        /// <summary><c>BackingStore.actions</c> での添字（復帰の合図の <c>UntriggerIndex</c> 用）。不明なときは -1。</summary>
        int Index { get; }
    }
}
