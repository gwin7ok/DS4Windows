using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // Lightweight per-device wrapper that delegates to existing static controllers
    // Mode is fixed at construction time (Press or Toggle).
    public partial class KeyButtonActionController : IDisposable, IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        public enum Mode { Press, Toggle }

        private readonly int device;
        private readonly Mode mode;
        private readonly string assignedActionName;
        private readonly SpecialAction? assignedActionDef;
        private readonly IKeyController impl;
        private readonly Action<SpecialAction, int, bool, bool> toggledOnHandler;
        // 旧 静的 ActionManager の代わりに、トグル状態の管理役をコンストラクターで受ける
        private readonly IManagedActionManager _manager;
        // 合成キーの送出。生成する側（登録簿・ファクトリー）が渡す
        private readonly ISyntheticDispatcher _dispatcher;
        // 長押しのリピートの開始待ち・間隔の時計。テストが仮想の時計を渡す
        private readonly TimeProvider _time;
        private readonly Func<Func<RepeatHelper>, IRepeater>? _repeaterFactory;   // 繰り返しの部品の差し替え（null なら RepeatHelper。主にテスト）

        // Expose assigned action name for diagnostics
        public string AssignedActionName => assignedActionName;

        // Expose whether this controller was created in Toggle mode (used by adapters)
        public bool IsToggleMode { get { return this.mode == Mode.Toggle; } }

        public KeyButtonActionController(int device, Mode mode, IManagedActionManager manager, ISyntheticDispatcher dispatcher, TimeProvider timeProvider, string? actionName = "<unknown>",
            Func<Func<RepeatHelper>, IRepeater>? repeaterFactory = null)
        {
            _repeaterFactory = repeaterFactory;
            ArgumentNullException.ThrowIfNull(timeProvider);
            _time = timeProvider;
            ArgumentNullException.ThrowIfNull(dispatcher);
            _dispatcher = dispatcher;
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            this.device = device;
            this.mode = mode;
            this.assignedActionName = actionName ?? "<null>";
            this.assignedActionDef = null;
            try
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyButtonActionController created: id={this.GetHashCode()} device={device} mode={mode} assignedAction={this.assignedActionName}");
            }
            catch { /* 診断の出力の失敗は無視してよい */ }

            // Create concrete implementation based on mode. These implementations are lightweight wrappers
            // around the existing static controllers for now; this allows per-device instance semantics later.
            if (mode == Mode.Toggle)
                impl = new KeyToggleController(device, this.GetHashCode(), this.assignedActionDef!, _manager, _dispatcher, _time, _repeaterFactory);   // Toggle の生成は、SpecialAction を渡された場合だけ
            else
                impl = new KeyPressController(device, this.GetHashCode(), _dispatcher, _time, _repeaterFactory);

            // Subscribe to toggled-on changes so controllers react to Action-level state changes.
            toggledOnHandler = (sa, dev, oldv, newv) =>
            {
                try
                {
                    if (sa == this.assignedActionDef && dev == this.device && oldv == true && newv == false)
                    {
                        try { impl.ClearAll(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                    }
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            };
            try { _manager.ToggledOnChanged += toggledOnHandler; } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }

        }

        // New constructor: determine mode from SpecialAction info (KeyButtonSwitchMode or keyType flags)
        public KeyButtonActionController(int device, SpecialAction sa, IManagedActionManager manager, ISyntheticDispatcher dispatcher, TimeProvider timeProvider, string? actionName = "<unknown>",
            Func<Func<RepeatHelper>, IRepeater>? repeaterFactory = null)
        {
            _repeaterFactory = repeaterFactory;
            ArgumentNullException.ThrowIfNull(timeProvider);
            _time = timeProvider;
            ArgumentNullException.ThrowIfNull(dispatcher);
            _dispatcher = dispatcher;
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            this.device = device;
            this.assignedActionName = actionName ?? (sa?.name ?? "<null>");
            this.assignedActionDef = sa;
            try
            {
                // Decide mode: explicit switch mode first, fall back to keyType Toggle flag
                if (sa != null && sa.KeyButtonSwitchMode.HasValue)
                    this.mode = sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle ? Mode.Toggle : Mode.Press;
                else
                {
                    bool isToggle = false;
                    try { isToggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggle); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    this.mode = isToggle ? Mode.Toggle : Mode.Press;
                }
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyButtonActionController created: id={this.GetHashCode()} device={device} mode={this.mode} assignedAction={this.assignedActionName}");
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }

            if (this.mode == Mode.Toggle)
                impl = new KeyToggleController(device, this.GetHashCode(), this.assignedActionDef!, _manager, _dispatcher, _time, _repeaterFactory);   // Toggle の生成は、SpecialAction を渡された場合だけ
            else
                impl = new KeyPressController(device, this.GetHashCode(), _dispatcher, _time, _repeaterFactory);

            // Subscribe to toggled-on changes so controllers react to Action-level state changes.
            toggledOnHandler = (sa, dev, oldv, newv) =>
            {
                try
                {
                    if (sa == this.assignedActionDef && dev == this.device && oldv == true && newv == false)
                    {
                        try { impl.ClearAll(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                    }
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            };
            try { _manager.ToggledOnChanged += toggledOnHandler; } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }

        }

        // New, clearer API names reflecting Mapping-trigger notifications
        public void OnSATriggerEstablished(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler, bool isSpecialAction)
        {
            try
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerEstablished: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey} isSpecialAction={isSpecialAction}");
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            impl.OnDown(kvpKey, nativeKey, useScanCode, handler, isSpecialAction);
        }

        public void OnSATriggerReleased(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler)
        {
            try
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerReleased: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey}");
            }
            catch { /* 診断の出力の失敗は無視してよい */ }
            // For Toggle-mode, ignore input-level release here; actual toggle-off should be handled explicitly.
            impl.OnUp(kvpKey, nativeKey, useScanCode, handler);
        }

        // Explicit API to indicate Toggle OFF (called by KeyAction when it decides to toggle off)
        public void OnSATriggerToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler)
        {
            try
            {
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerToggleOff: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey}");
            }
            catch { /* 診断の出力の失敗は無視してよい */ }
            impl.OnToggleOff(kvpKey, nativeKey, useScanCode, handler);
        }

        // (Removed backward-compatible wrappers to avoid accidental use of old API names.)

        public void ClearKeyEntries(ushort kvpKey)
        {
            impl.Clear(kvpKey);
        }

        private bool disposed = false;

        public virtual void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                impl.ClearAll();
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            // 静的イベントでは購読が残り続けていた。破棄後は反応しないよう、注入された管理役の購読を解除する
            try { _manager.ToggledOnChanged -= toggledOnHandler; } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyButtonActionController destroyed: device={device} assignedAction={this.assignedActionName}");
        }

        public void Destroy()
        {
            try { Dispose(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }
    }
}
