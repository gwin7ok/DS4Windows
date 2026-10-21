using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-A(2)-B（Phase9-A2-Plan.md）: アンチスナップバック（<c>StickInputConditioner.AntiSnapback</c>）を、旧実装（<c>Mapping.CalcAntiSnapbackStick</c> を逐語的に写したもの。
    /// 時刻だけ引数にした）と<b>突き合わせる差分テスト</b>。旧実装は時刻（<c>DateTimeOffset.Now</c>）を直接読むため、切り出しの前の挙動をゴールデンで固定できない。
    /// 固定の列と、決まった種の疑似乱数の列で、出力の値と履歴（キュー）の中身が、1 フレームごとに一致することを調べる。
    /// 旧実装の写しは、Phase9 の後のテスト整理まで残す（整理では、ゴールデンへの置き換えを検討する）。
    /// </summary>
    public class Phase9A2AntiSnapbackDifferentialTests
    {
        /// <summary>旧 <c>Mapping.CalcAntiSnapbackStick</c> の逐語的な写し（<c>timestamp</c> を引数にし、<c>out</c> のまま）。</summary>
        private static void OldCalcAntiSnapbackStick(Queue<DS4TimedStickAxisValue> queue,
            double delta, long timeout, long timestamp, byte axisXValue, byte axisYValue,
            out byte useAxisX, out byte useAxisY)
        {
            while (queue.Count > 0 && queue.Peek().timestamp < timestamp - timeout)
            {
                queue.Dequeue();
            }
            if (queue.Any(oldValues =>
            {
                double distanceSquared = Math.Pow(axisXValue - oldValues.x, 2) + Math.Pow(axisYValue - oldValues.y, 2);
                if (distanceSquared >= (delta * delta))
                {
                    //Checks if the line between two points touches a 15 unit circle in the middle
                    double t = ((128 - axisXValue) * (oldValues.x - axisXValue) + (128 - axisYValue) * (oldValues.y - axisYValue)) / distanceSquared;
                    t = Math.Max(0, Math.Min(1, t));
                    double distanceToMiddleSquared = Math.Pow(128 - (axisXValue + t * (oldValues.x - axisXValue)), 2) + Math.Pow(128 - (axisYValue + t * (oldValues.y - axisYValue)), 2);
                    return distanceToMiddleSquared <= 15 * 15;
                }
                else
                {
                    return false;
                }
            }))
            {
                useAxisX = useAxisY = 128;
            }
            else
            {
                useAxisX = axisXValue;
                useAxisY = axisYValue;
            }
            queue.Enqueue(new DS4TimedStickAxisValue(axisXValue, axisYValue, timestamp));
        }

        private static readonly IStickInputConditioner Conditioner = new StickInputConditioner();

        private static int AssertSameSequence(IReadOnlyList<(long Time, byte X, byte Y)> frames, double delta, long timeout)
        {
            var oldQueue = new Queue<DS4TimedStickAxisValue>();
            var newQueue = new Queue<DS4TimedStickAxisValue>();
            int snapped = 0;

            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                OldCalcAntiSnapbackStick(oldQueue, delta, timeout, f.Time, f.X, f.Y, out byte oldX, out byte oldY);
                byte newX = f.X, newY = f.Y;
                Conditioner.AntiSnapback(newQueue, delta, timeout, f.Time, ref newX, ref newY);

                Assert.True(oldX == newX && oldY == newY,
                    $"frame {i} (t={f.Time}, {f.X},{f.Y}; delta={delta}, timeout={timeout}): 旧 {oldX},{oldY} / 新 {newX},{newY}");
                Assert.Equal(oldQueue.Count, newQueue.Count);
                Assert.True(oldQueue.SequenceEqual(newQueue), $"frame {i}: 履歴の中身が違う");
                if (oldX == 128 && oldY == 128 && !(f.X == 128 && f.Y == 128)) snapped++;
            }

            return snapped;
        }

        [Fact]
        public void ASnapbackThroughTheCenter_IsSuppressed_LikeTheOldImplementation()
        {
            // 左いっぱい → 中立を通って右いっぱい（スナップバック）→ 同じ向きにゆっくり戻る
            var frames = new List<(long, byte, byte)>
            {
                (1000, 128, 128), (1004, 20, 128), (1008, 22, 128), (1012, 128, 128), (1016, 230, 128), (1020, 232, 130),
                (1100, 230, 128), (1104, 128, 128), (1200, 128, 128), (1204, 128, 10), (1208, 128, 245), (1212, 128, 128),
            };

            // 旧実装と一致するだけでなく、この列では、実際に抑制（中立への置き換え）が起きること（比較が空振りでないこと）
            Assert.True(AssertSameSequence(frames, 135, 50) > 0);
        }

        [Fact]
        public void TheOldHistoryIsDropped_AfterTheTimeout_LikeTheOldImplementation()
        {
            var frames = new List<(long, byte, byte)>
            {
                (0, 10, 128), (10, 250, 128), (49, 250, 128), (51, 10, 128), (60, 250, 128), (200, 10, 128), (400, 250, 128),
            };

            AssertSameSequence(frames, 135, 50);
            AssertSameSequence(frames, 60, 20);
            AssertSameSequence(frames, 200, 200);
        }

        [Theory]
        [InlineData(1, 60.0, 20)]
        [InlineData(2, 135.0, 50)]
        [InlineData(3, 135.0, 200)]
        [InlineData(4, 200.0, 50)]
        [InlineData(5, 20.0, 100)]
        public void RandomSequences_MatchTheOldImplementation(int seed, double delta, long timeout)
        {
            var rng = new Random(seed);
            var frames = new List<(long, byte, byte)>();
            long time = 5000;
            for (int i = 0; i < 3000; i++)
            {
                time += rng.Next(1, 25);
                byte x, y;
                switch (rng.Next(4))
                {
                    case 0: x = (byte)rng.Next(256); y = (byte)rng.Next(256); break;      // 一様
                    case 1: x = (byte)(rng.Next(2) == 0 ? rng.Next(0, 30) : rng.Next(226, 256)); y = (byte)rng.Next(110, 146); break;   // 端（左右）
                    case 2: x = (byte)rng.Next(120, 137); y = (byte)rng.Next(120, 137); break;   // 中立付近
                    default: x = (byte)rng.Next(110, 146); y = (byte)(rng.Next(2) == 0 ? rng.Next(0, 30) : rng.Next(226, 256)); break;   // 端（上下）
                }

                frames.Add((time, x, y));
            }

            AssertSameSequence(frames, delta, timeout);
        }
    }
}
