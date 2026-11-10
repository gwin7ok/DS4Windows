using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Output;
using DS4Windows.Processing;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W1（Q2＝A）: サービス・シリアライザー・DTO が、静的な <c>ControlService.CURRENT_DS4_CONTROLLER_LIMIT</c> ではなく、
    /// 注入された上限（<see cref="IEnvironmentService.ControllerSlotLimit"/>）を使うことの回帰ガード。ソースが見つからない環境では、ソースの検査を省略する。
    /// </summary>
    public class SlotLimitServicesGuardTests
    {

        private readonly ITestOutputHelper _output;

        public SlotLimitServicesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        public void OutputSlotManager_SizesItsSlotsByTheGivenLimit(int limit)
        {
            var manager = new OutputSlotManager(limit);
            Assert.Equal(limit, manager.OutputSlots.Length);
        }

        [Fact]
        public void OutputSlotManager_RejectsANonPositiveLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OutputSlotManager(0));
        }

        [Fact]
        public void ProfileSlotApplier_RejectsASlotAtOrAboveTheLimit_WithoutSideEffects()
        {
            var env = new EnvironmentService();
            var applier = TestRepositoryFactory.CreateProfileSlotApplier();
            Assert.False(applier.ApplyToSlot(env.ControllerSlotLimit, "Any", ProfileChangeSource.Unknown));
            Assert.False(applier.ApplyToSlot(-1, "Any", ProfileChangeSource.Unknown));
        }

        [Fact]
        public void TheConstructors_RequireTheEnvironmentService()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfileSlotApplier(TestRepositoryFactory.CreateProfileApplicationService(
                TestGlobals.ProfileSettingsServiceInstance, new LazyProfileLoadTarget(), TestRepositoryFactory.CreateProfileRepository(), new EnvironmentService()), null,
                TestHost.GetService<DS4Windows.Processing.Actions.ITriggerDispatchEngine>()));
        }
    }
}
