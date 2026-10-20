using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-1 W1（Phase8-Step7-8f-Plan.md §5）: Controls タブの Button のうち、パッド（X360 のボタン・軸・トリガー）への割り当てと、
    /// Default の軸の素通しの特性化。旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。
    /// W1 の時点では、新経路が未実装のため、どちらでも旧経路が処理する。W2 で新経路が入るとき、承認済みの変更（Q3＝A: ボタン・トリガー・スティックの
    /// 「押した」判定を SA と同じにする。差はジャイロの閾値だけ）以外は、同じ期待値で通ること。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78fW1ControlsPadCharacterizationTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step78fW1ControlsPadCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Neutral() => ControlsKeyTestScope.Neutral();

        [Fact]
        public void ButtonToPadButton_MovesThePressToTheTarget_AndErasesTheOriginal()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.B);   // B の標準の割り当ては Circle
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            input.Square = true;   // 割り当てのないボタンは、そのまま
            DS4State output = t.Run(input);

            Assert.True(output.Circle);
            Assert.False(output.Cross);
            Assert.True(output.Square);

            DS4State released = t.Run(Neutral());
            Assert.False(released.Circle);
            Assert.False(released.Cross);
        }

        [Fact]
        public void PadButtonTarget_JudgesAStickDirectionBy55_AndATriggerAbove100()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.LXNeg, X360Controls.A);   // A の標準の割り当ては Cross
            t.MapButton(DS4Controls.L2, X360Controls.Y);      // Y の標準の割り当ては Triangle
            t.Publish();

            DS4State slightlyLeft = Neutral();
            slightlyLeft.LX = 100;   // 128 − 100 ＝ 28。幅 55 に届かない
            Assert.False(t.Run(slightlyLeft).Cross);

            DS4State farLeft = Neutral();
            farLeft.LX = 60;         // 128 − 60 ＝ 68 > 55
            Assert.True(t.Run(farLeft).Cross);

            DS4State at100 = Neutral();
            at100.L2 = 100;
            Assert.False(t.Run(at100).Triangle);

            DS4State at101 = Neutral();
            at101.L2 = 101;
            DS4State output = t.Run(at101);
            Assert.True(output.Triangle);
            Assert.Equal(0, output.L2);   // 割り当てたトリガーの元の出力は消える
        }

        [Fact]
        public void ButtonToAxisTarget_DrivesTheAxisToItsEnd_AndTheReleasedAxisStaysNeutral()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.LXPos);
            t.MapButton(DS4Controls.Circle, X360Controls.LYNeg);
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            DS4State output = t.Run(input);
            Assert.Equal(255, output.LX);
            Assert.False(output.Cross);

            input = Neutral();
            input.Circle = true;
            output = t.Run(input);
            Assert.Equal(0, output.LY);
            Assert.False(output.Circle);

            output = t.Run(Neutral());
            Assert.Equal(128, output.LX);
            Assert.Equal(128, output.LY);
        }

        [Fact]
        public void AnalogSourceToAxisTarget_KeepsTheAnalogValue_AndErasesTheSource()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.RXPos, X360Controls.LXPos);
            t.Publish();

            DS4State input = Neutral();
            input.RX = 200;
            DS4State output = t.Run(input);

            _output.WriteLine($"[7.8f-1 W1 new] RX=200 → LXPos: LX={output.LX} RX={output.RX}");
            Assert.Equal(200, output.LX);
            Assert.Equal(128, output.RX);   // 元は消える。反対側（RXNeg）は Default の素通しだが、倒していないため 128 のまま
        }

        [Fact]
        public void TriggerTarget_FromAButtonIsFullScale_AndFromATriggerKeepsTheValue()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.R1, X360Controls.LT);
            t.MapButton(DS4Controls.R2, X360Controls.RT);
            t.Publish();

            DS4State input = Neutral();
            input.R1 = true;
            DS4State output = t.Run(input);
            Assert.Equal(255, output.L2);
            Assert.False(output.R1);

            input = Neutral();
            input.R2 = 150;
            output = t.Run(input);
            Assert.Equal(150, output.R2);   // R2 → RT。値を保つ
        }

        [Fact]
        public void UnboundTarget_OnlyErasesTheOriginal()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.Unbound);
            t.Publish();

            DS4State input = Neutral();
            input.Cross = true;
            input.Circle = true;
            DS4State output = t.Run(input);

            Assert.False(output.Cross);
            Assert.True(output.Circle);   // 割り当てのないボタンは、そのまま
            Assert.False(output.Square);
            Assert.False(output.Triangle);
        }

        [Fact]
        public void TheOppositeDirectionOfAMappedAxis_PassesThroughAsDefault()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.LXNeg, X360Controls.A);   // 左へ倒す側だけを割り当てる。右へ倒す側（LXPos）は Default
            t.Publish();

            DS4State right = Neutral();
            right.LX = 220;
            DS4State output = t.Run(right);
            Assert.Equal(220, output.LX);   // Default の素通しで戻る（割り当てたペアの消去は、両方向を 128 にする）
            Assert.False(output.Cross);

            DS4State left = Neutral();
            left.LX = 40;
            output = t.Run(left);
            Assert.Equal(128, output.LX);   // 割り当て側は消える
            Assert.True(output.Cross);
        }

        [Fact]
        public void WithoutAnyAssignment_TheStickPassesThroughUnchanged()
        {
            using var t = new ControlsKeyTestScope();
            t.Publish();

            DS4State input = Neutral();
            input.LX = 220;
            input.LY = 40;
            input.RX = 90;
            DS4State output = t.Run(input);

            Assert.Equal(220, output.LX);
            Assert.Equal(40, output.LY);
            Assert.Equal(90, output.RX);
            Assert.Equal(128, output.RY);
        }

        [Fact]
        public void APadButtonWithExtras_BothWork()
        {
            using var t = new ControlsKeyTestScope();
            DS4ControlSettings dcs = t.MapButton(DS4Controls.Cross, X360Controls.B);
            dcs.extras = "100,50,1,10,20,30,5,1,7";
            t.PublishControlsOnly();

            t.Run(Neutral());
            DS4State input = Neutral();
            input.Cross = true;
            DS4State output = t.Run(input);

            Assert.True(output.Circle);   // Button が動く
            Assert.False(output.Cross);
            Assert.True(t.ForceLight);    // Extras も動く
            Assert.True(t.RumbleActive);

            t.Run(Neutral());
            Assert.False(t.ForceLight);
        }
    }
}
