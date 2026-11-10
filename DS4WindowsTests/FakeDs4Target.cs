using System;
using System.Collections.Generic;
using DS4Windows.Output.Backend;

namespace DS4WindowsTests
{
    /// <summary>受けた操作を記録する、偽の <see cref="IDs4PadTarget"/>。</summary>
    public sealed class FakeDs4Target : IDs4PadTarget
    {
        public ushort Buttons { get; private set; }
        public byte SpecialButtons { get; private set; }
        public Ds4DPadDirection DPad { get; private set; } = Ds4DPadDirection.None;
        public byte LeftTriggerValue { get; private set; }
        public byte RightTriggerValue { get; private set; }
        public byte LeftThumbXValue { get; private set; }
        public byte LeftThumbYValue { get; private set; }
        public byte RightThumbXValue { get; private set; }
        public byte RightThumbYValue { get; private set; }
        public int SubmitCount { get; private set; }
        public int ResetCount { get; private set; }
        public int ConnectCount { get; private set; }
        public int DisconnectCount { get; private set; }
        public List<byte[]> RawReports { get; } = new List<byte[]>();

        public byte LeftTrigger { set => LeftTriggerValue = value; }
        public byte RightTrigger { set => RightTriggerValue = value; }
        public byte LeftThumbX { set => LeftThumbXValue = value; }
        public byte LeftThumbY { set => LeftThumbYValue = value; }
        public byte RightThumbX { set => RightThumbXValue = value; }
        public byte RightThumbY { set => RightThumbYValue = value; }

        public void SetButtonsFull(ushort buttons) => Buttons = buttons;
        public void SetSpecialButtonsFull(byte buttons) => SpecialButtons = buttons;
        public void SetDPadDirection(Ds4DPadDirection direction) => DPad = direction;
        public void SubmitReport() => SubmitCount++;
        public void ResetReport() => ResetCount++;
        public void SubmitRawReport(byte[] report) => RawReports.Add((byte[])report.Clone());
        public void Connect() => ConnectCount++;
        public void Disconnect() => DisconnectCount++;

        public byte[] AwaitRawOutputReport(int timeoutMs, out bool timedOut)
        {
            timedOut = true;
            return Array.Empty<byte>();
        }
    }
}
