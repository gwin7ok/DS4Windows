using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// <c>delayTime</c> つきのスペシャルアクションが共有する、デバイスごとの基準時刻
    /// （現行の <c>Mapping.nowAction[device]</c>／<c>oldnowAction[device]</c> の 1 デバイス分）。
    /// 現行は、そのデバイスのすべての遅延つきアクションで 1 組を共有している（どれか 1 つのトリガーが押されていないと、共有の基準時刻が更新される）。
    /// 束縛ごとに持つと、遅延つきアクションが 2 つ以上あるプロファイルで挙動が変わるため、デバイスごとの再利用オブジェクトにする。
    /// </summary>
    public sealed class DelayClock
    {
        /// <summary>直近に「遅延つきアクションのトリガーがすべて押されていた」時刻。</summary>
        public DateTime Now { get; set; } = DateTime.MinValue;

        /// <summary>遅延の基準時刻（トリガーが押されていない間に更新される。旧: <c>oldnowAction</c>）。</summary>
        public DateTime Old { get; set; } = DateTime.MinValue;

        /// <summary>プロファイル適用・切断で初期状態に戻す。</summary>
        public void Reset()
        {
            Now = DateTime.MinValue;
            Old = DateTime.MinValue;
        }
    }
}
