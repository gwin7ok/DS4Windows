namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: マクロの 1 つの値（キー、マウスボタン、ゲームパッドのボタン、ライトバーの色、振動、待ち）の実行。
    /// 以前は <c>Mapping.PlayMacroCodeValue</c>（private。約 160 行）だった。
    /// ゲームパッドのボタンの出力フラグと個数は <c>MacroRuntimeState</c> にデバイスごとに持つ（旧: 全コントローラーで 1 つを共有。ユーザー判断 Q2）。
    /// </summary>
    public interface IMacroStepExecutor
    {
        /// <summary>
        /// マクロの値 <paramref name="macroCodeValue"/> を実行する。同じキー・ボタンの値が 2 回続くと、押下と離上になる（<paramref name="keydown"/> が現在の押下の状態を持つ）。
        /// </summary>
        /// <param name="keydown">そのマクロの 1 回分の再生で押している値の表（512 要素。呼び出し側が持つ）。</param>
        /// <returns>true のとき、呼び出し側が <c>macroCodeValue - 300</c> ミリ秒の待ちを入れる（値が待ちの指定だった）。</returns>
        bool Execute(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown);
    }
}
