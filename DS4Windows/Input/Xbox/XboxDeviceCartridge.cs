using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// Xbox 系コントローラーのカートリッジ。機器は HID の列挙ではなく XInput の供給元（<see cref="XInputTransportSource"/>）が見つけるため、
    /// VID／PID の表は空（HID の列挙に、Xbox の HID の子を載せない）。生成と、シリアル（通信路が作る擬似のシリアル）は、共通の土台のとおり。
    /// </summary>
    public sealed class XboxDeviceCartridge : ProtocolDeviceCartridge
    {
        private static readonly IDeviceProtocol protocol = new XboxProtocol();

        public override InputDeviceFamily Family => InputDeviceFamily.Xbox;

        public override IReadOnlyList<VidPidInfo> KnownDevices { get; } = Array.Empty<VidPidInfo>();

        protected override IDeviceProtocol Protocol => protocol;
    }
}
