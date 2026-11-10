using System;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力デバイスと HID 機器の通信の窓口（<see cref="IHidTransport"/>）の確認。
    /// <c>HidDevice</c>（OS の具象）を参照してよいのは <c>DS4Windows.Input.Hid</c> だけ（規則 L10）で、
    /// それ以外は契約を通す。偽の transport（<see cref="FakeHidTransport"/>）でデバイスを動かし、通信が契約の外へ出ないことを確かめる。
    /// </summary>
    public class HidTransportTests
    {
        [Fact]
        public void OnlyTheHidNamespace_ReferencesTheConcreteHidDevice()
        {
            var sources = LayerDependencyScanner.Edges
                .Where(e => e.Target == "DS4Windows.Input.Hid.HidDevice")
                .Select(e => e.Source)
                .Distinct()
                .ToList();

            // 空振りの防止: 列挙（HidDevices）からは具象が見えること
            Assert.Contains("DS4Windows.Input.Hid.HidDevices", sources);
            var outside = sources.Where(t => !t.StartsWith("DS4Windows.Input.Hid.", StringComparison.Ordinal)).OrderBy(t => t, StringComparer.Ordinal).ToList();
            Assert.True(outside.Count == 0, "HidDevice の具象を参照している（Input.Hid の外）: " + string.Join(", ", outside));
        }

        [Fact]
        public void HidDevice_ImplementsTheContract_AndTheReadStatusValuesAreUnchanged()
        {
            Assert.True(typeof(IHidTransport).IsAssignableFrom(typeof(HidDevice)));
            Assert.Equal(0, (int)HidReadStatus.Success);
            Assert.Equal(1, (int)HidReadStatus.WaitTimedOut);
            Assert.Equal(2, (int)HidReadStatus.WaitFail);
            Assert.Equal(3, (int)HidReadStatus.NoDataRead);
            Assert.Equal(4, (int)HidReadStatus.ReadError);
            Assert.Equal(5, (int)HidReadStatus.NotConnected);
        }

        [Theory]
        [InlineData(64, 0, ConnectionType.USB)]
        [InlineData(64, 22, ConnectionType.SONYWA)]
        [InlineData(547, 0, ConnectionType.BT)]
        public void ConnectionType_IsDecidedFromTheCapabilitiesOfTheTransport(short inputLength, short featureIndices, ConnectionType expected)
        {
            var fake = new FakeHidTransport(inputReportByteLength: inputLength, numberFeatureDataIndices: featureIndices);

            Assert.Equal(expected, DS4Device.HidConnectionType(fake));
        }

        [Fact]
        public void TheDevice_KeepsTheTransportItWasGiven_AndReportsItsExclusiveMode()
        {
            var fake = new FakeHidTransport { IsExclusive = true };
            var device = new DualShock4Device(fake, "Fake DS4");

            Assert.Same(fake, device.HidDevice);
            Assert.True(device.IsHidExclusive);
        }

        [Fact]
        public void PostInit_OfAUsbDevice_ReadsTheSerialAndWritesTheFirstOutputReportThroughTheTransport()
        {
            var fake = new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 32);
            var device = new DualShock4Device(fake, "Fake DS4", VidPidFeatureSet.NoGyroCalib);

            device.PostInit();

            Assert.Equal("00:11:22:33:44:55", device.getMacAddress());
            Assert.Equal(ConnectionType.USB, device.getConnectionType());
            Assert.NotEmpty(fake.Writes);   // 初期の出力レポート
            Assert.All(fake.Writes, w => Assert.Equal("interrupt", w.Kind));
        }

        [Fact]
        public void AFailedWrite_ThroughTheTransport_DoesNotThrow()
        {
            var fake = new FakeHidTransport { FailWrites = true };
            var device = new DualShock4Device(fake, "Fake DS4", VidPidFeatureSet.NoGyroCalib);

            var ex = Record.Exception(() => device.PostInit());

            Assert.Null(ex);
            Assert.NotEmpty(fake.Writes);
        }

        [Fact]
        public void TheFake_ReturnsTheScriptedReads_ThenReportsADisconnect()
        {
            var fake = new FakeHidTransport();
            fake.EnqueueRead(HidReadStatus.Success, new byte[] { 1, 2, 3 });
            fake.EnqueueRead(HidReadStatus.WaitTimedOut);
            var buffer = new byte[4];

            Assert.Equal(HidReadStatus.Success, fake.ReadFile(buffer));
            Assert.Equal(new byte[] { 1, 2, 3, 0 }, buffer);
            Assert.Equal(HidReadStatus.WaitTimedOut, fake.ReadFile(buffer));
            Assert.Equal(HidReadStatus.NotConnected, fake.ReadFile(buffer));
        }
    }
}
