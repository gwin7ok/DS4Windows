namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(2)-C（Phase9-A2-Plan.md）: フリックスティック（スティックをはじいた角度を、マウスの X 移動量にする）の 1 フレーム分の処理。
    /// 以前は <c>Mapping</c> の private メソッド（<c>ProcessFlickStick</c>／<c>HandleFlickStickAngle</c>／<c>WarpEaseOut</c>）だった。状態は持たない（フリックの進行は <see cref="FlickStickMappingData"/> が持ち、引数で渡す）。
    /// 時刻には依存しない（フレーム間隔は <paramref name="elapsedTime"/> で受ける）。
    /// </summary>
    public interface IFlickStickProcessor
    {
        /// <summary>
        /// 1 フレーム分の処理を行い、マウスの X 移動量を <paramref name="mouseDeltaX"/> に加算する。
        /// </summary>
        /// <param name="data">そのデバイスのフリックの状態（進行、大きさ、持ち越しの端数。更新する）。</param>
        /// <param name="settings">フリックスティックの設定（しきい値、フリック時間、補正係数、最小角度）。</param>
        /// <param name="elapsedTime">前のフレームからの経過時間（秒。<c>DS4State.elapsedTime</c>）。</param>
        /// <param name="stickX">現在のスティック値。</param>
        /// <param name="prevStickX">1 フレーム前のスティック値。</param>
        void Process(FlickStickMappingData data, FlickStickSettings settings, double elapsedTime,
            byte stickX, byte stickY, byte prevStickX, byte prevStickY, ref double mouseDeltaX);
    }
}
