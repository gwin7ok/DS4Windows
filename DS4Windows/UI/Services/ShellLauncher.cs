using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using DS4Windows.Core.Utilities;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary><see cref="IShellLauncher"/> の本体。<c>Process.Start</c> の失敗（ファイルがない・権限がない）は無視して続ける（従来の動き）。</summary>
    public sealed class ShellLauncher : IShellLauncher
    {
        public void OpenPath(string path)
        {
            var startInfo = new ProcessStartInfo(path) { UseShellExecute = true };
            Start(startInfo);
        }

        public void RunProgram(string fileName, string? arguments = null)
        {
            using Process? started = arguments == null ? Process.Start(fileName) : Process.Start(fileName, arguments);
        }

        public void RunProgramIfExists(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                using Process? started = Process.Start(path);
            }
            catch (Exception caught) when (caught is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                // 起動できなくても画面の操作は続ける（失敗は呼び出し元へ伝えない）
                return;
            }
        }

        public void OpenUrl(string url) => Util.StartProcessHelper(url);

        public void RunElevatedAndWait(string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo { FileName = fileName, Arguments = arguments, Verb = "runas", UseShellExecute = true };
            try
            {
                using Process? started = Process.Start(startInfo);
                started?.WaitForExit();
            }
            catch (Exception caught) when (caught is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                // 利用者が昇格を拒んだ場合も含め、導入できなくても画面の操作は続ける
                return;
            }
        }

        private static void Start(ProcessStartInfo startInfo)
        {
            try
            {
                using Process? started = Process.Start(startInfo);
            }
            catch (Exception caught) when (caught is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                // 起動できなくても画面の操作は続ける（失敗は呼び出し元へ伝えない）
                return;
            }
        }
    }
}
