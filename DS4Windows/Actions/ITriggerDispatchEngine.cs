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

        /// <summary>
        /// Phase8-Step7 7.8a: Controls タブの割り当て（<c>ProcessControlSettingAction</c>）を、新経路が所有するか。真なら、旧経路は本体を素通りする（抑制の判定は共通）。
        /// <c>BindingMigrationOptions.MigratedControlKinds</c> が空の間は常に false（7.8a では、空の配線のみ）。
        /// </summary>
        // 7.8b: 判定は、そのボタンの束縛（ボタン単位。Q2＝A）が登録されているか。束縛は、移行済みの区分の条件を満たす割り当てだけに作られる（ControlBindingBuilder）。
        bool OwnsControl(int device, DS4Controls control);

        /// <summary>
        /// Phase8-Step7 7.8b: Controls タブのボタンの束縛を、1 フレーム分処理する（<see cref="OwnsControl"/> が真のときに呼ぶ）。押下・解放のエッジで出力を実行し、
        /// 方針（<c>ErasesOutputEveryFrame</c>）に従って、毎フレーム、そのボタンの通常の出力を消す。
        /// </summary>
        void ProcessControl(int device, DS4Controls control, ITriggerInput input, IFrameOutput frame);

        // ---- 最終形の入口（現行の MapCustomAction と Stage3 の置き換え）----

        /// <summary>通常の割り当ての前。束縛を順に処理する。</summary>
        void ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame);

        /// <summary>通常の割り当ての後（現行の Stage3 の位置）。同じフレームの評価結果を再利用して、毎フレームの出力を適用する。</summary>
        void ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame);

        /// <summary>
        /// Phase8-Step7 7.5b: 復帰の確認（旧 <c>Mapping.MapCustomAction</c> の後段の <c>UNTRIGGER check</c>）。旧ループの後、旧経路の復帰の確認と同じ位置で呼ぶ。
        /// 合図（復帰待ちの Profile）が空なら、ロックも割り当てもなしで戻る。切替のフレーム（旧ループが <c>return</c>）では呼ばれない。
        /// </summary>
        void ProcessUntrigger(int device, ITriggerInput input);

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
