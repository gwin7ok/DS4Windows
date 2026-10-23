using System.Diagnostics;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-2 W4（R2-MacroToggleRepeat-Plan.md §3.2）: マクロの Execute Mode「Repeat (Toggle)」の実行。
    /// <c>MacroAction</c> は、立ち上がりのたびにトグルを反転し、ON になったときだけ再生を始める。
    /// 再生（<c>MacroPlayback</c>）は、トグルが ON の間、繰り返す。Controls のマクロを通して、押す→離しても続く→もう一度押すと止まる、を確かめる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class R2W4MacroToggleRepeatTests
    {
        private static readonly int[] StepsWithWait = { 65, 65, 320 };   // 320 ＝ 20ms の待ち
        private readonly ITestOutputHelper _output;

        public R2W4MacroToggleRepeatTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private sealed class CountingPlayer : IMacroPlayer
        {
            public int PlayCount;
            public bool IsPlaying(int deviceIndex) => false;
            public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default) => PlayCount++;
            public void Stop(int deviceIndex) { }
        }

        private static SpecialAction MacroSa(string name, DS4KeyType keyType)
        {
            var sa = new SpecialAction(name, "Cross", "Macro", "65/65");
            sa.keyType = keyType;
            return sa;
        }

        [Fact]
        public void MacroAction_Toggle_PlaysOnlyWhenTurnedOn()
        {
            var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var player = new CountingPlayer();
            SpecialAction sa = MacroSa("R2W4_Toggle", DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro);
            var action = new MacroAction(sa, 0, player, 0, null, manager);

            action.Execute(null);   // OFF → ON: 再生する
            Assert.Equal(1, player.PlayCount);
            Assert.True(manager.GetStateFor(sa, 0).IsToggledOn);

            action.Execute(null);   // ON → OFF: 再生しない（ループが自分で止まる）
            Assert.Equal(1, player.PlayCount);
            Assert.False(manager.GetStateFor(sa, 0).IsToggledOn);

            action.Execute(null);   // OFF → ON
            Assert.Equal(2, player.PlayCount);
            Assert.True(manager.GetStateFor(sa, 0).IsToggledOn);
        }

        [Theory]
        [InlineData(DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        public void MacroAction_OtherModes_AlwaysPlay_AndNeverTouchTheToggle(DS4KeyType keyType)
        {
            var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var player = new CountingPlayer();
            SpecialAction sa = MacroSa("R2W4_Other_" + (int)keyType, keyType);
            var action = new MacroAction(sa, 0, player, 0, null, manager);

            action.Execute(null);
            action.Execute(null);

            Assert.Equal(2, player.PlayCount);
            Assert.False(manager.GetStateFor(sa, 0).IsToggledOn);
        }

        private static DS4State Cross(bool held)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = held;
            return state;
        }

        private static void RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);
        }

        private static int Presses(ControlsKeyTestScope t) => t.Recorder.KeyPresses.Count;

        private static void WaitForPresses(ControlsKeyTestScope t, int count)
        {
            var sw = Stopwatch.StartNew();
            while (Presses(t) < count && sw.ElapsedMilliseconds < 1500)
                Thread.Sleep(10);
        }

        [Fact]
        public void ControlsMacro_Toggle_StartsOnPress_ContinuesAfterRelease_AndStopsOnTheNextPress()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, StepsWithWait, DS4KeyType.RepeatLatchMacro);
            t.Publish();

            RunFor(t, Cross(false), 150);   // Fresh-press ガード（離して 80ms 続くまで実行しない）を先に満たす
            RunFor(t, Cross(true), 200);    // 1 回目の押下: 繰り返しが始まる
            WaitForPresses(t, 1);
            Assert.True(Presses(t) >= 1);

            RunFor(t, Cross(false), 300);   // 離しても、続く
            int afterRelease = Presses(t);
            RunFor(t, Cross(false), 500);
            int stillRunning = Presses(t);
            _output.WriteLine($"[R-2 W4] 離した直後={afterRelease} 500ms 後={stillRunning}");
            Assert.True(stillRunning > afterRelease);

            RunFor(t, Cross(true), 200);    // 2 回目の押下: 止まる
            RunFor(t, Cross(false), 300);   // 実行中の 1 回分を終える時間
            int stopped = Presses(t);
            RunFor(t, Cross(false), 500);
            int afterStop = Presses(t);
            _output.WriteLine($"[R-2 W4] 止まった後={stopped} 500ms 後={afterStop}");
            Assert.Equal(stopped, afterStop);
        }

        [Fact]
        public void ControlsMacro_Toggle_CanBeStartedAgainAfterStopping()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, StepsWithWait, DS4KeyType.RepeatLatchMacro);
            t.Publish();

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 200);    // 開始
            RunFor(t, Cross(false), 300);
            RunFor(t, Cross(true), 200);    // 停止
            RunFor(t, Cross(false), 400);
            int stopped = Presses(t);

            RunFor(t, Cross(true), 200);    // もう一度開始
            RunFor(t, Cross(false), 500);

            Assert.True(Presses(t) > stopped);
        }
    }
}
