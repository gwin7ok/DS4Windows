using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.5a-W1（Phase8-Step7-5-Plan.md §3 7.5a の W1）: Profile 種別（復帰を持たない「切替のみ」）の特性化。
    /// 前半は、すでに新経路が旧経路と同じ動きをするはずの項目（切替の回数、保持中、ガード中、実行中、AND 条件、押している最中の適用）を、旧・新の両経路で比べる。
    /// 後半は、新経路に **まだ無い** 項目（フレームの打ち切り、トリガーのボタンのキーの解放、連鎖の防止）の、旧経路の挙動の固定。
    /// 後半は W2 で新経路を実装したあと、新経路（<c>InlineData(true)</c>）の検査を足す。
    /// 観察は、<see cref="RecordingProfileApplicationService"/>（<c>InstallProfileRecorder</c>）で、実際のプロファイルの読込・適用は走らせない。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75aW1ProfileSwitchDualPathTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers, double delay = 0)
        {
            // 復帰なし（uTrigger なし、AutomaticUntrigger なし）の切替のみ
            SpecialAction sa = t.Add(new SpecialAction("p8s75_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s75-target", delay, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        /// <summary>入力の列を流し、フレームごとの（累計の切替の回数、累計の復帰の回数）を返す。</summary>
        private static List<(int Applies, int Restores)> Run(bool migrated, string triggers, bool freshPressGuard, bool executing,
            DS4State[] frames, double delay = 0, int applyBeforeFrame = -1)
        {
            using var t = new SpecialActionTestScope(migrated);
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, triggers, delay);
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = executing;

            var result = new List<(int, int)>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (i == applyBeforeFrame)
                    t.SimulateProfileApply();

                t.RunFrame(frames[i]);
                result.Add((recorder.ApplyFromActionCalls.Count, recorder.RestoreFromActionCalls.Count));
            }

            return result;
        }

        // ---- 前半: 旧・新の両経路 ----

        [Fact]
        public void Sequence_BothPathsSwitchTheSameNumberOfTimes()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false) };

            var legacy = Run(false, "L1", false, false, frames);
            var migrated = Run(true, "L1", false, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(1, legacy.Last().Applies);      // 押して 1 回だけ切り替える（保持中は再び切り替えない）
            Assert.Equal(0, legacy.Last().Restores);     // 復帰なしの切替は、離しても復帰しない
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingSwitches_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var legacy = Run(false, "L1", true, false, frames);
            var migrated = Run(true, "L1", true, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(0, legacy.Last().Applies);
        }

        [Fact]
        public void WhileExecuting_NothingSwitches_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", false, true, frames);
            var migrated = Run(true, "L1", false, true, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(0, legacy.Last().Applies);
        }

        [Fact]
        public void AndCondition_BothPathsSwitchTheSameNumberOfTimes()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true), Frame(false, true), Frame(false, false) };

            var legacy = Run(false, "L1/R1", false, false, frames);
            var migrated = Run(true, "L1/R1", false, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(1, legacy.Last().Applies);
        }

        [Fact]
        public void AProfileApplyWhileHeld_BothPathsGiveTheSameCounts()
        {
            // 押している最中（フレーム 3 の前）に、適用の後始末が入る（再現。記録用の実装は、適用を呼ばない）
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false) };

            var legacy = Run(false, "L1", false, false, frames, applyBeforeFrame: 3);
            var migrated = Run(true, "L1", false, false, frames, applyBeforeFrame: 3);

            Assert.Equal(legacy, migrated);
        }

        [Fact]
        public void ADelay_BehavesTheSame_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", false, false, frames, delay: 60);
            var migrated = Run(true, "L1", false, false, frames, delay: 60);

            Assert.Equal(legacy, migrated);
        }

        // ---- 後半: 新経路に、まだ無い項目（旧経路の挙動の固定。W2 で新経路を足す）----

        [Theory]
        [InlineData(false)]
        public void AfterTheSwitch_TheRestOfTheFrameIsNotProcessed(bool migrated)
        {
            // 切替（Intent-Decisions 項目 5）のあと、同じフレームの後ろの特殊アクションは処理されない（旧ループの return）。
            // 後ろの Key は、次のフレーム（切替が済んでいる）で処理される。
            using var t = new SpecialActionTestScope(migrated);
            t.InstallProfileRecorder().SimulateStateReset = false;   // 状態の掃除をしない（Key の状態を、そのまま観察する）
            AddProfile(t, "L1");
            SpecialAction key = t.AddKey("L1", toggle: false);
            t.StateOf(key).RequiresFreshPressAfterReset = false;

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            bool afterTheSwitchFrame = ActionManager.IsBeingTriggered(key, t.Device);
            t.RunFrame(Frame(true));
            bool nextFrame = ActionManager.IsBeingTriggered(key, t.Device);

            Assert.False(afterTheSwitchFrame);   // 切替のフレームでは、Key は処理されない
            Assert.True(nextFrame);              // 次のフレームで、処理される
        }

        [Theory]
        [InlineData(false)]
        public void TheKeysOfTheTriggerButtonsOwnAssignment_AreReleasedBeforeTheSwitch(bool migrated)
        {
            // トリガーのボタン（L1）の通常の割り当てがキーなら、切替の前にそのキーを離す
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(MappingCharacterizationScope.Device, DS4Controls.L1);
            try
            {
                dcs.Reset();
                dcs.actionType = DS4ControlSettings.ActionType.Key;
                dcs.action.actionKey = 65;

                using var t = new SpecialActionTestScope(migrated);
                RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
                AddProfile(t, "L1");
                int before = t.Scope.Recorder.KeyReleases.Count;

                t.RunFrame(Frame(true));

                Assert.Single(recorder.ApplyFromActionCalls);
                Assert.Equal(1, t.Scope.Recorder.KeyReleases.Count - before);
            }
            finally
            {
                dcs.Reset();
            }
        }

        [Theory]
        [InlineData(false)]
        public void WhileATemporaryProfileHasAPendingProfileUntrigger_AnotherSwitchDoesNotStart(bool migrated)
        {
            // 連鎖の防止: 一時プロファイルの間、復帰の合図が Profile なら、別の Profile 切替を始めない
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            using var t = new SpecialActionTestScope(migrated);
            bool savedTemp = settings.GetUseTempProfile(t.Device);
            try
            {
                RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
                AddProfile(t, "L1");
                // 復帰の合図（別の Profile の特殊アクション。解除トリガー R1 は押されないので、合図は残り続ける）
                var pending = new SpecialAction("p8s75_pending", "Cross", "Profile", "p8s75-other", 0, "R1");
                t.Mapping.RestoreProfileUntrigger(t.Device, new ProfileUntriggerSnapshot(pending, 0));
                settings.SetUseTempProfile(t.Device, true);

                t.RunFrame(Frame(true));
                t.RunFrame(Frame(true));

                Assert.Empty(recorder.ApplyFromActionCalls);   // 切り替えない

                settings.SetUseTempProfile(t.Device, false);
                t.RunFrame(Frame(false));
                t.RunFrame(Frame(true));

                Assert.Single(recorder.ApplyFromActionCalls);  // 一時プロファイルでなければ、切り替える
            }
            finally
            {
                settings.SetUseTempProfile(t.Device, savedTemp);
            }
        }
    }
}
