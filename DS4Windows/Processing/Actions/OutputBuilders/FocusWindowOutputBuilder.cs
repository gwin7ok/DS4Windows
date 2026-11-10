using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>FocusWindow の出力。</summary>
    public sealed class FocusWindowOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IWindowFocuser _windowFocuser;

        public FocusWindowOutputBuilder(IWindowFocuser windowFocuser)
        {
            _windowFocuser = windowFocuser ?? throw new ArgumentNullException(nameof(windowFocuser));
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.FocusWindow;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            // 立ち上がりの 1 回だけ。前面化は別のスレッドで行う（FocusWindowAction）ため、実行中のブロックは要らない。Fresh-press ガードは既定のまま
            return new SpecialActionOutputs(new IOutputAction[] { new FocusWindowAction(sa, _windowFocuser) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
