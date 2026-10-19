using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8c-W1（Phase8-Step7-8c-Plan.md §5）: Controls タブの Macro（<c>ProcessControlSettingAction</c> の Macro の枝）の特性化。
    /// 旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。W2 で新経路（SA の Macro の仕様に合わせた）が入り、
    /// <b>承認済みの動作の変更</b>（Play Once の連続再実行を、不具合として 1 回だけに直す。Fresh-press ガードを SA と同じく見る）は、<c>migrated</c> で期待値を分けて固定する。
    /// マクロは背景のタスクで再生されるため、実際に待つ（待ち時間に余裕を持たせる）。手順は { 65, 65 }（キー 65 を押して離す）。
    /// <b>押し続けたときの再実行の有無</b>など、読むだけでは断定できない挙動は、回数を <c>ITestOutputHelper</c> に出力して記録する（W2 で、SA の仕様に合わせたときの変更点を確定する材料）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78cW1ControlsMacroCharacterizationTests
    {
        private static readonly int[] Steps = { 65, 65 };
        private static readonly int[] StepsWithWait = { 65, 65, 320 };   // 320 ＝ 20ms の待ち（繰り返しの確認で、再生が詰まらないように）
        private readonly ITestOutputHelper _output;

        public Phase8Step78cW1ControlsMacroCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Cross(bool held)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = held;
            return state;
        }

        /// <summary>指定の時間、10ms おきにフレームを流す。最後の出力を返す。</summary>
        private static DS4State RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            DS4State last = null;
            var sw = Stopwatch.StartNew();
            do
            {
                last = t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);

            return last;
        }

        private static int Presses(ControlsKeyTestScope t) => t.Recorder.KeyPresses.Count;

        /// <summary>マクロの再生（背景のタスク）で、押されたキーが指定の数になるまで待つ（最大 1.5 秒）。</summary>
        private static void WaitForPresses(ControlsKeyTestScope t, int count)
        {
            var sw = Stopwatch.StartNew();
            while (Presses(t) < count && sw.ElapsedMilliseconds < 1500)
                Thread.Sleep(10);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Press_PlaysTheMacroSteps_AndNothingMoreAfterTheRelease(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.Cross, Steps);
            t.Publish();

            RunFor(t, Cross(false), 150);   // 新経路は、SA と同じ Fresh-press ガード（離して 80ms 続くまで実行しない）を見る
            RunFor(t, Cross(true), 300);
            WaitForPresses(t, 1);

            Assert.True(Presses(t) >= 1);
            Assert.Equal(t.Native(65), t.Recorder.KeyPresses[0]);

            int whileHeld = Presses(t);
            RunFor(t, Cross(true), 600);
            int stillHeld = Presses(t);

            RunFor(t, Cross(false), 150);
            int atRelease = Presses(t);
            RunFor(t, Cross(false), 300);
            int afterRelease = Presses(t);

            _output.WriteLine($"[7.8c W1 {(migrated ? "migrated" : "legacy  ")}] 押し続けて: 300ms 後={whileHeld} 900ms 後={stillHeld} / 離したあと: {atRelease} → {afterRelease}");
            Assert.Equal(atRelease, afterRelease);   // 離したあとは、増えない

            // 承認済みの動作の変更: Play Once（フラグなし）は、旧経路では押している間ずっと再実行される（実測: 900ms で 58 回）。新経路（SA の仕様）では、押した瞬間の 1 回だけ
            if (migrated) Assert.Equal(1, stillHeld);
            else Assert.True(stillHeld > 5, $"旧経路は、押している間、再実行される（実測 {stillHeld} 回）");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RepeatWhileHeld_FlagsAreReported(bool migrated)
        {
            // 編集画面が保存する「押している間繰り返す」（RepeatMacro と HoldMacro の両方）の挙動を記録する
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.Cross, StepsWithWait, DS4KeyType.HoldMacro | DS4KeyType.RepeatMacro);
            t.Publish();

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 300);
            WaitForPresses(t, 1);
            int early = Presses(t);

            RunFor(t, Cross(true), 700);
            int later = Presses(t);

            RunFor(t, Cross(false), 150);
            int atRelease = Presses(t);
            RunFor(t, Cross(false), 300);
            int afterRelease = Presses(t);

            _output.WriteLine($"[7.8c W1 {(migrated ? "migrated" : "legacy  ")}] 繰り返し: 300ms 後={early} 1000ms 後={later} / 離したあと: {atRelease} → {afterRelease}");
            Assert.True(early >= 1);
            Assert.True(later > early);              // 「押している間繰り返す」は、旧・新とも、押している間に増える
            Assert.Equal(atRelease, afterRelease);   // 離したあとは、止まる
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StickDirection_UsesThe55WideThreshold(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.LXNeg, Steps);
            t.Publish();

            DS4State slightlyLeft = ControlsKeyTestScope.Neutral();
            slightlyLeft.LX = 100;   // 128 − 100 ＝ 28。幅 55 に届かない（Key の判定［角度の扇形］では押下になる）
            RunFor(t, slightlyLeft, 400);
            Assert.Equal(0, Presses(t));

            t.Run(ControlsKeyTestScope.Neutral());
            DS4State farLeft = ControlsKeyTestScope.Neutral();
            farLeft.LX = 60;         // 128 − 60 ＝ 68 > 55
            RunFor(t, farLeft, 300);
            WaitForPresses(t, 1);
            Assert.True(Presses(t) >= 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Trigger_IsPressedAbove100(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.L2, Steps);
            t.Publish();

            DS4State at100 = ControlsKeyTestScope.Neutral();
            at100.L2 = 100;
            RunFor(t, at100, 400);
            Assert.Equal(0, Presses(t));

            t.Run(ControlsKeyTestScope.Neutral());
            DS4State at101 = ControlsKeyTestScope.Neutral();
            at101.L2 = 101;
            RunFor(t, at101, 300);
            WaitForPresses(t, 1);
            Assert.True(Presses(t) >= 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheAssignedButtonsOutput_IsErasedWhileHeld(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.Cross, Steps);
            t.Publish();

            DS4State output = t.Run(Cross(true));

            Assert.False(output.Cross);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ADcsWithExtras_IsNotOwnedByTheNewPath(bool migrated)
        {
            // 7.8e: Extras つきの Macro は、新経路が処理する（Extras の束縛 ＋ Macro の束縛）。新経路の指定がなければ、束縛を作らず、旧経路が処理する
            using var t = new ControlsKeyTestScope(migrated);
            DS4ControlSettings dcs = t.MapMacro(DS4Controls.Cross, Steps);
            dcs.extras = "1,2,0,0,0,0,0,0,0";
            t.Publish();

            Assert.Equal(migrated ? 2 : 0, TestHost.GetService<DS4Windows.DI.IControlBindingBuilder>().Build(t.Device).Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ProfileApplyOrDisconnectWhileHeld_IsReported(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapMacro(DS4Controls.Cross, Steps);
            t.Publish();

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 300);
            WaitForPresses(t, 1);
            int before = Presses(t);

            ActionManager.ClearDeviceState(t.Device);
            RunFor(t, Cross(true), 400);
            int afterApply = Presses(t);

            t.Mapping.HandleDeviceDisconnect(t.Device);
            RunFor(t, Cross(true), 400);
            int afterDisconnect = Presses(t);

            _output.WriteLine($"[7.8c W1 {(migrated ? "migrated" : "legacy  ")}] 押したまま: 最初={before} 適用のあと={afterApply} 切断のあと={afterDisconnect}");
            Assert.True(before >= 1);
        }
    }
}
