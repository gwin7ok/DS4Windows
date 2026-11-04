using System;
using System.Collections.Generic;
using DS4Windows.DI;
using SharpOSC;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-1: <see cref="IOscListenerHandle"/> の実装。SharpOSC の <c>UDPListener</c> を包む（旧 <c>ControlService.oscListener</c>）。
    /// 受けたパケットは <c>OscMessage</c> にキャストし（旧実装と同じ。バンドルなどは <see cref="InvalidCastException"/>）、アドレスと引数を <c>onMessage</c> へ渡す。
    /// </summary>
    public sealed class SharpOscListenerHandle : IOscListenerHandle
    {
        private readonly UDPListener _listener;

        public SharpOscListenerHandle(int port, Action<string, IList<object>> onMessage)
        {
            HandleOscPacket callback = delegate (OscPacket packet)
            {
                var messageReceived = (OscMessage)packet;

                // If typecase fails, exit
                if (messageReceived == null)
                {
                    return;
                }

                onMessage(messageReceived.Address, messageReceived.Arguments);
            };

            _listener = new UDPListener(port, callback: callback);
        }

        public void Close()
        {
            _listener.Close();
        }
    }
}
