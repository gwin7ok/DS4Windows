#nullable enable
using System;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力機種（カートリッジ）の共通の契約を、仮想コントローラー（<see cref="VirtualPad"/>）で確かめる。
    /// 同じ <see cref="PadInput"/> を DS4・DualSense・Switch Pro・Xbox の全機種へ与え、本番のデバイスとパーサーを通した
    /// <see cref="ControllerState"/> が、機種によらず同じ意味になることを調べる。新機種は <see cref="VirtualPad.All"/> へ足すと、この全検査に載る。
    /// </summary>
    public class VirtualPadContractTests
    {
        public static TheoryData<string> PadNames()
        {
            var data = new TheoryData<string>();
            foreach (VirtualPad pad in VirtualPad.All) data.Add(pad.Name);
            return data;
        }

        private static VirtualPad Pad(string name) => VirtualPad.All.Single(p => p.Name == name);

        // ボタン 1 つだけを押した入力と、その押されたはずのボタンを読む関数
        private static readonly (string Name, PadInput Input, Func<ControllerState, bool> Read)[] Buttons =
        {
            ("Cross", new PadInput { Cross = true }, s => s.Cross),
            ("Circle", new PadInput { Circle = true }, s => s.Circle),
            ("Square", new PadInput { Square = true }, s => s.Square),
            ("Triangle", new PadInput { Triangle = true }, s => s.Triangle),
            ("L1", new PadInput { L1 = true }, s => s.L1),
            ("R1", new PadInput { R1 = true }, s => s.R1),
            ("DpadUp", new PadInput { DpadUp = true }, s => s.DpadUp),
        };

        private static bool[] AllButtons(ControllerState s) =>
            Buttons.Select(b => b.Read(s)).ToArray();

        [Theory]
        [MemberData(nameof(PadNames))]
        public void ThePad_IsCreatedAsTheKindItClaims(string name)
        {
            VirtualPad pad = Pad(name);
            using VirtualPadDevice opened = pad.Open(new PadInput());

            Assert.Equal(pad.DeviceType, opened.Device.DeviceType);
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void NoInput_ReportsNoButtons_NoTriggers_AndCenteredSticks(string name)
        {
            ControllerState s = Pad(name).ReadFirstState(new PadInput());

            Assert.All(AllButtons(s), pressed => Assert.False(pressed));
            Assert.Equal(0, s.L2);
            Assert.Equal(0, s.R2);
            // 校正のある機種（Switch Pro）は、中立が 128 からわずかにずれる
            Assert.InRange(s.LX, 116, 140);
            Assert.InRange(s.LY, 116, 140);
            Assert.InRange(s.RX, 116, 140);
            Assert.InRange(s.RY, 116, 140);
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void EachButton_ReportsOnlyThatButton(string name)
        {
            VirtualPad pad = Pad(name);
            for (int pressedIndex = 0; pressedIndex < Buttons.Length; pressedIndex++)
            {
                ControllerState s = pad.ReadFirstState(Buttons[pressedIndex].Input);

                for (int i = 0; i < Buttons.Length; i++)
                {
                    Assert.True(Buttons[i].Read(s) == (i == pressedIndex),
                        $"{pad.Name}: {Buttons[pressedIndex].Name} を押したとき、{Buttons[i].Name} が {Buttons[i].Read(s)}");
                }
            }
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void ThePressedTriggers_ReportTheFullValue_AndOnlyTheirOwnSide(string name)
        {
            VirtualPad pad = Pad(name);

            ControllerState left = pad.ReadFirstState(new PadInput { L2Pressed = true });
            Assert.Equal(255, left.L2);
            Assert.Equal(0, left.R2);

            ControllerState right = pad.ReadFirstState(new PadInput { R2Pressed = true });
            Assert.Equal(0, right.L2);
            Assert.Equal(255, right.R2);
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void TheSticks_FollowTheDirectionOfTheInput_OnEachAxis(string name)
        {
            VirtualPad pad = Pad(name);

            ControllerState s = pad.ReadFirstState(new PadInput { LX = 255, LY = 0, RX = 0, RY = 255 });

            Assert.True(s.LX > 200, $"LX={s.LX}");
            Assert.True(s.LY < 56, $"LY={s.LY}");
            Assert.True(s.RX < 56, $"RX={s.RX}");
            Assert.True(s.RY > 200, $"RY={s.RY}");
        }

        [Fact]
        public void AllTheFamilies_AreCoveredByAVirtualPad()
        {
            // 新しい機種の種類を足したら、仮想コントローラーも足す（足さないなら、ここに除外の理由を書く）
            var covered = VirtualPad.All.Select(p => p.DeviceType).ToHashSet();

            Assert.Contains(InputDeviceType.DS4, covered);
            Assert.Contains(InputDeviceType.DualSense, covered);
            Assert.Contains(InputDeviceType.SwitchPro, covered);
            Assert.Contains(InputDeviceType.Xbox, covered);
        }
    }
}
