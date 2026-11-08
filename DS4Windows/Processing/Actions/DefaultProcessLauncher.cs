using System;
using System.Diagnostics;
using System.IO;
using DS4Windows.Core.Logging;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// IProcessLauncher の標準実装
    /// プロセス起動・引数渡し・非表示ウィンドウ起動をカプセル化
    /// </summary>
    public class DefaultProcessLauncher : IProcessLauncher
    {
        public void Launch(string filePath)
        {
            Launch(filePath, string.Empty, true, false);
        }

        public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = useShellExecute
                };

                if (hidden)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                }

                using var process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Process_LaunchFailed", fileName, ex.Message);
            }
        }
    }
}