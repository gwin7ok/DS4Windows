using DS4Windows.Services;
using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions.Tests
{
    /// <summary>
    /// IManagedActionManager の軽量モック（フェーズ0-4 雛形）。
    /// 実際のテストでの利用はフェーズ1以降で拡張予定。
    /// </summary>
    public class MockManagedActionManager : IManagedActionManager
    {
        public List<string> RegisteredActions { get; } = new List<string>();

        public event Action<SpecialAction, int, bool, bool> ToggledOnChanged;

        public ActionInstanceState GetStateFor(SpecialAction action, int device) => new ActionInstanceState();
        public void ClearToggledOnForKey(ushort key) { }
        public void ClearAllToggledOn() { }
        public void ClearAllEntries() { }
        public void ClearDeviceState(int device) { }
        public void SetToggledOn(SpecialAction action, int device, bool value)
        {
            ToggledOnChanged?.Invoke(action, device, false, value);
        }

        public void RegisterAction(string actionName, IActionBinding binding)
        {
            RegisteredActions.Add(actionName);
        }

        public IActionBinding GetBinding(string actionName) => null;
        public IEnumerable<string> GetRegisteredActions() => RegisteredActions;
    }
}
