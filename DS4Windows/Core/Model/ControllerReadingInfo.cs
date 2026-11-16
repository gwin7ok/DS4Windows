namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 読み取り表示が、状態のコピーと同じ時点で受け取る補助の値。毎回の読み取りで割り当てが出ないよう、構造体にしている。
    /// </summary>
    public readonly struct ControllerReadingInfo
    {
        public ControllerReadingInfo(long calibrationCount, double latency, int warnInterval, double processingDelayMs)
        {
            CalibrationCount = calibrationCount;
            Latency = latency;
            WarnInterval = warnInterval;
            ProcessingDelayMs = processingDelayMs;
        }

        /// <summary>ジャイロの連続較正の進み具合（0 なら較正中ではない）。</summary>
        public long CalibrationCount { get; }

        /// <summary>入力の遅延（ms）。</summary>
        public double Latency { get; }

        /// <summary>入力の遅延の警告のしきい値（ms）。</summary>
        public int WarnInterval { get; }

        /// <summary>出力までの処理の遅延（ms）。計測が無効なときは 0。</summary>
        public double ProcessingDelayMs { get; }
    }
}
