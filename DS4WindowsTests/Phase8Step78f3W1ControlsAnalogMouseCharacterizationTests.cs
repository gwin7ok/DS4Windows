using System;
using System.Linq;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-3 W1（Phase8-Step7-8f-Plan.md §5）: Controls タブの Button のうち、マウスの移動（MouseUp／Down／Left／Right）と、
    /// アナログのホイール（元がスティック・トリガーの WUP／WDOWN）の特性化。旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。
    /// W1 の時点では、新経路が未実装のため、どちらでも旧経路が処理する。W2 で新経路が入るとき、同じ期待値で通ること
    /// （例外: ホイールの状態の共有は、Q5＝B で束縛ごとに変わる。<c>...LegacyShares...</c> のテストは旧経路だけの記録）。
    /// 実機がないため、スロットにテスト用のデバイス（経過時間つき）を置く。ホイールの 10ms の間隔は、<c>Mapping.oldnow</c> をフレームごとに戻して決定的にする。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78f3W1ControlsAnalogMouseCharacterizationTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step78f3W1ControlsAnalogMouseCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Neutral() => ControlsKeyTestScope.Neutral();

        /// <summary>スロットにテスト用のデバイスを置き、フレームごとにホイールの間隔の基準時刻を戻して、1 フレームずつ流す。</summary>
        private sealed class Harness : IDisposable
        {
            private readonly TestDs4Device _device = TestDs4Device.Create();
            private readonly DS4Device _savedDevice;

            public Harness(bool migrated, double elapsedMs = 5.0)
            {
                T = new ControlsKeyTestScope(migrated);
                _savedDevice = T.Scope.Control.DS4Controllers[T.Device];
                _device.lastTimeElapsedDouble = elapsedMs;
                T.Scope.Control.DS4Controllers[T.Device] = _device;
                ResetWheelState();
            }

            // Mapping は、ホストが共有する 1 つのインスタンス。前のテストが残したホイールの端数・向き・移動の端数を初期化する
            private void ResetWheelState()
            {
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                void Set(string name, object value) => typeof(Mapping).GetField(name, flags)?.SetValue(T.Mapping, value);
                Set("stickWheelRemainder", 0.0);
                Set("stickWheel", 0.0);
                Set("stickWheelDownDir", false);
                Set("pressagain", false);
                Set("horizontalRemainder", 0.0);
                Set("verticalRemainder", 0.0);
            }

            public ControlsKeyTestScope T { get; }

            public System.Collections.Generic.List<(int X, int Y)> Moves => T.Scope.Recorder.MoveEvents;

            public System.Collections.Generic.List<(int Vertical, int Horizontal)> Wheels => T.Scope.Recorder.WheelEvents;

            public DS4State Step(DS4State input)
            {
                T.Mapping.oldnow = DateTime.MinValue;   // 10ms の間隔を満たした状態にする（時刻に依らず、決定的にする）
                return T.Run(input);
            }

            public void Dispose()
            {
                T.Scope.Control.DS4Controllers[T.Device] = _savedDevice;
                T.Dispose();
            }
        }

        // ---- マウスの移動 ----

        [Theory]
        [InlineData(false, X360Controls.MouseRight, 1, 0)]
        [InlineData(true, X360Controls.MouseRight, 1, 0)]
        [InlineData(false, X360Controls.MouseLeft, -1, 0)]
        [InlineData(true, X360Controls.MouseLeft, -1, 0)]
        [InlineData(false, X360Controls.MouseDown, 0, 1)]
        [InlineData(true, X360Controls.MouseDown, 0, 1)]
        [InlineData(false, X360Controls.MouseUp, 0, -1)]
        [InlineData(true, X360Controls.MouseUp, 0, -1)]
        public void MouseMovement_FromAButton_MovesInItsDirection_AndErasesTheOriginal(bool migrated, X360Controls target, int signX, int signY)
        {
            using var h = new Harness(migrated);
            h.T.MapButton(DS4Controls.Cross, target);
            h.T.Publish();
            h.T.MouseInfo.activeButtonSensitivity = 100;   // 1 フレームで 1 以上動く大きさにする（小さいと、端数が次のフレームに持ち越される）

            DS4State input = Neutral();
            input.Cross = true;
            input.Square = true;   // 割り当てのないボタンは、そのまま
            DS4State output = h.Step(input);

            _output.WriteLine($"[7.8f-3 W1 {(migrated ? "migrated" : "legacy  ")}] {target}: moves = {string.Join(" ", h.Moves)}");
            Assert.False(output.Cross);
            Assert.True(output.Square);
            (int X, int Y) move = Assert.Single(h.Moves);
            Assert.Equal(signX, Math.Sign(move.X));
            Assert.Equal(signY, Math.Sign(move.Y));

            h.Moves.Clear();
            h.Step(Neutral());
            Assert.Empty(h.Moves);   // 離すと、動かない（端数は、押していない間に使われない）
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MouseMovement_TwoButtonsToTheSameDirection_DoNotAdd(bool migrated)
        {
            using var h = new Harness(migrated);
            h.T.MapButton(DS4Controls.Cross, X360Controls.MouseRight);
            h.T.MapButton(DS4Controls.Circle, X360Controls.MouseRight);
            h.T.Publish();
            h.T.MouseInfo.activeButtonSensitivity = 100;

            DS4State input = Neutral();
            input.Cross = true;
            input.Circle = true;
            h.Step(input);

            // 先に書いた方が勝つ（局所変数が 0 のときだけ書く）。足し合わせない。1 つ分は 100 の感度で 2 前後、足すと 4 以上になる
            (int X, int Y) move = Assert.Single(h.Moves);
            _output.WriteLine($"[7.8f-3 W1 {(migrated ? "migrated" : "legacy  ")}] two buttons → MouseRight: {move}");
            Assert.InRange(move.X, 1, 3);
            Assert.Equal(0, move.Y);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MouseMovement_FromAStick_ScalesWithTheDeflection_AndUsesTheElapsedTime(bool migrated)
        {
            using var h = new Harness(migrated, 5.0);
            h.T.MapButton(DS4Controls.LXPos, X360Controls.MouseRight);
            h.T.Publish();

            DS4State full = Neutral();
            full.LX = 255;
            DS4State output = h.Step(full);
            Assert.Equal(128, output.LX);   // 割り当てた軸の出力は消える
            (int X, int Y) fullMove = Assert.Single(h.Moves);
            _output.WriteLine($"[7.8f-3 W1 {(migrated ? "migrated" : "legacy  ")}] stick full right, 5ms: {fullMove}");
            Assert.InRange(fullMove.X, 4, 8);   // 感度 25 × 48 × 0.005 秒 ≒ 6
            Assert.Equal(0, fullMove.Y);

            h.Moves.Clear();
            h.Step(Neutral());
            Assert.Empty(h.Moves);   // 倒していなければ、動かない
        }

        // ---- アナログのホイール ----

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AnalogWheel_FromATrigger_NeedsThreeRunsForOneTick_AndErasesTheSource(bool migrated)
        {
            using var h = new Harness(migrated);
            h.T.MapButton(DS4Controls.L2, X360Controls.WUP);
            h.T.Publish();

            DS4State input = Neutral();
            input.L2 = 255;   // 全押しは、3 回で 1 目盛り（端数を持ち越す）
            DS4State output = h.Step(input);
            Assert.Equal(0, output.L2);   // 割り当てたトリガーの出力は消える
            Assert.Empty(h.Wheels);
            h.Step(input);
            Assert.Empty(h.Wheels);
            h.Step(input);

            _output.WriteLine($"[7.8f-3 W1 {(migrated ? "migrated" : "legacy  ")}] wheel after 3 runs: {string.Join(" ", h.Wheels)}");
            Assert.Single(h.Wheels);
            Assert.True(h.Wheels[0].Vertical > 0);   // WUP は正の向き
            Assert.Equal(0, h.Wheels[0].Horizontal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AnalogWheel_FromAStick_Down_IsNegative_AndALightPressIsSlower(bool migrated)
        {
            using var h = new Harness(migrated);
            h.T.MapButton(DS4Controls.LYPos, X360Controls.WDOWN);   // 左スティックを下へ（値が大きい向き）
            h.T.Publish();

            DS4State full = Neutral();
            full.LY = 255;
            for (int i = 0; i < 3; i++) h.Step(full);
            Assert.Single(h.Wheels);
            Assert.True(h.Wheels[0].Vertical < 0);   // WDOWN は負の向き

            // 軽く倒すと、1 回あたりの量が小さく（0.05 + 0.95 × 値/255）、3 回では 1 目盛りに届かない
            h.Wheels.Clear();
            DS4State light = Neutral();
            light.LY = 128 + 70;   // 幅 55 を超えて、成立する
            for (int i = 0; i < 3; i++) h.Step(light);
            _output.WriteLine($"[7.8f-3 W1 {(migrated ? "migrated" : "legacy  ")}] light press wheel events: {h.Wheels.Count}");
            Assert.Empty(h.Wheels);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AnalogWheel_Released_DoesNothing(bool migrated)
        {
            using var h = new Harness(migrated);
            h.T.MapButton(DS4Controls.R2, X360Controls.WUP);
            h.T.Publish();

            for (int i = 0; i < 6; i++) h.Step(Neutral());
            Assert.Empty(h.Wheels);
        }

        /// <summary>
        /// 旧経路の、Mapping 全体で 1 つのホイールの状態（Q5）。旧経路だけの記録（W2 で Q5＝B［束縛ごとに持つ］に直すとき、新しい期待値に更新する）。
        ///  1. 向きを変えると、持ち越した端数を捨てる。
        ///  2. 同じフレームに 2 つのアナログ元が上に割り当てられていても、10ms の間隔の基準時刻が共有のため、2 つ目は進まない。
        /// </summary>
        [Fact]
        public void AnalogWheel_LegacySharesOneStateAcrossAllAssignments()
        {
            // 1. 向きを変えると、端数を捨てる
            using (var h = new Harness(false))
            {
                h.T.MapButton(DS4Controls.L2, X360Controls.WUP);
                h.T.MapButton(DS4Controls.R2, X360Controls.WDOWN);
                h.T.Publish();

                DS4State up = Neutral();
                up.L2 = 255;
                h.Step(up);
                h.Step(up);   // 端数 2/3 を持ち越す

                DS4State down = Neutral();
                down.R2 = 255;
                h.Step(down);   // 向きが変わるので端数を捨てて、1/3 から
                h.Step(down);
                _output.WriteLine($"[7.8f-3 W1 legacy] after switching direction (2 runs): {h.Wheels.Count} events");
                Assert.Empty(h.Wheels);   // 捨てていなければ、2/3 + 1/3 ＋ 1/3 で 1 目盛りに届いていた
            }

            // 2. 同じフレームの 2 つ目のアナログ元は進まない
            using (var h = new Harness(false))
            {
                h.T.MapButton(DS4Controls.L2, X360Controls.WUP);
                h.T.MapButton(DS4Controls.R2, X360Controls.WUP);
                h.T.Publish();

                DS4State both = Neutral();
                both.L2 = 255;
                both.R2 = 255;
                for (int i = 0; i < 3; i++) h.Step(both);
                _output.WriteLine($"[7.8f-3 W1 legacy] two sources on WUP, 3 runs: {h.Wheels.Count} events");
                Assert.Single(h.Wheels);   // 3 回で 1 目盛り。2 つ目が進めば、2 目盛りになっていた
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AMouseMovementWithExtras_BothWork(bool migrated)
        {
            using var h = new Harness(migrated);
            DS4ControlSettings dcs = h.T.MapButton(DS4Controls.Cross, X360Controls.MouseRight);
            dcs.extras = "100,50,1,10,20,30,5,1,7";
            h.T.PublishControlsOnly();
            h.T.MouseInfo.activeButtonSensitivity = 100;

            h.Step(Neutral());
            DS4State input = Neutral();
            input.Cross = true;
            DS4State output = h.Step(input);

            Assert.False(output.Cross);        // Button が処理した（移動の大きさは、Extras が感度を一時的に変えるため、ここでは見ない）
            Assert.True(h.T.ForceLight);       // Extras も動く
            Assert.True(h.T.RumbleActive);
        }
    }
}
