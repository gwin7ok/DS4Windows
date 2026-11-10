using System;
using System.Runtime.InteropServices;
using System.Threading;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;
using DS4Windows.UI;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// コマンドライン（<c>-command ...</c>）のクライアントとしての動作。動いている本体のメイン画面へ、コマンドを
    /// <c>WM_COPYDATA</c> で送り、<c>query.</c> のコマンドでは、結果を MMF 経由で受け取ってコンソールへ書く。
    /// このクライアントは、ホストを構築しない短命のプロセスで、終わったらアプリを終了する（終了は呼び出し側）。
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public sealed class CommandLineClient
    {
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string sClass, string sWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        private readonly IpcMemoryMap _ipc;

        public CommandLineClient(IpcMemoryMap ipc)
        {
            _ipc = ipc ?? throw new ArgumentNullException(nameof(ipc));
        }

        /// <summary>コマンドを、動いている本体へ送る。本体が見つからなければ何もしない。</summary>
        public void Run(ArgumentParser parser)
        {
            IntPtr hWndDS4WindowsForm = FindWindow(_ipc.ReadWindowClassName(), "DS4Windows");
            if (hWndDS4WindowsForm == IntPtr.Zero)
                return;

            bool bDoSendMsg = true;
            bool bWaitResultData = false;
            bool bOwnsMutex = false;
            Mutex ipcSingleTaskMutex = null;
            EventWaitHandle ipcNotifyEvent = null;

            COPYDATASTRUCT cds;
            cds.lpData = IntPtr.Zero;

            try
            {
                if (parser.CommandArgs.ToLower().StartsWith("query."))
                {
                    // Query.device# (1..4) command returns a string result via memory mapped file. The cmd is sent to the background DS4Windows
                    // process (via WM_COPYDATA wnd msg), then this client process waits for the availability of the result and prints it to console output pipe.
                    // Use mutex obj to make sure that concurrent client calls won't try to write and read the same MMF result file at the same time.
                    ipcSingleTaskMutex = new Mutex(false, "DS4Windows_IPCResultData_SingleTaskMtx");
                    try
                    {
                        bOwnsMutex = ipcSingleTaskMutex.WaitOne(10000);
                    }
                    catch (AbandonedMutexException)
                    {
                        bOwnsMutex = true;
                    }

                    if (bOwnsMutex)
                    {
                        // This process owns the inter-process sync mutex obj. Let's proceed with creating the output MMF file and waiting for a result.
                        bWaitResultData = true;
                        _ipc.CreateResultBuffer();
                        ipcNotifyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DS4Windows_IPCResultData_ReadyEvent");
                    }
                    else
                        // If the mtx failed then something must be seriously wrong. Cannot do anything in that case because MMF file may be modified by concurrent processes.
                        bDoSendMsg = false;
                }

                if (bDoSendMsg)
                {
                    cds.dwData = IntPtr.Zero;
                    // UTF-8 のバイト列で送る（日本語などの非 ASCII のプロファイル名を通すため。cbData は文字数ではなくバイト数）。ASCII だけの場合は従来と同一
                    byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes(parser.CommandArgs);
                    cds.cbData = cmdBytes.Length;
                    cds.lpData = Marshal.AllocHGlobal(cmdBytes.Length + 1);
                    Marshal.Copy(cmdBytes, 0, cds.lpData, cmdBytes.Length);
                    Marshal.WriteByte(cds.lpData, cmdBytes.Length, 0);
                    SendMessage(hWndDS4WindowsForm, MainWindow.WM_COPYDATA, IntPtr.Zero, ref cds);

                    if (bWaitResultData)
                        // |more なしでも表示し（起動元のコンソールに接続）、コンソールのコードページで書く
                        new CliResultConsoleWriter().WriteLine(_ipc.WaitAndReadResult(ipcNotifyEvent));
                }
            }
            finally
            {
                // Release the result MMF file in the client process before releasing the mtx and letting other client process to proceed with the same MMF file
                _ipc.DisposeResultBuffer();

                // If this was "Query.xxx" cmdline client call then release the inter-process mutex and let other concurrent clients to proceed (if there are anyone waiting for the MMF result file)
                if (bOwnsMutex && ipcSingleTaskMutex != null)
                    ipcSingleTaskMutex.ReleaseMutex();

                if (cds.lpData != IntPtr.Zero)
                    Marshal.FreeHGlobal(cds.lpData);
            }
        }
    }
}
