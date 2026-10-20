namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W1: 束縛（<see cref="IActionBinding"/>）の実行方針。種別ごとの安全弁の差を、エンジンの分岐ではなく設定で表す
    /// （Phase8-Step2-Design.md §6、§14）。不変。
    /// 種別ごとの構成の例（ビルダーは Step5）: Button の Press は <see cref="RespectsFreshPressGuard"/> が false、Program・Profile は
    /// <see cref="BlocksWhileExecuting"/> が true、Profile は <see cref="Untrigger"/> を持つ。
    /// </summary>
    public sealed class BindingPolicy
    {
        /// <summary>既定の方針（Fresh-press ガードあり、実行中のブロックなし、復帰なし、トリガーのボタンを抑制・消去、離したとき Stop を呼ぶ）。</summary>
        public static readonly BindingPolicy Default = new BindingPolicy();

        public BindingPolicy(
            bool respectsFreshPressGuard = true,
            bool blocksWhileExecuting = false,
            UntriggerPolicy untrigger = UntriggerPolicy.None,
            bool suppressesTriggerButtons = true,
            bool resetsTriggerOutput = true,
            bool stopOnRelease = true,
            string exclusionGroup = null,
            bool routesOutputsByGesture = false,
            bool erasesOutputEveryFrame = false)
        {
            RespectsFreshPressGuard = respectsFreshPressGuard;
            BlocksWhileExecuting = blocksWhileExecuting;
            Untrigger = untrigger;
            SuppressesTriggerButtons = suppressesTriggerButtons;
            ResetsTriggerOutput = resetsTriggerOutput;
            StopOnRelease = stopOnRelease;
            ExclusionGroup = exclusionGroup;
            RoutesOutputsByGesture = routesOutputsByGesture;
            ErasesOutputEveryFrame = erasesOutputEveryFrame;
        }

        /// <summary>トリガーが連続して未成立になるまで（<c>RequiresFreshPressAfterReset</c> が true の間）、実行しないか。台帳 §4.2: Button の Press のみ false。</summary>
        public bool RespectsFreshPressGuard { get; }

        /// <summary>実行中（<c>IsExecuting</c>）は、新規実行しないか。Program・Profile のみ true。</summary>
        public bool BlocksWhileExecuting { get; }

        /// <summary>復帰のポリシー（Profile）。</summary>
        public UntriggerPolicy Untrigger { get; }

        /// <summary>成立中、トリガーのボタンの通常出力を抑制するか。</summary>
        public bool SuppressesTriggerButtons { get; }

        /// <summary>成立中に、トリガーのボタンの出力を消すか（現行の <c>ResetToDefaultValue</c>）。<c>typeID != None</c> の既存の全種別が true。</summary>
        public bool ResetsTriggerOutput { get; }

        /// <summary>離したエッジ（<c>BeingTriggered</c> が true → false）で、出力の <c>Stop</c> を呼ぶか。</summary>
        public bool StopOnRelease { get; }

        /// <summary>シフト（Q6）: 同じグループで先に成立した束縛だけが実行される。null ＝ なし。</summary>
        public string ExclusionGroup { get; }

        /// <summary>
        /// Phase8-Step7 7.7b（W2b。MultiAction）: 出力を**ジェスチャーで選ぶ**束縛か。真のとき、エンジンは、通常の立ち上がり・保持・離したときの出力の実行をせず
        /// （トリガーのボタンの抑制と出力の消去は行う）、入力アクションが <c>TriggerState.Gesture</c> を返したフレームだけ、<c>Outputs[Gesture - 1]</c> を実行する。
        /// 「どのジェスチャーでどの出力か」の対応は、束縛を作る側（ビルダー。信号変換層）が決める。既定は偽。
        /// </summary>
        public bool RoutesOutputsByGesture { get; }

        /// <summary>
        /// Phase8-Step7 7.8b（Q3＝A）: 成立の有無によらず、毎フレーム、トリガーのボタンの通常の出力を消すか（現行の Controls タブの <c>ResetToDefaultValue</c>）。
        /// 割り当てのあるボタンだけが対象（Default＝未設定は消さない）。既定は偽（特殊アクションは、成立中だけ <see cref="ResetsTriggerOutput"/> で消す）。
        /// </summary>
        public bool ErasesOutputEveryFrame { get; }
    }
}
