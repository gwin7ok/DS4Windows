#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services.Conditioning;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 手順が空のマクロを「押している間繰り返す」とき、繰り返しの間に待ちを置き、止めずに続けること（空回りも、勝手な停止もしない）。
    /// 待ちは <see cref="TimeProvider"/> のタイマーなので、手動で進める時計を渡し、待ちの有無と再開を挙動で確かめる。
    /// </summary>
    public class MacroPlaybackEmptyRepeatTests
    {
        private sealed class FakeManager : IManagedActionManager
        {
            private readonly ActionInstanceState _state = new ActionInstanceState();
            public volatile bool Triggered = true;
            public ActionInstanceState? GetStateFor(SpecialAction action, int device) => _state;
            public bool IsBeingTriggered(SpecialAction action, int device) => Triggered;
            public void ClearToggledOnForKey(ushort key) { }
            public void ClearAllToggledOn() { }
            public void ClearAllEntries() { }
            public void ClearDeviceState(int device) { }
            public void SetToggledOn(SpecialAction action, int device, bool value) { }
            public event Action<SpecialAction, int, bool, bool> ToggledOnChanged { add { } remove { } }
        }

        private sealed class NoopExecutor : IMacroStepExecutor
        {
            public bool Execute(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown) => false;
        }

        private static void WaitFor(Func<bool> condition)
        {
            DateTime limit = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < limit) Thread.Sleep(5);
        }

        [Fact]
        public void AnEmptyMacro_HeldDown_WaitsBetweenRuns_ThenKeepsRepeating_ThenStopsOnRelease()
        {
            var time = new ManualTimeProvider();
            var manager = new FakeManager();
            var playback = new MacroPlayback(TestLightbar.State, new NoopExecutor(), new MacroRuntimeState(), manager, time);
            var action = new SpecialAction("N", "Cross", "Macro", "");

            try
            {
            playback.Play(0, string.Empty, new List<int>(), null, DS4KeyType.Macro | DS4KeyType.RepeatMacro, action);

            // 1 回目の再生のあと、押している間は、待ちの時計を 1 つ作って待つ（待ちなしで空回りしない）
            WaitFor(() => time.ActiveTimerCount == 1);
            Assert.Equal(1, time.ActiveTimerCount);

            // 待ちが終わり、まだ押していれば、止めずに次の繰り返しへ進み、また待つ
            time.Advance(TimeSpan.FromMilliseconds(50));
            WaitFor(() => time.ActiveTimerCount == 1);
            Assert.Equal(1, time.ActiveTimerCount);

            // 離したら、待ちが終わったあとの確認で繰り返しを終える（それ以上、待ちの時計を作らない）
            manager.Triggered = false;
            time.Advance(TimeSpan.FromMilliseconds(50));
            Thread.Sleep(100);
            Assert.Equal(0, time.ActiveTimerCount);
            }
            finally { manager.Triggered = false; }   // 失敗しても、繰り返しのスレッドを残さない
        }
    }
}
