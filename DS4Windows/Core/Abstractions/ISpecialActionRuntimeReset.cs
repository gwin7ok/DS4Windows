namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase14b-Step14（決定 D14）: 特殊アクションの保存・読込（Core の <c>SpecialActionRepository</c>／<c>SpecialActionsXmlSerializer</c>）の後に行う、
    /// 実行時状態のリセットだけを切り出した契約。実装は第2層（<c>SpecialActionRuntimeReset</c>。アクションの状態の管理役と合成入力の状態を持つ）。
    /// 旧: Core が第2層の <c>IManagedActionManager</c>・<c>SyntheticInputState</c> を直接受けていた。
    /// </summary>
    public interface ISpecialActionRuntimeReset
    {
        /// <summary>全アクションの実行時状態（トリガーの状態・トグル）を消す（旧 <c>IManagedActionManager.ClearAllEntries</c>）。</summary>
        void ClearAllActionStates();

        /// <summary>キー型のアクションを保存した後に、そのキーの押下の状態とトグルを消す（旧 <c>SyntheticInputState.ResetRuntimeStateForKey</c>）。</summary>
        void ResetRuntimeStateForKey(ushort key);
    }
}
