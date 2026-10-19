namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6a（Phase8-Step7-6-Plan.md Q2＝A）: GyroCalibrate の出力が、実機のコントローラー（<c>DS4Device</c>）を操作するための窓口。
    /// 出力アクションが <c>DS4Device</c> を直接持たないようにし、単体テストでは記録用の偽の実装に差し替える。
    /// </summary>
    public interface IGyroCalibrationPort
    {
        /// <summary>
        /// そのスロットのコントローラーのジャイロの連続較正をやり直す（<c>SixAxis.ResetContinuousCalibration()</c>）。
        /// 結合デバイス（Joy-Con の 2 台組）があれば、もう一方も行う。コントローラーがいなければ何もしない。
        /// </summary>
        void ResetContinuousCalibration(int deviceIndex);
    }
}
