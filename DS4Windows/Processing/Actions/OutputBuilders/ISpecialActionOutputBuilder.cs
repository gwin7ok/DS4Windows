using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>
    /// 特殊アクションの種別 1 つぶんの、出力アクションと実行方針の作り方。<see cref="SpecialActionOutputFactory"/> が、種別（<see cref="Kind"/>）で選んで呼ぶ。
    /// </summary>
    public interface ISpecialActionOutputBuilder
    {
        SpecialAction.ActionTypeId Kind { get; }

        /// <summary>定義が、この種別に必要な値を持たないときは null（束縛を作らない）。</summary>
        SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index);
    }
}
