using System;
using System.Collections.Generic;
using DS4Windows.Output.Backend;

namespace DS4WindowsTests
{
    /// <summary>受けた操作を記録する、偽の <see cref="IXbox360PadTarget"/>。</summary>
    public sealed class FakeXbox360Target : IXbox360PadTarget
    {
        private readonly List<Action<byte, byte>> _subscribers = new List<Action<byte, byte>>();

        public ushort Buttons { get; private set; }
        public byte LeftTriggerValue { get; private set; }
        public byte RightTriggerValue { get; private set; }
        public short LeftThumbXValue { get; private set; }
        public short LeftThumbYValue { get; private set; }
        public short RightThumbXValue { get; private set; }
        public short RightThumbYValue { get; private set; }
        public int SubmitCount { get; private set; }
        public int ResetCount { get; private set; }
        public int ConnectCount { get; private set; }
        public int DisconnectCount { get; private set; }
        public int ActiveSubscriptions => _subscribers.Count;
        public int UserIndexValue { get; set; } = 1;

        public byte LeftTrigger { set => LeftTriggerValue = value; }
        public byte RightTrigger { set => RightTriggerValue = value; }
        public short LeftThumbX { set => LeftThumbXValue = value; }
        public short LeftThumbY { set => LeftThumbYValue = value; }
        public short RightThumbX { set => RightThumbXValue = value; }
        public short RightThumbY { set => RightThumbYValue = value; }
        public int UserIndex => UserIndexValue;

        public void SetButtonsFull(ushort buttons) => Buttons = buttons;
        public void SubmitReport() => SubmitCount++;
        public void ResetReport() => ResetCount++;
        public void Connect() => ConnectCount++;
        public void Disconnect() => DisconnectCount++;

        public IDisposable SubscribeFeedback(Action<byte, byte> onRumble)
        {
            _subscribers.Add(onRumble);
            return new Subscription(this, onRumble);
        }

        /// <summary>ゲームからの振動の戻りを模す。</summary>
        public void RaiseFeedback(byte largeMotor, byte smallMotor)
        {
            foreach (Action<byte, byte> subscriber in _subscribers.ToArray())
                subscriber(largeMotor, smallMotor);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly FakeXbox360Target _owner;
            private readonly Action<byte, byte> _handler;

            public Subscription(FakeXbox360Target owner, Action<byte, byte> handler)
            {
                _owner = owner;
                _handler = handler;
            }

            public void Dispose() => _owner._subscribers.Remove(_handler);
        }
    }
}
