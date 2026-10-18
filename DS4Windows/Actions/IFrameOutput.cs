namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W1: 1 フレームの出力への書き込み口（Phase8-Step2-Design.md §5）。Button の毎フレームの出力と、成立中のトリガーのボタンの出力の消去に使う。
    /// 実装（W2）は、デバイスごとに再利用し、<c>Mapping</c> が毎フレーム出力の表を差し替える（割り当て 0）。
    /// </summary>
    public interface IFrameOutput
    {
        /// <summary>成立中のトリガーのボタンの出力を消す（現行の <c>ResetToDefaultValue</c> 相当）。</summary>
        void ResetTriggerControl(DS4Controls control);

        /// <summary>
        /// Button 種別の出力（現行の Stage3 と同じ。ボタン配列／マウスのカウンター／タッチパッドクリック）を、このフレームの出力に加える。
        /// </summary>
        void PressX360(X360Controls control);
    }
}
