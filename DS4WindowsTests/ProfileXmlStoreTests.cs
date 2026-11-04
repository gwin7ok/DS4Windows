using System;
using System.IO;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;

namespace DS4WindowsTests
{
    public class ProfileXmlStoreTests
    {
        [Fact]
        public void SaveProfileXml_ShouldReturnTrueOnSuccessfulWrite()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = pathService.AppDataPath;
            }

            string profilesDir = pathService.ProfilesPath;
            if (!Directory.Exists(profilesDir))
            {
                try { Directory.CreateDirectory(profilesDir); } catch { }
            }

            var store = TestRepositoryFactory.CreateProfileXmlStore(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            string profileName = "Phase5Step2_ProfileXmlStore_SaveTest";
            string path = pathService.GetProfilePath(profileName);

            try
            {
                bool result = store.SaveProfileXml(0, profileName);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(path))
                {
                    try { File.Delete(path); } catch { }
                }
            }
        }

        [Fact]
        public void LoadProfileXml_NonExistentPath_ShouldNotThrow()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = pathService.AppDataPath;
            }

            DS4Windows.Composition.AppHost.CreateHost();
            var control = TestHost.GetService<IProfileLoadTarget>();

            var store = TestRepositoryFactory.CreateProfileXmlStore(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));

            var exception = Record.Exception(() =>
            {
                store.LoadProfileXml(0, false, control, @"NonExistent_Phase5Step2_Path.xml", false, true);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void XmlIoLock_ShouldBeSharedStaticObject()
        {
            // Step6(AppSettingsService)が同一ロックを共有できることを保証する回帰テスト
            Assert.NotNull(ProfileXmlStore.XmlIoLock);
        }

        [Fact]
        public void Constructor_WithNullBackingStore_Throws()
        {
            // Phase7-Step3 W1: Global.store へのフォールバックを廃止した
            Assert.Throws<System.ArgumentNullException>(() => new ProfileXmlStore(null, new DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService())), TestRepositoryFactory.CreateAppSettingsXml(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService())), TestRepositoryFactory.CreateLegacyProfileXml(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()))));
        }
    }
}
