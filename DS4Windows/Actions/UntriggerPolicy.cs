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

        /// <summary>解除トリガー（<c>uTrigger</c>）がすべて押されたときに、元のプロファイルへ復帰する。</summary>
        Buttons = 2,
    }
}
