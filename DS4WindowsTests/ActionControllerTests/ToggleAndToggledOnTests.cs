using System;
using DS4WindowsTests;
using Xunit;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Actions.Tests
{

    // 共有のテスト用ホストのサービスを使うテストは同じコレクションで直列に実行する
    [Collection(SharedHostStateCollection.Name)]
    public class ToggleAndToggledOnTests
    {
        /// <summary>
        /// Phase13-Step4: 旧 <c>ServiceProviderHolder.SetProvider(TestServiceProvider)</c> の代わりに、テストごとに独立の管理役・登録簿を作って
        /// <see cref="KeyAction"/>／アダプターへ直接渡す（静的な Service Locator を介さない）。
        /// </summary>
        private sealed class Parts
        {
            public readonly DefaultActionManager Manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            public readonly DS4Windows.Processing.Services.KeyButtonControllerRegistry Registry;

            public Parts()
            {
                var controllerRegistry = new DefaultControllerRegistry();
                Registry = new DS4Windows.Processing.Services.KeyButtonControllerRegistry(
                    new DefaultKeyButtonActionControllerFactory(controllerRegistry, Manager, DS4WindowsTests.TestActions.Dispatcher), controllerRegistry, Manager, DS4WindowsTests.TestActions.Dispatcher);
            }
        }

        [Fact]
        public void Toggle_OnTrigger_Toggles_IsToggledOn_State()
        {
            var p = new Parts();
            var dam = p.Manager;
            var registry = p.Registry;

            // create a SpecialAction configured as Toggle
            var sa = new DS4Windows.Core.Settings.SpecialAction("toggletest", "0", "Key", "0");
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

            var ka = new DS4Windows.Processing.Actions.KeyAction(sa, 0, dam, registry);
            var handler = new FakeKBMHandler();

            // Ensure a clean starting state
            dam.ClearAllToggledOn();
            dam.ClearAllEntries();

            // First trigger should set IsToggledOn true
            ka.OnTrigger(0, 0, 0, false, handler);
            var stateAfter = dam.GetStateFor(sa, 0);
            Assert.True(stateAfter.IsToggledOn);

            // Simulate explicit Toggle-OFF by invoking controller's OnSATriggerToggleOff
            // Phase7-Step8 W2: 登録簿は Mapping から KeyButtonControllerRegistry へ移った
            var dictField = typeof(DS4Windows.Processing.Services.KeyButtonControllerRegistry).GetField("_controllers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var dict = dictField.GetValue(registry) as System.Collections.IDictionary;
            // Find controller instance matching action name (sa.name) and invoke ToggleOff
            if (dict != null)
            {
                // As a last resort in tests, invoke ToggleOff on every controller to ensure any active toggle is cleared.
                foreach (System.Collections.DictionaryEntry de in dict)
                {
                    var inst = de.Value as DS4Windows.Processing.Actions.KeyButtonActionController;
                    try { inst?.OnSATriggerToggleOff(0, 0, false, handler); } catch { }
                }
            }

            var stateAfterOff = dam.GetStateFor(sa, 0);
            Assert.False(stateAfterOff.IsToggledOn);
        }

        [Fact]
        public void Press_OnTrigger_DoesNot_Set_IsToggledOn()
        {
            var p = new Parts();
            var dam = p.Manager;

            var sa = new DS4Windows.Core.Settings.SpecialAction("presstest", "0", "Key", "0");
            // ensure Press mode
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Press;

            var ka = new DS4Windows.Processing.Actions.KeyAction(sa, 0, dam, p.Registry);
            var handler = new FakeKBMHandler();

            // Ensure a clean starting state
            dam.ClearAllToggledOn();
            dam.ClearAllEntries();

            ka.OnTrigger(0, 0, 0, false, handler);
            var stateAfter = dam.GetStateFor(sa, 0);
            Assert.False(stateAfter.IsToggledOn);
        }

        [Fact]
        public void IsToggledOn_SingleWriter_EventOnlyFromManager()
        {
            var dam = new DefaultActionManager(TestRepositoryFactory.MappingProvider);

            var sa = new DS4Windows.Core.Settings.SpecialAction("invarianttest", "0", "Key", "0");
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

            dam.ClearAllToggledOn();
            dam.ClearAllEntries();

            bool eventFired = false;
            Action<DS4Windows.Core.Settings.SpecialAction, int, bool, bool> handler = (a, d, oldv, newv) => { eventFired = true; };
            dam.ToggledOnChanged += handler;

            // Directly mutate state: should NOT fire ToggledOnChanged event
            var st = dam.GetStateFor(sa, 0);
            Assert.NotNull(st);
            st.IsToggledOn = true;
            Assert.True(st.IsToggledOn);
            Assert.False(eventFired);

            // Now use manager API to change: this SHOULD fire the event
            dam.SetToggledOn(sa, 0, false);
            Assert.False(st.IsToggledOn);
            Assert.True(eventFired);

            dam.ToggledOnChanged -= handler;
        }

        [Fact]
        public void DeterministicRepeater_TriggerViaFake()
        {
            var p = new Parts();
            var dam = p.Manager;
            var registry = p.Registry;

            // Install override to create FakeDeterministicRepeater for any adapter constructions
            DS4Windows.Processing.Actions.RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride = (origFactory) => new FakeDeterministicRepeater();

            try
            {
                // Create a SpecialAction configured as Toggle to drive controller path
                var sa = new DS4Windows.Core.Settings.SpecialAction("repeattest", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                var ka = new DS4Windows.Processing.Actions.KeyAction(sa, 0, dam, registry);
                var handler = new FakeKBMHandler();

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                // Trigger action: controller should create adapter that wraps FakeDeterministicRepeater
                ka.OnTrigger(0, 0, 0, false, handler);

                // Inspect mapping controllers and invoke TriggerOnce on any fake repeater found
                // Phase7-Step8 W2: 登録簿は Mapping から KeyButtonControllerRegistry へ移った
                var dictField = typeof(DS4Windows.Processing.Services.KeyButtonControllerRegistry).GetField("_controllers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var dict = dictField.GetValue(registry) as System.Collections.IDictionary;
                if (dict != null)
                {
                    foreach (System.Collections.DictionaryEntry de in dict)
                    {
                        var inst = de.Value as DS4Windows.Processing.Actions.KeyButtonActionController;
                        if (inst == null) continue;
                        // attempt to access internal entries via reflection to find the IRepeater and trigger it
                        var implField = typeof(DS4Windows.Processing.Actions.KeyButtonActionController).GetField("impl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var impl = implField?.GetValue(inst);
                        if (impl == null) continue;
                        var toggleImplType = impl.GetType();
                        var entriesField = toggleImplType.GetField("entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var entries = entriesField?.GetValue(impl) as System.Collections.IDictionary;
                        if (entries == null) continue;
                        foreach (System.Collections.DictionaryEntry e in entries)
                        {
                            var entry = e.Value;
                            var repField = entry.GetType().GetField("repeater", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            var rep = repField?.GetValue(entry) as FakeDeterministicRepeater;
                            try { rep?.TriggerOnce(); } catch { }
                        }
                    }
                }
            }
            finally
            {
                DS4Windows.Processing.Actions.RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride = null;
            }
        }

        [Fact]
        public void KeyButtonActionControllerAdapter_StartStop_Toggle_Clears_IsToggledOn()
        {
            var dam = new DefaultActionManager(TestRepositoryFactory.MappingProvider);

            var sa = new DS4Windows.Core.Settings.SpecialAction("adaptertoggle", "0", "Key", "0");
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

            dam.ClearAllToggledOn();
            dam.ClearAllEntries();

            var handler = new FakeKBMHandler();
            var adapter = new DS4Windows.Processing.Actions.KeyButtonActionControllerAdapter(0, sa, dam, DS4WindowsTests.TestActions.Dispatcher);
            var binding = new DS4Windows.Processing.Actions.KeyActionBinding(sa);
            var trigger = new DS4Windows.Processing.Actions.TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0,
                NativeValue = 0,
                OutputHandler = handler,
                Timestamp = DateTime.UtcNow
            };

            // Start should set IsToggledOn
            adapter.Start(binding, trigger);
            var st = dam.GetStateFor(sa, 0);
            Assert.NotNull(st);
            Assert.True(st.IsToggledOn);

            // Stop should clear IsToggledOn via ToggleOff path
            adapter.Stop(binding, trigger);
            Assert.False(st.IsToggledOn);
        }

        [Fact]
        public void KeyButtonActionControllerAdapter_Handle_Release_DoesNot_Clear_Toggle()
        {
            var dam = new DefaultActionManager(TestRepositoryFactory.MappingProvider);

            var sa = new DS4Windows.Core.Settings.SpecialAction("adaptertoggle_handle", "0", "Key", "0");
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

            dam.ClearAllToggledOn();
            dam.ClearAllEntries();

            var handler = new FakeKBMHandler();
            var adapter = new DS4Windows.Processing.Actions.KeyButtonActionControllerAdapter(0, sa, dam, DS4WindowsTests.TestActions.Dispatcher);
            var binding = new DS4Windows.Processing.Actions.KeyActionBinding(sa);
            var triggerEst = new DS4Windows.Processing.Actions.TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0,
                NativeValue = 0,
                OutputHandler = handler,
                Timestamp = DateTime.UtcNow
            };

            // Start should set IsToggledOn
            adapter.Start(binding, triggerEst);
            var st = dam.GetStateFor(sa, 0);
            Assert.NotNull(st);
            Assert.True(st.IsToggledOn);

            // Now call Handle with a release edge — toggle-mode controllers should ignore release
            var triggerRel = new DS4Windows.Processing.Actions.TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = false,
                LogicalValue = 0,
                NativeValue = 0,
                OutputHandler = handler,
                Timestamp = DateTime.UtcNow
            };

            adapter.Handle(binding, triggerRel);

            // Toggled state should remain true after release handled
            Assert.True(st.IsToggledOn);
        }

        /// <summary>
        /// Phase13-Step4: コントローラーは、注入された管理役のイベントを購読する。OFF になったら（旧: 静的イベント）保持中の出力を解放し、
        /// 破棄したあとは反応しない（購読を解除する）。
        /// </summary>
        [Fact]
        public void KeyButtonActionController_SubscribesToTheInjectedManager_AndUnsubscribesOnDispose()
        {
            var dam = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var sa = new DS4Windows.Core.Settings.SpecialAction("injectedsub", "0", "Key", "0");
            sa.KeyButtonSwitchMode = DS4Windows.Core.Settings.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

            var controller = new DS4Windows.Processing.Actions.KeyButtonActionController(0, sa, dam, DS4WindowsTests.TestActions.Dispatcher, "injectedsub");
            var handler = new FakeKBMHandler();

            controller.OnSATriggerEstablished(30, 0, false, handler, true);
            Assert.True(dam.GetStateFor(sa, 0).IsToggledOn);

            // 購読の有無は、イベントのデリゲートの購読者数で確かめる
            var evt = typeof(DefaultActionManager).GetField("ToggledOnChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var del = (Delegate)evt.GetValue(dam);
            int before = del.GetInvocationList().Length;

            controller.Dispose();

            del = (Delegate)evt.GetValue(dam);
            Assert.Equal(before - 1, del.GetInvocationList().Length);
        }

        [Fact]
        public void Constructors_RequireTheirManager()
        {
            var sa = new DS4Windows.Core.Settings.SpecialAction("ctorreq", "0", "Key", "0");
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Processing.Actions.KeyButtonActionController(0, sa, null, DS4WindowsTests.TestActions.Dispatcher, "x"));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Processing.Actions.KeyButtonActionController(0, DS4Windows.Processing.Actions.KeyButtonActionController.Mode.Press, null, DS4WindowsTests.TestActions.Dispatcher, "x"));
            Assert.Throws<ArgumentNullException>(() => new DefaultKeyButtonActionControllerFactory(new DefaultControllerRegistry(), null, DS4WindowsTests.TestActions.Dispatcher));
            Assert.Throws<ArgumentNullException>(() => new DefaultKeyButtonActionControllerFactory(null, new DefaultActionManager(TestRepositoryFactory.MappingProvider), DS4WindowsTests.TestActions.Dispatcher));
        }
    }
}
