using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step5 W1（Phase8-Step5-Plan.md §3.1。Phase8-Step2-Design.md §8）: プロファイルの特殊アクション（<c>ProfileActions</c>）から、
    /// デバイスの束縛（<see cref="TriggerBinding"/>）を組み立てる。プロファイルの読込・適用の完了後に、
    /// <c>IActionRegistry.Replace(device, Build(device))</c> の形で呼ばれる（呼び出しは <c>ProfileApplicationService</c>。W2）。
    /// 組み立てるのは、<see cref="BindingMigrationOptions.MigratedTypes"/> に含まれる種別だけ。既定は空なので、本番では常に空のリストを返す。
    /// </summary>
    public interface IActionBindingBuilder
    {
        /// <summary>
        /// 不変の束縛のリストを返す（null は返さない。ビルド対象がなければ空のリスト）。順序は <c>ProfileActions</c> の並びと同じ
        /// （旧ループのフレーム打ち切りの順序を保つため）。XML は読まず、メモリ上の設定から作る。
        /// </summary>
        IReadOnlyList<IActionBinding> Build(int device);
    }
}
