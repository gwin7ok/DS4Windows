using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(2)-B（Phase9-A2-Plan.md）: スティックの入力の前処理（ドリフト補正、アンチスナップバック、ファズ）。
    /// 以前は <c>Mapping.SetCurveAndDeadzone</c> の冒頭と、<c>Mapping</c> の private メソッド（<c>CalcAntiSnapbackStick</c>／<c>CalcStickAxisFuzz</c>）、
    /// <c>ApplyStickCalibration</c> にあった。状態は持たない（履歴・前回の値は呼び出し側［<see cref="StickMotionState"/>］が持ち、引数で渡す）。時刻も引数で受ける。
    /// 回転は、<c>DS4State.rotateLSCoordinates</c>／<c>rotateRSCoordinates</c>（すでに <c>DS4State</c> のメソッド）のまま。
    /// </summary>
    public interface IStickInputConditioner
    {
        /// <summary>ドリフト補正（4 軸。設定の値を差し引き、0〜255 に丸める）。<paramref name="state"/> を書き換えて、そのまま返す。</summary>
        DS4State ApplyDrift(int device, DS4State state, IProfileSettingsService settings);

        /// <summary>
        /// アンチスナップバック。<paramref name="history"/> は、そのスティックの履歴（古いものを捨て、今回の値を足す）、<paramref name="timeoutMs"/> は残す時間、
        /// <paramref name="timestamp"/> は現在時刻（Unix ミリ秒）。直前の値を通る急な戻りと判断したら、中立（128）にする。
        /// </summary>
        void AntiSnapback(Queue<DS4TimedStickAxisValue> history, double delta, long timeoutMs, long timestamp, ref byte x, ref byte y);

        /// <summary>
        /// ファズ（小さな揺れの無視）。<paramref name="last"/> は、そのデバイスの前回の値（[LX, LY, RX, RY]。更新する）、<paramref name="stickId"/> は 0＝左、1＝右。
        /// </summary>
        void Fuzz(byte[] last, int stickId, int delta, ref byte x, ref byte y);
    }
}
