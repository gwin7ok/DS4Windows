namespace DS4Windows.UI
{
    /// <summary>
    /// 出力ピッカーの、コントローラーの図（<c>ControllerVisualizerControl</c>）が配置表から生成するボタンと、割り当てる出力（<c>X360Controls</c> の名前）の表。
    /// XAML に書けないため、ここに 1 か所で持つ。XAML に置いたボタンは、<c>Command</c>＋<c>CommandParameter</c> を直接持つ。
    /// </summary>
    public static class BindingWindowVisualizerButtons
    {
        public static readonly (string Id, string Control)[] Table =
        {
            ("aBtn", "A"),
            ("bBtn", "B"),
            ("xBtn", "X"),
            ("yBtn", "Y"),
            ("lbBtn", "LB"),
            ("ltBtn", "LT"),
            ("rbBtn", "RB"),
            ("rtBtn", "RT"),
            ("backBtn", "Back"),
            ("startBtn", "Start"),
            ("guideBtn", "Guide"),
            ("lsbBtn", "LS"),
            ("lsuBtn", "LYNeg"),
            ("lsrBtn", "LXPos"),
            ("lsdBtn", "LYPos"),
            ("lslBtn", "LXNeg"),
            ("dpadUBtn", "DpadUp"),
            ("dpadRBtn", "DpadRight"),
            ("dpadDBtn", "DpadDown"),
            ("dpadLBtn", "DpadLeft"),
            ("rsbBtn", "RS"),
            ("rsuBtn", "RYNeg"),
            ("rsrBtn", "RXPos"),
            ("rsdBtn", "RYPos"),
            ("rslBtn", "RXNeg"),
            ("touchpadClickBtn", "TouchpadClick"),
        };
    }
}
