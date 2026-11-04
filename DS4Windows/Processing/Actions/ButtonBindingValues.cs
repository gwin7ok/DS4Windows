namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step7-8a-Plan.md W2。Q9＝A）: Button の出力が受け取る「解釈済みの値」。
    /// 設定から何を出すかを決める解釈は変換層（ビルダー）が行い、出力（<see cref="ButtonOutputAction"/>）は、この値を実行するだけにする。
    /// </summary>
    public readonly struct ButtonBindingValues
    {
        public ButtonBindingValues(bool hasControl, X360Controls control, bool toggle, int strength = 100)
        {
            HasControl = hasControl;
            Control = control;
            Toggle = toggle;
            Strength = strength < 0 ? 0 : (strength > 100 ? 100 : strength);
        }

        /// <summary>出力するボタンが解釈できたか（<c>details</c> が整数として読めなければ偽。偽のときは何も出力しない）。</summary>
        public bool HasControl { get; }

        /// <summary>出力する X360 のボタン（<see cref="HasControl"/> が偽のときは意味を持たない）。</summary>
        public X360Controls Control { get; }

        /// <summary>Toggle（立ち上がりで反転し、ON の間は出力する）か。偽なら Press（成立中のみ出力）。</summary>
        public bool Toggle { get; }

        /// <summary>
        /// Phase10-Step6 後の Step5: アナログな出力（軸・トリガー・マウス移動。<see cref="AnalogOutputTargets"/>）の強さ（0〜100%。既定 100）。
        /// アナログでない出力先では、使わない。
        /// </summary>
        public int Strength { get; }
    }
}
