using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 新しい単純な機種を、プロトコルとカートリッジだけで足せることの確認（偽の機種）。
    /// 読み取りのループ、解析、電池、<c>Report</c>、出力の書き込み、切断（<c>Removal</c>）は、共通の <see cref="ProtocolDevice"/> が受け持つ。
    /// </summary>
    public class ProtocolDeviceTests
    {
        // 偽の機種のプロトコル: [0]=レポート ID、[1]=Cross ボタン、[2]=左スティック X、[3]=電池（0〜100）
        private sealed class FakeGamepadProtocol : IDeviceProtocol
        {
            private sealed class FakeParser : IInputReportParser
            {
                public void Parse(ReadOnlySpan<byte> report, DS4State state)
                {
                    state.Cross = report[1] != 0;
                    state.LX = report[2];
                }
            }

            public InputDeviceType DeviceType => InputDeviceType.SwitchPro;

            public ConnectionType DetermineConnectionType(IHidTransport hidDevice) => ConnectionType.USB;

            public IInputReportParser Parser { get; } = new FakeParser();

            public bool TryReadBattery(ReadOnlySpan<byte> report, out int percent, out bool charging)
            {
                percent = report[3];
                charging = false;
                return true;
            }

            public bool TryBuildOutputReport(DS4HapticState state, int batteryPercent, byte[] report)
            {
                report[0] = 0x05;
                report[1] = state.rumbleState.RumbleMotorStrengthLeftHeavySlow;
                return true;
            }
        }

        private sealed class FakeGamepadCartridge : ProtocolDeviceCartridge
        {
            private static readonly VidPidInfo[] Table =
            {
                new VidPidInfo(0x1234, 0x5678, "Fake Gamepad", InputDeviceType.SwitchPro, VidPidFeatureSet.DefaultDS4),
            };

            public override InputDeviceFamily Family => InputDeviceFamily.SwitchPro;

            public override IReadOnlyList<VidPidInfo> KnownDevices => Table;

            protected override IDeviceProtocol Protocol { get; } = new FakeGamepadProtocol();
        }

        private static byte[] Report(byte cross, byte lx, byte battery) => new byte[] { 0x01, cross, lx, battery, 0, 0, 0, 0 };

        [Fact]
        public void CartridgeCreatesAProtocolDevice_ThatRunsTheCommonLoop()
        {
            var transport = new FakeHidTransport(inputReportByteLength: 8, outputReportByteLength: 8, vendorId: 0x1234, productId: 0x5678);
            transport.EnqueueRead(HidReadStatus.Success, Report(1, 200, 80));
            transport.EnqueueRead(HidReadStatus.Success, Report(0, 10, 80));
            // 台本が尽きると切断（NotConnected）として扱われる

            var cartridge = new FakeGamepadCartridge();
            DS4Device device = cartridge.Create(InputDeviceType.SwitchPro, transport, "Fake Gamepad", VidPidFeatureSet.DefaultDS4);
            Assert.IsType<ProtocolDevice>(device);
            device.PostInit();

            var reportedCross = new List<bool>();
            var reportedLx = new List<byte>();
            device.Report += (sender, args) =>
            {
                DS4State state = sender.getCurrentStateRef();
                reportedCross.Add(state.Cross);
                reportedLx.Add(state.LX);
            };
            using var removed = new ManualResetEventSlim();
            device.Removal += (sender, args) => removed.Set();

            device.StartUpdate();

            Assert.True(removed.Wait(5000), "台本が尽きたら Removal が来るはず");
            device.StopUpdate();

            Assert.Equal(new[] { true, false }, reportedCross);
            Assert.Equal(new byte[] { 200, 10 }, reportedLx);
            Assert.Equal(80, device.Battery);
            // 電池が変わった最初のフレームで、出力レポートが書かれる（[0]=レポート ID）
            Assert.Contains(transport.Writes, w => w.Kind == "interrupt" && w.Data[0] == 0x05);
        }

        [Fact]
        public void TheCartridgeFollowsTheFamilyContract()
        {
            IInputDeviceCartridge cartridge = new FakeGamepadCartridge();
            Assert.True(cartridge.Handles(InputDeviceType.SwitchPro));
            Assert.False(cartridge.Handles(InputDeviceType.DS4));
            Assert.Single(cartridge.KnownDevices);
        }
    }
}
