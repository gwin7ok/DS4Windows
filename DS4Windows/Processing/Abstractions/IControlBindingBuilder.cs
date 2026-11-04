using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step7-8a-Plan.md W3。Phase8-Step2b-ControlsTab-Design.md §3.4）: Controls タブの割り当て（<c>ControlSettingsGroup</c>）から、
    /// 新経路（エンジン）の束縛を組み立てる。SA タブの <see cref="IActionBindingBuilder"/> に対応する、Controls タブ側の変換層の入口。
    /// 組み立てるのは、各区分（<c>ControlBindingKind</c>）の条件を満たす割り当て。条件を満たさない割り当ては束縛を作らない（Phase8-Step8 8d で移行のスイッチを削除）。
    /// </summary>
    // TODO(Phase8-Step7 7.8b〜f。規約 2.4): 7.8a は空の配線のみ。割り当て（通常・シフト）ごとの束縛の組み立ては、区分ごとの PR（Key は 7.8b）で実装する。
    // 束縛の置き場（IActionRegistry への Controls 用の領域）とエンジンの所有の判定（OwnsControl）も、7.8b で消費側と一緒に設計する。
    public interface IControlBindingBuilder
    {
        /// <summary>不変の束縛のリストを返す（null は返さない。対象がなければ空のリスト）。</summary>
        IReadOnlyList<IActionBinding> Build(int device);
    }
}
