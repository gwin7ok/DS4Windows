#nullable enable

using System.Text;

namespace DS4Windows.UI
{
    /// <summary>
    /// IPC コマンド（WM_COPYDATA）の文字列の読み取り。旧 <c>MainWindow.WndProc</c> の中の同じ処理を、そのまま移した（状態なし）。
    /// </summary>
    public static class IpcCommandParser
    {
        public const int MinCommandBytes = 4;
        public const int MaxCommandBytes = 256;

        /// <summary>バッファのサイズが、コマンドとして読める範囲（4〜256 バイト）か。</summary>
        public static bool IsValidSize(int byteCount) => byteCount >= MinCommandBytes && byteCount <= MaxCommandBytes;

        /// <summary>
        /// UTF-8 の文字列（ASCII を含む）を「.」で分け、先頭（コマンド名）を小文字にして返す。
        /// 例: 「Disconnect.1」→ { "disconnect", "1" }。
        /// </summary>
        public static string[] Parse(byte[] buffer)
        {
            string[] strData = Encoding.UTF8.GetString(buffer).Split('.'); // UTF-8（ASCII 互換。非 ASCII のプロファイル名を通す）
            if (strData.Length >= 1)
            {
                strData[0] = strData[0].ToLower();
            }

            return strData;
        }
    }
}
