using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>BatteryCheck（電池残量の表示）の出力。</summary>
    public sealed class BatteryCheckOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IBatteryCheckPort _batteryCheckPort;
        private readonly ISpecialActionRepository _actionRepository;

        public BatteryCheckOutputBuilder(IBatteryCheckPort batteryCheckPort, ISpecialActionRepository actionRepository = null)
        {
            _batteryCheckPort = batteryCheckPort ?? throw new ArgumentNullException(nameof(batteryCheckPort));
            _actionRepository = actionRepository;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.BatteryCheck;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            // 押している間、毎フレームの色の遷移。離したときに戻す（既定の StopOnRelease）。Fresh-press ガードは既定のまま。実行中のブロックなし
            return new SpecialActionOutputs(new IOutputAction[] { new BatteryCheckAction(sa, device, _batteryCheckPort, index, _actionRepository) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
