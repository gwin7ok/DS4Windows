using System;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 仮想パッドの出力（Xbox 360／DS4）が、ドライバーの代わりの偽のバックエンド（<see cref="FakeVirtualPadBackend"/>）だけで確かめられること。
    /// ボタン・軸・ライフサイクル・振動の戻りの購読の解除を、実機（ViGEmBus）なしで固定する。
    /// </summary>
    public class VirtualPadOutputDevicesTests
    {
        [Fact]
        public void AllocateController_Xbox360_MapsButtonsAxesAndTriggers_ThroughTheTarget()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice device = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0));
            Assert.IsType<Xbox360OutDevice>(device);
            FakeXbox360Target target = Assert.Single(backend.Xbox360Targets);

            device.Connect();
            Assert.Equal(1, target.ConnectCount);

            var state = new ControllerState { Cross = true, Square = true, DpadUp = true, L1 = true, Share = true, PS = true, L2 = 200, R2 = 7, LX = 0x80, LY = 0xFF, RX = 0, RY = 0x80 };
            device.ConvertandSendReport(state, 0);

            ushort expected = (ushort)(Xbox360Buttons.A | Xbox360Buttons.X | Xbox360Buttons.Up | Xbox360Buttons.LeftShoulder | Xbox360Buttons.Back | Xbox360Buttons.Guide);
            Assert.Equal(expected, target.Buttons);
            Assert.Equal(200, target.LeftTriggerValue);
            Assert.Equal(7, target.RightTriggerValue);
            Assert.Equal(1, target.SubmitCount);
            // 中心（0x80）は 0 付近、左スティックの Y は上下を反転する
            Assert.InRange((int)target.LeftThumbXValue, -300, 300);
            Assert.True(target.LeftThumbYValue < -30000, "LY=0xFF（下）は Y 軸の反転で負の最大側になる");
            Assert.True(target.RightThumbXValue < -30000, "RX=0 は負の最大側");
        }

        [Fact]
        public void Xbox360_Disconnect_ReleasesEveryFeedbackSubscription()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            var device = (Xbox360OutDevice)TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0));
            FakeXbox360Target target = backend.Xbox360Targets[0];
            device.Connect();
            device.forceFeedbacksDict.Add(0, target.SubscribeFeedback((l, s) => { }));
            device.forceFeedbacksDict.Add(1, target.SubscribeFeedback((l, s) => { }));
            Assert.Equal(2, target.ActiveSubscriptions);

            device.RemoveFeedback(0);
            Assert.Equal(1, target.ActiveSubscriptions);

            device.Disconnect();
            Assert.Equal(0, target.ActiveSubscriptions);
            Assert.Empty(device.forceFeedbacksDict);
            Assert.Equal(1, target.DisconnectCount);
        }

        [Fact]
        public void Xbox360_ResetState_ResetsAndOptionallySubmits()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice device = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0));
            FakeXbox360Target target = backend.Xbox360Targets[0];

            device.ResetState(submit: false);
            Assert.Equal(1, target.ResetCount);
            Assert.Equal(0, target.SubmitCount);
            device.ResetState();
            Assert.Equal(2, target.ResetCount);
            Assert.Equal(1, target.SubmitCount);
        }

        [Fact]
        public void AllocateController_Ds4_ChoosesTheDeviceKindByTheDriverVersion()
        {
            // 1.19 以降: 出力バッファを待つ拡張（生のレポートを送る）。1.17.333 以降: 拡張（生のレポート）。それ未満: 基本（ボタンと軸を個別に設定）
            var state = new ControllerState { Cross = true };

            var extBackend = new FakeVirtualPadBackend();
            extBackend.Start();
            OutputDevice ext = TestOutputCartridges.Allocate(OutContType.DS4, extBackend, new Version(1, 19, 0, 0));
            ext.Connect();
            ext.ConvertandSendReport(state, 0);
            Assert.Single(extBackend.Ds4Targets[0].RawReports);
            Assert.Equal(63, extBackend.Ds4Targets[0].RawReports[0].Length);

            var midBackend = new FakeVirtualPadBackend();
            midBackend.Start();
            OutputDevice mid = TestOutputCartridges.Allocate(OutContType.DS4, midBackend, new Version(1, 17, 333, 0));
            mid.Connect();
            mid.ConvertandSendReport(state, 0);
            Assert.Single(midBackend.Ds4Targets[0].RawReports);

            var basicBackend = new FakeVirtualPadBackend();
            basicBackend.Start();
            OutputDevice basic = TestOutputCartridges.Allocate(OutContType.DS4, basicBackend, new Version(1, 16, 0, 0));
            basic.Connect();
            basic.ConvertandSendReport(state, 0);
            Assert.Empty(basicBackend.Ds4Targets[0].RawReports);
            Assert.Equal(1, basicBackend.Ds4Targets[0].SubmitCount);
        }

        [Fact]
        public void Ds4Basic_MapsButtonsDPadAndAxes_ThroughTheTarget()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice device = TestOutputCartridges.Allocate(OutContType.DS4, backend, new Version(1, 16, 0, 0));
            FakeDs4Target target = backend.Ds4Targets[0];
            device.Connect();

            var state = new ControllerState { Cross = true, Share = true, L2Btn = true, PS = true, OutputTouchButton = true, DpadUp = true, DpadRight = true, L2 = 9, R2 = 8, LX = 1, LY = 2, RX = 3, RY = 4 };
            device.ConvertandSendReport(state, 0);

            Assert.Equal((ushort)(Ds4Buttons.Cross | Ds4Buttons.Share | Ds4Buttons.TriggerLeft), target.Buttons);
            Assert.Equal((byte)(Ds4SpecialButtons.Ps | Ds4SpecialButtons.Touchpad), target.SpecialButtons);
            Assert.Equal(Ds4DPadDirection.Northeast, target.DPad);
            Assert.Equal(9, target.LeftTriggerValue);
            Assert.Equal(8, target.RightTriggerValue);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, new[] { target.LeftThumbXValue, target.LeftThumbYValue, target.RightThumbXValue, target.RightThumbYValue });
        }

        [Fact]
        public void Ds4_RemoveFeedbacks_AndDisconnect_AreSafeWithoutSubscriptions()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice device = TestOutputCartridges.Allocate(OutContType.DS4, backend, new Version(1, 16, 0, 0));
            device.Connect();
            device.RemoveFeedbacks();
            device.RemoveFeedback(3);
            device.Disconnect();
            Assert.Equal(1, backend.Ds4Targets[0].DisconnectCount);
        }

        [Fact]
        public void AllocateController_WithAStoppedBackend_Throws_AndNoneReturnsNull()
        {
            var backend = new FakeVirtualPadBackend();   // 未開始
            var version = new Version(1, 19, 0, 0);
            Assert.Throws<InvalidOperationException>(() => TestOutputCartridges.Allocate(OutContType.X360, backend, version));
            Assert.Throws<InvalidOperationException>(() => TestOutputCartridges.Allocate(OutContType.DS4, backend, version));
            Assert.Null(TestOutputCartridges.Allocate(OutContType.None, backend, version));
        }
    }
}
