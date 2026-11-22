#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイルの保存・適用の後の再適用（<see cref="ProfileSelectionCoordinator.SyncProfileListAndControllers"/>）は、
    /// コントローラーが接続されているスロットにだけ行う。サービスの停止中は、<c>ProfilePath</c> が同じ名前のまま残るため、
    /// 接続していないスロットにも適用され、無意味なログ（<c>Controller 1 is now using Profile … (Battery: N/A%)</c>）と通知が出ていた。
    /// 偽の接続の一覧と、呼び出しを記録するスロット適用役を渡して、挙動で確かめる。
    /// </summary>
    public class ReapplyConnectedSlotsOnlyTests : IDisposable
    {
        private sealed class FakeSlotQuery : IControllerSlotQuery
        {
            private readonly HashSet<int> _connected;
            private readonly IControllerSlotView _view = RecordingProxy.Create<IControllerSlotView>();
            public FakeSlotQuery(params int[] connected) { _connected = new HashSet<int>(connected); }
            public int SlotCount => ControllerLimits.MaxControllerCount;
            public IControllerSlotView? GetSlot(int slot) => _connected.Contains(slot) ? _view : null;
            public IReadOnlyList<ControllerSlotEntry>? Snapshot() => null;
            public event EventHandler<ControllerSlotChangedEventArgs> SlotChanged { add { } remove { } }
        }

        private readonly string _dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Reapply_" + Guid.NewGuid().ToString("N"));

        public ReapplyConnectedSlotsOnlyTests()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_dir, "Profiles"));
        }

        public void Dispose()
        {
            try { System.IO.Directory.Delete(_dir, true); } catch (System.IO.IOException) { /* 一時フォルダーの後始末の失敗は無視してよい */ }
        }

        private (ProfileSelectionCoordinator Coordinator, RecordingProxy Applier, int SlotCount, ProfileSlotNames Paths) Create(params int[] connected)
        {
            var env = TestHost.GetService<IEnvironmentService>();
            var paths = new ProfileSlotNames(env.ControllerSlotLimit);
            var repo = RecordingProxy.Create<IProfileRepository>();
            RecordingProxy.Of(repo).Returns["get_ProfilePath"] = paths;
            var applier = RecordingProxy.Create<IProfileSlotApplier>();
            RecordingProxy.Of(applier).Returns["ApplyToSlot"] = true;
            var coordinator = new ProfileSelectionCoordinator(
                TestHost.GetService<DS4Windows.UI.Abstractions.IEditorWindowFactory>(),
                repo, applier,
                TestHost.GetService<IAppSettingsService>(), env,
                new FakeSlotQuery(connected),
                new TempPathService(_dir), new FakeDialogService(), TestHost.GetService<IProfileIdentityService>());
            return (coordinator, RecordingProxy.Of(applier), env.ControllerSlotLimit, paths);
        }

        private void Sync(ProfileSelectionCoordinator coordinator)
        {
            coordinator.SyncProfileListAndControllers(new DS4Windows.Core.Profiles.ProfileList(new TempPathService(_dir)), () => { });
        }

        [Fact]
        public void TheReapplyLoop_AppliesOnlyToConnectedSlots()
        {
            var rig = Create(1, 3);
            rig.Paths[0] = "A";
            rig.Paths[1] = "A";
            rig.Paths[2] = "A";
            rig.Paths[3] = "A";

            Sync(rig.Coordinator);

            var applied = rig.Applier.ArgsOf("ApplyToSlot").Select(a => (int)a[0]!).OrderBy(i => i).ToList();
            Assert.Equal(new[] { 1, 3 }, applied);   // 接続していない 0・2 には適用しない
        }

        [Fact]
        public void TheReapplyLoop_AppliesNothing_WhenNoControllerIsConnected()
        {
            var rig = Create();
            rig.Paths[0] = "A";
            rig.Paths[1] = "B";

            Sync(rig.Coordinator);

            Assert.Empty(rig.Applier.ArgsOf("ApplyToSlot"));
        }

        [Fact]
        public void TheReapplyLoop_AppliesTheSlotsOwnProfile_AsAManualChange()
        {
            var rig = Create(0, 1);
            rig.Paths[0] = "A";
            rig.Paths[1] = "B";

            Sync(rig.Coordinator);

            var calls = rig.Applier.ArgsOf("ApplyToSlot").OrderBy(a => (int)a[0]!).ToList();
            Assert.Equal("A", calls[0][1]);
            Assert.Equal("B", calls[1][1]);
            Assert.All(calls, a => Assert.Equal(ProfileChangeSource.Manual, a[2]));
        }
    }
}
