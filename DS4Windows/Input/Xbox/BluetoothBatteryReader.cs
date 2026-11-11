using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using DS4Windows.Core.Logging;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// <see cref="IBluetoothBatteryReader"/> の実装。機器ごとに、最後に読めた値を持ち、一定の間隔（既定 30 秒）をすぎていれば、裏で読み直す。
    /// 呼び出し（入力のループから来る）を待たせない。読めなかった機器は、次の間隔まで再試行しない（失敗のたびに無線を叩かないため）。
    /// </summary>
    public sealed class BluetoothBatteryReader : IBluetoothBatteryReader
    {
        private sealed class Entry
        {
            public int? Percent;
            public long NextRefreshTicks;
            public int Refreshing;
        }

        private readonly IGattBatteryFetcher fetcher;
        private readonly long intervalTicks;
        private readonly ConcurrentDictionary<ulong, Entry> entries = new ConcurrentDictionary<ulong, Entry>();

        public BluetoothBatteryReader(IGattBatteryFetcher fetcher)
            : this(fetcher, TimeSpan.FromSeconds(30))
        {
        }

        public BluetoothBatteryReader(IGattBatteryFetcher fetcher, TimeSpan interval)
        {
            this.fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            intervalTicks = (long)(interval.TotalSeconds * Stopwatch.Frequency);
        }

        public bool TryGetPercent(ulong bluetoothAddress, out int percent)
        {
            Entry entry = entries.GetOrAdd(bluetoothAddress, _ => new Entry());
            if (Stopwatch.GetTimestamp() >= entry.NextRefreshTicks && System.Threading.Interlocked.CompareExchange(ref entry.Refreshing, 1, 0) == 0)
            {
                _ = RefreshAsync(bluetoothAddress, entry);
            }

            int? value = entry.Percent;
            percent = value ?? 0;
            return value.HasValue;
        }

        private async Task RefreshAsync(ulong address, Entry entry)
        {
            try
            {
                int? fetched = await fetcher.FetchAsync(address).ConfigureAwait(false);
                if (fetched.HasValue)
                {
                    entry.Percent = fetched;   // 一時的に読めなかったときは、前回の値を保つ
                }

                AppLogger.LogDebug($"[BluetoothBattery] {address:X12}: {(fetched.HasValue ? fetched.Value + "%" : "not readable")}");
            }
            catch (Exception ex)
            {
                AppLogger.LogDebug($"[BluetoothBattery] {address:X12}: failed: {ex.Message}");
            }
            finally
            {
                entry.NextRefreshTicks = Stopwatch.GetTimestamp() + intervalTicks;
                System.Threading.Volatile.Write(ref entry.Refreshing, 0);
            }
        }
    }
}
