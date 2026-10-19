using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.2-W1（Phase8-Step7-2-Plan.md §3 W1）: Key 種別の「旧経路（<c>MapCustomAction</c> の Key の分岐）」と「新経路（エンジン。実物のビルダーが作る束縛）」の特性化。
    /// 同じ入力の列を両経路に流し、フレームごとの状態（<c>BeingTriggered</c>、<c>IsToggledOn</c>、合成キーの押下の数、解放の数）が一致することを確かめる。
    /// 例外（Q1）: Key を 2 つ同時に押したときの解放は、旧経路が <c>UntriggerIndex</c> の癖で先のキーを解放しない。新経路は各キーを解放する（是正）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step72W1KeyDualPathTests
    {
        private const ushort KeyA = 65;

        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static (bool Being, bool Toggled, int Vk, int Scan, int ToggleCount, int Releases) Snapshot(
            SpecialActionTestScope t, SpecialAction sa, ushort key)
        {
            ActionInstanceState state = t.StateOf(sa);
            int vk = -1, scan = -1, toggleCount = -1;
            if (t.Scope.Synthetic.DeviceState[t.Device].keyPresses.TryGetValue(key, out SyntheticState.KeyPresses kp))
            {
                vk = kp.current.vkCount;
                scan = kp.current.scanCodeCount;
                toggleCount = kp.current.toggleCount;
            }

            return (state.BeingTriggered, state.IsToggledOn, vk, scan, toggleCount, t.Scope.Recorder.KeyReleases.Count);
        }

        private static List<(bool Being, bool Toggled, int Vk, int Scan, int ToggleCount, int Releases)> Run(
            bool migrated, string triggers, bool toggle, double delay, bool freshPressGuard, DS4State[] frames, bool applyBeforeFrame3 = false)
        {
            using var t = new SpecialActionTestScope(migrated);
            SpecialAction sa = t.Add(new SpecialAction("p8s72_key_" + Guid.NewGuid().ToString("N"), triggers, "Key", KeyA.ToString(), delay,
                (toggle ? "Toggle" : "Press") + "\n"));
            ActionInstanceState state = t.StateOf(sa);
            state.IsToggledOn = false;
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.UntriggeredTimestampTicks = 0;

            var result = new List<(bool, bool, int, int, int, int)>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (applyBeforeFrame3 && i == 3)
                    t.SimulateProfileApply();

                t.RunFrame(frames[i]);
                result.Add(Snapshot(t, sa, KeyA));
            }

            return result;
        }

        [Fact]
        public void Press_Sequence_BothPathsGiveTheSameState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(false) };

            var legacy = Run(false, "L1", false, 0, false, frames);
            var migrated = Run(true, "L1", false, 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Being);      // 比較が「何もしない」同士にならないこと
            Assert.Contains(legacy, f => f.Releases > 0);   // 離したときに、解放が 1 回送られる
        }

        [Fact]
        public void Toggle_Sequence_BothPathsGiveTheSameState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(false), Frame(true), Frame(false) };

            var legacy = Run(false, "L1", true, 0, false, frames);
            var migrated = Run(true, "L1", true, 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Toggled);    // ON の区間がある
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhileTheFreshPressGuardIsOn_NothingFires_OnEitherPath(bool toggle)
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var legacy = Run(false, "L1", toggle, 0, true, frames);
            var migrated = Run(true, "L1", toggle, 0, true, frames);

            Assert.Equal(legacy, migrated);
            Assert.DoesNotContain(legacy, f => f.Being || f.Toggled);
        }

        [Fact]
        public void AndCondition_BothPathsGiveTheSameState()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true), Frame(false, true), Frame(false, false) };

            var legacy = Run(false, "L1/R1", false, 0, false, frames);
            var migrated = Run(true, "L1/R1", false, 0, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Being);
        }

        [Fact]
        public void ADelay_BehavesTheSame_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", false, 60, false, frames);   // 60 秒
            var migrated = Run(true, "L1", false, 60, false, frames);

            // 現行の挙動の固定（W1 の実測）: 遅延の基準時刻の初期値が最小値のため、最初の押下は遅延なしで成立する（新経路も同じ）
            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, f => f.Being);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AProfileApplyWhileHeld_BothPathsGiveTheSameState(bool toggle)
        {
            // 押している最中（フレーム 3 の前）に、適用の後始末（状態の初期化・束縛の差し替え）が入る
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false) };

            var legacy = Run(false, "L1", toggle, 0, false, frames, applyBeforeFrame3: true);
            var migrated = Run(true, "L1", toggle, 0, false, frames, applyBeforeFrame3: true);

            // 解放の回数（Releases）は比べない: 適用の後始末（切断と同じ）で、旧経路は同じキーの解放を 1 回多く送る
            // （Mapping が作る nativeKeyAlias の項目があるため。キーを離す効果は同じ。Phase8-Step7-2-Plan.md の W1 の結果、Q2）
            Assert.Equal(legacy.Select(f => (f.Being, f.Toggled, f.Vk, f.Scan, f.ToggleCount)),
                migrated.Select(f => (f.Being, f.Toggled, f.Vk, f.Scan, f.ToggleCount)));
            Assert.True(migrated[3].Releases >= 1, "押している最中の適用で、キーが解放される");
            Assert.True(legacy[3].Releases >= migrated[3].Releases);
        }

        // ---- Q1: 2 つのキーの同時押し ----

        private static List<(bool First, bool Second)> TwoKeys(bool migrated, bool releaseFirstKeyFirst)
        {
            using var t = new SpecialActionTestScope(migrated);
            SpecialAction first = t.Add(new SpecialAction("p8s72_two_a_" + Guid.NewGuid().ToString("N"), "L1", "Key", "65", 0, "Press\n"));
            SpecialAction second = t.Add(new SpecialAction("p8s72_two_b_" + Guid.NewGuid().ToString("N"), "R1", "Key", "66", 0, "Press\n"));
            foreach (SpecialAction sa in new[] { first, second })
            {
                ActionInstanceState state = t.StateOf(sa);
                state.IsToggledOn = false;
                state.RequiresFreshPressAfterReset = false;
            }

            DS4State[] frames = releaseFirstKeyFirst
                ? new[] { Frame(false, false), Frame(true, true), Frame(false, true), Frame(false, true), Frame(false, false), Frame(false, false) }
                : new[] { Frame(false, false), Frame(true, true), Frame(true, false), Frame(true, false), Frame(false, false), Frame(false, false) };

            var result = new List<(bool, bool)>();
            foreach (DS4State frame in frames)
            {
                t.RunFrame(frame);
                result.Add((t.StateOf(first).BeingTriggered, t.StateOf(second).BeingTriggered));
            }

            return result;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TwoKeysHeldTogether_EachIsReleasedWhenItsOwnTriggerIsReleased(bool releaseFirstKeyFirst)
        {
            var legacy = TwoKeys(false, releaseFirstKeyFirst);
            var migrated = TwoKeys(true, releaseFirstKeyFirst);

            // 新経路: 離したキーだけが解放され、最後はどちらも解放される
            Assert.Equal((true, true), migrated[1]);
            Assert.Equal(releaseFirstKeyFirst ? (false, true) : (true, false), migrated[3]);
            Assert.Equal((false, false), migrated[5]);

            // 旧経路との一致（Q1 の前提の確認）。食い違った場合は、旧経路の癖（UntriggerIndex を 1 つしか持たない）が出ている
            Assert.Equal(legacy, migrated);
        }
    }
}
