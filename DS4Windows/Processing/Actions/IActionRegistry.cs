using System.Collections.Generic;

namespace DS4Windows.Actions
{
    // Phase8-Step4 W1 で改訂した（旧: Register／Unregister／GetBindingsForDevice。実装がなく、呼び出し元は 0 件だった）。
    // 実装は ActionBindingRegistry。束縛の組み立てと Replace の呼び出し（プロファイルの読込・適用の完了後）は Step5、切断時の Clear は Step6。
    /// <summary>
    /// デバイスごとの束縛の一覧の置き場（Phase8-Step2-Design.md §8）。一覧は不変のスナップショットで、参照ごと差し替える（アトミック）。
    /// 読み出し側（エンジン）は、フレームの先頭で <see cref="GetBindings"/> を 1 回だけ呼び、そのフレームは古い一覧で最後まで処理する。
    /// </summary>
    public interface IActionRegistry
    {
        /// <summary>デバイスの束縛の一覧（不変）。null は返さない（空のリスト）。範囲外のデバイスも空のリスト。</summary>
        IReadOnlyList<IActionBinding> GetBindings(int device);

        /// <summary>デバイスの束縛の一覧を差し替える（<paramref name="bindings"/> は複製して保持する。null は空）。範囲外のデバイスは例外。</summary>
        void Replace(int device, IReadOnlyList<IActionBinding> bindings);

        /// <summary>デバイスの束縛の一覧を空にする（切断）。範囲外のデバイスは何もしない。</summary>
        void Clear(int device);
    }
}
