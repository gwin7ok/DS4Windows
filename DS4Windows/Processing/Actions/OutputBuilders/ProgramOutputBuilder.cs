using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>Program（プログラムの起動）の出力。</summary>
    public sealed class ProgramOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IProcessLauncher _processLauncher;
        private readonly ISpecialActionRepository _actionRepository;

        public ProgramOutputBuilder(IProcessLauncher processLauncher, ISpecialActionRepository actionRepository = null)
        {
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _actionRepository = actionRepository;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.Program;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            return new SpecialActionOutputs(new IOutputAction[] { new LaunchProcessAction(sa, _processLauncher, index, _actionRepository) },
                OutputBindingPolicy.For(origin, definition, blocksWhileExecuting: true));
        }
    }
}
