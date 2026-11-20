#nullable enable
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="RuntimeFeatureSwitch"/> は、OSC・UDP・HidHide・タッチパッド・入力の検出へ転送するだけの窓口。
    /// 偽の相手を差して、どの呼び出しがどの相手へ、同じ引数で渡るかを確かめる（旧: 転送の文の文字列をソースで確かめていた）。
    /// </summary>
    public class RuntimeFeatureSwitchForwardingTests
    {
        private readonly IOscService _osc = RecordingProxy.Create<IOscService>();
        private readonly IUdpMotionService _udp = RecordingProxy.Create<IUdpMotionService>();
        private readonly IHidHideService _hidHide = RecordingProxy.Create<IHidHideService>();
        private readonly ITouchpadToggleService _toggle = RecordingProxy.Create<ITouchpadToggleService>();
        private readonly IActiveInputProbe _probe = RecordingProxy.Create<IActiveInputProbe>();
        private readonly IDeviceRumbleService _rumble = RecordingProxy.Create<IDeviceRumbleService>();
        private readonly DeviceSlotState _slots = new DeviceSlotState();

        private RuntimeFeatureSwitch Create()
        {
            return new RuntimeFeatureSwitch(_osc, _udp, _hidHide, _toggle, _slots, _probe, _rumble, new EnvironmentService());
        }

        [Fact]
        public void TheOscCommands_GoToTheOscService()
        {
            RuntimeFeatureSwitch sw = Create();
            sw.ChangeOscListenerStatus(true);
            sw.ChangeOscSenderStatus(false);

            var calls = RecordingProxy.Of(_osc).Calls;
            Assert.Equal(new[] { "ChangeListenerStatus", "ChangeSenderStatus" }, calls.Select(c => c.Name));
            Assert.Equal(true, calls[0].Args[0]);
            Assert.Equal(false, calls[1].Args[0]);
        }

        [Fact]
        public void TheUdpCommands_GoToTheMotionService_WithTheirArguments()
        {
            RuntimeFeatureSwitch sw = Create();
            sw.ChangeUdpStatus(true, false);
            sw.ChangeMotionEventStatus(true);

            var calls = RecordingProxy.Of(_udp).Calls;
            Assert.Equal(new[] { "ChangeStatus", "ChangeMotionEventStatus" }, calls.Select(c => c.Name));
            Assert.Equal(new object?[] { true, false }, calls[0].Args);
            Assert.Equal(new object?[] { true }, calls[1].Args);
        }

        [Fact]
        public void TheHidHideCommands_GoToTheHidHideService_WithTheirArguments()
        {
            RuntimeFeatureSwitch sw = Create();
            sw.CheckHidHidePresence(@"C:\a.exe", "name", false);
            sw.UpdateHidHiddenAttributes();

            var calls = RecordingProxy.Of(_hidHide).Calls;
            Assert.Equal(new[] { "CheckPresence", "UpdateAttributes" }, calls.Select(c => c.Name));
            Assert.Equal(new object?[] { @"C:\a.exe", "name", false }, calls[0].Args);
        }

        [Fact]
        public void TheTouchpadSlide_AsksTheToggleServiceAboutTheSlot()
        {
            _slots.CurrentStates[2] = new ControllerState();
            RuntimeFeatureSwitch sw = Create();
            RecordingProxy.Of(_toggle).Returns["Slide"] = "swiped";

            Assert.Equal("swiped", sw.TouchpadSlide(2));

            object?[] args = Assert.Single(RecordingProxy.Of(_toggle).ArgsOf("Slide"));
            Assert.Equal(2, args[0]);
            Assert.Same(_slots.CurrentStates[2], args[1]);
            Assert.Equal(false, args[3]);   // 接続されたコントローラーがない
        }

        [Fact]
        public void TheActiveInput_AsksTheProbeAboutTheSlot()
        {
            _slots.CurrentStates[1] = new ControllerState();
            RuntimeFeatureSwitch sw = Create();
            RecordingProxy.Of(_probe).Returns["GetActive"] = DS4Controls.Cross;

            Assert.Equal(DS4Controls.Cross, sw.GetActiveInputControl(1));

            object?[] args = Assert.Single(RecordingProxy.Of(_probe).ArgsOf("GetActive"));
            Assert.Same(_slots.CurrentStates[1], args[0]);
        }
    }
}
