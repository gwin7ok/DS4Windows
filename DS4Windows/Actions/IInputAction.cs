using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// トリガー条件の判定（副作用なし。<c>delayTime</c> の基準時刻の更新だけは、判定の一部として <see cref="ITriggerInput.Delay"/> を更新する）。
    /// Phase8-Step3 W1 で改訂した（旧: <c>ITriggerContext Evaluate(DS4State)</c>。入力が <c>DS4State</c> だけでタッチ・ジャイロを判定できず、
    /// 呼ぶたびにオブジェクトを作るため、ホットパスで使えなかった。呼び出し元は 0 件だった）。
    /// 実装: <c>ComboTriggerInputAction</c>（Step3 W3。D2＝B1: Controls タブとスペシャルアクションの両方を 1 つの汎用クラスで扱う）、
    /// <c>MultiTapInputAction</c>（Step7.7）。設計は Phase8-Step2-Design.md §4。
    /// </summary>
    public interface IInputAction
    {
        string Name { get; }

        /// <summary>
        /// 構成するボタン。成立中の抑制対象（通常の割り当ての抑制）と、トリガーのボタンの出力の消去に使う。
        /// </summary>
        IReadOnlyList<DS4Controls> Triggers { get; }

        /// <summary>現在のフレームの入力から、トリガーが成立しているかを判定する（割り当て 0）。</summary>
        TriggerState Evaluate(ITriggerInput input);

        /// <summary>内部状態を初期化する（プロファイルの適用・切断で呼ぶ。例: <c>RunOnRelease</c> の準備状態）。</summary>
        void Reset();
    }
}
