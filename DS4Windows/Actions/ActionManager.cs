using DS4Windows.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{

    // Manages Action instances and provides access to per-device state.
    public static class ActionManager
    {
        private static readonly Dictionary<string, ActionEntry> actions = new Dictionary<string, ActionEntry>(StringComparer.OrdinalIgnoreCase);
        private const int ToggleReleaseHoldMsLocal = 200;

        private static ActionEntry GetOrCreateEntry(SpecialAction action)
        {
            if (action == null) return null;
            lock (actions)
            {
                if (!actions.TryGetValue(action.name, out ActionEntry ent) || ent == null)
                {
                    ent = new ActionEntry(action);
                    actions[action.name] = ent;
                }
                return ent;
            }
        }

        // Return the per-action per-device state object. Creates entry if needed.
        public static ActionInstanceState GetStateFor(SpecialAction action, int device)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.GetStateFor(action, device);
                }

                var ent = GetOrCreateEntry(action);
                if (ent == null) return null;
                if (device < 0 || device >= ent.States.Length) return null;
                return ent.States[device];
            }
            catch { return null; }
        }

        // Query whether given action/device is currently marked as being triggered.
        public static bool IsBeingTriggered(SpecialAction action, int device)
        {
            try
            {
                var st = GetStateFor(action, device);
                if (st != null) return st.BeingTriggered;
            }
            catch { }
            return false;
        }

        // Controlled setter for BeingTriggered — centralizes mutations so callers don't assign the field directly.
        private static void SetBeingTriggeredFor(SpecialAction action, int device, bool value)
        {
            try
            {
                var st = GetStateFor(action, device);
                if (st == null) return;
                bool old = st.BeingTriggered;
                if (old == value) return;
                st.SetBeingTriggeredInternal(value);
                try { AppLogger.LogTrace($"ActionManager.SetBeingTriggeredFor: name={action?.name} device={device} old={old} new={value}"); } catch { }
            }
            catch { }
        }

        // Clear toggled-on flag for actions that map to given native key
        public static void ClearToggledOnForKey(ushort key)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearToggledOnForKey(key); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.ActionDef == null) continue;
                            if (ushort.TryParse(ent.ActionDef.details, out ushort k) && k == key)
                            {
                                for (int d = 0; d < ent.States.Length; d++) SetToggledOn(ent.ActionDef, d, false);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Clear pressed-once flags for all known actions
        public static void ClearAllToggledOn()
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearAllToggledOn(); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            for (int d = 0; d < ent.States.Length; d++) SetToggledOn(ent.ActionDef, d, false);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Clear all registered Action entries. Used during profile reload / actions reparse.
        public static void ClearAllEntries()
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearAllEntries(); return; }
                }

                lock (actions)
                {
                    actions.Clear();
                }
            }
            catch { }
        }

        // Clear per-device ActionEntry state and controllers
        public static void ClearDeviceState(int device)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearDeviceState(device); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            if (device >= 0 && device < ent.States.Length)
                            {
                                ent.States[device] = new ActionInstanceState();
                                try { AppLogger.LogTrace($"ActionManager.ClearDeviceState: reset ActionInstanceState for action={(ent?.ActionDef?.name ?? "(null)")} device={device} (IsToggledOn cleared)"); } catch { }
                            }
                        }
                        catch { }
                    }
                }

                // Phase7-Step8 W2（Q8＝A）: 旧 Mapping.HandleDeviceDisconnect の静的呼び出し。取得済みの sp から解決する（ホストが無いときは、Mapping も無いので何もしない）
                // Phase7b W1: プロファイル切替の復帰の合図は、DefaultActionManager.ClearDeviceState と同じく退避して戻す。
                try
                {
                    var mapping = sp?.GetService(typeof(DS4Windows.DI.IMapping)) as DS4Windows.DI.IMapping;
                    if (mapping != null)
                    {
                        var untrigger = mapping.CaptureProfileUntrigger(device);
                        try { mapping.HandleDeviceDisconnect(device); }
                        finally { mapping.RestoreProfileUntrigger(device, untrigger); }
                    }
                }
                catch { }
            }
            catch { }
        }

        // Preallocate runtime instances at startup
        public static void PreallocateOnStartup()
        {
            try
            {
                int count = ActionRegistry.Count;

                try
                {
                    foreach (var sa in ActionRegistry.AllActions())
                    {
                        try { GetStateFor(sa, 0); } catch { }
                    }
                }
                catch { }

                try { AppLogger.LogTrace($"ActionManager.PreallocateOnStartup: preallocated {count} actions"); } catch { }
            }
            catch { }
        }

        public static event Action<SpecialAction, int, bool, bool> ToggledOnChanged;

        static ActionManager()
        {
            try
            {
                ToggledOnChanged += (sa, dev, oldv, newv) =>
                {
                    try { AppLogger.LogTrace($"ActionManager.ToggledOnChanged: name={sa?.name} device={dev} old={oldv} new={newv}"); } catch { }
                };
            }
            catch { }
        }

        public static void FireToggledOnChanged(SpecialAction action, int device, bool oldValue, bool newValue)
        {
            try
            {
                try { ToggledOnChanged?.Invoke(action, device, oldValue, newValue); } catch { }
            }
            catch { }
        }

        public static void SetToggledOn(SpecialAction action, int device, bool value)
        {
            var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                if (mgr != null)
                {
                    mgr.SetToggledOn(action, device, value);
                    return;
                }
            }

            var msg = $"ActionManager.SetToggledOn called but no IManagedActionManager is registered. action={(action?.name ?? "(null)")} device={device} value={value}";
            try { AppLogger.LogError(msg); } catch { }
            throw new InvalidOperationException(msg);
        }

        public static DS4Windows.Actions.IActionController GetOrCreateControllerForAction(int device, SpecialAction action)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                }

                // Phase7-Step8 W2（Q8＝A）: 旧 Mapping.GetOrCreateKeyButtonControllerForAction。取得済みの sp から登録簿を解決する
                return (sp?.GetService(typeof(DS4Windows.DI.IKeyButtonControllerRegistry)) as DS4Windows.DI.IKeyButtonControllerRegistry)?.GetOrCreateForAction(device, action);
            }
            catch { return null; }
        }

        public static void PreallocateForProfileApply(int device)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                var provider = sp?.GetService(typeof(DS4Windows.DI.IProfileActionProvider)) as DS4Windows.DI.IProfileActionProvider;
                if (provider == null) return;
                // Phase7-Step8 W2（Q8＝A）: 旧 Mapping.GetOrCreateKeyButtonControllerForAction の静的呼び出し
                var controllerRegistry = sp.GetService(typeof(DS4Windows.DI.IKeyButtonControllerRegistry)) as DS4Windows.DI.IKeyButtonControllerRegistry;
                var profileActions = provider.GetProfileActionNames(device);
                if (profileActions == null) return;

                foreach (var actionName in profileActions)
                {
                    try
                    {
                        var sa = provider.GetProfileAction(device, actionName);
                        if (sa == null) continue;

                        try { GetStateFor(sa, device); } catch { }
                        try { controllerRegistry?.GetOrCreateForAction(device, sa); } catch { }
                    }
                    catch { }
                }

                try { AppLogger.LogTrace($"ActionManager.PreallocateForProfileApply: preallocated actions/controllers for device {device}"); } catch { }
            }
            catch { }
        }
    }
}