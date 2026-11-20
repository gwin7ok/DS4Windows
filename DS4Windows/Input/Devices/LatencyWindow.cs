using System.Collections.Generic;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// 直近 20 フレームの間隔（ミリ秒）の平均を保つ窓。読み取りのループが毎フレーム <see cref="Add"/> して、遅延（<c>Latency</c>）を更新する。
    /// 毎フレームの経路のため、容量を最大＋1 で確保して再割り当てを避ける。
    /// </summary>
    public sealed class LatencyWindow
    {
        public const int Capacity = 20;

        private readonly Queue<long> queue = new Queue<long>(Capacity + 1);
        private long sum;

        /// <summary>間隔を足し、窓の平均を返す。窓が満杯のときは、いちばん古い値を捨てる。</summary>
        public double Add(long elapsedMs)
        {
            if (queue.Count >= Capacity)
            {
                sum -= queue.Dequeue();
            }

            sum += elapsedMs;
            queue.Enqueue(elapsedMs);
            return sum / (double)queue.Count;
        }
    }
}
