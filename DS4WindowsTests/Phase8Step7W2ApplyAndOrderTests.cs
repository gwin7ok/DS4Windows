using System;
using System.Collections.Generic;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.1-W2（Phase8-Step7-Plan.md §5.2・Q5＝A）: (1) プロファイル適用の前後で、新旧の出力・抑制の集合に差が出るかの特性化
    /// （差が出なければ <c>OnProfileApplied</c> は接続しない）、(2) 複数の Button の実行順序と二重ディスパッチの検出。
    /// 適用は <see cref="SpecialActionTestScope.SimulateProfileApply"/> で再現する（本番の掃除と同じ順序）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step7W2ApplyAndOrderTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static List<(bool Cross, bool Circle, bool L1, bool Suppressed, int LeftClicks)> Run(
            string extras, (bool L1, bool ApplyBefore)[] steps)
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.Add(new SpecialAction("p8s7w2_apply_" + Guid.NewGuid().ToString("N"), "L1", "Button",
                ((int)X360Controls.A).ToString(), 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.IsToggledOn = false;
            state.RequiresFreshPressAfterReset = false;

            var result = new List<(bool, bool, bool, bool, int)>();
            foreach (var step in steps)
            {
                if (step.ApplyBefore)
                    t.SimulateProfileApply();

                DS4State output = t.RunFrame(Frame(step.L1));
                result.Add((output.Cross, output.Circle, output.L1, t.Engine.IsSuppressed(t.Device, DS4Controls.L1),
                    t.Scope.Synthetic.DeviceState[t.Device].currentClicks.leftCount));
            }

            return result;
        }

        // ---- (1) 適用の前後 ----

        [Theory]
        [InlineData("Press\n")]
        [InlineData("Toggle\n")]
        public void ProfileApplyWhileHeld_SuppressesTheTrigger_WithoutOnProfileApplied(string extras)
        {
            // 押している最中に適用（状態の初期化と束縛の差し替え）が入る。適用後は Fresh-press ガードが有効で、押し直すまで Toggle は反転しない
            var steps = new (bool, bool)[]
            {
                (false, false), (true, false), (true, false), (true, true), (true, false), (false, false), (false, false)
            };

            var observed = Run(extras, steps);

            Assert.Contains(observed, f => f.Suppressed);
        }

        [Fact]
        public void TheSuppressionSet_SurvivesAProfileApply()
        {
            // 現行の挙動の固定: 適用（HandleDeviceDisconnect を含む後始末）は、抑制の集合を消さない。OnProfileApplied を無条件につなぐと、これが変わる（Q5 の B を採らない根拠）
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            t.StateOf(sa).RequiresFreshPressAfterReset = false;

            t.RunFrame(Frame(true));
            Assert.True(t.Engine.IsSuppressed(t.Device, DS4Controls.L1));

            t.SimulateProfileApply();

            Assert.True(t.Engine.IsSuppressed(t.Device, DS4Controls.L1));
        }

        // ---- (2) 順序と二重ディスパッチ ----

        [Fact]
        public void TwoButtons_EachClickIsCountedOnce()
        {
            List<(bool, bool, int)> Sequence()
            {
                using var t = new SpecialActionTestScope();
                t.Add(new SpecialAction("p8s7w2_two_a_" + Guid.NewGuid().ToString("N"), "L1", "Button", ((int)X360Controls.LeftMouse).ToString(), 0, "Press\n"));
                t.Add(new SpecialAction("p8s7w2_two_b_" + Guid.NewGuid().ToString("N"), "R1", "Button", ((int)X360Controls.B).ToString(), 0, "Press\n"));
                var result = new List<(bool, bool, int)>();
                foreach (var f in new[] { (false, false), (true, false), (true, true), (false, true), (false, false) })
                {
                    DS4State output = t.RunFrame(Frame(f.Item1, f.Item2));
                    result.Add((output.Cross, output.Circle, t.Scope.Synthetic.DeviceState[t.Device].currentClicks.leftCount));
                }

                return result;
            }

            var observed = Sequence();

            // 二重ディスパッチがないこと: 左クリックのカウンターは、押している 2 フレームで 1 ずつ（累積 1、2）になる
            Assert.Equal(new[] { 0, 1, 2, 2, 2 }, new[] { observed[0].Item3, observed[1].Item3, observed[2].Item3, observed[3].Item3, observed[4].Item3 });
        }

        [Fact]
        public void AMigratedButton_IsNotProcessedByTheLegacyStage3()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");

            Assert.True(t.Engine.Owns(t.Device, sa));
            DS4State output = t.RunFrame(Frame(true));

            Assert.True(output.Cross);
            // Phase8-Step8 8b: 旧 Stage3（lastButtonTriggerState のキャッシュ）は削除した。新経路だけが処理する
            var field = typeof(Mapping).GetField("lastButtonTriggerState", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            Assert.Null(field);
        }
    }
}
