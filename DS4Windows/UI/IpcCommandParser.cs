using System.Text;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase9 9-F F-2a（Phase9-F-Plan.md §6）: IPC コマンド（WM_COPYDATA）の文字列の読み取り。旧 <c>MainWindow.WndProc</c> の中の同じ処理を、そのまま移した（状態なし）。
    /// </summary>
    public static class IpcCommandParser
    {
        public const int MinCommandBytes = 4;
        public const int MaxCommandBytes = 256;

        /// <summary>バッファのサイズが、コマンドとして読める範囲（4〜256 バイト）か。</summary>
        public static bool IsValidSize(int byteCount) => byteCount >= MinCommandBytes && byteCount <= MaxCommandBytes;

        /// <summary>
        /// ASCII の文字列を「.」で分け、先頭（コマンド名）を小文字にして返す（旧コードと同じ。小文字化は、現在のカルチャーの規則）。
        /// 例: 「Disconnect.1」→ { "disconnect", "1" }。
        /// </summary>
        public static string[] Parse(byte[] buffer)
        {
            string[] strData = Encoding.ASCII.GetString(buffer).Split('.');
            if (strData.Length >= 1)
            {
                strData[0] = strData[0].ToLower();
            }

            return strData;
        }
    }
}
