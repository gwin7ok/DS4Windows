using System.Collections.Generic;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step4 W3〜W4): 実装（TriggerDispatchEngine）は W3、Mapping への配線は W4。ProcessTriggers は最終形の入口で、Step7 の全種別の切替後（Step8）に旧ループのフック（Owns／ProcessOwned）と入れ替わる。
    // OnProfileApplied は Step5、OnDeviceDisconnected は Step6 で本番に接続する（それまでは単体テストからのみ呼ばれる。規約 2.4）。
    /// <summary>
    /// 統一トリガーディスパッチのエンジン（Phase8-Step2-Design.md §9、§14-1、§15、§23.1）。デバイスごとの抑制の集合と、束縛ごとの実行時の状態を持つ。
    /// 状態（<c>BeingTriggered</c> ほか）は <c>ActionInstanceState</c> に残す。
    /// </summary>
    public interface ITriggerDispatchEngine
    {
        // ---- 旧ループのフック（並行期間。Step4〜Step7）----

        /// <summary>移行済みの種別で、束縛が登録されている特殊アクションか。<c>BindingMigrationOptions.MigratedTypes</c> が空の間は常に false。</summary>
        bool Owns(int device, SpecialAction action);

        /// <summary>旧ループの中の、そのアクションの位置で呼ぶ（順序を保つ）。<see cref="FrameResult.EndFrame"/> なら、旧ループは打ち切る。</summary>
        FrameResult ProcessOwned(int device, SpecialAction action, ITriggerInput input, IFrameOutput frame);

        // ---- 最終形の入口（現行の MapCustomAction と Stage3 の置き換え）----

        /// <summary>通常の割り当ての前。束縛を順に処理する。</summary>
        void ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame);

        /// <summary>通常の割り当ての後（現行の Stage3 の位置）。同じフレームの評価結果を再利用して、毎フレームの出力を適用する。</summary>
        void ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame);

        // ---- 抑制の集合（旧ループもこれを使う。所有はエンジン）----

        /// <summary>通常の割り当てがそのボタンを抑制するか（現行の <c>CheckForSpecialActionSuppression</c>）。</summary>
        bool IsSuppressed(int device, DS4Controls control);

        /// <summary>成立中のトリガーを構成するすべてのボタンを、抑制の対象に加える。</summary>
        void AddSuppressed(int device, IReadOnlyList<DS4Controls> controls);

        /// <summary>離されたボタンを、1 つずつ抑制から外す（デバイスにつき 1 フレームに 1 回）。</summary>
        void ReleaseSuppressed(int device, ITriggerInput input);

        // ---- 後始末 ----

        /// <summary>プロファイル適用の後。適用スレッドから呼ばれるため、エンジンの状態は直接書かず、次のフレームの先頭でリセットする要求を立てる。</summary>
        void OnProfileApplied(int device);

        /// <summary>物理切断の後。束縛・抑制・実行時の状態をすべて消す。</summary>
        void OnDeviceDisconnected(int device);
    }
}
