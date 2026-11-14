using System;
using System.Collections.Generic;
using DS4Windows.Processing;
using Xunit;
using DS4Windows.Processing.Services.Conditioning;

namespace DS4WindowsTests
{
    /// <summary>
    /// アンチスナップバック（<see cref="StickInputConditioner.AntiSnapback"/>）は、入力 1 フレームごとに呼ばれる。
    /// 履歴の調べ方（LINQ のラムダ）が毎フレーム割り当てを起こさないことを守る。
    /// </summary>
    public class StickInputConditionerAllocationTests
    {
        [Fact]
        public void AntiSnapback_DoesNotAllocate_PerFrame()
        {
            var conditioner = new StickInputConditioner();
            var history = new Queue<DS4TimedStickAxisValue>(64);

            byte x = 200, y = 128;
            long ts = 1000;
            // 暖機（JIT と、履歴のキューの容量の確保）
            for (int i = 0; i < 200; i++)
            {
                x = (byte)(i % 2 == 0 ? 20 : 230); y = 128;
                conditioner.AntiSnapback(history, 135, 50, ts += 4, ref x, ref y);
            }

            const int frames = 5000;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < frames; i++)
            {
                x = (byte)(i % 2 == 0 ? 20 : 230); y = 128;
                conditioner.AntiSnapback(history, 135, 50, ts += 4, ref x, ref y);
            }
            long perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / frames;

            Assert.True(perFrame == 0, $"1 フレームあたり {perFrame} バイト");
        }
    }
}
