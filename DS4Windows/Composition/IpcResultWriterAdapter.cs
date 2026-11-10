using System;
using DS4Windows.Core.Platform;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Composition
{
    /// <summary>
    /// <see cref="IIpcResultWriter"/> の実装（DI の Singleton）。第 2 層が、第 1 層でも第 3 層でもない通信の窓口
    /// （<see cref="IpcMemoryMap"/>。Core/Platform）を、契約を通して使うための橋渡し。
    /// </summary>
    public sealed class IpcResultWriterAdapter : IIpcResultWriter
    {
        private readonly IpcMemoryMap _ipc;

        public IpcResultWriterAdapter(IpcMemoryMap ipc)
        {
            _ipc = ipc ?? throw new ArgumentNullException(nameof(ipc));
        }

        public void WriteQueryResult(string value) => _ipc.WriteResult(value);
    }
}
