using System;
using System.Linq;
using DS4Windows.UI;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// メイン画面から責務ごと移した 2 つの部品（<see cref="TrayAppearanceController"/>、<see cref="ProfileSelectionCoordinator"/>）が
    /// コンテナから解決でき、窓がそれを受けて、引数が 15 以下であることを固定する。
    /// </summary>
    public class MainWindowResponsibilityPartsTests
    {
        [Fact]
        public void BothParts_AreSingletonsResolvedFromTheHost()
        {
            Assert.Same(TestHost.GetService<TrayAppearanceController>(), TestHost.GetService<TrayAppearanceController>());
            Assert.Same(TestHost.GetService<ProfileSelectionCoordinator>(), TestHost.GetService<ProfileSelectionCoordinator>());
        }

        [Fact]
        public void MainWindow_TakesTheTwoPartsAndAtMostFifteenParameters()
        {
            var types = typeof(MainWindow).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToList();
            Assert.Contains(typeof(TrayAppearanceController), types);
            Assert.Contains(typeof(ProfileSelectionCoordinator), types);
            Assert.True(types.Count <= 15);
        }

        [Fact]
        public void Parts_RejectNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new TrayAppearanceController(null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ProfileSelectionCoordinator(null, null, null, null, null, null));
        }
    }
}
