using System;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// 機種のレポートに入っているタイムスタンプ（巻き戻る整数）から、フレーム間の経過時間とマイクロ秒の累計を求める。
    /// 差分は <c>差 × multiplier / 3</c> マイクロ秒（DS4 は 16/3、DualSense は 1/3）。タイムスタンプが同じ（差が 0）のときは、システム時計の経過時間で代わりにする。
    /// 毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class TimestampClock
    {
        private readonly uint wrapMax;
        private readonly uint multiplier;
        private bool initialized;
        private uint previous;

        /// <param name="wrapMax">タイムスタンプが巻き戻る前の最大値（16 ビットなら <see cref="ushort.MaxValue"/>、32 ビットなら <see cref="uint.MaxValue"/>）。</param>
        /// <param name="multiplier">差に掛ける数（3 で割ってマイクロ秒にする）。</param>
        public TimestampClock(uint wrapMax, uint multiplier)
        {
            this.wrapMax = wrapMax;
            this.multiplier = multiplier;
        }

        /// <summary>
        /// 新しいタイムスタンプを取り込む。<paramref name="elapsedSeconds"/> にフレーム間の経過秒、
        /// <paramref name="totalMicroSec"/> に累計のマイクロ秒を返す。
        /// </summary>
        public void Advance(uint stamp, double systemElapsedSeconds, ulong previousTotalMicroSec, out double elapsedSeconds, out ulong totalMicroSec)
        {
            uint delta;
            unchecked
            {
                if (!initialized)
                {
                    initialized = true;
                    delta = stamp * multiplier / 3u;
                }
                else if (previous > stamp)
                {
                    delta = (wrapMax - previous + stamp + 1u) * multiplier / 3u;
                }
                else
                {
                    delta = (stamp - previous) * multiplier / 3u;
                }

                if (delta != 0)
                {
                    elapsedSeconds = 0.000001 * delta;
                    totalMicroSec = previousTotalMicroSec + delta;
                }
                else
                {
                    elapsedSeconds = systemElapsedSeconds;
                    totalMicroSec = previousTotalMicroSec + (uint)(elapsedSeconds * 1000000);
                }
            }

            previous = stamp;
        }
    }
}
