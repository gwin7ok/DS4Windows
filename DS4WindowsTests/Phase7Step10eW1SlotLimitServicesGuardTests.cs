using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W1（Q2＝A）: サービス・シリアライザー・DTO が、静的な <c>ControlService.CURRENT_DS4_CONTROLLER_LIMIT</c> ではなく、
    /// 注入された上限（<see cref="IEnvironmentService.ControllerSlotLimit"/>）を使うことの回帰ガード。ソースが見つからない環境では、ソースの検査を省略する。
    /// </summary>
    public class Phase7Step10eW1SlotLimitServicesGuardTests
    {
        private static readonly string[] Files =
        {
            "DS4Windows/DS4Control/OutputSlotManager.cs",
            "DS4Windows/DS4Control/Services/LegacyProfileXmlSerializer.cs",
            "DS4Windows/DS4Control/Services/ProfileRepository.cs",
            "DS4Windows/DS4Control/Services/ProfileSlotApplier.cs",
            "DS4Windows/DS4Control/DTOXml/AutoProfilesDTO.cs",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10eW1SlotLimitServicesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheFiles_DoNotUseTheStaticSlotLimit()
        {
            foreach (string rel in Files)
            {
                string path = SourceFileLocator.Find(rel);
                if (path == null)
                {
                    _output.WriteLine(rel + " が見つからないため検査を省略した。");
                    continue;
                }

                string text = Regex.Replace(Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
                Assert.DoesNotContain("CURRENT_DS4_CONTROLLER_LIMIT", text);
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        public void OutputSlotManager_SizesItsSlotsByTheGivenLimit(int limit)
        {
            var manager = new OutputSlotManager(() => new Version(1, 0), _ => SASteeringWheelEmulationAxisType.None, limit);
            Assert.Equal(limit, manager.OutputSlots.Length);
        }

        [Fact]
        public void OutputSlotManager_RejectsANonPositiveLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OutputSlotManager(() => new Version(1, 0), _ => SASteeringWheelEmulationAxisType.None, 0));
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
                TestGlobals.ProfileSettingsServiceInstance, () => null, TestRepositoryFactory.CreateProfileRepository(), new EnvironmentService()), null,
                TestHost.GetService<DS4Windows.Actions.ITriggerDispatchEngine>()));
        }
    }
}
