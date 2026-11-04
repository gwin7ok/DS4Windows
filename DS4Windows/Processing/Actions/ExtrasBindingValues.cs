namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e（Phase8-Step7-8e-Plan.md §3.2。Q9＝A の考え方）: Controls タブの Extras の出力（<see cref="ControlExtrasOutputAction"/>）が受け取る「解釈済みの値」。
    /// 設定の文字列（<c>DS4ControlSettings.extras</c>。カンマ区切りの 9 個）を読む解釈は変換層（<c>ControlBindingBuilder.TryInterpretExtras</c>）が行い、出力は実行するだけにする。
    /// 値は <c>int</c> のまま持つ。出力のとき、旧経路と同じ <c>(byte)</c> のキャストをする（範囲外の値も、旧経路と同じ結果になる）。
    /// </summary>
    public readonly struct ExtrasBindingValues
    {
        public ExtrasBindingValues(int heavyRumble, int lightRumble, bool lightbarEnabled, int red, int green, int blue, int flash, bool sensitivityEnabled, int sensitivity)
        {
            HeavyRumble = heavyRumble;
            LightRumble = lightRumble;
            LightbarEnabled = lightbarEnabled;
            Red = red;
            Green = green;
            Blue = blue;
            Flash = flash;
            SensitivityEnabled = sensitivityEnabled;
            Sensitivity = sensitivity;
        }

        /// <summary>強い振動（値 [0]）。</summary>
        public int HeavyRumble { get; }

        /// <summary>弱い振動（値 [1]）。</summary>
        public int LightRumble { get; }

        /// <summary>ライトバーの強制色を出すか（値 [2] が 1）。</summary>
        public bool LightbarEnabled { get; }

        /// <summary>ライトバーの色の R（値 [3]）。</summary>
        public int Red { get; }

        /// <summary>ライトバーの色の G（値 [4]）。</summary>
        public int Green { get; }

        /// <summary>ライトバーの色の B（値 [5]）。</summary>
        public int Blue { get; }

        /// <summary>ライトバーの点滅（値 [6]）。</summary>
        public int Flash { get; }

        /// <summary>ボタン感度を一時的に変えるか（値 [7] が 1）。</summary>
        public bool SensitivityEnabled { get; }

        /// <summary>一時的なボタン感度（値 [8]）。</summary>
        public int Sensitivity { get; }
    }
}
