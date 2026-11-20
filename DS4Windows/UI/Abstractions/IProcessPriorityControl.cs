using System.Diagnostics;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// このアプリ自身のプロセスの優先度を変える窓口。ViewModel が <c>Process</c> を直接触らず、テストで実際の優先度を変えないための契約
    /// （テストでは、呼び出しを記録する偽物を渡す）。
    /// </summary>
    public interface IProcessPriorityControl
    {
        /// <summary>自分のプロセスの優先度を設定する。</summary>
        void Apply(ProcessPriorityClass priority);
    }
}
