using System;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5（Phase13-Step5-Plan.md §6 計測①）: 入力ループ（On_Report）が毎レポート読むスロット状態の取得が、
    /// ヒープ割り当て（GC Alloc）を行わないこと（ゼロアロケーション方針）。<c>ControlServiceHotPathAllocationTests</c> と同じ形式。
    /// </summary>
    public class Phase13Step5SlotStateHotPathAllocationTests
    {
        private const int Iterations = 200000;
        private readonly ITestOutputHelper _output;

        public Phase13Step5SlotStateHotPathAllocationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static int ReadOnce(IDeviceSlotState slots, DS4State[] cachedCurrent, DS4State[] cachedTemp, Mouse[] cachedTouch, int ind)
        {
            int acc = 0;
            // ControlService が保持する形（コンストラクターで一度だけ取得したキャッシュ済みの配列）
            if (cachedCurrent[ind] != null) acc++;
            if (cachedTemp[ind] != null) acc++;
            if (cachedTouch[ind] != null) acc++;
            // 接続・切断の経路が使う形（窓口を通した取得）
            if (slots.GetController(ind) != null) acc++;
            if (slots.GetCurrentState(ind) != null) acc++;
            if (slots.GetTempState(ind) != null) acc++;
            if (slots.DS4Controllers[ind] != null) acc++;
            if (slots.TouchPads[ind] != null) acc++;
            return acc;
        }

        [Fact]
        public void SlotStateReads_DoNotAllocate()
        {
            IDeviceSlotState slots = new DeviceSlotState();
            for (int i = 0; i < slots.CurrentStates.Length; i++)
            {
                slots.CurrentStates[i] = new DS4State();
                slots.TempStates[i] = new DS4State();
            }

            DS4State[] cur = slots.CurrentStates;
            DS4State[] tmp = slots.TempStates;
            Mouse[] touch = slots.TouchPads;

            int sink = 0;
            for (int i = 0; i < Iterations; i++) sink += ReadOnce(slots, cur, tmp, touch, i & 3);   // ウォームアップ（JIT の段階的な最適化が終わるまで十分に回す）

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) sink += ReadOnce(slots, cur, tmp, touch, i & 3);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（sink={sink}）");
            Assert.Equal(0L, allocated);
        }
    }
}
