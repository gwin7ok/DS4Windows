using System;
using System.Threading;
using Nefarius.ViGEm.Client;

namespace DS4Windows.Output.Backend
{
    /// <summary>
    /// <see cref="IVirtualPadBackend"/> の ViGEmBus 実装。クライアントの生成は、優先度を上げた別スレッドで行い、
    /// 完了まで呼び出し元がスピン待ちする（旧 <c>VirtualPadService.StartViGEm</c> の手順を逐語で保つ）。
    /// </summary>
    public sealed class ViGEmVirtualPadBackend : IVirtualPadBackend
    {
        private ViGEmClient _client;

        public bool IsStarted => _client != null;

        public void Start()
        {
            Thread tempThread = new Thread(() =>
            {
                try
                {
                    _client = new ViGEmClient();
                }
                catch { }
            });
            tempThread.Priority = ThreadPriority.AboveNormal;
            tempThread.IsBackground = true;
            tempThread.Start();
            while (tempThread.IsAlive)
            {
                Thread.SpinWait(500);
            }
        }

        public void Stop()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public IXbox360PadTarget CreateXbox360Target()
        {
            return new ViGEmXbox360Target(RequireClient());
        }

        public IDs4PadTarget CreateDs4Target()
        {
            return new ViGEmDs4Target(RequireClient());
        }

        private ViGEmClient RequireClient()
        {
            return _client ?? throw new InvalidOperationException("The virtual pad backend is not started.");
        }
    }
}
