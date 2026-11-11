using System;
using System.Runtime.InteropServices;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// <see cref="IXInputApi"/> の実装（<c>xinput1_4.dll</c>）。Guide ボタンを読むため、文書化されていない <c>XInputGetStateEx</c>（序数 100）を先に使い、
    /// 使えない環境では、文書化された <c>XInputGetState</c> に切り替える（Guide は取れなくなる）。
    /// </summary>
    public sealed class XInputNativeApi : IXInputApi
    {
        private const uint ErrorSuccess = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct Gamepad
        {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short LX, LY, RX, RY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            public uint Packet;
            public Gamepad Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vibration
        {
            public ushort Left, Right;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "#100")]
        private static extern uint XInputGetStateEx(uint userIndex, out State state);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState(uint userIndex, out State state);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern uint XInputSetState(uint userIndex, ref Vibration vibration);

        private bool useDocumentedOnly;

        public bool TryGetState(int index, out XInputSnapshot state)
        {
            State s;
            uint result;
            if (useDocumentedOnly)
            {
                result = XInputGetState((uint)index, out s);
            }
            else
            {
                try
                {
                    result = XInputGetStateEx((uint)index, out s);
                }
                catch (EntryPointNotFoundException)
                {
                    useDocumentedOnly = true;   // 序数 100 がない環境では、文書化された関数だけにする（Guide は無し）
                    result = XInputGetState((uint)index, out s);
                }
            }

            if (result != ErrorSuccess)
            {
                state = default;
                return false;
            }

            state = new XInputSnapshot(s.Packet, s.Pad.Buttons, s.Pad.LeftTrigger, s.Pad.RightTrigger, s.Pad.LX, s.Pad.LY, s.Pad.RX, s.Pad.RY);
            return true;
        }

        public bool TrySetVibration(int index, ushort leftMotor, ushort rightMotor)
        {
            var vibration = new Vibration { Left = leftMotor, Right = rightMotor };
            return XInputSetState((uint)index, ref vibration) == ErrorSuccess;
        }
    }
}
