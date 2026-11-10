using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>Button の出力（押している間のボタン）。</summary>
    public sealed class ButtonOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IManagedActionManager _manager;

        public ButtonOutputBuilder(IManagedActionManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.Button;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            // Press は Fresh-press ガードを見ない／Toggle は見る（規則は FreshPressGuardTable）
            if (!definition.Button.HasValue) return null;
            return new SpecialActionOutputs(new IOutputAction[] { new ButtonOutputAction(sa, definition.Button.Value, _manager) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
