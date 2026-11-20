#nullable enable

using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// 出力アクションに渡す文脈（エンジンの <c>EngineContext</c>）が実装する、復帰の合図の問い合わせ。
    /// 連鎖の防止（<c>ProfileSwitchAction.CanExecute</c>）が、エンジンの持つ合図（復帰待ちの Profile）を参照するために使う。
    /// <c>IOutputContext</c> のメンバーは増やさない（<see cref="IOutputContextInput"/> と同じ方式）。
    /// </summary>
    public interface IPendingUntriggerQuery
    {
        /// <summary>復帰待ちのスペシャルアクション（切替前の Profile）。合図がなければ null。</summary>
        SpecialAction? PendingUntriggerAction { get; }
    }
}
