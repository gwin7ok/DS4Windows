using System.Linq;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace DS4Windows.Output.Backend
{
    /// <summary><see cref="IDs4PadTarget"/> の ViGEm 実装（<c>IDualShock4Controller</c> の薄い包み）。</summary>
    internal sealed class ViGEmDs4Target : IDs4PadTarget
    {
        private readonly IDualShock4Controller _cont;

        public ViGEmDs4Target(ViGEmClient client)
        {
            _cont = client.CreateDualShock4Controller();
            _cont.AutoSubmitReport = false;
        }

        public byte LeftTrigger { set => _cont.LeftTrigger = value; }
        public byte RightTrigger { set => _cont.RightTrigger = value; }
        public byte LeftThumbX { set => _cont.LeftThumbX = value; }
        public byte LeftThumbY { set => _cont.LeftThumbY = value; }
        public byte RightThumbX { set => _cont.RightThumbX = value; }
        public byte RightThumbY { set => _cont.RightThumbY = value; }

        public void SetButtonsFull(ushort buttons) => _cont.SetButtonsFull(buttons);
        public void SetSpecialButtonsFull(byte buttons) => _cont.SetSpecialButtonsFull(buttons);
        public void SetDPadDirection(Ds4DPadDirection direction) => _cont.SetDPadDirection(ToDriver(direction));
        public void SubmitReport() => _cont.SubmitReport();
        public void ResetReport() => _cont.ResetReport();
        public void SubmitRawReport(byte[] report) => _cont.SubmitRawReport(report);

        private static DualShock4DPadDirection ToDriver(Ds4DPadDirection direction)
        {
            switch (direction)
            {
                case Ds4DPadDirection.North: return DualShock4DPadDirection.North;
                case Ds4DPadDirection.Northeast: return DualShock4DPadDirection.Northeast;
                case Ds4DPadDirection.East: return DualShock4DPadDirection.East;
                case Ds4DPadDirection.Southeast: return DualShock4DPadDirection.Southeast;
                case Ds4DPadDirection.South: return DualShock4DPadDirection.South;
                case Ds4DPadDirection.Southwest: return DualShock4DPadDirection.Southwest;
                case Ds4DPadDirection.West: return DualShock4DPadDirection.West;
                case Ds4DPadDirection.Northwest: return DualShock4DPadDirection.Northwest;
                default: return DualShock4DPadDirection.None;
            }
        }

        public byte[] AwaitRawOutputReport(int timeoutMs, out bool timedOut)
        {
            return _cont.AwaitRawOutputReport(timeoutMs, out timedOut).ToArray();
        }

        public void Connect() => _cont.Connect();
        public void Disconnect() => _cont.Disconnect();
    }
}
