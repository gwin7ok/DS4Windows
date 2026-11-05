using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// CLI の問い合わせ（<c>-command Query…</c>）の結果を、起動元のコンソールへ書く（クライアントのプロセスで、<c>App</c> が使う）。
    /// 不具合の修正（2026-11-05）:
    /// <list type="bullet">
    /// <item>DS4Windows.exe は GUI アプリ（WinExe）でコンソールを持たないため、<c>|more</c> などで標準出力をパイプにしないと結果が表示されなかった。
    /// 標準出力がリダイレクトされていないときは、起動元（親）のコンソールに接続（AttachConsole）して書く。リダイレクトされているときは、従来どおり標準出力へ書く。</item>
    /// <item>文字コードは、起動元のコンソールの出力コードページ（日本語の Windows の既定は 932）にする。パイプ（<c>|more</c>、<c>findstr</c>）や PowerShell の取り込みも
    /// 同じコードページで読むため。コンソールが無いとき・コードページが使えないときは UTF-8（BOM なし）。</item>
    /// </list>
    /// 対話モードのコマンドプロンプトは GUI アプリの終了を待たないため、結果が次のプロンプトの後ろに出ることがある（承認済みの制約）。
    /// </summary>
    public sealed class CliResultConsoleWriter
    {
        private const int AttachParentProcess = -1;
        private const int StdOutputHandle = -11;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetConsoleOutputCP();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern uint GetFileType(IntPtr hFile);

        private const uint FileTypeChar = 0x0002;
        private const uint FileTypeRemote = 0x8000;

        public void WriteLine(string text)
        {
            try
            {
                // 不具合の修正（2026-11-05 の再確認で発覚）: Console.IsOutputRedirected は、GUI アプリで標準出力のハンドルが無いとき（cmd から |more なしで起動）
                // も true を返す（ハンドルの種類が文字デバイスでないため）。そのため出力先の無い標準出力へ書いて、何も表示されなかった。ハンドルの有無と種類で自分で判定する
                IntPtr stdout = GetStdHandle(StdOutputHandle);
                bool redirected = IsRedirectedOutput(stdout != IntPtr.Zero && stdout != new IntPtr(-1), GetFileType(stdout));
                // リダイレクト先（パイプ・ファイル）は、コンソールに接続する前に開く（接続で標準ハンドルが変わらないように）
                Stream output = redirected ? Console.OpenStandardOutput() : null;

                bool attached = AttachConsole(AttachParentProcess);
                Encoding encoding = SelectEncoding(attached ? GetConsoleOutputCP() : 0);

                if (!redirected)
                {
                    if (!attached)
                        return;   // 起動元にコンソールが無い（エクスプローラーからの起動など）。従来どおり何も表示しない
                    output = OpenConsoleOutput();
                    if (output == null)
                        return;
                }

                using (var writer = new StreamWriter(output, encoding))
                {
                    writer.WriteLine(text);
                    writer.Flush();
                }
            }
            catch (Exception)
            {
                /* 表示の失敗は致命的ではない（従来の Console.WriteLine と同じく、例外で止めない） */
            }
        }

        /// <summary>
        /// 標準出力がパイプ・ファイルへリダイレクトされているか。ハンドルが無ければ false（起動元のコンソールへ書く）。
        /// ハンドルがコンソール（文字デバイス）なら false（コンソールへ書く）。それ以外（ディスク、パイプなど）なら true。
        /// </summary>
        public static bool IsRedirectedOutput(bool hasHandle, uint fileType)
        {
            if (!hasHandle)
                return false;
            return (fileType & ~FileTypeRemote) != FileTypeChar;
        }

        /// <summary>
        /// 出力の文字コードを、コンソールの出力コードページから選ぶ。0（コンソールなし）・65001・使えないコードページは UTF-8（BOM なし）。
        /// コードページは BOM を書かないものだけを返す（StreamWriter が先頭に BOM を書かないため）。
        /// </summary>
        public static Encoding SelectEncoding(uint codePage)
        {
            var utf8 = new UTF8Encoding(false);
            if (codePage == 0 || codePage == 65001)
                return utf8;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // 932 などのコードページを使えるようにする（何度呼んでもよい）
                Encoding encoding = Encoding.GetEncoding((int)codePage);
                return encoding.GetPreamble().Length == 0 ? encoding : utf8;
            }
            catch (Exception)
            {
                return utf8;
            }
        }

        // 接続したコンソールの出力。GUI アプリは接続しても標準ハンドルが設定されないことがあるため、常に CONOUT$ を直接開く
        private static Stream OpenConsoleOutput()
        {
            SafeFileHandle conout = CreateFile("CONOUT$", GenericRead | GenericWrite, FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (conout.IsInvalid)
                return null;
            return new FileStream(conout, FileAccess.Write);
        }
    }
}
