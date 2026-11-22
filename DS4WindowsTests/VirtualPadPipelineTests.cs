#nullable enable
using System;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Devices;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力から仮想パッドの出力までの全体を、仮想コントローラー（<see cref="VirtualPad"/>）で通して確かめる。
    /// 偽の通信路の台本 → 本番のデバイス（読み取りのループ、パーサー）→ 本番の <c>IInputReportProcessor</c>（曲線・デッドゾーン、マッピング）→
    /// 本番の Xbox 360 の出力デバイス → 偽のバックエンド（<see cref="FakeVirtualPadBackend"/>）の対象、までを 1 本につなぐ。
    /// どの入力機種で入れても、出力の仮想パッドの状態が同じになることを確かめる（機種の追加で、出力側の結果が変わらない保証）。
    /// スロットの出力の差し込み（<c>OutputDevices</c>）だけは、ViGEm の起動・接続の手順を通さず、テストが直接入れ、終わりに戻す。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class VirtualPadPipelineTests
    {
        private const int Slot = 0;

        public VirtualPadPipelineTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.InitializeRuntime();
        }

        public static TheoryData<string> PadNames() => VirtualPadContractTests.PadNames();

        /// <summary>入力を、機種の仮想コントローラー → 処理 → 仮想パッドの出力へ流し、出力側の状態（<see cref="FakeXbox360Target"/>）を返す。</summary>
        private static FakeXbox360Target Run(VirtualPad pad, PadInput input)
        {
            var service = (VirtualPadService)TestHost.GetService<IVirtualPadService>();
            var settings = TestHost.GetService<IProfileSettingsService>();
            var processor = TestHost.GetService<IInputReportProcessor>();

            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice output = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0))!;
            FakeXbox360Target target = Assert.Single(backend.Xbox360Targets);
            output.Connect();   // 接続前の出力は、送られた状態を捨てる

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
                    // ControlService が、デバイスの Report を処理へつなぐのと同じ（スロット番号つき）
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
        public void TheButtons_ReachTheVirtualXbox360_WithTheSameBits_FromEveryInputKind(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeXbox360Target target = Run(pad, new PadInput { Cross = true, L1 = true, DpadUp = true });

            ushort expected = (ushort)(Xbox360Buttons.A | Xbox360Buttons.LeftShoulder | Xbox360Buttons.Up);
            Assert.Equal(expected, target.Buttons);
            Assert.Equal(1, target.SubmitCount);
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void TheTriggersAndSticks_ReachTheVirtualXbox360_InTheSameDirection_FromEveryInputKind(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeXbox360Target target = Run(pad, new PadInput { L2Pressed = true, LX = 255, LY = 0, RX = 0, RY = 255 });

            Assert.Equal(255, target.LeftTriggerValue);
            Assert.Equal(0, target.RightTriggerValue);
            Assert.True(target.LeftThumbXValue > 20000, $"LX={target.LeftThumbXValue}");
            Assert.True(target.LeftThumbYValue > 20000, $"LY（上）={target.LeftThumbYValue}");   // DS4 の上（0）は、Xbox の正
            Assert.True(target.RightThumbXValue < -20000, $"RX={target.RightThumbXValue}");
            Assert.True(target.RightThumbYValue < -20000, $"RY（下）={target.RightThumbYValue}");
        }

        [Theory]
        [MemberData(nameof(PadNames))]
        public void NoInput_ReachesTheVirtualXbox360_AsAnIdlePad(string name)
        {
            VirtualPad pad = VirtualPad.All.Single(p => p.Name == name);

            FakeXbox360Target target = Run(pad, new PadInput());

            Assert.Equal(0, target.Buttons);
            Assert.Equal(0, target.LeftTriggerValue);
            Assert.Equal(0, target.RightTriggerValue);
            Assert.InRange((int)target.LeftThumbXValue, -4000, 4000);
            Assert.InRange((int)target.LeftThumbYValue, -4000, 4000);
        }
    }
}
