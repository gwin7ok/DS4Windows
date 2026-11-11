using System;
using System.Buffers.Binary;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// XInput を HID の通信路（<see cref="IHidTransport"/>）に見せる <see cref="XInputTransport"/> と、機器を見つける <see cref="XInputTransportSource"/> の確認。
    /// OS の API（xinput1_4.dll、XUSB の IOCTL）は偽物に替える。
    /// </summary>
    public class XInputTransportTests
    {
        private const string Path = @"\\?\usb#vid_045e&pid_028e#serial1#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

        private static (XInputTransport Transport, FakeXInputApi Api, FakeXusbInterfaceSource Interfaces) Create(int playerIndex = 0)
        {
            var api = new FakeXInputApi();
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path, 0x045E, 0x028E, playerIndex);
            var transport = new XInputTransport(api, interfaces, interfaces.Enumerate().Single());
            return (transport, api, interfaces);
        }

        [Fact]
        public void ReadFile_ComposesTheSyntheticReport_FromTheXInputState()
        {
            var (transport, api, _) = Create();
            api.Set(0, new XInputSnapshot(1, XInputReportLayout.A | XInputReportLayout.DpadUp, 200, 50, 1000, -2000, 3000, -4000));
            var buffer = new byte[XInputReportLayout.ReportLength];

            HidReadStatus status = transport.ReadFile(buffer);

            Assert.Equal(HidReadStatus.Success, status);
            Assert.Equal(0, buffer[0]);
            Assert.Equal(XInputReportLayout.A | XInputReportLayout.DpadUp, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(XInputReportLayout.ButtonsOffset)));
            Assert.Equal(200, buffer[XInputReportLayout.LeftTriggerOffset]);
            Assert.Equal(50, buffer[XInputReportLayout.RightTriggerOffset]);
            Assert.Equal(1000, BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(XInputReportLayout.LeftStickXOffset)));
            Assert.Equal(-2000, BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(XInputReportLayout.LeftStickYOffset)));
            Assert.Equal(3000, BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(XInputReportLayout.RightStickXOffset)));
            Assert.Equal(-4000, BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(XInputReportLayout.RightStickYOffset)));
        }

        [Fact]
        public void ReadFile_ReResolvesTheIndex_WhenTheDeviceWasReplugged()
        {
            var (transport, api, interfaces) = Create(playerIndex: 0);
            api.Set(1, new XInputSnapshot(1, XInputReportLayout.B, 0, 0, 0, 0, 0, 0));
            interfaces.MoveTo(Path, 1);   // 繋ぎ直して、番号が 0 から 1 に変わった
            var buffer = new byte[XInputReportLayout.ReportLength];

            Assert.Equal(HidReadStatus.Success, transport.ReadFile(buffer));

            Assert.Equal(1, transport.PlayerIndex);
            Assert.Equal(XInputReportLayout.B, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(XInputReportLayout.ButtonsOffset)));
        }

        [Fact]
        public void ReadFile_ReportsADisconnect_WhenTheStateCanNoLongerBeRead()
        {
            var (transport, _, interfaces) = Create();
            interfaces.MoveTo(Path, -1);

            Assert.Equal(HidReadStatus.NotConnected, transport.ReadFile(new byte[XInputReportLayout.ReportLength]));
        }

        [Fact]
        public void ReadFile_ReportsADisconnect_AfterCancelIO_AndForATooSmallBuffer()
        {
            var (transport, api, _) = Create();
            api.Set(0, new XInputSnapshot(1, 0, 0, 0, 0, 0, 0, 0));

            Assert.Equal(HidReadStatus.NotConnected, transport.ReadFile(new byte[4]));

            transport.CancelIO();
            Assert.Equal(HidReadStatus.NotConnected, transport.ReadFile(new byte[XInputReportLayout.ReportLength]));
        }

        [Fact]
        public void TheOutputReport_BecomesAVibration_OnTheCurrentIndex()
        {
            var (transport, api, _) = Create(playerIndex: 2);
            api.Set(2, new XInputSnapshot(1, 0, 0, 0, 0, 0, 0, 0));

            Assert.True(transport.WriteOutputReportViaInterrupt(new byte[] { 0, 255, 128 }, 0));

            Assert.Equal((2, (ushort)65535, (ushort)32896), api.Vibrations.Single());
            Assert.False(transport.WriteOutputReportViaInterrupt(new byte[1], 0));
        }

        [Fact]
        public void TheTransportDescribesItself_LikeAnotherHidDevice()
        {
            var (transport, _, _) = Create();

            Assert.Equal(0x045E, transport.Attributes.VendorId);
            Assert.Equal(0x028E, transport.Attributes.ProductId);
            Assert.Equal(XInputReportLayout.ReportLength, transport.Capabilities.InputReportByteLength);
            Assert.Equal(XInputTransport.OutputReportLength, transport.Capabilities.OutputReportByteLength);
            Assert.Equal(0, transport.Capabilities.UsagePage);   // ベンダー定義の用途のページ（0xFF00 以上）の機器として、読み飛ばされない
            Assert.True(transport.IsOpen);
            Assert.Equal(Path, transport.DevicePath);
        }

        [Fact]
        public void TheSerial_IsStablePerDevice_AndDiffersBetweenDevices()
        {
            var api = new FakeXInputApi();
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path, 0x045E, 0x028E, 0, instanceId: @"USB\VID_045E&PID_028E\A");
            interfaces.Add(Path + "2", 0x045E, 0x028E, 1, instanceId: @"USB\VID_045E&PID_028E\B");
            var infos = interfaces.Enumerate();

            string first = new XInputTransport(api, interfaces, infos[0]).GenerateFakeHwSerial();
            string again = new XInputTransport(api, interfaces, infos[0]).ReadSerial();
            string other = new XInputTransport(api, interfaces, infos[1]).GenerateFakeHwSerial();

            Assert.Matches("^[0-9A-F]{2}(:[0-9A-F]{2}){5}$", first);
            Assert.Equal(first, again);
            Assert.NotEqual(first, other);
        }

        [Fact]
        public void TheSource_ReturnsOnlyDevicesWhoseIndexCanBeRead_AsXboxDevices()
        {
            var api = new FakeXInputApi();
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path, 0x045E, 0x028E, 0);
            interfaces.Add(Path + "2", 0x045E, 0x02FF, -1);
            var source = new XInputTransportSource(api, interfaces);

            var found = source.Enumerate();

            InputTransportCandidate candidate = Assert.Single(found);
            Assert.Equal(Path, candidate.Transport.DevicePath);
            Assert.Equal(InputDeviceType.Xbox, candidate.Info.inputDevType);
            Assert.Equal(0x045E, candidate.Info.vid);
            Assert.Equal(ConnectionType.USB, candidate.Info.checkConnection(candidate.Transport));
        }
    }
}
