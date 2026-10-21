namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step2b-ControlsTab-Design.md §3.4）: Controls タブの割り当ての、新経路（エンジン）へ移行する区分。
    /// 各区分の条件を満たす割り当てを、新経路が処理する（Q2＝A: ボタン単位の所有）。Phase8-Step8 8d: 区分ごとの移行のスイッチは削除した。
    /// 区分は、後続の PR（7.8b〜f）が 1 つずつ有効にする。7.8a では、どれも有効にしない。
    /// </summary>
    public enum ControlBindingKind
    {
        /// <summary>Key の出力（7.8b）。</summary>
        Key,

        /// <summary>Macro の出力（7.8c）。</summary>
        Macro,

        /// <summary>Button のうち、パッド（X360 の軸・トリガー・ボタン）への割り当てと、Default の軸の素通し（7.8f-1）。マウス、ホイールは 7.8f-2・7.8f-3 で別の区分にする。</summary>
        ButtonPad,

        /// <summary>Button のうち、マウスのボタン・タッチパッドクリック・デジタルのホイールへの割り当てと、Toggle つきの割り当て（7.8f-2）。アナログのホイール・マウスの移動は 7.8f-3 で別の区分にする。</summary>
        ButtonMouse,

        /// <summary>Button のうち、マウスの移動（MouseUp／Down／Left／Right）と、アナログのホイール（元がスティック・トリガー・ジャイロの WUP／WDOWN）への割り当て（7.8f-3）。</summary>
        ButtonAnalogMouse,

        /// <summary>Extras（振動、ライトバー、感度。7.8e）。Lightbar マクロは、7.8e で機能を廃止した。</summary>
        Extras,

        /// <summary>SA 専用の種別（Disconnect BT、Calibrate gyro、Calibrate sixaxis wheel ほか）の割り当て（Phase10-Step4 W4）。出力は SA と同じ部品が作る。</summary>
        Special,
    }
}
