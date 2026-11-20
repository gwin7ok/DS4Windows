#nullable enable

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// プロファイル切替アクションの「復帰」のポリシー（docs-forDIMG/MadeByAgent/Archive/Phase8-Step2-Intent-Decisions.md 項目 11。明示的な仕様にする）。
    /// </summary>
    public enum UntriggerPolicy
    {
        /// <summary>復帰しない（切り替えたままにする）。</summary>
        None = 0,

        /// <summary>「Unload on trigger release」: トリガーのどれか 1 つが離された時点で、元のプロファイルへ復帰する（<c>AutomaticUntrigger</c>）。</summary>
        Automatic = 1,

        // 解除トリガー（uTrigger）による復帰は、機能廃止（8b。ユーザー承認）に伴い削除した。
    }
}
