using System;
using System.Reflection;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// UI 向けの、接続中のコントローラーの読み取りの窓口（<see cref="IControllerSlotQuery"/>）の振る舞い。
    /// 機器ごとに同じ面を返すこと（一覧での同一判定が参照の比較でできる）、一覧が接続の順でスロット番号を持つこと、
    /// 接続の通知と機器のイベントが、面を送り主として UI に届くことを守る。
    /// 機器は <see cref="TestDs4Device"/>（HID なし）。機器のイベントはバッキングフィールド経由で発生させる。
    /// </summary>
    public class ControllerSlotQueryTests
    {
        private sealed class FakeLifecycle : IServiceLifecycle
        {
            public bool Running { get; set; }
            public bool LoopControllers { get; set; }
            public bool InServiceTask { get; set; }
            public int ActiveControllers { get; set; }
            public bool Suspending { get; set; }
#pragma warning disable CS0067
            public event EventHandler ServiceStarted;
            public event EventHandler PreServiceStop;
            public event EventHandler ServiceStopped;
            public event EventHandler RunningChanged;
#pragma warning restore CS0067
            public event Action<InputDeviceCore, int> HotplugController;
            public bool Start(bool showlog = true) => true;
            public bool Stop(bool showlog = true, bool immediateUnplug = false) => true;
            public bool HotPlug() => true;
            public void PrepareAbort() { }
            public void RaiseHotplug(InputDeviceCore device, int index) => HotplugController?.Invoke(device, index);
        }

        private static (ControllerSlotQuery Query, DeviceSlotState State, FakeLifecycle Lifecycle) Create()
        {
            var state = new DeviceSlotState();
            var lifecycle = new FakeLifecycle();
            return (new ControllerSlotQuery(state, lifecycle, null), state, lifecycle);
        }

        private static void RaiseDeviceEvent(InputDeviceCore device, string eventName)
        {
            FieldInfo field = typeof(InputDeviceCore).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            Delegate handler = (Delegate)field.GetValue(device);
            handler?.DynamicInvoke(device, EventArgs.Empty);
        }

        [Fact]
        public void GetSlot_ReturnsNull_ForEmptyOrOutOfRangeSlots()
        {
            var (query, _, _) = Create();

            Assert.Null(query.GetSlot(0));
            Assert.Null(query.GetSlot(-1));
            Assert.Null(query.GetSlot(query.SlotCount));
            Assert.True(query.SlotCount > 0);
        }

        [Fact]
        public void GetSlot_ReturnsTheSameViewForTheSameDevice()
        {
            var (query, state, _) = Create();
            TestDs4Device device = TestDs4Device.Create();
            state.DS4Controllers[2] = device;

            IControllerSlotView first = query.GetSlot(2);

            Assert.NotNull(first);
            Assert.Same(first, query.GetSlot(2));
        }

        [Fact]
        public void Snapshot_ListsDevicesInConnectionOrderWithTheirSlotNumbers()
        {
            var (query, state, _) = Create();
            TestDs4Device first = TestDs4Device.Create();
            TestDs4Device second = TestDs4Device.Create();
            state.SlotManager.AddController(first, 3);
            state.SlotManager.AddController(second, 1);
            state.DS4Controllers[3] = first;
            state.DS4Controllers[1] = second;

            var entries = query.Snapshot();

            Assert.Equal(2, entries.Count);
            Assert.Equal(3, entries[0].SlotIndex);
            Assert.Same(query.GetSlot(3), entries[0].View);
            Assert.Equal(1, entries[1].SlotIndex);
            Assert.Same(query.GetSlot(1), entries[1].View);
        }

        [Fact]
        public void Snapshot_IsACopy_SoLaterChangesDoNotAffectIt()
        {
            var (query, state, _) = Create();
            TestDs4Device device = TestDs4Device.Create();
            state.SlotManager.AddController(device, 0);

            var entries = query.Snapshot();
            state.SlotManager.RemoveController(device, 0);

            Assert.Single(entries);
            Assert.Empty(query.Snapshot());
        }

        [Fact]
        public void SlotChanged_IsRaisedOnHotplug_WithTheViewAndTheSlotNumber()
        {
            var (query, state, lifecycle) = Create();
            TestDs4Device device = TestDs4Device.Create();
            state.DS4Controllers[4] = device;
            ControllerSlotChangedEventArgs received = null;
            query.SlotChanged += (s, e) => received = e;

            lifecycle.RaiseHotplug(device, 4);

            Assert.NotNull(received);
            Assert.Equal(4, received.SlotIndex);
            Assert.Same(query.GetSlot(4), received.View);
        }

        [Theory]
        [InlineData("BatteryChanged")]
        [InlineData("ChargingChanged")]
        [InlineData("MacAddressChanged")]
        [InlineData("SyncChange")]
        [InlineData("Removal")]
        public void DeviceEvents_AreForwardedWithTheViewAsSender(string eventName)
        {
            var (query, state, _) = Create();
            TestDs4Device device = TestDs4Device.Create();
            state.DS4Controllers[0] = device;
            IControllerSlotView view = query.GetSlot(0);
            object sender = null;
            EventHandler handler = (s, e) => sender = s;
            switch (eventName)
            {
                case "BatteryChanged": view.BatteryChanged += handler; break;
                case "ChargingChanged": view.ChargingChanged += handler; break;
                case "MacAddressChanged": view.MacAddressChanged += handler; break;
                case "SyncChange": view.SyncChange += handler; break;
                default: view.Removal += handler; break;
            }

            RaiseDeviceEvent(device, eventName);

            Assert.Same(view, sender);
        }

        [Fact]
        public void ViewEvents_CanBeUnsubscribedWithTheSameDelegate()
        {
            var (query, state, _) = Create();
            TestDs4Device device = TestDs4Device.Create();
            state.DS4Controllers[0] = device;
            IControllerSlotView view = query.GetSlot(0);
            int count = 0;
            EventHandler handler = (s, e) => count++;
            view.Removal += handler;
            RaiseDeviceEvent(device, "Removal");
            view.Removal -= handler;

            RaiseDeviceEvent(device, "Removal");

            Assert.Equal(1, count);
        }
    }
}
