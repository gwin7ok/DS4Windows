#nullable enable

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// IPC コマンドの実行（<see cref="IIpcCommandProcessor"/>）が、UI（メイン画面）に頼む操作の窓口。
    /// コントローラー一覧（UI のモデル）、サービスの切り替え、アプリの終了、結果の書き込みなど、UI にしかできないものだけを持つ。実装は <c>MainWindow</c>。
    /// 例外の握りつぶしは呼び出し側（<c>MainWindow.HandleCopyData</c>）が行うため、ここでは例外を握らない。
    /// </summary>
    public interface IIpcCommandHost
    {
        /// <summary>サービスを切り替える（動いていれば停止、止まっていれば開始。旧 <c>MainWindow.ChangeService</c>）。</summary>
        void ToggleService();

        /// <summary>アプリを終了する。</summary>
        void CloseApplication();

        /// <summary>接続中のワイヤレスのコントローラーを、すべて切断する（一覧の写しを作ってから、各行に切断を要求する）。</summary>
        void DisconnectAllControllers();

        /// <summary>指定の入力スロットのコントローラーに切断を要求する（一覧に無ければ何もしない）。</summary>
        void DisconnectController(int index);

        /// <summary>指定の入力スロットのライトバーのカスタム色を変える（そのスロットに行がなければ何もしない）。</summary>
        void SetLightColor(int index, byte red, byte green, byte blue);

        /// <summary>プロファイル一覧での、そのプロファイル名の位置（無ければ -1。大文字小文字は区別しない）。</summary>
        int FindProfileIndex(string profileName);

        /// <summary>プロファイル一覧のその位置の、正式なプロファイル名（大文字小文字は一覧のとおり）。範囲外は null。</summary>
        string GetProfileName(int index);

        /// <summary>指定の入力スロットのデバイス（行が無い・未接続・範囲外なら null）。</summary>
        IControllerSlotView GetControllerDevice(int index);

        /// <summary>ログ（GUI のログ、ログファイル。旧 <c>ControlService.LogDebug</c>）。</summary>
        void LogDebug(string message);
    }
}
