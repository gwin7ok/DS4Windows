namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 後の Step5: Button の出力先（<see cref="X360Controls"/>）から、アナログな出力の種類（<see cref="AnalogOutputKind"/>）を決める 1 か所の規則。
    /// 割り当て編集画面（強さの入力を出すか）と、SA の出力の組み立て（強さを使う出力にするか）が、同じ規則を使う。
    /// 分類は、Controls の <c>ControlBindingBuilder.TryInterpretPadTarget</c>／<c>TryInterpretAnalogMouse</c> と同じ（Controls 側は、そのまま残す）。
    /// </summary>
    public static class AnalogOutputTargets
    {
        public static AnalogOutputKind KindOf(X360Controls control)
        {
            switch (control)
            {
                case X360Controls.MouseUp:
                case X360Controls.MouseDown:
                case X360Controls.MouseLeft:
                case X360Controls.MouseRight:
                    return AnalogOutputKind.MouseMove;
            }

            if (control < X360Controls.LXNeg || control > X360Controls.Start) return AnalogOutputKind.None;

            DS4Controls output = X360ControlTables.reverseX360ButtonMapping[(int)control];
            if (output >= DS4Controls.LXNeg && output <= DS4Controls.RYPos) return AnalogOutputKind.PadAxis;
            if (output == DS4Controls.L2 || output == DS4Controls.R2) return AnalogOutputKind.PadTrigger;
            return AnalogOutputKind.None;
        }
    }
}
