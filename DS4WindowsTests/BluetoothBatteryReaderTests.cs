using System;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Bluetooth の Xbox の電池。GATT のバッテリーサービスの値を、入力のループを待たせず（裏で読み、最後の値を返す）に使い、
    /// 通信路が合成レポートへ入れ、プロトコルが % として返す。読めない間は XInput の API にもどる。実際の無線の読み取りは、ここでは行わない。
    /// </summary>
    public class BluetoothBatteryReaderTests
    {
        private const string BlePath = @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}&dev&vid_045e&pid_0b22&rev_0521&3cfa0617287c&col01&ig_00#d&14fe1d93&0&0000#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private const ulong Address = 0x3CFA0617287C;

        private sealed class FakeFetcher : IGattBatteryFetcher
        {
            public int Calls;
            public int? Next;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);

            public Task<int?> FetchAsync(ulong bluetoothAddress)
            {
                Interlocked.Increment(ref Calls);
                int? value = Next;
                Done.Set();
                return Task.FromResult(value);
            }
        }

        private static bool WaitFor(Func<bool> condition)
        {
            for (int i = 0; i < 200 && !condition(); i++) Thread.Sleep(10);
            return condition();
        }

        [Fact]
        public void TheReader_DoesNotWait_ReturnsNothingFirst_AndTheValueOnceItHasBeenRead()
        {
            var fetcher = new FakeFetcher { Next = 66 };
            var reader = new BluetoothBatteryReader(fetcher);

            reader.TryGetPercent(Address, out _);   // 最初の呼び出しは待たない（値が先に入ることもある）
            Assert.True(WaitFor(() => reader.TryGetPercent(Address, out int p) && p == 66));
            Assert.Equal(1, fetcher.Calls);   // 間隔の内は、読み直さない
        }

        [Fact]
        public void TheReader_ReadsAgainAfterTheInterval_AndKeepsTheLastValueWhenItCannotBeRead()
        {
            var fetcher = new FakeFetcher { Next = 100 };
            var reader = new BluetoothBatteryReader(fetcher, TimeSpan.Zero);
            Assert.True(WaitFor(() => reader.TryGetPercent(Address, out int p) && p == 100));

            fetcher.Next = null;   // 読めなくなっても、前回の値を返す
            Assert.True(WaitFor(() => { bool ok = reader.TryGetPercent(Address, out int p); return fetcher.Calls >= 3 && ok && p == 100; }));
        }

        [Fact]
        public void TheReader_ReportsNothing_WhenTheBatteryIsNeverReadable()
        {
            var fetcher = new FakeFetcher { Next = null };
            var reader = new BluetoothBatteryReader(fetcher);

            reader.TryGetPercent(Address, out _);
            Assert.True(fetcher.Done.Wait(2000));
            Assert.False(reader.TryGetPercent(Address, out _));
        }

        [Fact]
        public void TheReader_CanWaitForTheFirstValue_WhenAskedTo()
        {
            var reader = new BluetoothBatteryReader(new FakeFetcher { Next = 77 });

            Assert.True(reader.TryGetPercent(Address, TimeSpan.FromSeconds(2), out int percent));
            Assert.Equal(77, percent);
        }

        private sealed class FixedReader : IBluetoothBatteryReader
        {
            public bool TryGetPercent(ulong bluetoothAddress, TimeSpan maxWait, out int percent) => TryGetPercent(bluetoothAddress, out percent);

            public bool TryGetPercent(ulong bluetoothAddress, out int percent)
            {
                percent = bluetoothAddress == Address ? 42 : 0;
                return bluetoothAddress == Address;
            }
        }

        [Fact]
        public void TheTransport_PutsTheGattPercentInTheReport_AndTheProtocolReadsItAsIs()
        {
            var api = new FakeXInputApi();
            api.Set(0, new XInputSnapshot(1, 0, 0, 0, 0, 0, 0, 0));
            api.Batteries[0] = new XInputBattery(XInputBattery.TypeWired, 0);   // XInput は役に立たない値（有線）
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(BlePath, 0x045E, 0x0B22, 0);
            var transport = new XInputTransport(api, interfaces, new XusbInterfaceInfo(BlePath, "HID\\X", 0x045E, 0x0B22, 0), new FixedReader());
            var buffer = new byte[XInputReportLayout.ReportLength];

            Assert.Equal(HidReadStatus.Success, transport.ReadFile(buffer));

            Assert.Equal(XInputBattery.TypePercent, buffer[XInputReportLayout.BatteryTypeOffset]);
            Assert.True(new XboxProtocol().TryReadBattery(buffer, out int percent, out _));
            Assert.Equal(42, percent);
        }

        [Fact]
        public void TheTransport_FallsBackToXInput_WhenTheGattValueIsNotReadyYet()
        {
            var api = new FakeXInputApi();
            api.Set(0, new XInputSnapshot(1, 0, 0, 0, 0, 0, 0, 0));
            api.Batteries[0] = new XInputBattery(XInputBattery.TypeNiMh, 2);
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(BlePath, 0x045E, 0x0B22, 0);
            var transport = new XInputTransport(api, interfaces, new XusbInterfaceInfo(BlePath, "HID\\X", 0x045E, 0x0B22, 0), new BluetoothBatteryReader(new FakeFetcher { Next = null }));
            var buffer = new byte[XInputReportLayout.ReportLength];

            transport.ReadFile(buffer);

            Assert.Equal(XInputBattery.TypeNiMh, buffer[XInputReportLayout.BatteryTypeOffset]);
        }
    }
}
