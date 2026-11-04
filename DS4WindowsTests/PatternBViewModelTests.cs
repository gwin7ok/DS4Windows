using System;
using Xunit;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    public class PatternBViewModelTests
    {
        [Fact]
        public void AppHost_ShouldResolve_MainWindowsViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var vm = TestHost.GetService<MainWindowsViewModel>();
            Assert.NotNull(vm);
        }

        [Fact]
        public void MainWindowsViewModel_ShouldBeSingleton()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var vm1 = TestHost.GetService<MainWindowsViewModel>();
            var vm2 = TestHost.GetService<MainWindowsViewModel>();

            Assert.NotNull(vm1);
            Assert.NotNull(vm2);
            Assert.Same(vm1, vm2);
        }
    }
}
