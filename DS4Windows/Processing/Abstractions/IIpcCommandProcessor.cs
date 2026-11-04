namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step11 11-b（台帳 F-2c）: IPC コマンド（解析済みの文字列の配列）を実行する。旧 <c>MainWindow.Ipc.cs</c> の 9 個の <c>Ipc*</c> を、UI から独立させたもの。
    /// 受け付けの順序、引数の個数の条件、範囲の検査は従来のまま。UI にしかできない操作は <see cref="IIpcCommandHost"/> に頼む。
    /// </summary>
    public interface IIpcCommandProcessor
    {
        /// <summary>
        /// コマンドを実行する。<paramref name="strData"/> の先頭がコマンド名（小文字。<c>IpcCommandParser.Parse</c> の結果）。
        /// 未知のコマンド、引数の足りないコマンドは何もしない。例外は握らない（呼び出し側が握る）。
        /// </summary>
        void Execute(IIpcCommandHost host, string[] strData);
    }
}
