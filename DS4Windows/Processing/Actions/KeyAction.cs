using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // KeyAction implementation that delegates to Mapping's KeyButtonActionController cache.
    public class KeyAction
    {
        private readonly SpecialAction action;
        private readonly int index;

        // Detailed constructor below initializes parsed key/native values.

        private bool IsToggle()
        {
            try
            {
                if (action == null) return false;
                if (action.KeyButtonSwitchMode.HasValue)
                    return action.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                return action.keyType.HasFlag(DS4KeyType.Toggle);
            }
            catch { return false; }
        }

        private readonly ushort keyId;
        private readonly uint nativeKey;
        private readonly bool useScan;
        // No per-action repeater here; RepeatHelper should be used only by controllers.

        // 旧 静的 ActionManager（ServiceProviderHolder 経由の解決）の代わりに、状態の管理役とコントローラーの登録簿をコンストラクターで受ける
        private readonly IManagedActionManager manager;
        private readonly IKeyButtonControllerRegistry controllers;

        public KeyAction(SpecialAction action, int index, IManagedActionManager manager, IKeyButtonControllerRegistry controllers)
            : this(action, index, ParseKeyId(action), action != null && action.keyType.HasFlag(DS4KeyType.ScanCode), manager, controllers)
        {
        }

        /// <summary>
        /// 解釈済みの値（論理キー、ScanCode）を受け取る。<paramref name="action"/> は、状態の鍵と系統 B（コントローラー）への入力として使う。
        /// </summary>
        public KeyAction(SpecialAction action, int index, ushort keyId, bool useScan, IManagedActionManager manager, IKeyButtonControllerRegistry controllers)
        {
            ArgumentNullException.ThrowIfNull(manager);
            this.manager = manager;
            ArgumentNullException.ThrowIfNull(controllers);
            this.controllers = controllers;
            this.action = action;
            this.index = index;
            this.keyId = keyId;
            try { nativeKey = this.controllers.Dispatcher.ResolveNativeKey(keyId); } catch { nativeKey = 0; }   // 旧 静的 SyntheticDispatcher
            this.useScan = useScan;
            // Do not parse repeater options here; controllers handle repeat behavior.
        }

        private static ushort ParseKeyId(SpecialAction action)
        {
            ushort k = 0;
            if (action != null && !string.IsNullOrEmpty(action.details))
                ushort.TryParse(action.details, out k);
            return k;
        }

        // 旧 ActionManager.GetOrCreateControllerForAction と同じ（登録簿から取得。例外のときは null）
        private IActionController GetController(int device)
        {
            try { return controllers.GetOrCreateForAction(device, action); }
            catch { return null; }
        }

        public void OnTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode, IVirtualKBM handler)
        {
            try
            {
                var state = manager.GetStateFor(action, device);
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyAction.OnTrigger ENTER: name={action?.name} device={device} logical={logicalValue} native={nativeValue} useScan={useScan} isToggledOn={(state?.IsToggledOn ?? false)}");
                var ctrl = GetController(device);
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyAction.OnTrigger: obtained controller={(ctrl==null?"<null>":ctrl.ControllerId.ToString())} for name={action?.name} device={device}");

                var trigger = new DS4Windows.Processing.Actions.TriggerContextImpl
                {
                    Device = device,
                    IsEdgeEstablished = true,
                    LogicalValue = logicalValue,
                    NativeValue = nativeValue == 0 ? nativeKey : nativeValue,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new DS4Windows.Processing.Actions.KeyActionBinding(action);

                // Forward the trigger (established) to the controller and let it decide semantics (press vs toggle)
                try { ctrl?.Handle(binding, trigger); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyAction: trigger(established) forwarded to controller name={action?.name} device={device} key={logicalValue}");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"KeyAction.OnTrigger failed: {ex}");
            }
        }

        public void OnRelease(int device, ushort logicalValue, uint nativeValue, bool useScanCode, IVirtualKBM handler)
        {
            try
            {
                var state = manager.GetStateFor(action, device);
                var ctrl = GetController(device);
                var trigger = new DS4Windows.Processing.Actions.TriggerContextImpl
                {
                    Device = device,
                    IsEdgeEstablished = false,
                    LogicalValue = logicalValue,
                    NativeValue = nativeValue == 0 ? nativeKey : nativeValue,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };
                var binding = new DS4Windows.Processing.Actions.KeyActionBinding(action);
                // Forward release edge to controller and let controller instance decide how to handle it.
                try { ctrl?.Handle(binding, trigger); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"KeyAction.OnRelease failed: {ex}");
            }
        }
    }
}
