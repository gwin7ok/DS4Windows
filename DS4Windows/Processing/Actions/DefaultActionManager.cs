using System;
using System.Collections.Generic;
using System.Diagnostics;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Actions
{
    // Instance-based ActionManager implementation for DI registration.
    public class DefaultActionManager : IManagedActionManager, IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        private readonly Dictionary<string, ActionEntry> actions = new Dictionary<string, ActionEntry>(StringComparer.OrdinalIgnoreCase);
        // Button state table: device x controlIndex
        private bool[,]? buttonStates = null;
        private const int ToggleReleaseHoldMsLocal = 200;
        // 切断時の後始末。ホスト構築時（PreallocateEntries）に Mapping とその依存を
        // 作らないよう、遅延（Func）で受ける。
        // 遅延取得のまま残す（意図的）。Mapping の依存グラフの内側（Mapping → … → ActionManager）にあり、直接受けると循環する。
        // ホスト構築時（AppHost.PreallocateActionEntries）に Mapping を作らない理由も兼ねる。再検討条件: Mapping が IManagedActionManager に依存しなくなったとき。許可リスト: 対応するテスト
        private readonly Func<DS4Windows.Processing.Abstractions.IMapping> _mappingProvider;

        public event Action<SpecialAction, int, bool, bool> ToggledOnChanged;

        public DefaultActionManager(Func<DS4Windows.Processing.Abstractions.IMapping> mappingProvider)
        {
            ArgumentNullException.ThrowIfNull(mappingProvider);
            _mappingProvider = mappingProvider;

            // 旧 静的 ActionManager の静的コンストラクターが購読していたログ（文言は同じ）。静的イベントの廃止に伴い、本クラスが持つ
            ToggledOnChanged += (sa, dev, oldv, newv) =>
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ActionManager.ToggledOnChanged: name={sa?.name} device={dev} old={oldv} new={newv}");
            };

            // 旧 Mapping のコンストラクターが購読していたトレースログ（文言は同じ。動作の確認用）
            ToggledOnChanged += (sa, dev, oldv, newv) =>
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Mapping: ToggledOnChanged name={sa?.name} device={dev} old={oldv} new={newv}");
            };
        }

        private static ActionEntry? GetOrCreateEntryInternal(Dictionary<string, ActionEntry> actionsDict, SpecialAction action)
        {
            if (action == null) return null;
            lock (actionsDict)
            {
                if (!actionsDict.TryGetValue(action.name, out ActionEntry? ent) || ent == null)
                {
                    ent = new ActionEntry(action);
                    actionsDict[action.name] = ent;
                }
                return ent;
            }
        }

        // Preallocate ActionEntry objects for all registered SpecialActions and initialize button state table.
        public void PreallocateEntries()
        {
            try
            {
                // ActionRegistry は本番では一度も Initialize されず（作られた 2025-12-18 から呼び出しなし）、ループは常に空だった。ActionEntry は GetStateFor が必要な時に作る

                // Initialize button state table for all devices × supported control indices
                try
                {
                    int devices = ControllerLimits.MaxControllerCount;
                    int controls = (int)DS4ControlSettings.LAST_DS4_ACTION + 1;
                    buttonStates = new bool[devices, controls];
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"DefaultActionManager.PreallocateEntries: allocated buttonStates {devices}x{controls} and {actions.Count} action entries");
                }
                catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        public bool IsBeingTriggered(SpecialAction action, int device)
        {
            try
            {
                var st = GetStateFor(action, device);
                if (st != null) return st.BeingTriggered;
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            return false;
        }

        public ActionInstanceState? GetStateFor(SpecialAction action, int device)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
                if (ent == null) return null;
                if (device < 0 || device >= ent.States.Length) return null;
                return ent.States[device];
            }
            catch { return null; }
        }

        public void ClearToggledOnForKey(ushort key)
        {
            try
            {
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
                        catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    }
                }
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        public void ClearAllToggledOn()
        {
            try
            {
                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            for (int d = 0; d < ent.States.Length; d++) SetToggledOn(ent.ActionDef, d, false);
                        }
                        catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    }
                }
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        public void ClearAllEntries()
        {
            try
            {
                lock (actions)
                {
                    actions.Clear();
                }
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }

        public void ClearDeviceState(int device)
        {
            try
            {
                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            if (device >= 0 && device < ent.States.Length)
                            {
                                try
                                {
                                    bool old = ent.States[device]?.IsToggledOn ?? false;
                                    ent.States[device] = new ActionInstanceState();
                                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"DefaultActionManager.ClearDeviceState: reset ActionInstanceState for action={ent.ActionDef.name} device={device} (IsToggledOn cleared)");
                                    if (old != false)
                                    {
                                        try { ToggledOnChanged?.Invoke(ent.ActionDef, device, old, false); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                                    }
                                }
                                catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                            }
                        }
                        catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    }
                }

                //
                // Mapping.HandleDeviceDisconnect を呼ぶ（保持中の合成キーの解放、カスタムマッピングキューとマクロキューの
                // クリア、KeyButtonActionController の破棄［ClearKeyButtonControllersForDevice を内包］）。
                // 2026-09-04（31dddc43）から本クラスは未登録で静的実装が使われ、実機確認はその挙動で行われていたため、
                // 再登録で挙動が変わらないよう揃えた（以前の ClearKeyButtonControllersForDevice 単体では、
                // 押しっぱなしの合成キーが解放されない）。
                // HandleDeviceDisconnect は deviceRuntime を作り直し、プロファイル切替のスペシャルアクション
                // （「Unload on trigger release」など）の復帰の合図（UntriggerAction）も消す。ClearDeviceState は
                // プロファイル適用でも呼ばれ、切断ではないため、合図を退避して戻す（実際の切断は ControlService が
                // Mapping.HandleDeviceDisconnect を直接呼ぶので、合図は消える）。
                try
                {
                    var mapping = _mappingProvider();
                    var untrigger = mapping.CaptureProfileUntrigger(device);
                    try { mapping.HandleDeviceDisconnect(device); }
                    finally { mapping.RestoreProfileUntrigger(device, untrigger); }
                }
                catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        public void SetToggledOn(SpecialAction action, int device, bool value)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
                if (ent == null) return;
                if (device < 0 || device >= ent.States.Length) return;
                var st = ent.States[device];
                if (st == null) return;
                bool old = st.IsToggledOn;
                if (old == value) return;
                st.IsToggledOn = value;
                try { ToggledOnChanged?.Invoke(action, device, old, value); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }

                // スタックトレースの取得は高価で、切り替えのたびに起きるため、Trace が有効のときだけ行う
                if (AppLogger.IsTraceEnabled)
                try
                {
                    var trace = new StackTrace(1, false);
                    string callerInfo = "(unknown)";
                    try
                    {
                        for (int i = 0; i < trace.FrameCount; i++)
                        {
                            var fr = trace.GetFrame(i);
                            var method = fr?.GetMethod();
                            if (method == null) continue;
                            var declaring = method.DeclaringType;
                            if (declaring == null) continue;
                            if (declaring == typeof(DefaultActionManager)) continue;
                            callerInfo = declaring.FullName + "." + method.Name;
                            break;
                        }
                    }
                    catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    string stackSnippet = trace.ToString();
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"DefaultActionManager.SetToggledOn: action={(action?.name ?? "(null)")} device={device} old={old} new={value} caller={callerInfo} stack={stackSnippet}");
                }
                catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        // Button state accessors: device in [0, MAX_DS4_CONTROLLER_COUNT), controlIndex as DS4Controls enum value
        public bool GetButtonState(int device, int controlIndex)
        {
            try
            {
                if (buttonStates == null) return false;
                if (device < 0 || device >= buttonStates.GetLength(0)) return false;
                if (controlIndex < 0 || controlIndex >= buttonStates.GetLength(1)) return false;
                return buttonStates[device, controlIndex];
            }
            catch { return false; }
        }

        public void SetButtonState(int device, int controlIndex, bool value)
        {
            try
            {
                if (buttonStates == null) return;
                if (device < 0 || device >= buttonStates.GetLength(0)) return;
                if (controlIndex < 0 || controlIndex >= buttonStates.GetLength(1)) return;
                buttonStates[device, controlIndex] = value;
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

    }
}
