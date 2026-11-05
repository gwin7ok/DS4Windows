using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase14b-Step14（決定 D14）: <see cref="ISpecialActionRuntimeReset"/> の実装（DI の Singleton）。
    /// 特殊アクションの保存・読込の後に、第2層の実行時状態（アクションの状態の管理役と、合成入力のキーの状態）を消す。中身は旧呼び出しと同じ。
    /// </summary>
    public sealed class SpecialActionRuntimeReset : ISpecialActionRuntimeReset
    {
        private readonly SyntheticInputState _syntheticInput;
        private readonly IManagedActionManager _actionManager;

        public SpecialActionRuntimeReset(SyntheticInputState syntheticInput, IManagedActionManager actionManager)
        {
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        }

        public void ClearAllActionStates() => _actionManager.ClearAllEntries();

        public void ResetRuntimeStateForKey(ushort key) => _syntheticInput.ResetRuntimeStateForKey(key, _actionManager);
    }
}
