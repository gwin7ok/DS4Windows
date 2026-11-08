using System;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-7: Load Profile の切り替え先が空・削除済み（割り当て編集画面で (none) 表示）のとき、ボタンを押しても何もせず、
    /// 離しても元のプロファイルを適用し直さない（以前は、押すと仮想コントローラーが切断され、離すと再適用で再接続した）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class Phase11W6LoadProfileNoTargetTests
    {
        private sealed class StubSwitcher : IProfileSwitcher
        {
            public bool Loadable = true;
            public int Switched, Restored;
            public bool CanSwitch(SpecialAction action) => Loadable;
            public void SwitchProfile(int deviceIndex, SpecialAction action) { Switched++; }
            public void RestoreProfile(int deviceIndex) { Restored++; }
        }

        private static ProfileApplicationService NewService(IProfileIdentityService identity)
        {
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            return TestRepositoryFactory.CreateProfileApplicationService(settings, new LazyProfileLoadTarget(),
                TestRepositoryFactory.CreateProfileRepository(settings), new EnvironmentService(), profileIdentity: identity);
        }

        private static SpecialAction Action(string details, string profileId = null)
        {
            return new SpecialAction("a", "Button", "Profile", details) { profileId = profileId };
        }

        private sealed class FakeIdentity : IProfileIdentityService
        {
            public string NameOfId;   // GetName(any id)
            public string IdOfName;   // GetId(any name)
            public string GetId(string profileName) => IdOfName;
            public string GetName(string profileId) => NameOfId;
            public string AssignNewId(string profileName) => null;
            public System.Collections.Generic.IReadOnlyList<string> EnsureAllPersisted() => new string[0];
            public void Refresh() { }
            public string EnsurePersisted(string profileName) => null;
            public bool TryResolve(string profileId, string nameHint, out string profileName) { profileName = null; return false; }
        }

        [Fact]
        public void EmptyTarget_IsNotLoadable()
        {
            var service = NewService(null);
            Assert.False(service.HasLoadableTarget(Action("")));
            Assert.False(service.HasLoadableTarget(Action(null)));
            Assert.False(service.HasLoadableTarget(null));
        }

        [Fact]
        public void NamedTargetWithoutId_IsLoadable_AsBefore()
        {
            Assert.True(NewService(null).HasLoadableTarget(Action("verifyB")));
        }

        [Fact]
        public void DeletedTarget_IdAndNameBothUnresolved_IsNotLoadable()
        {
            var service = NewService(new FakeIdentity());
            Assert.False(service.HasLoadableTarget(Action("verifyB", "deadbeef")));
        }

        [Fact]
        public void TargetWhoseIdIsStale_ButWhoseNameStillExists_IsLoadable()
        {
            var service = NewService(new FakeIdentity { IdOfName = "someid" });
            Assert.True(service.HasLoadableTarget(Action("verifyB", "staleid")));
        }

        [Fact]
        public void TargetWhoseIdResolves_IsLoadable_AndFollowsTheRename()
        {
            var service = NewService(new FakeIdentity { NameOfId = "verifyB-renamed" });
            var action = Action("verifyB", "id1");
            Assert.True(service.HasLoadableTarget(action));
            Assert.Equal("verifyB-renamed", action.details);
        }

        [Fact]
        public void ProfileSwitchAction_DoesNothing_OnPressAndRelease_WhenThereIsNoTarget()
        {
            var switcher = new StubSwitcher { Loadable = false };
            var action = new ProfileSwitchAction(Action("verifyB", "deadbeef"), 0, switcher);

            action.Execute(null);
            action.Stop(null);

            Assert.Equal(0, switcher.Switched);
            Assert.Equal(0, switcher.Restored);
        }

        [Fact]
        public void ProfileSwitchAction_SwitchesAndRestores_WhenTheTargetIsLoadable()
        {
            var switcher = new StubSwitcher { Loadable = true };
            var action = new ProfileSwitchAction(Action("verifyB"), 0, switcher);

            action.Execute(null);
            action.Stop(null);

            Assert.Equal(1, switcher.Switched);
            Assert.Equal(1, switcher.Restored);
        }
    }
}
