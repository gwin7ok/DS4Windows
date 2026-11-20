namespace DS4Windows.Core.Settings
{
    /// <summary>平滑化の方式と、プロファイルへ保存する識別子（"one-euro"・"weighted-average"・"none"）の相互変換。</summary>
    public static class GyroSmoothingMethods
    {
        public static GyroSmoothingMethod Parse(string? identifier) => identifier switch
        {
            "weighted-average" => GyroSmoothingMethod.WeightedAverage,
            "one-euro" => GyroSmoothingMethod.OneEuro,
            _ => GyroSmoothingMethod.None,
        };

        public static string Identifier(GyroSmoothingMethod method) => method switch
        {
            GyroSmoothingMethod.WeightedAverage => "weighted-average",
            GyroSmoothingMethod.OneEuro => "one-euro",
            _ => "none",
        };
    }
}
