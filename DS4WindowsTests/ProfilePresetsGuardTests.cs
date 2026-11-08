using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W6（Q1・Q6＝A）: プロファイルのプリセット（空のプロファイルと既定の割り当て）の読込を <c>BackingStore</c> から
    /// <c>ProfilePresets</c> へ移したことの回帰ガード。読込後の状態の同一性は <c>Phase7Step10cGoldenPersistenceTests.Presets_LoadedState_MatchesGolden</c>
    /// （10 種）が固定する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class Phase7Step10cW6ProfilePresetsGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "LoadBlankDS4Profile", "LoadBlankProfile", "LoadDefaultGamepadGyroProfile", "LoadDefaultDS4GamepadGyroProfile",
            "LoadDefaultMixedGyroMouseProfile", "LoadDefaultDS4MixedGyroMouseProfile", "LoadDefaultDS4MixedControlsProfile",
            "LoadDefaultMixedControlsProfile", "LoadDefaultKBMProfile", "LoadDefaultKBMGyroMouseProfile", "PrepareBlankingProfile",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10cW6ProfilePresetsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresThePresetMethods()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            foreach (string moved in MovedMethods)
                Assert.DoesNotContain(moved, names);
        }

        [Fact]
        public void ProfilePresets_HasTheTenPresets_AndRequiresTheStore()
        {
            foreach (string name in MovedMethods.Where(n => n != "PrepareBlankingProfile"))
                Assert.NotNull(typeof(ProfilePresets).GetMethod(name, BindingFlags.Instance | BindingFlags.Public));

            Assert.Throws<ArgumentNullException>(() => new ProfilePresets(null));
        }

        [Fact]
        public void TheLoader_UsesThePresets_AndRequiresThem()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfilePresetLoader(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()), TestGlobals.ProfileSettingsServiceInstance, null));

            DS4Windows.Composition.AppHost.CreateHost();
            Assert.Same(TestHost.GetService<ProfilePresets>(), TestHost.GetService<ProfilePresets>());
            Assert.NotNull(TestHost.GetService<IProfilePresetLoader>());
        }
    }
}
