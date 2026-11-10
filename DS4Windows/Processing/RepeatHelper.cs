using System;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing
{
    // RepeatHelper: creates an instance that immediately starts issuing KeyPress
    // events at a fixed interval. Call Stop() to stop repeating and send a single
    // KeyRelease; the instance should then be discarded.
    public class RepeatHelper : IDisposable, IInstanceIdentifiable
    {
        private readonly ISyntheticDispatcher _dispatcher;
        // Public instance identifier for logging consumers
        public int InstanceId => this.GetHashCode();

        private readonly int device;
        private readonly ushort kvpKey;
        private readonly uint nativeKey;
        private readonly bool useScanCode;
        private readonly IVirtualKBM handler;
        private readonly int controllerId;
        private readonly ITimer timer;
        private int disposed;
        private volatile bool isRunning;
        // public exposure of configured interval
        public int IntervalMs { get; }
        public bool SendFirstImmediate { get; }

        // intervalMillis: repeat period in milliseconds (50ms typical)
        // sendFirstImmediate: if true, send one immediate KeyPress now and then start periodic sends
        //                     if false, first KeyPress will be issued after intervalMillis
        public RepeatHelper(ISyntheticDispatcher dispatcher, int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler, TimeProvider timeProvider, int intervalMillis = 50, bool sendFirstImmediate = true, int controllerId = -1)
        {
            // Phase14c-Step3（R-06）: 合成キーの送出（旧: 静的な橋渡し SyntheticDispatcher）
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.device = device;
            this.kvpKey = kvpKey;
            this.nativeKey = nativeKey;
            this.useScanCode = useScanCode;
            this.handler = handler;
            this.controllerId = controllerId;
            this.IntervalMs = intervalMillis;
            this.SendFirstImmediate = sendFirstImmediate;

            // Create timer but do not start it until Start() is called. This allows Stop() to pause
            // repeating without disposing the instance so it can be reused.
            timer = (timeProvider ?? throw new ArgumentNullException(nameof(timeProvider))).CreateTimer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            isRunning = false;
            try { DS4Windows.Core.Logging.AppLogger.LogTrace($"RepeatHelper created: id={this.GetHashCode()} device={device} kvpKey={kvpKey} nativeKey={nativeKey} interval={IntervalMs} sendFirstImmediate={SendFirstImmediate} controllerId={(controllerId>=0?controllerId:-1)}"); } catch { }
            if (sendFirstImmediate)
            {
                Start();
            }
        }

        // Convenience ctor that reads the default repeat interval from KeyboardSettings
        public RepeatHelper(ISyntheticDispatcher dispatcher, int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler, TimeProvider timeProvider, bool sendFirstImmediate = true)
            : this(dispatcher, device, kvpKey, nativeKey, useScanCode, handler, timeProvider, DS4Windows.Core.Platform.KeyboardSettings.RepeatIntervalMs, sendFirstImmediate)
        {
        }

        private void TimerCallback(object state)
        {
            try
            {
                _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler);
            }
            catch { }
        }

        // Start repeating. If configured to send first immediate, this will issue a press immediately
        // and then begin periodic sends. Safe to call multiple times.
        public void Start()
        {
            if (Interlocked.CompareExchange(ref disposed, 0, 0) == 1) return;
            if (isRunning) return;

            try
            {
                try { DS4Windows.Core.Logging.AppLogger.LogTrace($"RepeatHelper.Start: id={this.GetHashCode()} device={device} kvpKey={kvpKey} controllerId={(controllerId>=0?controllerId:-1)}"); } catch { }
                if (SendFirstImmediate)
                {
                    try { _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                }
                timer.Change(TimeSpan.FromMilliseconds(IntervalMs), TimeSpan.FromMilliseconds(IntervalMs));
                isRunning = true;
            }
            catch { }
        }

        // Stop repeating. Safe to call multiple times. Does not dispose
        // the underlying timer so the instance can be restarted by calling Start().
        // NOTE: Release is no longer sent here; caller is responsible for sending KeyRelease.
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref disposed, 0, 0) == 1) return;
            if (!isRunning) return;

            try
            {
                try { DS4Windows.Core.Logging.AppLogger.LogTrace($"RepeatHelper.Stop: id={this.GetHashCode()} device={device} kvpKey={kvpKey} controllerId={(controllerId>=0?controllerId:-1)}"); } catch { }
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                isRunning = false;
            }
            catch { }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1) return;
            try
            {
                try { DS4Windows.Core.Logging.AppLogger.LogTrace($"RepeatHelper.Dispose: id={this.GetHashCode()} device={device} kvpKey={kvpKey} controllerId={(controllerId>=0?controllerId:-1)}"); } catch { }
                try { timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); } catch { }
                try { timer.Dispose(); } catch { }
            }
            catch { }
            // Do NOT send release here; disposal should not implicitly generate key events.
            isRunning = false;
        }
    }
}
