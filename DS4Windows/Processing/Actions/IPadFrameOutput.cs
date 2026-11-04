namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2）: 1 フレームの出力への、パッド（X360 の軸・トリガー・ボタン）の書き込み口。
    /// 現行の <c>customMapQueue</c> の処理（全 dcs のあと）が出力の表へ書いていたものを、そのまま移した。<see cref="IFrameOutput"/> とその偽物を変えないために、別のインターフェースにした。
    /// 出力の表を持たない（<c>Bind</c> 前の）ときは、何もしない。
    /// </summary>
    public interface IPadFrameOutput : IFrameOutput
    {
        /// <summary>軸のペア（<paramref name="outControl"/> とその反対側の向き）の両方に、同じ値を書く（旧キューの処理と同じ）。</summary>
        void SetAxisPair(DS4Controls outControl, byte value);

        /// <summary>トリガーの出力の値を書く。</summary>
        void SetTrigger(DS4Controls outControl, byte value);

        /// <summary>ボタンの出力を ON にする（旧キューの処理と同じく、ON にするだけで OFF にはしない）。</summary>
        void SetButton(DS4Controls outControl);
    }
}
