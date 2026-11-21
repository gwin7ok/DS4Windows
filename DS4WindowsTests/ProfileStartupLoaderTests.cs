#nullable enable
using System.Collections.Generic;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 起動時の、プロファイルの固定 ID の確定 → SA の読み込み。SA の「切り替え先」は ID で解決するため、この順序を守る。
    /// </summary>
    public class ProfileStartupLoaderTests
    {
        private static (ProfileStartupLoader Loader, List<string> Order) Create(bool loadResult)
        {
            var order = new List<string>();
            var identity = RecordingProxy.Create<IProfileIdentityService>();
            RecordingProxy.Of(identity).OnCall = name => order.Add("identity." + name);
            var actions = RecordingProxy.Create<ISpecialActionRepository>();
            var actionsProxy = RecordingProxy.Of(actions);
            actionsProxy.Returns["LoadActions"] = loadResult;
            actionsProxy.OnCall = name => order.Add("actions." + name);
            return (new ProfileStartupLoader(identity, actions), order);
        }

        [Fact]
        public void TheIdsAreEnsured_BeforeTheActionsAreLoaded()
        {
            var (loader, order) = Create(true);

            loader.Run();

            Assert.Equal(new[] { "identity.EnsureAllPersisted", "actions.LoadActions" }, order);
        }

        [Fact]
        public void WhenTheActionsFileIsMissing_TheStandardActionsAreCreated_AfterTheLoad()
        {
            var (loader, order) = Create(false);

            loader.Run();

            Assert.Equal(new[] { "identity.EnsureAllPersisted", "actions.LoadActions", "actions.CreateStandardActions" }, order);
        }

        [Fact]
        public void RequiresBothServices()
        {
            Assert.Throws<System.ArgumentNullException>(() => new ProfileStartupLoader(null!, RecordingProxy.Create<ISpecialActionRepository>()));
            Assert.Throws<System.ArgumentNullException>(() => new ProfileStartupLoader(RecordingProxy.Create<IProfileIdentityService>(), null!));
        }
    }
}
