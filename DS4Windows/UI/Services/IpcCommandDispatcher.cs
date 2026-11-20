#nullable enable

using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// CLI からの IPC コマンドの実行を、<see cref="IIpcCommandProcessor"/> へ渡す窓口。
    /// 画面（MainWindow）が処理の契約を直接参照しないためのもので、画面への操作の依頼は <see cref="MainWindowIpcHost"/> を渡す。
    /// </summary>
    public sealed class IpcCommandDispatcher
    {
        private readonly IIpcCommandProcessor _processor;

        public IpcCommandDispatcher(IIpcCommandProcessor processor)
        {
            ArgumentNullException.ThrowIfNull(processor);
            _processor = processor;
        }

        public void Execute(MainWindowIpcHost host, string[] data) => _processor.Execute(host, data);
    }
}
