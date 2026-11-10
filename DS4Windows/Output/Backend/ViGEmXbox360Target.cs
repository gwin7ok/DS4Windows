using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace DS4Windows.Output.Backend
{
    /// <summary><see cref="IXbox360PadTarget"/> の ViGEm 実装（<c>IXbox360Controller</c> の薄い包み）。</summary>
    internal sealed class ViGEmXbox360Target : IXbox360PadTarget
    {
        private readonly IXbox360Controller _cont;

        public ViGEmXbox360Target(ViGEmClient client)
        {
            _cont = client.CreateXbox360Controller();
            _cont.AutoSubmitReport = false;
        }

        public byte LeftTrigger { set => _cont.LeftTrigger = value; }
        public byte RightTrigger { set => _cont.RightTrigger = value; }
        public short LeftThumbX { set => _cont.LeftThumbX = value; }
        public short LeftThumbY { set => _cont.LeftThumbY = value; }
        public short RightThumbX { set => _cont.RightThumbX = value; }
        public short RightThumbY { set => _cont.RightThumbY = value; }

        public int UserIndex => _cont.UserIndex;

        public void SetButtonsFull(ushort buttons) => _cont.SetButtonsFull(buttons);
        public void SubmitReport() => _cont.SubmitReport();
        public void ResetReport() => _cont.ResetReport();
        public void Connect() => _cont.Connect();
        public void Disconnect() => _cont.Disconnect();

        public IDisposable SubscribeFeedback(Action<byte, byte> onRumble)
        {
            Xbox360FeedbackReceivedEventHandler handler = (sender, args) => onRumble(args.LargeMotor, args.SmallMotor);
            _cont.FeedbackReceived += handler;
            return new Subscription(_cont, handler);
        }

        private sealed class Subscription : IDisposable
        {
            private IXbox360Controller _cont;
            private Xbox360FeedbackReceivedEventHandler _handler;

            public Subscription(IXbox360Controller cont, Xbox360FeedbackReceivedEventHandler handler)
            {
                _cont = cont;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_cont != null)
                {
                    _cont.FeedbackReceived -= _handler;
                    _cont = null;
                    _handler = null;
                }
            }
        }
    }
}
