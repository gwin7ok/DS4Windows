using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10b W2（Q2＝A）: 色の純粋な計算（旧: <c>Global</c> の静的メンバー。宣言は移動のみ）。
    /// <c>DS4LightBar</c>（ライトバーの色の遷移）と <c>Mapping</c>（ライトバーのマクロ）が使う。
    /// </summary>
    public static class ColorUtil
    {
        private static byte applyRatio(byte b1, byte b2, double r)
        {
            if (r > 100.0)
                r = 100.0;
            else if (r < 0.0)
                r = 0.0;

            r *= 0.01;
            return (byte)Math.Round((b1 * (1 - r)) + b2 * r, 0);
        }

        public static DS4Color getTransitionedColor(ref DS4Color c1, ref DS4Color c2, double ratio)
        {
            //Color cs = Color.FromArgb(c1.red, c1.green, c1.blue);
            DS4Color cs = new DS4Color
            {
                red = applyRatio(c1.red, c2.red, ratio),
                green = applyRatio(c1.green, c2.green, ratio),
                blue = applyRatio(c1.blue, c2.blue, ratio)
            };
            return cs;
        }

        // Phase7-Step10e W4（承認済み）: 旧 Global から移した孤立コード applyRatio(Color, Color, uint) と HuetoRGB(float, float, float) を削除した。
        // 移動前から呼び出し元が 0 件の WPF 色ブレンドの残骸で、本体のライトバーは DS4LightBar.HuetoRGB(float, byte) を使う。
    }
}
