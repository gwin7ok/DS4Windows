using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step9: <c>ControlService.running</c>／<c>suspending</c>（<c>IServiceLifecycle</c> への転送）を削除したことのガード。
    /// 利用者（App／MainWindow／TrayIconViewModel／AutoProfileService）は <c>IServiceLifecycle.Running</c>／<c>Suspending</c> を直接使う。
    /// </summary>
    public class Phase13Step9RunningForwardRemovalGuardTests
    {
        [Theory]
        [InlineData("running")]
        [InlineData("suspending")]
        public void ControlService_NoLongerExposesTheForward(string name)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Assert.DoesNotContain(typeof(ControlService).GetMembers(all), m => m.Name == name);
        }

        [Fact]
        public void TheLifecycle_StillHoldsTheState()
        {
            var lifecycle = TestHost.GetService<DS4Windows.Processing.Abstractions.IServiceLifecycle>();
            Assert.NotNull(lifecycle);
            bool before = lifecycle.Suspending;
            lifecycle.Suspending = !before;
            Assert.Equal(!before, lifecycle.Suspending);
            lifecycle.Suspending = before;
        }
    }
}
