#nullable enable
using System;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力から、仮想 DS4 パッドの出力までを、仮想コントローラー（<see cref="VirtualPad"/>）で通して確かめる（Xbox 360 の出力は <see cref="VirtualPadPipelineTests"/>）。
    /// どの入力機種で入れても、出力の仮想 DS4 の状態（ボタンのビット、方向キー、トリガー、スティック。DS4 は 0〜255 のまま、上が 0）が同じになる。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class VirtualPadDs4OutputPipelineTests
    {
        private const int Slot = 0;

        public VirtualPadDs4OutputPipelineTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.InitializeRuntime();
        }

        public static TheoryData<string> PadNames() => VirtualPadContractTests.PadNames();

        private static FakeDs4Target Run(VirtualPad pad, PadInput input)
        {
            var service = (VirtualPadService)TestHost.GetService<IVirtualPadService>();
            var settings = TestHost.GetService<IProfileSettingsService>();
            var processor = TestHost.GetService<IInputReportProcessor>();

            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice output = TestOutputCartridges.Allocate(OutContType.DS4, backend, new Version(1, 16, 0, 0))!;   // 基本の出力（ボタンのビットを直接渡す。1.17 以降は生のレポートを渡す）
            FakeDs4Target target = Assert.Single(backend.Ds4Targets);
            output.Connect();

            OutputDevice? savedOutput = service.OutputDevices[Slot];
            bool savedDInputOnly = settings.UseDInputOnlyArray[Slot];
            service.OutputDevices[Slot] = output;
            settings.UseDInputOnlyArray[Slot] = false;
            try
            {
                using VirtualPadDevice opened = pad.Open(input);
                using var reported = new ManualResetEventSlim();
                opened.Device.Report += (sender, args) =>
                {
                    processor.OnReport(sender, args, Slot);
                    reported.Set();
                };
                opened.Device.StartUpdate();
                try
                {
                    Assert.True(reported.Wait(8000), pad.Name + ": レポートが届かなかった");
                }
                finally
                {
                    opened.Device.StopUpdate();
                }
            }
            finally
            {
                service.OutputDevices[Slot] = savedOutput;
                settings.UseDInputOnlyArray[Slot] = savedDInputOnly;
            }

            return target;
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void TheButtonsAndTheDpad_ReachTheVirtualDs4_FromEveryInputKind(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeDs4Target target = Run(pad, new PadInput { Cross = true, L1 = true, DpadUp = true });

            Assert.Equal((ushort)(Ds4Buttons.Cross | Ds4Buttons.ShoulderLeft), target.Buttons);
            Assert.Equal(Ds4DPadDirection.North, target.DPad);
            Assert.True(target.SubmitCount >= 1);   // Xbox は台本が尽きず、報告が続くため、1 回とは限らない
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void TheTriggersAndSticks_ReachTheVirtualDs4_AsZeroTo255_WithUpAsZero(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeDs4Target target = Run(pad, new PadInput { L2Pressed = true, LX = 255, LY = 0, RX = 0, RY = 255 });

            Assert.Equal(255, target.LeftTriggerValue);
            Assert.Equal(0, target.RightTriggerValue);
            Assert.True(target.LeftThumbXValue > 235, $"LX={target.LeftThumbXValue}");
            Assert.True(target.LeftThumbYValue < 20, $"LY（上）={target.LeftThumbYValue}");
            Assert.True(target.RightThumbXValue < 20, $"RX={target.RightThumbXValue}");
            Assert.True(target.RightThumbYValue > 235, $"RY（下）={target.RightThumbYValue}");
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void NoInput_ReachesTheVirtualDs4_AsAnIdlePad(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeDs4Target target = Run(pad, new PadInput());

            Assert.Equal(0, target.Buttons);
            Assert.Equal(Ds4DPadDirection.None, target.DPad);
            Assert.Equal(0, target.LeftTriggerValue);
            Assert.InRange(target.LeftThumbXValue, 116, 140);
            Assert.InRange(target.LeftThumbYValue, 116, 140);
        }
    }
}
