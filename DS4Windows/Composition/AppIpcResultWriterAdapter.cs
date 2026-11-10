using System.Windows;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Composition
{
    /// <summary>
    /// Phase14b-Step16: <see cref="IIpcResultWriter"/> の実装（DI の Singleton）。結果のメモリマップドファイルを持つ <see cref="App"/> へ委譲する。
    /// <see cref="App"/> は呼び出しの時点で <see cref="Application.Current"/> から取る（旧呼び出しと同じ）。
    /// </summary>
    public sealed class AppIpcResultWriterAdapter : IIpcResultWriter
    {
        public void WriteQueryResult(string value) => (Application.Current as App).WriteIPCResultDataMMF(value);
    }
}
