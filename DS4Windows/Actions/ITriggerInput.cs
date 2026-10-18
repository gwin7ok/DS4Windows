using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W1: トリガー条件の判定に渡す、読み取り専用の入力（Phase8-Step2-Design.md §3。Q1＝A）。
    /// <c>Mapping</c> が毎フレーム、デバイスごとに再利用するオブジェクトを更新して渡す（割り当て 0）。
    /// タッチ・スワイプ・ジャイロの判定は <c>DS4State</c> だけでは足りないため、<c>DS4StateFieldMapping</c> を含む入力をまとめて表す。
    /// </summary>
    public interface ITriggerInput
    {
        int Device { get; }

        /// <summary>
        /// 現在のフレームで、そのコントロールが押されているか（現行の <c>Mapping.getBoolSpecialActionMapping</c> と同じ意味）。
        /// ボタン: そのまま／軸: 128±55 を超えたら／L2・R2: 100 を超えたら／タッチ: ボタン／スワイプ: フラグ／ジャイロ: ジャイロ制御のときだけ ±67 を超えたら。
        /// </summary>
        bool IsActive(DS4Controls control);

        /// <summary>前のフレームで押されていたか（MultiAction のエッジ判定用）。前フレームの表は、最初に要求されたときにだけ作る（遅延）。</summary>
        bool WasActive(DS4Controls control);

        /// <summary><c>delayTime</c> つきのアクションが共有する、デバイスごとの基準時刻。</summary>
        DelayClock Delay { get; }

        /// <summary>現在時刻（UTC）。テストで固定できるよう、入力から提供する。</summary>
        DateTime UtcNow { get; }

        /// <summary>起動からの経過ミリ秒（Fresh-press ガードの連続した未成立の判定に使う）。</summary>
        long TickCount64 { get; }
    }
}
