using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step15（決定 D12）: 旧 静的クラス <c>StartupMethods</c>（ルート直下）を、<see cref="IStartupEntryService"/> の実装
    /// <see cref="StartupEntryService"/>（Core/Platform、DI の Singleton）にした。設定画面は契約だけを受ける。
    /// </summary>
    public class StartupEntryServiceTests
    {
        [Fact]
        public void TheContainer_ResolvesTheServiceAsASingleton()
        {
            var first = TestHost.GetService<IStartupEntryService>();
            Assert.IsType<StartupEntryService>(first);
            Assert.Same(first, TestHost.GetService<IStartupEntryService>());
        }

        [Fact]
        public void SettingsViewModel_RequiresTheStartupEntryService()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                TestRepositoryFactory.CreateAppSettingsService(store: new BackingStore(new EnvironmentService())),
                TestRepositoryFactory.CreateAppearanceSettingsService(),
                new EnvironmentService(),
                new PathService(),
                new LogSettingsApplierProxy(),
                null));
        }

        [Fact]
        public void TheOldStaticClass_IsGone()
        {
            Assert.Null(typeof(StartupEntryService).Assembly.GetType("DS4Windows.StartupMethods"));
            Assert.False(typeof(StartupEntryService).IsAbstract && typeof(StartupEntryService).IsSealed);   // static class ではない
        }
    }
}
