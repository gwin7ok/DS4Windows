namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6d（Phase8-Step7-6-Plan.md Q2＝A）: ステアリング較正の出力が、実機のコントローラー（<c>DS4Device</c>）の較正の状態を読み書きするための窓口。
    /// 出力アクションが <c>DS4Device</c> を直接持たないようにし、単体テストでは記録用の偽の実装に差し替える。
    /// 状態の意味（<c>DS4Device.WheelRecalibrateActiveState</c>）: 0＝待機、1＝較正の開始の要求、2＝較正の中（確定の待ち）、3＝確定の要求、4＝取消。
    /// 1 と 3 は、特殊アクション（このクラスの利用者）が立て、2 と 0 への復帰は <c>Mapping</c> の較正の処理が行う。
    /// </summary>
    public interface IWheelCalibrationPort
    {
        /// <summary>そのスロットのコントローラーの較正の状態。コントローラーがいなければ -1。</summary>
        int GetCalibrationState(int deviceIndex);

        /// <summary>較正の状態を設定する。コントローラーがいなければ何もしない。</summary>
        void SetCalibrationState(int deviceIndex, int state);
    }
}
