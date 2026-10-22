using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: <see cref="UdpMotionService"/> の特性化テスト。
    /// ソケットとイベントスレッドは、スタブ（<see cref="FakeUdpMotionServerFactory"/>、<see cref="FakeUdpMotionHost"/>）に替える。
    /// サーバーの作成・開始・停止（イベントスレッドで実行、ポートの開始の失敗のログ）、モーションの送出のハンドラー（スムージングの有無、経過時間 0 の読み飛ばし、
    /// 出力バッファーの再利用）、スムージングのフィルターの作り直しを調べる。
    /// 旧実装は <c>ControlService</c> の private メンバーで、検査できる入口がなかったため、旧実装との差分テストではなく、期待する動作を直接確かめる。
    /// </summary>
    public class Phase9EUdpMotionServiceTests
    {
        private sealed class Fixture : IDisposable
        {
            public readonly IAppSettingsService Settings = TestHost.GetService<IAppSettingsService>();
            public readonly FakeUdpMotionServerFactory Factory = new FakeUdpMotionServerFactory();
            public readonly FakeUdpMotionHost Host = new FakeUdpMotionHost();
            public readonly UdpMotionService Service;
            public readonly List<(string Text, bool Warning)> Logs = new List<(string, bool)>();
            private readonly int _savedPort;
            private readonly string _savedAddress;
            private readonly bool _savedSmoothing;

            public Fixture()
            {
                _savedPort = Settings.UdpServerPort;
                _savedAddress = Settings.UdpServerListenAddress;
                _savedSmoothing = Settings.UseUdpServerSmoothing;
                Service = new UdpMotionService(Settings, TestHost.GetService<IDs4DeviceRegistry>(), Factory, () => Host);
                Service.DebugMessage += (text, warning) => Logs.Add((text, warning));
            }

            public void Dispose()
            {
                Settings.UdpServerPort = _savedPort;
                Settings.UdpServerListenAddress = _savedAddress;
                Settings.UseUdpServerSmoothing = _savedSmoothing;
            }
        }

        [Fact]
        public void InitialState_HasNoServer()
        {
            using var f = new Fixture();
            Assert.False(f.Service.HasServer);
            Assert.False(f.Service.IsChangingStatus);
            Assert.Equal(4, f.Service.SlotCount);
        }

        [Fact]
        public void ChangeStatus_Enable_CreatesTheServerOnTheEventThread_AndOpensThePort()
        {
            using var f = new Fixture();
            f.Settings.UdpServerPort = 26799;
            f.Settings.UdpServerListenAddress = "127.0.0.1";

            f.Service.ChangeStatus(true);

            Assert.Equal(1, f.Host.Queued);
            Assert.Equal(1, f.Factory.Created);
            Assert.True(f.Service.HasServer);
            Assert.False(f.Service.IsChangingStatus);   // 作成が終わると、処理中の印が戻る
            Assert.Equal(new[] { (26799, "127.0.0.1") }, f.Factory.Server.Starts);
            Assert.Contains(f.Logs, l => l.Text.Contains("UDP server listening on address 127.0.0.1 port 26799") && !l.Warning);
        }

        [Fact]
        public void ChangeStatus_EnableWithoutOpeningThePort_OnlyCreatesTheServer()
        {
            using var f = new Fixture();

            f.Service.ChangeStatus(true, openPort: false);

            Assert.True(f.Service.HasServer);
            Assert.Empty(f.Factory.Server.Starts);

            // Start の中で、あとから待ち受けを始める
            f.Settings.UdpServerPort = 26798;
            f.Service.StartListeningIfCreated();
            Assert.Equal(26798, f.Factory.Server.Starts.Single().Port);
        }

        [Fact]
        public void StartListeningIfCreated_DoesNothing_WithoutAServer()
        {
            using var f = new Fixture();
            f.Service.StartListeningIfCreated();
            Assert.Empty(f.Factory.Server.Starts);
            Assert.Empty(f.Logs);
        }

        [Fact]
        public void ChangeStatus_Enable_WhenAlreadyCreated_DoesNothing()
        {
            using var f = new Fixture();
            f.Service.ChangeStatus(true);
            f.Service.ChangeStatus(true);

            Assert.Equal(1, f.Factory.Created);
            Assert.Equal(1, f.Host.Queued);
        }

        [Fact]
        public void ChangeStatus_Disable_StopsTheServer_AndResetsTheFilters()
        {
            using var f = new Fixture();
            f.Service.ChangeStatus(true);
            object[] before = Filters(f.Service, "udpEuroPairAccel");

            f.Service.ChangeStatus(false);

            Assert.False(f.Service.HasServer);
            Assert.Equal(1, f.Factory.Server.Stops);
            object[] after = Filters(f.Service, "udpEuroPairAccel");
            for (int i = 0; i < before.Length; i++)
            {
                Assert.NotSame(before[i], after[i]);   // 停止のとき、全スロットのフィルターを作り直す
            }
        }

        [Fact]
        public void ChangeStatus_Disable_WithoutAServer_DoesNothing()
        {
            using var f = new Fixture();
            f.Service.ChangeStatus(false);
            Assert.Equal(0, f.Host.Queued);
            Assert.Equal(0, f.Factory.Server.Stops);
        }

        [Fact]
        public void ChangeStatus_Enable_WhenThePortCannotBeOpened_LogsAWarning()
        {
            using var f = new Fixture();
            f.Factory.Server.StartException = new SocketException((int)SocketError.AddressAlreadyInUse);

            f.Service.ChangeStatus(true);

            Assert.True(f.Service.HasServer);   // サーバーは作られたまま（旧実装と同じ）
            Assert.Contains(f.Logs, l => l.Warning && l.Text.StartsWith("Couldn't start UDP server on address"));
        }

        private static object[] Filters(UdpMotionService service, string field)
        {
            var array = (Array)typeof(UdpMotionService).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            return array.Cast<object>().ToArray();
        }

        private static (TestDs4Device Device, Fixture F) PreparedDevice(bool smoothing)
        {
            var f = new Fixture();
            f.Settings.UseUdpServerSmoothing = smoothing;
            f.Service.ChangeStatus(true);
            TestDs4Device device = TestDs4Device.Create();
            f.Service.PrepareDeviceMotion(device, 2);
            return (device, f);
        }

        private static void Fire(TestDs4Device device) => device.MotionEvent(device, EventArgs.Empty);

        [Fact]
        public void PrepareDeviceMotion_SetsTheMotionEvent_AndSendsTheCurrentStateOfThatSlot()
        {
            var (device, f) = PreparedDevice(smoothing: false);
            using (f)
            {
                Assert.NotNull(device.MotionEvent);
                f.Host.Current[2].Motion.accelXG = 0.5;
                f.Host.Current[2].Motion.angVelYaw = 12.0;

                Fire(device);

                var report = f.Factory.Server.Reports.Single();
                Assert.Equal(2, report.PadId);          // パッドの情報（GetPadDetailForIdx）のスロット
                Assert.Equal(0.5, report.AccelXG);      // スムージングなし: 値はそのまま
                Assert.Equal(12.0, report.AngVelYaw);
                Assert.Equal(100, report.Buffer.Length);   // 出力バッファー（UdpServer.DATA_RSP_PACKET_LEN）
            }
        }

        [Fact]
        public void PrepareDeviceMotion_ReusesTheSameOutputBufferPerSlot()
        {
            var (device, f) = PreparedDevice(smoothing: false);
            using (f)
            {
                Fire(device);
                Fire(device);

                Assert.Equal(2, f.Factory.Server.Reports.Count);
                Assert.Same(f.Factory.Server.Reports[0].Buffer, f.Factory.Server.Reports[1].Buffer);
            }
        }

        [Fact]
        public void PrepareDeviceMotion_WithSmoothing_SkipsAFrameWithoutATimestamp()
        {
            var (device, f) = PreparedDevice(smoothing: true);
            using (f)
            {
                f.Host.Current[2].elapsedTime = 0;   // タイムスタンプがない: 旧実装は何も送らず抜ける

                Fire(device);

                Assert.Empty(f.Factory.Server.Reports);
            }
        }

        [Fact]
        public void PrepareDeviceMotion_WithSmoothing_FirstFrameIsUnchanged_AndLaterFramesAreSmoothed()
        {
            var (device, f) = PreparedDevice(smoothing: true);
            using (f)
            {
                f.Settings.UDPServerSmoothingMincutoff = 0.4;
                f.Settings.UDPServerSmoothingBeta = 0.2;
                f.Service.ResetSmoothingFilters(2);   // 設定を反映した、新しいフィルターから始める

                DS4State s = f.Host.Current[2];
                s.elapsedTime = 0.004;
                s.Motion.accelXG = 1.0;
                Fire(device);

                s.Motion.accelXG = 3.0;
                Fire(device);

                double first = f.Factory.Server.Reports[0].AccelXG;
                double second = f.Factory.Server.Reports[1].AccelXG;
                Assert.Equal(1.0, first);                       // 最初の 1 点は、そのまま
                Assert.True(second > 1.0 && second < 3.0, $"2 点目が平らになっていない: {second}");   // 急な変化は、なだらかにされる
            }
        }

        [Fact]
        public void ResetSmoothingFilters_ReplacesTheFiltersOfThatSlotOnly_AndIgnoresAnOutOfRangeSlot()
        {
            using var f = new Fixture();
            object[] accelBefore = Filters(f.Service, "udpEuroPairAccel");
            object[] gyroBefore = Filters(f.Service, "udpEuroPairGyro");

            f.Service.ResetSmoothingFilters(1);
            f.Service.ResetSmoothingFilters(99);   // 範囲外は何もしない

            object[] accelAfter = Filters(f.Service, "udpEuroPairAccel");
            object[] gyroAfter = Filters(f.Service, "udpEuroPairGyro");
            Assert.NotSame(accelBefore[1], accelAfter[1]);
            Assert.NotSame(gyroBefore[1], gyroAfter[1]);
            Assert.Same(accelBefore[0], accelAfter[0]);
            Assert.Same(accelBefore[3], accelAfter[3]);
        }

        [Fact]
        public void ChangeMotionEventStatus_WithNoControllers_DoesNotThrow()
        {
            using var f = new Fixture();
            Assert.Null(Record.Exception(() => f.Service.ChangeMotionEventStatus(true)));
            Assert.Null(Record.Exception(() => f.Service.ChangeMotionEventStatus(false)));
        }

        [Fact]
        public void Constructor_RejectsNull()
        {
            IAppSettingsService settings = TestHost.GetService<IAppSettingsService>();
            IDs4DeviceRegistry registry = TestHost.GetService<IDs4DeviceRegistry>();
            var factory = new FakeUdpMotionServerFactory();
            Func<IUdpMotionHost> host = () => new FakeUdpMotionHost();
            Assert.Throws<ArgumentNullException>(() => new UdpMotionService(null, registry, factory, host));
            Assert.Throws<ArgumentNullException>(() => new UdpMotionService(settings, null, factory, host));
            Assert.Throws<ArgumentNullException>(() => new UdpMotionService(settings, registry, null, host));
            Assert.Throws<ArgumentNullException>(() => new UdpMotionService(settings, registry, factory, null));
        }
    }
}
