using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>Macro の出力。</summary>
    public sealed class MacroOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IMacroPlayer _macroPlayer;
        private readonly IManagedActionManager _manager;
        private readonly ISpecialActionRepository _actionRepository;

        public MacroOutputBuilder(IMacroPlayer macroPlayer, IManagedActionManager manager, ISpecialActionRepository actionRepository = null)
        {
            _macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _actionRepository = actionRepository;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.Macro;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            ControlSpecialItem item = definition.Item;
            ISpecialActionRepository logRepository = origin == AssignmentOrigin.Controls ? null : _actionRepository;
            return new SpecialActionOutputs(new IOutputAction[] { new MacroAction(sa, device, _macroPlayer, index, logRepository, _manager) },
                OutputBindingPolicy.For(origin, definition), invertOnRelease: item != null && item.RunOnRelease);
        }
    }
}
