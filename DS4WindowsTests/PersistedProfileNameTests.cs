using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 設定ファイルへ保存するスロットのプロファイル名。一時プロファイル（CLI の LoadTempProfile、自動プロファイル）の間は、
    /// 一時の名前ではなく通常のプロファイルを保存する（次回の起動の既定が、一時の設定にならないように）。
    /// </summary>
    public class PersistedProfileNameTests
    {
        private static (BackingStore store, ProfileSettingsService settings) Create()
        {
            var store = new BackingStore(new EnvironmentService());
            var settings = new ProfileSettingsService(store, new EnvironmentService(), new DeviceSlotState());
            return (store, settings);
        }

        [Fact]
        public void WithoutATemporaryProfile_TheSelectedProfileIsSaved()
        {
            var (store, _) = Create();
            store.profilePath[0] = "Normal";
            store.selectedProfile[0] = "Selected";

            Assert.Equal("Selected", store.GetPersistedProfileName(0));
        }

        [Fact]
        public void WithATemporaryProfile_TheNormalProfileIsSaved()
        {
            var (store, settings) = Create();
            store.profilePath[0] = "Normal";
            store.selectedProfile[0] = "Temp";
            settings.SetUseTempProfile(0, true);

            Assert.Equal("Normal", store.GetPersistedProfileName(0));
        }

        [Fact]
        public void WithATemporaryProfileAndNoNormalProfile_TheSelectedProfileIsSaved()
        {
            var (store, settings) = Create();
            store.selectedProfile[0] = "Temp";
            settings.SetUseTempProfile(0, true);

            Assert.Equal("Temp", store.GetPersistedProfileName(0));
        }
    }
}
