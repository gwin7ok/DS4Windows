namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W1: プロファイル切替アクションの「復帰」のポリシー（Phase8-Step2-Intent-Decisions.md 項目 11。明示的な仕様にする）。
    /// </summary>
    public enum UntriggerPolicy
    {
        /// <summary>復帰しない（切り替えたままにする）。</summary>
        None = 0,

        /// <summary>「Unload on trigger release」: トリガーのどれか 1 つが離された時点で、元のプロファイルへ復帰する（<c>AutomaticUntrigger</c>）。</summary>
        Automatic = 1,

        // Phase8-Step8 8d: 解除トリガー（uTrigger）による復帰（旧 Buttons = 2）は、機能廃止（8b。ユーザー承認）に伴い削除した。
    }
}
