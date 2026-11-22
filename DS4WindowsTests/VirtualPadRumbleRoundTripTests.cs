#nullable enable
using System;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ゲームの振動の指示が、仮想の Xbox 360（<see cref="FakeXbox360Target"/>）から、本番の振動の経路（<c>VirtualPadService</c>→<c>DeviceRumbleService</c>→デバイス）を通り、
    /// 入力機種の出力レポート（偽の通信路への書き込み）まで届くことを確かめる往復の試験。
    /// 出力レポートの位置は、Linux カーネルの <c>hid-playstation.c</c> の構造体（motor_right・motor_left）に従う。
    /// 同じ指示が、機種によらず「大きいモーター＝左・重い」「小さいモーター＝右・軽い」へ割り当てられることを見る。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class VirtualPadRumbleRoundTripTests
    {
        private const int Slot = 0;
        private const byte Large = 200;
        private const byte Small = 100;

        public VirtualPadRumbleRoundTripTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.InitializeRuntime();
        }

        // hid-playstation.c: DS4 USB 出力 0x05 は motor_right が byte 4、motor_left が byte 5。DualSense 出力 0x02 は motor_right が byte 3、motor_left が byte 4
        private static (int Id, int Right, int Left) Offsets(string padName) => padName switch
        {
            "DS4 (USB)" => (0x05, 4, 5),
            "DualSense (USB)" => (0x02, 3, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(padName)),
        };

        public static TheoryData<string> HidPadNames()
        {
            var data = new TheoryData<string>();
            data.Add(VirtualPad.Ds4.Name);
            data.Add(VirtualPad.DualSense.Name);
            return data;
        }

        [Theory]
        [MemberData(nameof(HidPadNames))]
        public void TheGamesRumble_ReachesTheOutputReport_OnTheLinuxOffsets(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);
            var service = (VirtualPadService)TestHost.GetService<IVirtualPadService>();

            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice output = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0))!;
            FakeXbox360Target target = Assert.Single(backend.Xbox360Targets);

            using VirtualPadDevice opened = pad.Open(new PadInput(), frames: 120);
            opened.Transport!.ReadDelayMs = 8;   // 出力の更新が間に合うよう、読み取りを少し遅くする
            service.EstablishOutFeedback(Slot, OutContType.X360, output, opened.Device);

            using var first = new ManualResetEventSlim();
            using var removed = new ManualResetEventSlim();
            opened.Device.Report += (sender, args) =>
            {
                if (first.IsSet) return;
                first.Set();
                target.RaiseFeedback(Large, Small);   // ゲームが振動を指示する
            };
            opened.Device.Removal += (sender, args) => removed.Set();

            try
            {
                opened.Device.StartUpdate();
                Assert.True(removed.Wait(15000), "台本が尽きたら切断になるはず");
            }
            finally
            {
                opened.Device.StopUpdate();
                output.RemoveFeedbacks();
            }

            (int id, int right, int left) = Offsets(name);
            var written = opened.Transport.Writes.Where(w => w.Data[0] == id && w.Data[right] == Small && w.Data[left] == Large).ToList();
            Assert.True(written.Count > 0,
                $"{name}: 小（右・軽い）={Small}、大（左・重い）={Large} の出力レポートが書かれなかった。書かれた振動値: " +
                string.Join(", ", opened.Transport.Writes.Where(w => w.Data[0] == id).Select(w => $"{w.Data[right]}/{w.Data[left]}").Distinct()));
        }
    }
}
