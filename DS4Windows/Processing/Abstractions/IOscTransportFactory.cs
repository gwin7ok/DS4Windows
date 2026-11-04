using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: OSC の通信（SharpOSC の <c>UDPListener</c>／<c>UDPSender</c>）の生成。
    /// <see cref="IOscService"/> が SharpOSC の型を直接持たないようにするための境界（実装は <c>SharpOscTransportFactory</c>）。
    /// </summary>
    public interface IOscTransportFactory
    {
        /// <summary>指定のポートで受信を始める。メッセージを受けるたびに <paramref name="onMessage"/>（アドレス、引数）を呼ぶ。</summary>
        IOscListenerHandle CreateListener(int port, Action<string, IList<object>> onMessage);

        /// <summary>指定の宛先へ送る送信側を作る。</summary>
        IOscMessageSender CreateSender(string address, int port);
    }
}
