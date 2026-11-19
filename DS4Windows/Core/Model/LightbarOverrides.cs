using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Model
{
    /// <summary>
    /// ライトバーの強制表示の状態（スロットごと）。DI の Singleton。
    /// Phase14c-Step4（R-05 の前半）: 旧 <c>DS4LightBar</c>（第1層）の public static な配列と旗（<c>forcelight</c>／<c>forcedColor</c>／<c>forcedFlash</c>／
    /// <c>defaultLight</c>／<c>shuttingdown</c>）を、この持ち主へ移した。全層が直接読み書きしていた静的な状態をなくすため、利用者はコンストラクターで受け取る。
    /// メンバーの名前・形は旧と同じ（置き換えを機械的にするため）。色の計算は第 2 層の <c>LightbarService</c>（<c>ILightbarService</c>）が、この状態を読んで行う。
    /// </summary>
    public sealed class LightbarOverrides
    {
        /// <summary>スロットのライトバーを強制色にするか（電池の確認、マクロのライトバーの指定、画面の色の試し表示など）。</summary>
        public readonly bool[] forcelight = new bool[ControllerLimits.MaxControllerCount];

        /// <summary>強制色。</summary>
        public readonly DS4Color[] forcedColor = new DS4Color[ControllerLimits.MaxControllerCount];

        /// <summary>強制色の点滅（0 は点滅なし）。</summary>
        public readonly byte[] forcedFlash = new byte[ControllerLimits.MaxControllerCount];

        /// <summary>既定の色で表示する（サービスの開始・停止の間など）。</summary>
        public bool defaultLight;

        /// <summary>終了の処理中（ライトバーを消す）。</summary>
        public bool shuttingdown;
    }
}
