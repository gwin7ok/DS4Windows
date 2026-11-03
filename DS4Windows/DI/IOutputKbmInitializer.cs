namespace DS4Windows.DI
{
    /// <summary>
    /// Phase13-Step3: キーボード／マウス出力のハンドラの選択・接続・フォールバック・マッピングの初期化
    /// （旧 <c>ControlService.InitOutputKBMHandler</c>）。サービスの開始（<c>Start</c>）が呼ぶ。
    /// </summary>
    public interface IOutputKbmInitializer
    {
        void Initialize();
    }
}
