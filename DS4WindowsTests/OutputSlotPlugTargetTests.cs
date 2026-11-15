using DS4Windows.Core.Model;
using DS4Windows.Output;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 仮想パッドを差す先のスロット。予約したスロットや、利用者が選んだスロットへ差す（以前は、常に最初の空きスロットへ差し、
    /// 予約のないスロットにパッドが残って、予約したスロットは空のままだった）。
    /// </summary>
    public class OutputSlotPlugTargetTests
    {
        private sealed class FakePad : OutputDevice
        {
            public override void ConvertandSendReport(ControllerState state, int device) { }
            public override void Connect() { connected = true; }
            public override void Disconnect() { connected = false; }
            public override void ResetState(bool submit = true) { }
            public override string GetDeviceType() => "X360";
            public override void RemoveFeedbacks() { }
            public override void RemoveFeedback(int inIdx) { }
        }

        private static OutputSlotManager NewManager() => TestRepositoryFactory.CreateOutputSlotManager();

        [Fact]
        public void APreferredSlot_ReceivesThePad()
        {
            OutputSlotManager manager = NewManager();
            var outdevs = new OutputDevice[8];

            manager.DeferredPlugin(new FakePad(), -1, "", outdevs, OutContType.X360, 4);

            Assert.Equal(OutputSlotAttachedStatus.Attached, manager.OutputSlots[4].CurrentAttachedStatus);
            Assert.Equal(OutputSlotAttachedStatus.UnAttached, manager.OutputSlots[0].CurrentAttachedStatus);
        }

        [Fact]
        public void WithoutAPreferredSlot_TheFirstEmptySlotIsUsed()
        {
            OutputSlotManager manager = NewManager();

            manager.DeferredPlugin(new FakePad(), -1, "", new OutputDevice[8], OutContType.X360);

            Assert.Equal(OutputSlotAttachedStatus.Attached, manager.OutputSlots[0].CurrentAttachedStatus);
        }

        [Fact]
        public void ABusyPreferredSlot_FallsBackToTheFirstEmptySlot()
        {
            OutputSlotManager manager = NewManager();
            var outdevs = new OutputDevice[8];
            manager.DeferredPlugin(new FakePad(), -1, "", outdevs, OutContType.X360, 2);

            manager.DeferredPlugin(new FakePad(), -1, "", outdevs, OutContType.X360, 2);

            Assert.Equal(OutputSlotAttachedStatus.Attached, manager.OutputSlots[0].CurrentAttachedStatus);
            Assert.Equal(OutputSlotAttachedStatus.Attached, manager.OutputSlots[2].CurrentAttachedStatus);
        }

        [Fact]
        public void AnOutOfRangePreferredSlot_FallsBackToTheFirstEmptySlot()
        {
            OutputSlotManager manager = NewManager();

            manager.DeferredPlugin(new FakePad(), -1, "", new OutputDevice[8], OutContType.X360, 99);

            Assert.Equal(OutputSlotAttachedStatus.Attached, manager.OutputSlots[0].CurrentAttachedStatus);
        }
    }
}
