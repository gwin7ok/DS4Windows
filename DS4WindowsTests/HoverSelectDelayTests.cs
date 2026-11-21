#nullable enable
using System;
using System.Collections.Generic;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Controls タブのコントローラー図のマウスオーバーは、ボタン一覧の選択の切り替えを 500ms 遅らせる。
    /// マウスが離れたら取り消し、遅延の前のクリックは、クリックした対象を先に確定する。
    /// 手動で進める時計を渡し、遅延の判断を挙動で確かめる（コントローラーの入力による選択は、この部品を通らず即時のまま）。
    /// </summary>
    public class HoverSelectDelayTests
    {
        private const int DelayMs = 500;

        private static (HoverSelectDelay<string> Delay, ManualTimeProvider Time, List<string> Selected) Create()
        {
            var time = new ManualTimeProvider();
            var selected = new List<string>();
            var delay = new HoverSelectDelay<string>(time, DelayMs, selected.Add, action => action());
            return (delay, time, selected);
        }

        private static void Pass(ManualTimeProvider time, int ms) => time.Advance(TimeSpan.FromMilliseconds(ms));

        [Fact]
        public void TheDelay_Is500ms()
        {
            Assert.Equal(DelayMs, ConstantValueReader.Get<int>(typeof(DS4Windows.UI.ProfileEditor), "HoverSelectDelayMs"));
        }

        [Fact]
        public void TheSelection_HappensOnlyAfterTheDelay()
        {
            var (delay, time, selected) = Create();

            delay.Start("A");
            Pass(time, DelayMs - 1);
            Assert.Empty(selected);
            Assert.True(delay.HasPending);

            Pass(time, 1);
            Assert.Equal(new[] { "A" }, selected);
            Assert.False(delay.HasPending);
        }

        [Fact]
        public void MouseLeave_CancelsThePendingSelection()
        {
            var (delay, time, selected) = Create();
            delay.Start("A");
            Pass(time, 300);

            delay.Cancel();
            Pass(time, 1000);

            Assert.Empty(selected);
            Assert.False(delay.HasPending);
            Assert.Equal(0, time.ActiveTimerCount);
        }

        [Fact]
        public void MovingToAnotherButton_RestartsTheDelay_ForTheNewOne()
        {
            var (delay, time, selected) = Create();
            delay.Start("A");
            Pass(time, 400);

            delay.Start("B");   // A の保留は捨て、遅延は B から数え直す
            Pass(time, 400);
            Assert.Empty(selected);

            Pass(time, 100);
            Assert.Equal(new[] { "B" }, selected);
        }

        [Fact]
        public void AClickBeforeTheDelay_SelectsTheClickedOneAtOnce_AndDropsThePending()
        {
            var (delay, time, selected) = Create();
            delay.Start("A");
            Pass(time, 100);

            delay.SelectNow("A");
            Assert.Equal(new[] { "A" }, selected);

            Pass(time, 1000);   // 保留は捨てられているので、二重に選ばれない
            Assert.Equal(new[] { "A" }, selected);
        }

        [Fact]
        public void TheSelection_IsHandedToTheMarshaller_SoItRunsOnTheUiThread()
        {
            var time = new ManualTimeProvider();
            var selected = new List<string>();
            var queued = new List<Action>();
            var delay = new HoverSelectDelay<string>(time, DelayMs, selected.Add, queued.Add);

            delay.Start("A");
            Pass(time, DelayMs);

            Assert.Empty(selected);   // タイマーのスレッドでは選ばない
            Assert.Single(queued);
            queued[0]();
            Assert.Equal(new[] { "A" }, selected);
        }
    }
}
