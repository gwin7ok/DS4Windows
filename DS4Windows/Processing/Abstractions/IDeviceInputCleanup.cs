namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 入力デバイスの切断（またはプロファイルの適用）のときの、スロット単位の後始末: 保持中の合成キーの解放、マクロのキューの消去、
    /// ボタン／キーのコントローラーの登録の解除。どの段階で失敗しても、残りの後始末は続ける（例外は外へ出さない）。
    /// </summary>
    public interface IDeviceInputCleanup
    {
        void Clear(int device);
    }
}
