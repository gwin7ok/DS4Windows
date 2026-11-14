using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using DS4Windows.Output.Udp;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>DSU クライアントの登録簿の、要求の記録・5 秒の期限・死んだクライアントの除去（偽の時計で進める）。</summary>
    public class DsuClientRegistryTests
    {
        private static readonly IPEndPoint ClientA = new IPEndPoint(IPAddress.Loopback, 5001);
        private static readonly IPEndPoint ClientB = new IPEndPoint(IPAddress.Loopback, 5002);
        private static readonly PhysicalAddress Mac = PhysicalAddress.Parse("AA-BB-CC-DD-EE-FF");

        private static List<IPEndPoint> Collect(DsuClientRegistry r, int padId = 0, PhysicalAddress mac = null)
        {
            var list = new List<IPEndPoint> { new IPEndPoint(IPAddress.Any, 1) }; // 先に空にされること
            r.CollectRecipients(padId, mac ?? Mac, list);
            return list;
        }

        [Fact]
        public void AllPadsRequest_IsServedForEveryPad_UntilFiveSeconds()
        {
            var time = new ManualTimeProvider();
            var r = new DsuClientRegistry(time);
            r.RegisterRequest(ClientA, 0, 0, Mac);

            Assert.Equal(new[] { ClientA }, Collect(r, 3));
            time.Advance(TimeSpan.FromSeconds(4.9));
            Assert.Equal(new[] { ClientA }, Collect(r, 0));
            time.Advance(TimeSpan.FromSeconds(0.2));
            Assert.Empty(Collect(r, 0));
        }

        [Fact]
        public void IdRequest_IsServedOnlyForThatPad()
        {
            var r = new DsuClientRegistry(new ManualTimeProvider());
            r.RegisterRequest(ClientA, 0x01, 2, Mac);

            Assert.Equal(new[] { ClientA }, Collect(r, 2, PhysicalAddress.None));
            Assert.Empty(Collect(r, 1, PhysicalAddress.None));
        }

        [Fact]
        public void MacRequest_IsServedOnlyForThatMac()
        {
            var r = new DsuClientRegistry(new ManualTimeProvider());
            r.RegisterRequest(ClientA, 0x02, 0, Mac);

            Assert.Equal(new[] { ClientA }, Collect(r, 3, Mac));
            Assert.Empty(Collect(r, 3, PhysicalAddress.Parse("11-22-33-44-55-66")));
        }

        [Fact]
        public void OutOfRangeId_IsIgnored_AndANullMacIsNotAnError()
        {
            var r = new DsuClientRegistry(new ManualTimeProvider());
            r.RegisterRequest(ClientA, 0x01, 9, Mac);

            var list = new List<IPEndPoint>();
            r.CollectRecipients(9, null, list);
            Assert.Empty(list);
        }

        [Fact]
        public void ADeadClient_IsRemoved_AndALiveOneStays()
        {
            var time = new ManualTimeProvider();
            var r = new DsuClientRegistry(time);
            r.RegisterRequest(ClientA, 0x01, 0, Mac);
            time.Advance(TimeSpan.FromSeconds(3));
            r.RegisterRequest(ClientB, 0x01, 0, Mac);
            Assert.Equal(2, r.Count);

            time.Advance(TimeSpan.FromSeconds(3)); // A は 6 秒、B は 3 秒
            Assert.Equal(new[] { ClientB }, Collect(r, 0));
            Assert.Equal(1, r.Count);
        }
    }
}
