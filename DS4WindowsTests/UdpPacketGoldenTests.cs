using System;
using System.Net.NetworkInformation;
using DS4Windows.Core.Model;
using DS4Windows.Output.Udp;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Cemuhook（DSU）の応答パケットのバイト列を凍結する黄金テスト。外部のクライアントとの互換のため、
    /// 組み立ての部品化・割り当ての削減を行っても、バイト列は 1 つも変えない。
    /// CRC の欄（先頭から 8〜11 バイト目）は、CRC のテーブルが初期化済みの状態（実運用と同じ）の値。
    /// 旧実装は、他で CRC を使う前に最初のパケットを作ると CRC が不正（FFFFFFFF）になる欠陥があり、
    /// <c>DsuPacketFormatter</c> が自分でテーブルを初期化して直した。
    /// </summary>
    public class UdpPacketGoldenTests
    {
        private const string BusyPadData =
            "44535553E90354000BD60537443322110200100002020202AABBCCDDEEFF0301040302019AA901010AEBC805FF0000FFFF00FF00FF004D210105E803F401000614001E0008070605040302010000003F0000A0BF000000C0000028410000A2C10000F041";
        private const string IdlePadData =
            "44535553E903540045420DFD443322110200100002020202AABBCCDDEEFF03010000000000000000807F807F0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        private const string VersionResponse = "44535553E90308006EA83AA24433221100001000E9030000";
        private const string PortInfoResponse = "44535553E90310008665C7C84433221101001000020202020102030405060300";

        private static ControllerState Busy()
        {
            var s = new ControllerState();
            s.PacketCounter = 0x01020304;
            s.DpadLeft = true; s.DpadUp = true; s.Options = true; s.L3 = true;
            s.Square = true; s.Circle = true; s.R1 = true; s.L2Btn = true; s.PS = true; s.TouchButton = true;
            s.LX = 10; s.LY = 20; s.RX = 200; s.RY = 250; s.L2 = 33; s.R2 = 77;
            s.TrackPadTouch0 = new ControllerState.TrackPadTouch { IsActive = true, Id = 5, X = 1000, Y = 500 };
            s.TrackPadTouch1 = new ControllerState.TrackPadTouch { IsActive = false, Id = 6, X = 20, Y = 30 };
            s.totalMicroSec = 0x0102030405060708;
            s.Motion = new SixAxis(1, 2, 3, 4, 5, 6, 0.0);
            s.Motion.accelXG = 0.5; s.Motion.accelYG = -1.25; s.Motion.accelZG = 2.0;
            s.Motion.angVelPitch = 10.5; s.Motion.angVelYaw = -20.25; s.Motion.angVelRoll = 30.0;
            return s;
        }

        private const uint ServerId = 0x11223344u;

        private static string PadData(ControllerState report)
        {
            var meta = new DualShockPadMeta
            {
                PadId = 2, PadState = DsState.Connected, Model = DsModel.DS4, ConnectionType = DsConnection.Bluetooth,
                PadMacAddress = PhysicalAddress.Parse("AA-BB-CC-DD-EE-FF"), BatteryStatus = DsBattery.Medium, IsActive = true
            };
            var rsp = new PadDataRspPacket();
            DsuPacketFormatter.BeginDataRspPacket(ref rsp, ServerId, 1001);
            rsp.messageType = 0x100002u;
            DsuPacketFormatter.FillPadInfo(ref rsp, meta, report.PacketCounter);
            DsuPacketFormatter.FillPadData(report, ref rsp);
            byte[] output = new byte[100];
            DsuPacketFormatter.FinishDataRspPacket(ref rsp, output);
            return Convert.ToHexString(output);
        }

        private static string Wrapped(byte[] payload)
        {
            byte[] packet = new byte[payload.Length + 16];
            int idx = DsuPacketFormatter.BeginPacket(packet, ServerId, 1001);
            Array.Copy(payload, 0, packet, idx, payload.Length);
            DsuPacketFormatter.FinishPacket(packet);
            return Convert.ToHexString(packet);
        }

        [Fact]
        public void PadData_WithAllInputs_MatchesGolden() => Assert.Equal(BusyPadData, PadData(Busy()));

        [Fact]
        public void PadData_WithoutMotion_MatchesGolden() => Assert.Equal(IdlePadData, PadData(new ControllerState { Motion = null }));

        [Fact]
        public void VersionResponse_MatchesGolden() =>
            Assert.Equal(VersionResponse, Wrapped(new byte[] { 0, 0, 0x10, 0, 0xE9, 0x03, 0, 0 }));

        [Fact]
        public void PortInfoResponse_MatchesGolden() =>
            Assert.Equal(PortInfoResponse, Wrapped(new byte[] { 1, 0, 0x10, 0, 2, 2, 2, 2, 1, 2, 3, 4, 5, 6, 3, 0 }));
    }
}
