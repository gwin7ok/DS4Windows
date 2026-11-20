namespace DS4Windows.Input.Abstractions
{
    /// <summary>XInput で読んだ 1 回分の状態（<c>XINPUT_STATE</c> の写し）。</summary>
    public readonly struct XInputSnapshot
    {
        public XInputSnapshot(uint packet, ushort buttons, byte leftTrigger, byte rightTrigger, short lx, short ly, short rx, short ry)
        {
            Packet = packet;
            Buttons = buttons;
            LeftTrigger = leftTrigger;
            RightTrigger = rightTrigger;
            LX = lx;
            LY = ly;
            RX = rx;
            RY = ry;
        }

        /// <summary>状態が変わるたびに進む番号。</summary>
        public uint Packet { get; }

        /// <summary><c>XINPUT_GAMEPAD.wButtons</c>（Guide の 0x0400 を含みうる）。</summary>
        public ushort Buttons { get; }

        public byte LeftTrigger { get; }
        public byte RightTrigger { get; }
        public short LX { get; }
        public short LY { get; }
        public short RX { get; }
        public short RY { get; }
    }
}
