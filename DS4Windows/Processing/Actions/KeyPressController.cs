#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // Press implementation: per-instance management of press state and optional repeat (minimal)
    internal sealed class KeyPressController : IKeyController
    {
        private readonly int device;
        private readonly int controllerId;
        private class Entry
        {
            public bool isPressed;
            public uint nativeKey;
            public bool useScanCode;
            public IVirtualKBM? handler;
            public IRepeater? repeater;
            public CancellationTokenSource? delayCts;
        }
        private readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();

        private readonly ISyntheticDispatcher _dispatcher;   // Phase14c-Step3（R-06）
        private readonly TimeProvider _time;
        private readonly Func<Func<RepeatHelper>, IRepeater>? _repeaterFactory;
        public KeyPressController(int device, int controllerId, ISyntheticDispatcher dispatcher, TimeProvider time, Func<Func<RepeatHelper>, IRepeater>? repeaterFactory = null) { _repeaterFactory = repeaterFactory; _time = time; this.device = device; this.controllerId = controllerId; _dispatcher = dispatcher; }

        public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler, bool isSpecialAction)
        {
            if (!entries.TryGetValue(kvpKey, out Entry? e))
            {
                e = new Entry() { isPressed = false, nativeKey = nativeKey, useScanCode = useScanCode, handler = handler };
                entries[kvpKey] = e;
            }
            if (!e.isPressed)
            {
                if (e.nativeKey == 0) e.nativeKey = _dispatcher.ResolveNativeKey(kvpKey);
                e.isPressed = true;
                try
                {
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"PressImpl.OnDown: controllerId={controllerId} device={device} kvpKey={kvpKey} native={e.nativeKey} useScan={e.useScanCode}");
                    // Primary behavior: send one immediate press (hold). Release will be sent on OnUp.
                    _dispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScanCode, e.handler);
                }
                catch (Exception ex) { AppLogger.LogWarn($"PressImpl.OnDown failed: {ex}"); }

                // Start delayed creation of repeater after InitialRepeatDelayMs for press-repeat semantics
                try
                {
                    e.delayCts = new CancellationTokenSource();
                    var localEntry = e;
                    Task.Run(async () =>
                    {
                            try
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(DS4Windows.Core.Platform.KeyboardSettings.InitialRepeatDelayMs), _time, localEntry.delayCts.Token).ConfigureAwait(false);
                                if (localEntry.delayCts.IsCancellationRequested) return;
                                // create or start repeater that begins immediate repeating (send first immediate press)
                                // If handler provides fakeKeyRepeat (e.g., SendInput), Mapping will perform repeats.
                                if (localEntry.handler == null || !localEntry.handler.fakeKeyRepeat)
                                {
                                    if (localEntry.repeater == null)
                                    {
                                        localEntry.repeater = new RepeatHelperToIRepeaterAdapter(() => new DS4Windows.Processing.RepeatHelper(_dispatcher, device, kvpKey, localEntry.nativeKey, localEntry.useScanCode, localEntry.handler!, _time, DS4Windows.Core.Platform.KeyboardSettings.RepeatIntervalMs, true, controllerId), _repeaterFactory);   // 同上
                                        try { localEntry.repeater.Start(); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                                    }
                                    else
                                    {
                                        try { localEntry.repeater.Start(); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                                    }
                                }
                            }
                        catch (OperationCanceledException) { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                        catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    });
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }

        }

        public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler)
        {
            if (entries.TryGetValue(kvpKey, out Entry? e) && e.isPressed)
            {
                try
                {
                    // cancel pending delayed repeater creation
                    try { e.delayCts?.Cancel(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                    // stop repeater if running; Stop() sends single release. Keep instance for reuse.
                    if (e.repeater != null)
                    {
                        try { e.repeater.Stop(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                    }
                    // Ensure release is sent now for the held press
                    try { _dispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                    e.isPressed = false;
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
            else
            {
                try { _dispatcher.ResetKeyTiming(device, kvpKey); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
        }

        public void OnToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler)
        {
            // For press-mode controller, treat toggle-off as a clear of any press state.
            try { Clear(kvpKey); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }

        public void Clear(ushort kvpKey)
        {
            if (entries.TryGetValue(kvpKey, out Entry? e))
            {
                try { e.delayCts?.Cancel(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                try { e.repeater?.Stop(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                try { if (e.isPressed) _dispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                e.isPressed = false;
                // keep entry for reuse; do not dispose here
            }
            try { _dispatcher.ResetKeyTiming(0, kvpKey); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }
            public void ClearAll()
            {
                try
                {
                    var keys = new List<ushort>(entries.Keys);
                    foreach (var k in keys)
                    {
                        try
                        {
                            var e = entries[k];
                                    try { e.delayCts?.Cancel(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                                    try { if (e.isPressed) _dispatcher.SendRelease(device, k, e.nativeKey, e.useScanCode, e.handler); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                                    try { e.repeater?.Dispose(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                            entries.Remove(k);
                            try { _dispatcher.ResetKeyTiming(0, k); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                        }
                        catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                    }
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
    }
}
