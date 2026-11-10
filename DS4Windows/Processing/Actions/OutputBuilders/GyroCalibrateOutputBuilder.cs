using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>GyroCalibrate の出力。</summary>
    public sealed class GyroCalibrateOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IGyroCalibrationPort _gyroCalibration;
        private readonly ISpecialActionRepository _actionRepository;

        public GyroCalibrateOutputBuilder(IGyroCalibrationPort gyroCalibration, ISpecialActionRepository actionRepository = null)
        {
            _gyroCalibration = gyroCalibration ?? throw new ArgumentNullException(nameof(gyroCalibration));
            _actionRepository = actionRepository;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.GyroCalibrate;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            // 立ち上がりの 1 回だけ。Fresh-press ガードは既定のまま（旧経路の独自の判定と同じ効果）。実行中のブロックなし
            return new SpecialActionOutputs(new IOutputAction[] { new GyroCalibrateAction(sa, device, _gyroCalibration, index, _actionRepository) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
