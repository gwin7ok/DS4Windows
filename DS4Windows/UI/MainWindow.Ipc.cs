#nullable enable

using System;
using System.Runtime.InteropServices;

namespace DS4Windows.UI
{
    /// <summary>
    /// MainWindow の IPC コマンド（WM_COPYDATA）の処理。旧 <c>WndProc</c> の巨大な if の連なりを、コマンドごとのメソッドへ分けた。
    /// コマンドの受け付け順（if／else if の順序）、条件、例外の握りつぶし、query の結果の書き込みは、従来のまま。UI の部品に触れるため、MainWindow の partial に置く。
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// WM_COPYDATA: DS4Win のコマンドが、文字列として lpData のバッファに入っている。例外は、すべて握りつぶす（致命的ではないため）。
        /// </summary>
        // IPC の終了コマンド: 編集画面を閉じる Closing を通してから、アプリの終了の処理（Closed）へ進む
        private void CloseApplicationForIpc()
        {
            MainDS4Window_Closing(null, new System.ComponentModel.CancelEventArgs());
            MainDS4Window_Closed(this, new System.EventArgs());
        }

        private void HandleCopyData(IntPtr lParam)
        {
            try
            {
                DS4Windows.Core.Platform.COPYDATASTRUCT cds = Marshal.PtrToStructure<DS4Windows.Core.Platform.COPYDATASTRUCT>(lParam);   // 旧 App.COPYDATASTRUCT
                if (IpcCommandParser.IsValidSize(cds.cbData))
                {
                    byte[] buffer = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, buffer, 0, cds.cbData);
                    string[] strData = IpcCommandParser.Parse(buffer);

                    // コマンドの実行は IpcCommandProcessor へ移した（受け付けの順序・条件は従来のまま）
                    ipcDispatcher.Execute(ipcHost, strData);
                }
            }
            catch
            {
                // Eat all exceptions in WM_COPYDATA because exceptions here are not fatal for DS4Windows background app
            }
        }
    }
}
