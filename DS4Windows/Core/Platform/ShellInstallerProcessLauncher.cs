#nullable enable
using System;
using System.Diagnostics;

namespace DS4Windows.Core.Platform
{
    /// <summary>シェルで導入プログラムを起動する（管理者への昇格のため <c>UseShellExecute</c>）。</summary>
    public sealed class ShellInstallerProcessLauncher : IInstallerProcessLauncher
    {
        public Func<bool>? Start(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(filePath);
            startInfo.UseShellExecute = true; // 管理者として実行するために必要
            Process? process = Process.Start(startInfo);
            return process == null ? null : () => process.HasExited;
        }
    }
}
