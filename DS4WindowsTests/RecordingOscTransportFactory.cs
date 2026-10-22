using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-1 のテスト用スタブ。OSC の通信（SharpOSC のソケット）を作らず、送信したメッセージ（アドレス、値）と、
    /// リスナー／送信側の生成・終了を記録する（テストプロジェクトは SharpOSC を参照しない）。
    /// </summary>
    public sealed class RecordingOscTransportFactory : IOscTransportFactory
    {
        public readonly List<(string Address, object Value)> Sent = new List<(string, object)>();
        public readonly List<int> ListenerPorts = new List<int>();
        public readonly List<(string Address, int Port)> SenderTargets = new List<(string, int)>();
        public int ListenersClosed;
        public int SendersClosed;
        public Action<string, IList<object>> LastOnMessage;

        public IOscListenerHandle CreateListener(int port, Action<string, IList<object>> onMessage)
        {
            ListenerPorts.Add(port);
            LastOnMessage = onMessage;
            return new Listener(this);
        }

        public IOscMessageSender CreateSender(string address, int port)
        {
            SenderTargets.Add((address, port));
            return new Sender(this);
        }

        private sealed class Listener : IOscListenerHandle
        {
            private readonly RecordingOscTransportFactory _owner;
            public Listener(RecordingOscTransportFactory owner) { _owner = owner; }
            public void Close() { _owner.ListenersClosed++; }
        }

        private sealed class Sender : IOscMessageSender
        {
            private readonly RecordingOscTransportFactory _owner;
            public Sender(RecordingOscTransportFactory owner) { _owner = owner; }
            public void Send(string address, object value) { lock (_owner.Sent) { _owner.Sent.Add((address, value)); } }
            public void Close() { _owner.SendersClosed++; }
        }
    }
}
