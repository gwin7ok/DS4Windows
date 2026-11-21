using DS4Windows.Core.Model;

namespace DS4Windows.Processing
{
    /// <summary>スティック・トリガーのカーブとデッドゾーンの適用。</summary>
    public partial class Mapping
    {
        private static int ClampInt(int min, int value, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        // 補正の段（回転・ドリフト・デッドゾーン・出力カーブ・ジャイロ）は CurveAndDeadzoneStage が行う
        public ControllerState SetCurveAndDeadzone(int device, ControllerState cState, ControllerState dState,
                DS4Windows.Core.Abstractions.IProfileSettingsService settings)
        {
            return _curveStage.Apply(device, cState, dState, settings);
        }
    }
}
