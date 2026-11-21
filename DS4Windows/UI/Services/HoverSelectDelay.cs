#nullable enable
using System;
using System.Threading;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// マウスオーバーで対象を選ぶ処理に、遅延を入れる（通過しただけの意図しないマウスオーバーで、選択が切り替わらないようにする）。
    /// 開始から遅延が過ぎるまでにキャンセルされなければ、保留していた対象を選ぶ。選ぶ処理は、<c>marshal</c> を通して UI のスレッドへ戻す
    /// （タイマーは UI を触らないので、<see cref="TimeProvider"/> のタイマーを使い、テストでは時計を差し替える）。
    /// </summary>
    public sealed class HoverSelectDelay<T> where T : class
    {
        private readonly TimeProvider _time;
        private readonly TimeSpan _delay;
        private readonly Action<T> _select;
        private readonly Action<Action> _marshal;
        private readonly object _lock = new object();
        private ITimer? _timer;
        private T? _pending;
        private long _generation;

        public HoverSelectDelay(TimeProvider time, int delayMs, Action<T> select, Action<Action> marshal)
        {
            ArgumentNullException.ThrowIfNull(time);
            ArgumentNullException.ThrowIfNull(select);
            ArgumentNullException.ThrowIfNull(marshal);
            _time = time;
            _delay = TimeSpan.FromMilliseconds(delayMs);
            _select = select;
            _marshal = marshal;
        }

        /// <summary>保留中の対象があるか。</summary>
        public bool HasPending { get { lock (_lock) { return _pending != null; } } }

        /// <summary>遅延を始める。すでに保留があれば、そちらは捨てて、この対象に差し替える（遅延は、最初から数え直す）。</summary>
        public void Start(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            long generation;
            lock (_lock)
            {
                _timer?.Dispose();
                _pending = item;
                generation = ++_generation;
                _timer = _time.CreateTimer(_ => OnElapsed(generation), null, _delay, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>保留をやめる（マウスが離れたとき）。</summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _pending = null;
                _generation++;
            }
        }

        /// <summary>遅延を待たずに選ぶ。保留はやめる（遅延の前にクリックされたとき、クリックした対象を先に確定する）。</summary>
        public void SelectNow(T item)
        {
            Cancel();
            _select(item);
        }

        private void OnElapsed(long generation)
        {
            T? item;
            lock (_lock)
            {
                // キャンセル・差し替えのあとに届いた通知は、無視する
                if (generation != _generation) return;
                item = _pending;
                _pending = null;
            }

            if (item != null) _marshal(() => _select(item));
        }
    }
}
