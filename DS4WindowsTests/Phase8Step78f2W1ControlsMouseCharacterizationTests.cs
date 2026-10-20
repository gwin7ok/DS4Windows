using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-2 W1（Phase8-Step7-8f-Plan.md §5）: Controls タブの Button のうち、マウスのボタン（左・右・中・4・5）、タッチパッドクリック、
    /// ホイール（デジタル。元がボタン）、Toggle つきの割り当ての特性化。旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。
    /// W1 の時点では、新経路が未実装のため、どちらでも旧経路が処理する。W2 で新経路が入るとき、同じ期待値で通ること
    /// （例外: Toggle は、Q8＝A の不具合の是正で変わる。<see cref="Toggle_LegacyFlipsEveryFrameWhileHeld"/> は旧経路だけの記録）。
    /// <c>MapCustom</c> は <c>Commit</c> を呼ばないため、フレームの区切りは <c>SaveToPrevious(true)</c>（Commit のデバイスごとの後始末）で再現する。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78f2W1ControlsMouseCharacterizationTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step78f2W1ControlsMouseCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Neutral() => ControlsKeyTestScope.Neutral();

        private static SyntheticState StateOf(ControlsKeyTestScope t) => t.Scope.Synthetic.DeviceState[t.Device];

        // 他のテストが残した反転の状態を初期化する（currentClicks は構造体のフィールドのため、コピーを書き戻す）
        private static void ResetToggle(ControlsKeyTestScope t)
        {
            SyntheticState state = StateOf(t);
            SyntheticState.MouseClick clicks = state.currentClicks;
            clicks.toggle = false;
            state.currentClicks = clicks;
            state.previousClicks = clicks;
        }

        /// <summary>1 フレーム流して、そのフレームのクリックの数を取り、Commit のデバイスごとの後始末（カウンターのクリア）をする。</summary>
        private static (DS4State Output, SyntheticState.MouseClick Clicks) Frame(ControlsKeyTestScope t, DS4State input)
        {
            DS4State output = t.Run(input);
            SyntheticState state = StateOf(t);
            SyntheticState.MouseClick clicks = state.currentClicks;
            state.SaveToPrevious(true);
            return (output, clicks);
        }

        [Theory]
        [InlineData(false, X360Controls.LeftMouse)]
        [InlineData(true, X360Controls.LeftMouse)]
        [InlineData(false, X360Controls.RightMouse)]
        [InlineData(true, X360Controls.RightMouse)]
        [InlineData(false, X360Controls.MiddleMouse)]
        [InlineData(true, X360Controls.MiddleMouse)]
        [InlineData(false, X360Controls.FourthMouse)]
        [InlineData(true, X360Controls.FourthMouse)]
        [InlineData(false, X360Controls.FifthMouse)]
        [InlineData(true, X360Controls.FifthMouse)]
        public void MouseButton_CountsOnlyItsOwnKind_WhileHeld_AndErasesTheOriginal(bool migrated, X360Controls target)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapButton(DS4Controls.Cross, target);
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            input.Square = true;   // 割り当てのないボタンは、そのまま
            var (output, clicks) = Frame(t, input);

            int[] counts = { clicks.leftCount, clicks.rightCount, clicks.middleCount, clicks.fourthCount, clicks.fifthCount, clicks.wUpCount, clicks.wDownCount };
            int index = target switch
            {
                X360Controls.LeftMouse => 0,
                X360Controls.RightMouse => 1,
                X360Controls.MiddleMouse => 2,
                X360Controls.FourthMouse => 3,
                _ => 4,
            };
            for (int i = 0; i < counts.Length; i++)
                Assert.Equal(i == index ? 1 : 0, counts[i]);
            Assert.False(output.Cross);
            Assert.True(output.Square);

            // 押している間は、毎フレーム数える（Commit が立ち上がり・立ち下がりを見る）
            (_, clicks) = Frame(t, input);
            Assert.Equal(1, new[] { clicks.leftCount, clicks.rightCount, clicks.middleCount, clicks.fourthCount, clicks.fifthCount }[index]);

            (output, clicks) = Frame(t, Neutral());
            Assert.Equal(0, clicks.leftCount + clicks.rightCount + clicks.middleCount + clicks.fourthCount + clicks.fifthCount);
            Assert.False(output.Cross);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MouseButton_StickDirectionUsesTheSectorRule_AndTheTriggerIsPressedAboveZero(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapButton(DS4Controls.LXNeg, X360Controls.RightMouse);
            t.MapButton(DS4Controls.L2, X360Controls.MiddleMouse);
            t.Publish();

            DS4State slightlyLeft = Neutral();
            slightlyLeft.LX = 100;   // 128 − 100 ＝ 28。幅 55 には届かないが、角度の扇形の中なので、押している（Controls の判定）
            var (output, clicks) = Frame(t, slightlyLeft);
            Assert.Equal(1, clicks.rightCount);
            Assert.Equal(128, output.LX);   // 方向の出力は消える

            DS4State slightTrigger = Neutral();
            slightTrigger.L2 = 1;   // 100 には届かないが、0 より大きいので、押している
            (output, clicks) = Frame(t, slightTrigger);
            Assert.Equal(1, clicks.middleCount);
            Assert.Equal(0, output.L2);

            (_, clicks) = Frame(t, Neutral());
            Assert.Equal(0, clicks.rightCount + clicks.middleCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TouchpadClick_MovesThePressToTheTouchButton_JudgedLikeTheSpecialAction(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapButton(DS4Controls.Cross, X360Controls.TouchpadClick);
            t.MapButton(DS4Controls.LXNeg, X360Controls.TouchpadClick);
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            var (output, clicks) = Frame(t, input);
            Assert.True(output.OutputTouchButton);
            Assert.False(output.Cross);
            Assert.Equal(0, clicks.leftCount);   // マウスのカウンターは動かない

            // スティックは、Controls の判定ではなく、幅 55 の判定（GetBoolMapping）
            DS4State slightlyLeft = Neutral();
            slightlyLeft.LX = 100;
            Assert.False(Frame(t, slightlyLeft).Output.OutputTouchButton);

            DS4State farLeft = Neutral();
            farLeft.LX = 60;
            Assert.True(Frame(t, farLeft).Output.OutputTouchButton);

            Assert.False(Frame(t, Neutral()).Output.OutputTouchButton);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DigitalWheel_FromAButton_CountsWheelUpAndDown(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapButton(DS4Controls.Cross, X360Controls.WUP);
            t.MapButton(DS4Controls.Circle, X360Controls.WDOWN);
            t.Publish();

            DS4State up = Neutral();
            up.Cross = true;
            var (output, clicks) = Frame(t, up);
            Assert.Equal(1, clicks.wUpCount);
            Assert.Equal(0, clicks.wDownCount);
            Assert.False(output.Cross);

            DS4State down = Neutral();
            down.Circle = true;
            (output, clicks) = Frame(t, down);
            Assert.Equal(0, clicks.wUpCount);
            Assert.Equal(1, clicks.wDownCount);
            Assert.False(output.Circle);

            (_, clicks) = Frame(t, Neutral());
            Assert.Equal(0, clicks.wUpCount + clicks.wDownCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AMouseButtonWithExtras_BothWork(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            DS4ControlSettings dcs = t.MapButton(DS4Controls.Cross, X360Controls.LeftMouse);
            dcs.extras = "100,50,1,10,20,30,5,1,7";
            t.PublishControlsOnly();

            Frame(t, Neutral());
            DS4State input = Neutral();
            input.Cross = true;
            var (output, clicks) = Frame(t, input);

            Assert.Equal(1, clicks.leftCount);   // Button が動く
            Assert.False(output.Cross);
            Assert.True(t.ForceLight);           // Extras も動く
            Assert.True(t.RumbleActive);

            Frame(t, Neutral());
            Assert.False(t.ForceLight);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ThePadTargetKeepsWorking_AndANeighbouringMouseAssignmentIsIndependent(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapButton(DS4Controls.Cross, X360Controls.B);          // パッド（7.8f-1）
            t.MapButton(DS4Controls.Square, X360Controls.LeftMouse); // マウス（7.8f-2）
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            input.Square = true;
            var (output, clicks) = Frame(t, input);

            Assert.True(output.Circle);
            Assert.False(output.Cross);
            Assert.False(output.Square);
            Assert.Equal(1, clicks.leftCount);
        }

    }
}
