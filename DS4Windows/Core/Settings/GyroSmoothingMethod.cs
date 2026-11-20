#nullable enable
namespace DS4Windows.Core.Settings
{
    /// <summary>ジャイロのマウス／マウススティックが共有する平滑化の方式（保存する識別子は <see cref="GyroSmoothingMethods"/>）。</summary>
    public enum GyroSmoothingMethod : byte
    {
        None,
        OneEuro,
        WeightedAverage,
    }
}
