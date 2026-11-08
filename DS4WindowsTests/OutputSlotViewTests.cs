using System;
using System.Reflection;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// UI 向けの出力スロットの面（<see cref="IOutputSlotView"/>）と、スロットへの割り当て・解除の通知の振る舞い。
    /// スロットごとに同じ面を返すこと（一覧での同一判定）、書き込みが出力スロットへ届くこと、
    /// 通知が面（またはサービス）を送り主にして届くことを守る。
    /// </summary>
    public class OutputSlotViewTests
    {
        private static IOutputSlotService Create(out OutputSlotManager manager)
        {
            manager = TestRepositoryFactory.CreateOutputSlotManager();
            return TestRepositoryFactory.CreateOutputSlotService(manager);
        }

        [Fact]
        public void SlotViews_HaveOneViewPerSlot_InSlotOrder_AndAreStable()
        {
            var service = Create(out var manager);

            var first = service.SlotViews;
            var second = service.SlotViews;

            Assert.Equal(manager.OutputSlots.Length, first.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(i, first[i].Index);
                Assert.Same(first[i], second[i]);
            }
        }

        [Fact]
        public void EmptySlot_ShowsNoDeviceAndNoInput()
        {
            var service = Create(out _);
            IOutputSlotView view = service.SlotViews[0];

            Assert.Equal("Empty", view.CurrentTypeName);
            Assert.Equal(OutputSlotAttachedStatus.UnAttached, view.AttachedStatus);
            Assert.Equal(OutputSlotInputBound.Unbound, view.InputBound);
            Assert.False(view.SupportsXInputSlotNumber);
            Assert.Equal(-1, view.XInputSlotNumber);
            Assert.Equal(-1, view.InputIndex);
        }

        [Fact]
        public void Writes_ReachTheOutputSlot()
        {
            var service = Create(out var manager);
            IOutputSlotView view = service.SlotViews[1];

            view.CurrentType = OutContType.X360;
            view.ReserveStatus = OutputSlotReserveStatus.Permanent;

            OutSlotDevice slot = manager.OutputSlots[1];
            Assert.Equal(OutContType.X360, slot.CurrentType);
            Assert.Equal(OutputSlotReserveStatus.Permanent, slot.CurrentReserveStatus);
            // 常設にした時点で、今の種別が常設の種別になる（スロット側の既存の動作）
            Assert.Equal(OutContType.X360, view.PermanentType);
        }

        [Fact]
        public void PermanentTypeChanged_IsForwardedWithTheViewAsSender()
        {
            var service = Create(out _);
            IOutputSlotView view = service.SlotViews[2];
            object sender = null;
            view.PermanentTypeChanged += (s, e) => sender = s;

            view.PermanentType = OutContType.DS4;

            Assert.Same(view, sender);
        }

        [Theory]
        [InlineData("SlotAssigned")]
        [InlineData("SlotUnassigned")]
        public void AssignmentNotifications_AreForwardedFromTheSlotManager(string eventName)
        {
            var service = Create(out var manager);
            OutputSlotAssignmentEventArgs received = null;
            object sender = null;
            EventHandler<OutputSlotAssignmentEventArgs> handler = (s, e) => { sender = s; received = e; };
            if (eventName == "SlotAssigned") service.SlotAssigned += handler; else service.SlotUnassigned += handler;

            FieldInfo field = typeof(OutputSlotManager).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            Delegate raise = (Delegate)field.GetValue(manager);
            raise.DynamicInvoke(manager, 3, manager.OutputSlots[3]);

            Assert.Same(service, sender);
            Assert.Equal(3, received.SlotIndex);
        }
    }
}
