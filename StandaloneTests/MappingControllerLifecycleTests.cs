using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Actions;

namespace StandaloneTests
{
    [TestClass]
    public class MappingControllerLifecycleTests
    {
        // Phase7-Step8 W2: 登録簿は Mapping から KeyButtonControllerRegistry へ移った。
        // Phase13-Step4: 静的 ActionManager.ClearDeviceState は廃止した。管理役（DefaultActionManager）が、遅延で受けた IMapping の HandleDeviceDisconnect を呼ぶ。
        // ここでは IMapping の代わりに、登録簿を破棄するだけの偽物を差す。
        private class DisconnectOnlyMapping : DS4Windows.Processing.Abstractions.IMapping
        {
            private readonly DS4Windows.Processing.Abstractions.IKeyButtonControllerRegistry registry;
            public DisconnectOnlyMapping(DS4Windows.Processing.Abstractions.IKeyButtonControllerRegistry registry) { this.registry = registry; }
            public void HandleDeviceDisconnect(int device) { registry.ClearForDevice(device); }
            // Phase7b W1: 復帰の合図の退避・復元（このスタブは合図を持たない）
            public ProfileUntriggerSnapshot CaptureProfileUntrigger(int device) { return default; }
            public void RestoreProfileUntrigger(int device, ProfileUntriggerSnapshot snapshot) { }
            public DS4State SetCurveAndDeadzone(int device, DS4State cState, DS4State dState, DS4Windows.Core.Abstractions.IProfileSettingsService settings) { throw new NotSupportedException(); }
            public void MapCustom(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState, Mouse tp) { throw new NotSupportedException(); }
            public void TempMouseJoystick(int device, DS4State MappedState) { throw new NotSupportedException(); }
            public void Commit(int device, DS4Windows.Core.Abstractions.IProfileSettingsService settings) { throw new NotSupportedException(); }
            public bool GetBoolMappingExternal(int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp) { throw new NotSupportedException(); }
            public void PlayMacroDirect(int device, SpecialAction action) { throw new NotSupportedException(); }
            public void EndMacroDirect(int device) { throw new NotSupportedException(); }
        }

        [TestMethod]
        public void ClearDeviceState_RemovesKeyButtonControllersForDevice()
        {
            // Arrange: ensure no controllers for device 5
            int device = 5;

            DS4Windows.Processing.Services.KeyButtonControllerRegistry registry = null;
            var manager = new DS4Windows.Processing.Actions.DefaultActionManager(() => new DisconnectOnlyMapping(registry));
            registry = new DS4Windows.Processing.Services.KeyButtonControllerRegistry(
                new DS4Windows.Processing.Actions.DefaultKeyButtonActionControllerFactory(new DS4Windows.Processing.Actions.DefaultControllerRegistry(), manager),
                new DS4Windows.Processing.Actions.DefaultControllerRegistry(), manager);
            var dictField = typeof(DS4Windows.Processing.Services.KeyButtonControllerRegistry).GetField("_controllers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dictField, "Could not find _controllers field via reflection");
            var dict = dictField.GetValue(registry) as System.Collections.IDictionary;
            Assert.IsNotNull(dict);

            {
                // Create entries
                registry.GetOrCreate(device, KeyButtonActionController.Mode.Press, "test-action-1");
                registry.GetOrCreate(device, KeyButtonActionController.Mode.Toggle, "test-action-2");

                // Verify entries exist for device
                var keysBefore = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.IsTrue(keysBefore.Length >= 1, "Expected at least one KeyButtonActionController entry for device before clear");

                // Act: clear device state via the manager (which calls IMapping.HandleDeviceDisconnect → registry.ClearForDevice)
                manager.ClearDeviceState(device);

                // Assert: no entries remain for that device
                var keysAfter = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.IsTrue(keysAfter.Length == 0, "Expected no KeyButtonActionController entries for device after ClearDeviceState");
            }
        }
    }
}
