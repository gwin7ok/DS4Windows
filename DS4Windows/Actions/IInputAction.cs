using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step3): 使うべき型。Evaluate の実装は、Step3 で新設する ComboTriggerInputAction（D2＝B1。Controls タブとスペシャルアクションの両方を 1 つの汎用クラスで扱う）。
    // 現状は呼び出し元 0 件（実装の KeyInputAction は null を返すだけのプレースホルダーで、Step3 で置き換えて削除する）。
    /// <summary>
    /// Evaluates controller state and produces a trigger context when conditions are met.
    /// </summary>
    public interface IInputAction
    {
        string Name { get; }
        /// <summary>Evaluate current device state; return a trigger context or null.</summary>
        ITriggerContext Evaluate(DS4State state);
        /// <summary>Reset internal state (e.g., timing windows).</summary>
        void Reset();
    }
}
