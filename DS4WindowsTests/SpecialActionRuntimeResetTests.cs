using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step14（決定 D14）: <see cref="SpecialActionRuntimeReset"/>（Core の契約 <see cref="ISpecialActionRuntimeReset"/> の第2層の実装）が、
    /// 旧呼び出し（<c>IManagedActionManager.ClearAllEntries</c>、<c>SyntheticInputState.ResetRuntimeStateForKey</c>）と同じことをする。
    /// </summary>
    public class SpecialActionRuntimeResetTests
    {
        [Fact]
        public void ClearAllActionStates_ClearsAllEntriesOfTheManager()
        {
            var manager = new RecordingManager();
            var reset = new SpecialActionRuntimeReset(new SyntheticInputState(), manager);

            reset.ClearAllActionStates();

            Assert.Equal(1, manager.ClearAllEntriesCalls);
        }

        [Fact]
        public void ResetRuntimeStateForKey_RemovesTheKeyOnEveryDevice_AndClearsItsToggle()
        {
            var state = new SyntheticInputState();
            foreach (var device in state.DeviceState)
            {
                device.keyPresses[65] = new SyntheticState.KeyPresses();
                device.keyPresses[66] = new SyntheticState.KeyPresses();
            }
            var manager = new RecordingManager();
            var reset = new SpecialActionRuntimeReset(state, manager);

            reset.ResetRuntimeStateForKey(65);

            foreach (var device in state.DeviceState)
            {
                Assert.False(device.keyPresses.ContainsKey(65));
                Assert.True(device.keyPresses.ContainsKey(66));
            }
            Assert.Equal(new ushort[] { 65 }, manager.ClearedToggleKeys);
        }

        [Fact]
        public void Constructor_RequiresBothDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRuntimeReset(null, new RecordingManager()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRuntimeReset(new SyntheticInputState(), null));
        }

        [Fact]
        public void TheContainer_ResolvesTheResetAsASingleton()
        {
            var first = TestHost.GetService<ISpecialActionRuntimeReset>();
            Assert.IsType<SpecialActionRuntimeReset>(first);
            Assert.Same(first, TestHost.GetService<ISpecialActionRuntimeReset>());
        }

        private sealed class RecordingManager : IManagedActionManager
        {
            public int ClearAllEntriesCalls { get; private set; }
            public List<ushort> ClearedToggleKeys { get; } = new List<ushort>();

            public event Action<SpecialAction, int, bool, bool> ToggledOnChanged { add { } remove { } }

            public ActionInstanceState GetStateFor(SpecialAction action, int device) => new ActionInstanceState();
            public bool IsBeingTriggered(SpecialAction action, int device) => false;
            public void ClearToggledOnForKey(ushort key) => ClearedToggleKeys.Add(key);
            public void ClearAllToggledOn() { }
            public void ClearAllEntries() => ClearAllEntriesCalls++;
            public void ClearDeviceState(int device) { }
            public void SetToggledOn(SpecialAction action, int device, bool value) { }
        }
    }
}
