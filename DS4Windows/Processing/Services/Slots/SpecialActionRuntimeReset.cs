#nullable enable

using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// Phase14b-Step14（決定 D14）: <see cref="ISpecialActionRuntimeReset"/> の実装（DI の Singleton）。
    /// スペシャルアクションの保存・読込の後に、第2層の実行時状態（アクションの状態の管理役と、合成入力のキーの状態）を消す。中身は旧呼び出しと同じ。
    /// </summary>
    public sealed class SpecialActionRuntimeReset : ISpecialActionRuntimeReset
    {
        private readonly SyntheticInputState _syntheticInput;
        private readonly IManagedActionManager _actionManager;

        public SpecialActionRuntimeReset(SyntheticInputState syntheticInput, IManagedActionManager actionManager)
        {
            ArgumentNullException.ThrowIfNull(syntheticInput);
            _syntheticInput = syntheticInput;
            ArgumentNullException.ThrowIfNull(actionManager);
            _actionManager = actionManager;
        }

        public void ClearAllActionStates() => _actionManager.ClearAllEntries();

        public void ResetRuntimeStateForKey(ushort key) => _syntheticInput.ResetRuntimeStateForKey(key, _actionManager);
    }
}
