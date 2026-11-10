using System;
using System.Collections.Generic;
using System.Threading;

namespace DS4WindowsTests
{
    /// <summary>
    /// 時間を手で進めるテスト用の <see cref="TimeProvider"/>。<see cref="CreateTimer"/> が返すタイマーは、
    /// <see cref="Advance"/> で時間を進めたときだけコールバックを呼ぶ（実時間を待たない）。
    /// </summary>
    public sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = new List<ManualTimer>();
        private long _nowTicks;

        public int ActiveTimerCount
        {
            get { lock (_timers) { return _timers.FindAll(t => t.IsActive).Count; } }
        }

        // 経過時間の計測（GetTimestamp／GetElapsedTime）も仮想時間に従わせる。単位は 100ns（TimeSpan の Tick）
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _nowTicks;

        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(_nowTicks);

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            lock (_timers) { _timers.Add(timer); }
            return timer;
        }

        /// <summary>時間を進め、期限が来たタイマーのコールバックを（周期ぶん繰り返して）呼ぶ。</summary>
        public void Advance(TimeSpan span)
        {
            long target = _nowTicks + span.Ticks;
            while (true)
            {
                ManualTimer next = null;
                lock (_timers)
                {
                    foreach (var t in _timers)
                    {
                        if (t.IsActive && t.DueTicks <= target && (next == null || t.DueTicks < next.DueTicks)) next = t;
                    }
                }
                if (next == null) break;
                _nowTicks = next.DueTicks;
                next.Fire();
            }
            _nowTicks = target;
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _owner;
            private readonly TimerCallback _callback;
            private readonly object _state;
            private long _periodTicks;
            public long DueTicks { get; private set; }
            public bool IsActive { get; private set; }

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object state)
            {
                _owner = owner; _callback = callback; _state = state;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime == Timeout.InfiniteTimeSpan) { IsActive = false; return true; }
                IsActive = true;
                DueTicks = _owner._nowTicks + dueTime.Ticks;
                _periodTicks = period == Timeout.InfiniteTimeSpan ? 0 : period.Ticks;
                return true;
            }

            public void Fire()
            {
                if (_periodTicks > 0) DueTicks += _periodTicks; else IsActive = false;
                _callback(_state);
            }

            public void Dispose() { IsActive = false; }
            public System.Threading.Tasks.ValueTask DisposeAsync() { IsActive = false; return default; }
        }
    }
}
