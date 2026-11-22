#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Model;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 複数のフレームを順に流したときの状態の移り変わり（押す → 離す → 押す、スティックの掃引）を、HID の全機種の仮想コントローラーで確かめる。
    /// 1 フレームずつの検査（<see cref="VirtualPadContractTests"/>）では見えない、前のフレームの状態の持ち越しや、離したことの検出を見る。
    /// </summary>
    public class VirtualPadSequenceTests
    {
        public static TheoryData<string> HidPadNames()
        {
            var data = new TheoryData<string>();
            foreach (VirtualPad pad in VirtualPad.All.Where(p => p != VirtualPad.Xbox)) data.Add(pad.Name);
            return data;
        }

        private static List<ControllerState> Play(VirtualPad pad, params PadInput[] sequence)
        {
            using VirtualPadDevice opened = pad.OpenSequence(sequence);
            opened.Transport!.ReadDelayMs = 12;   // 近すぎるフレームを読み飛ばす機種があるため、間隔を空ける
            var states = new List<ControllerState>();
            using var removed = new ManualResetEventSlim();
            opened.Device.Report += (sender, args) =>
            {
                lock (states) states.Add(sender.GetCurrentStateRef().Clone());
            };
            opened.Device.Removal += (sender, args) => removed.Set();
            try
            {
                opened.Device.StartUpdate();
                Assert.True(removed.Wait(15000), pad.Name + ": 台本が尽きて切断になるはず");
            }
            finally
            {
                opened.Device.StopUpdate();
            }

            lock (states) return states.ToList();
        }

        [Theory]
        [MemberData(nameof(HidPadNames))]
        public void PressReleasePress_IsReportedFrameByFrame(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);
            var none = new PadInput();
            var cross = new PadInput { Cross = true };
            var crossR1 = new PadInput { Cross = true, R1 = true };
            var square = new PadInput { Square = true };

            List<ControllerState> s = Play(pad, none, cross, crossR1, none, square);

            Assert.Equal(5, s.Count);
            Assert.Equal(new[] { false, true, true, false, false }, s.Select(x => x.Cross).ToArray());
            Assert.Equal(new[] { false, false, true, false, false }, s.Select(x => x.R1).ToArray());
            Assert.Equal(new[] { false, false, false, false, true }, s.Select(x => x.Square).ToArray());
        }

        [Theory]
        [MemberData(nameof(HidPadNames))]
        public void ASweepOfTheLeftStick_IsReportedInOrder_WithoutJumpsOrWrapAround(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);
            byte[] steps = { 0, 64, 128, 192, 255 };

            List<ControllerState> s = Play(pad, steps.Select(v => new PadInput { LX = v }).ToArray());

            Assert.Equal(steps.Length, s.Count);
            byte[] lx = s.Select(x => x.LX).ToArray();
            for (int i = 1; i < lx.Length; i++)
            {
                Assert.True(lx[i] > lx[i - 1], $"{name}: LX が単調に増えない: {string.Join(",", lx)}");
            }

            Assert.InRange(lx[0], 0, 8);
            Assert.InRange(lx[^1], 247, 255);
        }

        [Theory]
        [MemberData(nameof(HidPadNames))]
        public void TheTriggersAndTheDpad_AreReleasedWhenTheFrameLetsGo(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            List<ControllerState> s = Play(pad,
                new PadInput { L2Pressed = true, R2Pressed = true, DpadUp = true },
                new PadInput());

            Assert.Equal(2, s.Count);
            Assert.True(s[0].L2 > 0 && s[0].R2 > 0 && s[0].DpadUp);
            Assert.Equal(0, s[1].L2);
            Assert.Equal(0, s[1].R2);
            Assert.False(s[1].DpadUp);
        }
    }
}
