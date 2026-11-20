using System;
using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary><see cref="IOscTransportFactory"/> の実装（DI の Singleton。状態は持たない）。SharpOSC の通信を作る。</summary>
    public sealed class SharpOscTransportFactory : IOscTransportFactory
    {
        public IOscListenerHandle CreateListener(int port, Action<string, IList<object>> onMessage)
        {
            return new SharpOscListenerHandle(port, onMessage);
        }

        public IOscMessageSender CreateSender(string address, int port)
        {
            return new SharpOscMessageSender(address, port);
        }
    }
}
