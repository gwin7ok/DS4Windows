#nullable enable
using System.Linq;
using System.Threading;
using DS4Windows.Input.Hid;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 通信の失敗（読み取りの失敗、機器が抜けた）の扱いを、全機種の仮想コントローラー（<see cref="VirtualPad"/>）で確かめる。
    /// どの機種でも、失敗は切断（<c>Removal</c>）が 1 回だけ通知され、そのあとのフレームは報告されない。
    /// （Bluetooth の CRC の連続不一致は <c>InputLoopFailureHandlingTests</c>。ここは、USB と Xbox の通信路の失敗。）
    /// </summary>
    public class VirtualPadFailureTests
    {
        public static TheoryData<string, HidReadStatus> PadsAndFailures()
        {
            var data = new TheoryData<string, HidReadStatus>();
            foreach (VirtualPad pad in VirtualPad.All)
            {
                data.Add(pad.Name, HidReadStatus.ReadError);
                data.Add(pad.Name, HidReadStatus.WaitTimedOut);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(PadsAndFailures))]
        public void AFailureAfterTheFirstFrame_DisconnectsOnce_AndLaterFramesAreNotReported(string name, HidReadStatus failure)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);
            using VirtualPadDevice opened = pad.Open(new PadInput { Cross = true }, failureAfterFirst: failure);

            int reports = 0;
            int reportsAfterRemoval = 0;
            int removals = 0;
            using var firstReport = new ManualResetEventSlim();
            using var removed = new ManualResetEventSlim();
            opened.Device.Report += (sender, args) =>
            {
                if (Volatile.Read(ref removals) > 0) Interlocked.Increment(ref reportsAfterRemoval);
                Interlocked.Increment(ref reports);
                firstReport.Set();
            };
            opened.Device.Removal += (sender, args) =>
            {
                Interlocked.Increment(ref removals);
                removed.Set();
            };

            opened.Device.StartUpdate();
            Assert.True(firstReport.Wait(8000), pad.Name + ": 最初のフレームが届かなかった");
            opened.Disconnect();   // HID の機種は何もしない（台本の失敗が切断になる）。Xbox は、機器を抜く
            Assert.True(removed.Wait(8000), pad.Name + ": 失敗のあとに Removal が来なかった");
            Thread.Sleep(100);   // 切断の通知が二重に出ないこと、報告が続かないことを見るため、少し待つ
            opened.Device.StopUpdate();

            Assert.Equal(1, removals);
            Assert.Equal(0, reportsAfterRemoval);
            if (opened.Transport != null) Assert.Equal(1, reports);   // HID: 失敗の次にあった良いフレームは届かない
        }
    }
}
