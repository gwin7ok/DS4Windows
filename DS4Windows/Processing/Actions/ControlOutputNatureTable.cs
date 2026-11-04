namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I4a: Controls タブの束縛の出力の種類（<see cref="ControlOutputNature"/>）を決める、唯一の場所。
    /// 規則（Phase10-Step4c W4 の判定の表と同じ。判定の中身は変えない）: パッドの軸・トリガー、素通し、アナログマウス（マウスの移動・アナログのホイール）、
    /// 出力先のない（消すだけの）パッドはアナログ。それ以外はデジタル。
    /// </summary>
    public static class ControlOutputNatureTable
    {
        /// <summary>区分から決まる出力の種類。パッドへの割り当て（出力先で変わる）は <see cref="ForPad"/> を使う。<see cref="ControlBindingKind.ButtonPad"/> は、素通しの区分でもあり、アナログ。</summary>
        public static ControlOutputNature ForKind(ControlBindingKind kind)
        {
            switch (kind)
            {
                case ControlBindingKind.ButtonPad:
                case ControlBindingKind.ButtonAnalogMouse:
                    return ControlOutputNature.Analog;
                default:
                    return ControlOutputNature.Digital;
            }
        }

        /// <summary>パッドへの割り当て。出力先がボタンならデジタル、軸・トリガー、または出力を持たない（消すだけ）ならアナログ。</summary>
        public static ControlOutputNature ForPad(bool eraseOnly, PadTargetKind target)
            => !eraseOnly && target == PadTargetKind.Button ? ControlOutputNature.Digital : ControlOutputNature.Analog;
    }
}
