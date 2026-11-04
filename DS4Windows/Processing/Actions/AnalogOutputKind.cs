namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 後の Step5（SA 側のアナログ出力）: Button の出力先（<see cref="X360Controls"/>）の、アナログな出力の種類。
    /// 強さ（0〜100%）が効くのは、軸・トリガー・マウス移動。それ以外（デジタルのボタン、マウスのボタン、ホイール、タッチパッドクリック）は <see cref="None"/>。
    /// </summary>
    public enum AnalogOutputKind
    {
        /// <summary>アナログな出力ではない（強さは効かない）。</summary>
        None,

        /// <summary>パッドの軸（左右のスティックの 4 方向）。</summary>
        PadAxis,

        /// <summary>パッドのトリガー（L2／R2）。</summary>
        PadTrigger,

        /// <summary>マウスの移動（MouseUp／Down／Left／Right）。</summary>
        MouseMove,
    }
}
