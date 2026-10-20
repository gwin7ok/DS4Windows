using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.1-W1（Phase8-Step7-Plan.md §3・§5.1）: Button 種別の「旧経路（Stage3 と旧ループ）」と「新経路（エンジン。実物のビルダーが作る束縛）」の特性化。
    /// 同じ入力の列を両経路に流し、フレームごとの出力（Cross、L1／R1 の素通し、タッチパッドクリック、マウス左クリックの数）が一致することを確かめる。
    /// 期待値は旧経路に合わせる（Q3＝A。新経路を正として書き換えない）。
    /// 注意: 遅延（<c>delayTime</c>）は時刻に依存するため、ここでは「Button の出力は遅延（delayTime）で遅れない」ことだけを確かめる。時刻を伴う Fresh-press の解除は、
    /// <c>Phase7cW2SpecialActionTriggerCharacterizationTests</c>（両経路の <c>[Theory]</c>）が担う。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step7W1ButtonDualPathTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        /// <summary>1 つの Button 特殊アクションを登録し、入力の列を流して、フレームごとの出力を記録する。</summary>
        private static List<(bool Cross, bool L1, bool R1, bool Touch, int LeftClicks)> Run(
            bool migrated, string triggers, string details, string extras, int delayMs, bool freshPressGuard, DS4State[] frames)
        {
            using var t = new SpecialActionTestScope(migrated);
            SpecialAction sa = t.Add(new SpecialAction("p8s7_btn_" + Guid.NewGuid().ToString("N"), triggers, "Button", details, delayMs, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.IsToggledOn = false;
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.UntriggeredTimestampTicks = 0;

            var result = new List<(bool, bool, bool, bool, int)>();
            foreach (DS4State frame in frames)
            {
                DS4State output = t.RunFrame(frame);
                result.Add((output.Cross, output.L1, output.R1, output.OutputTouchButton,
                    t.Scope.Synthetic.DeviceState[t.Device].currentClicks.leftCount));
            }

            return result;
        }

        private static string Cross => ((int)X360Controls.A).ToString();

        [Fact]
        public void Press_Sequence_BothPathsGiveTheSameOutput()
        {
            DS4State[] frames = { Frame(false), Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(true), Frame(false), Frame(false), Frame(true), Frame(true), Frame(false) };

            var legacy = Run(false, "L1", Cross, "Press\n", 0, false, frames);
            var migrated = Run(true, "L1", Cross, "Press\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Cross);       // 比較が「何も出力しない」同士にならないこと
            Assert.Contains(legacy, f => !f.Cross);
        }

        [Fact]
        public void Toggle_Sequence_BothPathsGiveTheSameOutput()
        {
            DS4State[] frames = { Frame(false), Frame(false), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(false), Frame(true), Frame(true), Frame(false), Frame(false) };

            var legacy = Run(false, "L1", Cross, "Toggle\n", 0, false, frames);
            var migrated = Run(true, "L1", Cross, "Toggle\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Cross);       // ON の区間がある
            Assert.Contains(legacy, f => !f.Cross);      // OFF の区間がある
        }

        [Fact]
        public void Toggle_WhileTheFreshPressGuardIsOn_DoesNotFlip_OnEitherPath()
        {
            // 押しっぱなしの間は未成立のフレームがなく、ガードは解除されない（時刻に依存しない）
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var legacy = Run(false, "L1", Cross, "Toggle\n", 0, true, frames);
            var migrated = Run(true, "L1", Cross, "Toggle\n", 0, true, frames);

            Assert.Equal(legacy, migrated);
            Assert.DoesNotContain(legacy, f => f.Cross);  // ガード中は反転しない
        }

        [Fact]
        public void Press_AndCondition_BothPathsGiveTheSameOutput_IncludingThePassThrough()
        {
            DS4State[] frames =
            {
                Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true),
                Frame(false, true), Frame(true, true), Frame(false, false)
            };

            var legacy = Run(false, "L1/R1", Cross, "Press\n", 0, false, frames);
            var migrated = Run(true, "L1/R1", Cross, "Press\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Cross);
            Assert.Contains(legacy, f => f.L1 && !f.Cross);   // 片方だけのときは素通し
        }

        [Fact]
        public void TouchpadClickOutput_BothPathsGiveTheSameOutput()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(true), Frame(false) };
            string details = ((int)X360Controls.TouchpadClick).ToString();

            var legacy = Run(false, "L1", details, "Press\n", 0, false, frames);
            var migrated = Run(true, "L1", details, "Press\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Touch);
        }

        [Fact]
        public void MouseClickOutput_BothPathsGiveTheSameClickCounts()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(true), Frame(false) };
            string details = ((int)X360Controls.LeftMouse).ToString();

            var legacy = Run(false, "L1", details, "Press\n", 0, false, frames);
            var migrated = Run(true, "L1", details, "Press\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.LeftClicks > 0);
        }

        [Fact]
        public void UnreadableDetails_OutputNothing_OnEitherPath()
        {
            // 現行の Stage3 と同じ: details が整数として読めなければ、何も出力しない（トリガーのボタンは消える）
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false) };

            var legacy = Run(false, "L1", "not-a-number", "Press\n", 0, false, frames);
            var migrated = Run(true, "L1", "not-a-number", "Press\n", 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.DoesNotContain(legacy, f => f.Cross);
        }

        [Fact]
        public void Delay_DoesNotGateTheButtonOutput_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", Cross, "Press\n", 60_000, false, frames);
            var migrated = Run(true, "L1", Cross, "Press\n", 60_000, false, frames);

            Assert.Equal(legacy, migrated);
            // 現行の挙動の固定（W1 の実測）: delayTime は Button の出力を遅らせない。Press は成立中、そのまま出力される
            Assert.Contains(legacy, f => f.Cross);
        }

        [Fact]
        public void TheScope_RestoresTheRegistry()
        {
            var registry = TestHost.GetService<DS4Windows.Actions.IActionRegistry>();
            int device;

            using (var t = new SpecialActionTestScope(true))
            {
                device = t.Device;
                t.AddButtonPress("L1");
                Assert.Single(registry.GetBindings(device));
            }

            Assert.Empty(registry.GetBindings(device));
        }
    }
}
