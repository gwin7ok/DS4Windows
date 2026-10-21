using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: <see cref="MacroPlayback"/>（旧 <c>Mapping.PlayMacro</c> ほか）の予約・再生・後始末の特性化。
    /// 特殊アクションを介する経路（同期キュー、押している間の繰り返し）は、入口の <c>PlayMacroDirect</c> を通る既存の挙動テストが守る。
    /// ここでは、特殊アクションなし（単発の背景 Task）の再生と、文字列・配列・リストの手順、切断時のキューのクリアを調べる。
    /// </summary>
    public class Phase9CMacroPlaybackTests
    {
        private sealed class CountingExecutor : IMacroStepExecutor
        {
            public readonly List<(int Device, int Code)> Calls = new List<(int, int)>();
            private readonly object _lock = new object();

            public bool Execute(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown)
            {
                lock (_lock)
                {
                    Calls.Add((device, macroCodeValue));
                    // 押下の表を、実際の実装と同じく反転させる（後始末が押しっぱなしの値を離すのを確かめるため）
                    if (macroCodeValue < 300) keydown[macroCodeValue] = !keydown[macroCodeValue];   // 待ちの指定（300 以上）は、押下の表に触れない（実際の実装と同じ）
                }

                return macroCodeValue >= 300;   // 300 以上は待ちの指定（macroCodeValue - 300 ミリ秒）
            }

            public int Count { get { lock (_lock) { return Calls.Count; } } }
        }

        private static void WaitFor(Func<bool> condition)
        {
            DateTime limit = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < limit) Thread.Sleep(5);
        }

        [Fact]
        public void Play_RunsAStringMacroStepByStep_OnABackgroundTask()
        {
            var executor = new CountingExecutor();
            var playback = new MacroPlayback(executor, new MacroRuntimeState());

            playback.Play(1, "65/310/66/66/65", null, null, DS4KeyType.None);

            WaitFor(() => executor.Count >= 5);
            Assert.Equal(new[] { 65, 310, 66, 66, 65 }, executor.Calls.ConvertAll(c => c.Code));
            Assert.All(executor.Calls, c => Assert.Equal(1, c.Device));
        }

        [Fact]
        public void Play_RunsAListAndAnArray()
        {
            var executorList = new CountingExecutor();
            new MacroPlayback(executorList, new MacroRuntimeState()).Play(0, string.Empty, new List<int> { 65, 65 }, null, DS4KeyType.None);
            var executorArray = new CountingExecutor();
            new MacroPlayback(executorArray, new MacroRuntimeState()).Play(0, string.Empty, null, new[] { 70, 70 }, DS4KeyType.None);

            WaitFor(() => executorList.Count >= 2 && executorArray.Count >= 2);
            Assert.Equal(new[] { 65, 65 }, executorList.Calls.ConvertAll(c => c.Code));
            Assert.Equal(new[] { 70, 70 }, executorArray.Calls.ConvertAll(c => c.Code));
        }

        [Fact]
        public void Play_ReleasesAKeyThatIsStillDown_WhenTheMacroEnds()
        {
            // 手順が 65 だけ（押したまま）。特殊アクションなし（keepKeyState の指定なし）なので、終わりに 65 をもう一度実行して離す
            var executor = new CountingExecutor();
            new MacroPlayback(executor, new MacroRuntimeState()).Play(0, string.Empty, new List<int> { 65 }, null, DS4KeyType.None);

            WaitFor(() => executor.Count >= 2);
            Assert.Equal(new[] { 65, 65 }, executor.Calls.ConvertAll(c => c.Code));
        }

        [Fact]
        public void ClearDevice_EmptiesOnlyThatDevicesQueue()
        {
            var state = new MacroRuntimeState();
            state.TaskQueue[2]["a"] = System.Threading.Tasks.Task.CompletedTask;
            state.TaskQueue[3]["b"] = System.Threading.Tasks.Task.CompletedTask;
            var playback = new MacroPlayback(new CountingExecutor(), state);

            playback.ClearDevice(2);

            Assert.Empty(state.TaskQueue[2]);
            Assert.Single(state.TaskQueue[3]);
        }

        [Fact]
        public void ClearDevice_IgnoresAnOutOfRangeDevice()
        {
            var playback = new MacroPlayback(new CountingExecutor(), new MacroRuntimeState());
            playback.ClearDevice(-1);
            playback.ClearDevice(ControllerLimits.MaxControllerCount);
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MacroPlayback(null, new MacroRuntimeState()));
            Assert.Throws<ArgumentNullException>(() => new MacroPlayback(new CountingExecutor(), null));
        }
    }
}
