using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    public class Ds4DeviceRegistryTests
    {
        private class MockDs4DeviceRegistry : IInputDeviceRegistry
        {
            public List<InputDeviceCore> MockDevices { get; } = new List<InputDeviceCore>();
            public int FindControllersCalls { get; private set; }
            public int StopControllersCalls { get; private set; }
            public List<InputDeviceCore> RemoveCalls { get; } = new List<InputDeviceCore>();
            public bool IsExclusiveMode { get; set; }

            // CS0067 警告回避のための明示的空アクセサ
            public event RequestElevationDelegate RequestElevation
            {
                add { }
                remove { }
            }

            public PrepareInitDelegate PrepareDS4Init { get; set; }
            public PrepareInitDelegate PostDS4Init { get; set; }
            public CheckPendingDevice PreparePendingDevice { get; set; }

            public IEnumerable<InputDeviceCore> FindControllers()
            {
                FindControllersCalls++;
                return MockDevices;
            }

            public IEnumerable<InputDeviceCore> GetControllers() => MockDevices;

            public void StopControllers()
            {
                StopControllersCalls++;
            }

            public bool RemoveDevice(InputDeviceCore device)
            {
                RemoveCalls.Add(device);
                return MockDevices.Remove(device);
            }

            public void OnRemoval(object sender, EventArgs e) { }
            public void UpdateSerial(object sender, EventArgs e) { }
            public void ReEnableDevice(string deviceInstanceId) { }

        }

        [Fact]
        public void Adapter_InstantiatesAndImplementsInterfaceSafely()
        {
            var adapter = new InputDeviceRegistryAdapter(new InputDeviceRegistry(new FakeDeviceLayerSettings(), System.Array.Empty<DS4Windows.Input.Abstractions.IInputDeviceCartridge>()));

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.GetControllers());

            // Null 引数に対する安全性の検証
            bool removeNull = adapter.RemoveDevice(null);
            Assert.False(removeNull);

            var onRemovalEx = Record.Exception(() => adapter.OnRemoval(null, EventArgs.Empty));
            Assert.Null(onRemovalEx);

            var updateSerialEx = Record.Exception(() => adapter.UpdateSerial(null, EventArgs.Empty));
            Assert.Null(updateSerialEx);

            var reEnableEx = Record.Exception(() => adapter.ReEnableDevice(null));
            Assert.Null(reEnableEx);
        }

        [Fact]
        public void MockRegistry_TracksDeviceOperationsAccurately()
        {
            var mockRegistry = new MockDs4DeviceRegistry();

            // FindControllers
            var found = mockRegistry.FindControllers();
            Assert.NotNull(found);
            Assert.Equal(1, mockRegistry.FindControllersCalls);

            // StopControllers
            mockRegistry.StopControllers();
            Assert.Equal(1, mockRegistry.StopControllersCalls);

            // RemoveDevice
            bool removed = mockRegistry.RemoveDevice(null);
            Assert.False(removed);
            Assert.Single(mockRegistry.RemoveCalls);
        }
    }
}
