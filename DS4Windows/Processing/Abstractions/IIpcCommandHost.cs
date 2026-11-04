using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step11 11-b（Phase13-Step11-Plan.md、台帳 F-2c）: IPC コマンドの実行（<see cref="IIpcCommandProcessor"/>）が、UI（メイン画面）に頼む操作の窓口。
    /// コントローラー一覧（UI のモデル）、サービスの切り替え、アプリの終了、結果の書き込みなど、UI にしかできないものだけを持つ。実装は <c>MainWindow</c>。
    /// 例外の握りつぶしは呼び出し側（<c>MainWindow.HandleCopyData</c>）が行うため、ここでは例外を握らない。
    /// </summary>
    public interface IIpcCommandHost
    {
        /// <summary>サービスを切り替える（動いていれば停止、止まっていれば開始。旧 <c>MainWindow.ChangeService</c>）。</summary>
        void ToggleService();

        /// <summary>アプリを終了する（旧: 編集画面を閉じる <c>MainDS4Window_Closing</c> のあと <c>MainDS4Window_Closed</c>）。</summary>
        void CloseApplication();

        /// <summary>接続中のワイヤレスのコントローラーを、すべて切断する（一覧の写しを作ってから、各行に切断を要求する）。</summary>
        void DisconnectAllControllers();

        /// <summary>指定のスロットのコントローラーに切断を要求する（一覧に無ければ何もしない）。</summary>
        void DisconnectController(int index);

        /// <summary>指定のスロットのライトバーのカスタム色を変える（範囲外の例外は呼び出し側が握る）。</summary>
        void SetLightColor(int index, byte red, byte green, byte blue);

        /// <summary>プロファイル一覧での、そのプロファイル名の位置（無ければ -1）。</summary>
        int FindProfileIndex(string profileName);

        /// <summary>コントローラー一覧の行数。</summary>
        int ControllerCount { get; }

        /// <summary>指定のスロットのデバイス（行が無い・未接続なら null。範囲外は例外）。</summary>
        DS4Device GetControllerDevice(int index);

        /// <summary>ログ（GUI のログ、ログファイル。旧 <c>ControlService.LogDebug</c>）。</summary>
        void LogDebug(string message);

        /// <summary>問い合わせ（query）の結果を、クライアントへ書き込んで通知する（旧 <c>App.WriteIPCResultDataMMF</c>）。</summary>
        void WriteQueryResult(string value);
    }
}
