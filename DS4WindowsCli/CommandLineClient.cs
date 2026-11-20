#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DS4Windows.Core.Platform;

namespace DS4Windows.Cli
{
    /// <summary>
    /// 動いている本体のメイン画面へ、コマンドを <c>WM_COPYDATA</c> で送り、<c>query.</c> のコマンドでは、結果を共有メモリ（MMF）で受け取って書く。
    /// 結果は、コンソール種別のプロセスの標準出力へ書くため、cmd.exe はこのプロセスの終了を待ち、結果は次のプロンプトより前に出る。
    /// 複数のクライアントが同時に問い合わせても結果が混ざらないよう、名前つきミューテックスで 1 つずつ行う。
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public sealed class CommandLineClient
    {
        public const int ExitOk = 0;
        public const int ExitNotRunning = 1;
        public const int ExitBadArguments = 2;
        public const int ExitBusy = 3;

        private const uint WmCopyData = 0x004A;

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? sClass, string sWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        private readonly IpcMemoryMap _ipc;
        private readonly TextWriter _output;

        public CommandLineClient(IpcMemoryMap ipc, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(ipc);
            _ipc = ipc;
            ArgumentNullException.ThrowIfNull(output);
            _output = output;
        }

        /// <summary>コマンドを送る。終了コード（<see cref="ExitOk"/> など）を返す。</summary>
        public int Run(string command, bool isQuery)
        {
            IntPtr hWnd = FindWindow(_ipc.ReadWindowClassName(), "DS4Windows");
            if (hWnd == IntPtr.Zero)
                return ExitNotRunning;

            bool ownsMutex = false;
            Mutex? mutex = null;
            EventWaitHandle? readyEvent = null;
            COPYDATASTRUCT cds;
            cds.lpData = IntPtr.Zero;
            cds.dwData = IntPtr.Zero;
            cds.cbData = 0;

            try
            {
                if (isQuery)
                {
                    mutex = new Mutex(false, "DS4Windows_IPCResultData_SingleTaskMtx");
                    try
                    {
                        ownsMutex = mutex.WaitOne(10000);
                    }
                    catch (AbandonedMutexException)
                    {
                        ownsMutex = true;
                    }

                    if (!ownsMutex)
                        return ExitBusy;   // 他のクライアントが使っている間は、結果の共有メモリを触れない

                    _ipc.CreateResultBuffer();
                    readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DS4Windows_IPCResultData_ReadyEvent");
                }

                // UTF-8 のバイト列で送る（日本語などの非 ASCII のプロファイル名を通すため。cbData は文字数ではなくバイト数）
                byte[] bytes = Encoding.UTF8.GetBytes(command);
                cds.cbData = bytes.Length;
                cds.lpData = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, cds.lpData, bytes.Length);
                Marshal.WriteByte(cds.lpData, bytes.Length, 0);
                SendMessage(hWnd, WmCopyData, IntPtr.Zero, ref cds);

                if (isQuery)
                {
                    _output.WriteLine(_ipc.WaitAndReadResult(readyEvent));
                    _output.Flush();
                }
            }
            finally
            {
                // 次のクライアントが同じ共有メモリを作れるよう、ミューテックスを手放す前に結果の共有メモリを手放す
                _ipc.DisposeResultBuffer();
                if (ownsMutex && mutex != null)
                    mutex.ReleaseMutex();
                if (cds.lpData != IntPtr.Zero)
                    Marshal.FreeHGlobal(cds.lpData);
                readyEvent?.Dispose();
                mutex?.Dispose();
            }

            return ExitOk;
        }
    }
}
