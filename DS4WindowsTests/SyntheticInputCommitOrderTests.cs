#nullable enable
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Conditioning;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 合成入力の確定（<see cref="SyntheticInputCommitter.Commit"/>）の順序を、呼び出しの記録で確かめる。
    /// 集計 → 出力 → 全体の前回値の保存（書き込みロックの中）→ ロックの解放 → デバイスの前回値の保存 → Sync（最後）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class SyntheticInputCommitOrderTests
    {
        /// <summary>Sync が呼ばれた時点の、デバイスの前回値（左クリック）と、書き込みロックの保持（最後に呼ばれたときの値）。</summary>
        private static (int PreviousLeft, bool LockHeld) _atSync;

        private static (SyntheticInputCommitter Committer, SyntheticInputState State, List<string> Calls) Create()
        {
            var calls = new List<string>();
            var state = new SyntheticInputState();
            var kbm = RecordingProxy.Create<IVirtualKBM>();
            RecordingProxy.Of(kbm).OnCall = name =>
            {
                calls.Add(name);
                if (name == "Sync") _atSync = (state.DeviceState[0].previousClicks.leftCount, state.SyncStateLock.IsWriteLockHeld);
            };
            return (new SyntheticInputCommitter(state, kbm), state, calls);
        }

        [Fact]
        public void AClick_IsSentOnce_AndTheSyncComesLast()
        {
            using var scope = new MappingCharacterizationScope();
            var (committer, state, calls) = Create();
            state.CountClick(0, Click.Left);

            committer.Commit(0, scope.Settings);

            Assert.Equal(new[] { "PerformMouseButtonEvent", "Sync" }, calls);
        }

        [Fact]
        public void TheStates_AreSavedAsPrevious_SoTheSameClickIsNotSentAgain_AndTheLockIsReleased()
        {
            using var scope = new MappingCharacterizationScope();
            var (committer, state, calls) = Create();
            state.CountClick(0, Click.Left);
            committer.Commit(0, scope.Settings);
            calls.Clear();

            state.CountClick(0, Click.Left);   // 押しっぱなし（毎フレーム数え直す）。新しい遷移はない
            committer.Commit(0, scope.Settings);

            Assert.Equal(new[] { "Sync" }, calls);
            Assert.False(state.SyncStateLock.IsWriteLockHeld);
        }

        [Fact]
        public void AReleaseAfterAPress_SendsTheUp_ThenSyncs()
        {
            using var scope = new MappingCharacterizationScope();
            var (committer, state, calls) = Create();
            state.CountClick(0, Click.Left);
            committer.Commit(0, scope.Settings);
            calls.Clear();

            committer.Commit(0, scope.Settings);   // 数え直さない = 離した（デバイスの現在値は確定のたびに空になる）

            Assert.Equal(new[] { "PerformMouseButtonEvent", "Sync" }, calls);
        }

        [Fact]
        public void WhenTheSyncIsSent_TheDeviceStateIsAlreadySaved_AndTheLockIsAlreadyReleased()
        {
            using var scope = new MappingCharacterizationScope();
            var (committer, state, _) = Create();
            state.CountClick(0, Click.Left);

            committer.Commit(0, scope.Settings);

            Assert.Equal(1, _atSync.PreviousLeft);
            Assert.False(_atSync.LockHeld);
        }

        [Fact]
        public void WithNoInput_OnlyTheSyncIsSent()
        {
            using var scope = new MappingCharacterizationScope();
            var (committer, _, calls) = Create();

            committer.Commit(0, scope.Settings);

            Assert.Equal(new[] { "Sync" }, calls);
        }
    }
}
