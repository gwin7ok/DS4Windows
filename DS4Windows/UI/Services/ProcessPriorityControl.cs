using System.Diagnostics;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary><see cref="IProcessPriorityControl"/> の本体。現在のプロセスの <c>PriorityClass</c> を設定する。</summary>
    public sealed class ProcessPriorityControl : IProcessPriorityControl
    {
        public void Apply(ProcessPriorityClass priority)
        {
            using Process process = Process.GetCurrentProcess();
            process.PriorityClass = priority;
        }
    }
}
