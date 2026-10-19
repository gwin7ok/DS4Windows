namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e（Phase8-Step7-8e-Plan.md §3.2。Q5＝A）: Controls タブの Extras の出力（<see cref="ControlExtrasOutputAction"/>）が、
    /// 実機の振動、ライトバーの強制色（静的な <c>DS4LightBar</c>）、ボタン感度の一時変更（<c>ButtonMouseInfo</c>）を操作する窓口。
    /// 出力が静的な状態や <c>ControlService</c> に直接触れず、テストで差し替えられるようにする（前例: <see cref="IBatteryCheckPort"/>）。
    /// 範囲外のデバイスや、コントローラーがいないときは、何もしない（例外にしない）。
    /// </summary>
    public interface IControlExtrasPort
    {
        /// <summary>振動を設定する（旧 <c>ControlService.setRumble(heavy, light, device)</c>）。</summary>
        void SetRumble(int deviceIndex, byte heavy, byte light);

        /// <summary>ライトバーの強制色と点滅を設定して、強制を有効にする。</summary>
        void ForceLightbar(int deviceIndex, DS4Color color, byte flash);

        /// <summary>ライトバーの強制を解除し、点滅を 0 に戻す。</summary>
        void ClearForcedLightbar(int deviceIndex);

        /// <summary>一時のボタン感度がまだなければ（-1 のとき）だけ、設定する。すでにあれば、上書きしない。</summary>
        void TrySetTemporarySensitivity(int deviceIndex, int sensitivity);

        /// <summary>一時のボタン感度があれば（-1 でないとき）、通常の感度に戻して、一時の感度を -1 にする。</summary>
        void RestoreSensitivity(int deviceIndex);
    }
}
