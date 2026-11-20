namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// キーボード／マウス出力のハンドラの選択・接続・フォールバック・マッピングの初期化。
    /// サービスの開始（<c>Start</c>）が呼ぶ。
    /// </summary>
    public interface IOutputKbmInitializer
    {
        void Initialize();
    }
}
