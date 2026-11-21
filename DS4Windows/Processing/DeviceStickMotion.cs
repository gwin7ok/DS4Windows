#nullable enable

using DS4Windows.Core.Settings;
using DS4Windows.Processing.Gyro;

namespace DS4Windows.Processing
{
    /// <summary>
    /// 1 台ぶんのスティック・ジャイロ・トリガーの処理状態（旧: <see cref="StickMotionState"/> の、デバイスの番号で引く配列の同じ位置の要素）。
    /// 状態の持ち主をデバイスごとの 1 つの型にし、番号の取り違えを、持ち主を取り違えない形に変える。
    /// </summary>
    public sealed class DeviceStickMotion
    {
        /// <summary>ジャイロのマウススティック出力の軸の値（128＝中立）。</summary>
        /// <summary>プロファイルの割り当て後に重ねるスティックの出力（ジャイロが書き、<c>MapCustom</c> が読む）。</summary>
        public PostMapStickData MapStickActionData { get; } = new PostMapStickData();

        public FlickStickMappingData FlickMappingData { get; } = new FlickStickMappingData();

        public TwoStageTriggerMappingData L2TwoStageMappingData { get; } = new TwoStageTriggerMappingData();

        public TwoStageTriggerMappingData R2TwoStageMappingData { get; } = new TwoStageTriggerMappingData();

        public DeltaSettingsProcessorGroup DeltaAccelProcessors { get; } = new DeltaSettingsProcessorGroup();
    }
}
