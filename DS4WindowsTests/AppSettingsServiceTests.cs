using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class AppSettingsServiceTests
    {
        public AppSettingsServiceTests()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = pathService.AppDataPath;
            }
        }

        private class MockProfileXmlStore : IProfileXmlStore
        {
            public bool LoadAppSettingsResult { get; set; } = true;
            public bool SaveAppSettingsResult { get; set; } = true;
            public int LoadCallCount { get; private set; }
            public int SaveCallCount { get; private set; }

            public bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
                string overridePath = "", bool xinputChange = true, bool postLoad = true) => true;

            public bool SaveProfileXml(int deviceIndex, string profileName) => true;

            // Phase6-Step2-2 (PR-2) / Phase6-Step3-1: IProfileXmlStore の拡張に追随（本テストでは使用しない）
            public bool LoadControllerConfigsForDevice(DS4Device device) => true;
            public bool SaveControllerConfigsForDevice(DS4Device device) => true;

            public bool LoadAppSettingsXml()
            {
                LoadCallCount++;
                return LoadAppSettingsResult;
            }

            public bool SaveAppSettingsXml()
            {
                SaveCallCount++;
                return SaveAppSettingsResult;
            }
        }

        [Fact]
        public void Save_DelegatesToXmlStore_ReturnsTrueOnSuccess()
        {
            var mockXmlStore = new MockProfileXmlStore { SaveAppSettingsResult = true };
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Save_DelegatesToXmlStore_ReturnsFalseOnFailure()
        {
            var mockXmlStore = new MockProfileXmlStore { SaveAppSettingsResult = false };
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.False(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Load_DelegatesToXmlStore_ReturnsTrueOnSuccess()
        {
            var mockXmlStore = new MockProfileXmlStore { LoadAppSettingsResult = true };
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Load();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.LoadCallCount);
        }

        [Fact]
        public void Properties_SetAndGet_FiresSettingChangedEvent()
        {
            var mockXmlStore = new MockProfileXmlStore();
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            // StartMinimized
            service.StartMinimized = true;
            Assert.Equal(nameof(service.StartMinimized), changedProperty);
            Assert.True(service.StartMinimized);

            // CloseMinimizes
            changedProperty = null;
            service.CloseMinimizes = true;
            Assert.Equal(nameof(service.CloseMinimizes), changedProperty);
            Assert.True(service.CloseMinimizes);

            // CheckWhen
            changedProperty = null;
            service.CheckWhen = 24;
            Assert.Equal(nameof(service.CheckWhen), changedProperty);
            Assert.Equal(24, service.CheckWhen);

            // UdpServerPort
            changedProperty = null;
            service.UdpServerPort = 26761;
            Assert.Equal(nameof(service.UdpServerPort), changedProperty);
            Assert.Equal(26761, service.UdpServerPort);
        }

        [Fact]
        public void GlobalShim_SaveAndLoad_ShouldSynchronizeWithService()
        {
            DS4WinWPF.AppHost.CreateHost();
            var service = DS4WinWPF.AppHost.GetService<IAppSettingsService>();

            Assert.NotNull(service);

            var saveEx = Record.Exception(() => service.Save());
            Assert.Null(saveEx);

            // Phase7-Step4 W2: Global.Load は削除した。サービスの Load が同じ処理（旧 Global.Load の中身）
            var loadEx = Record.Exception(() => service.Load());
            Assert.Null(loadEx);
        }

        // Phase5-Step13-9 (回帰テスト): StartMinimized/MinimizeToTaskbar/CloseMinimizes/
        // UseUdpServer/UdpServerPort/UdpServerListenAddress/UseExclusiveModeは、かつて独立した
        // privateフィールドを持ち、Global側の実体と一切連動しない「孤立重複状態」バグがあった
        // （Step13-7調査時に発覚、根本修正済み）。このテストは、サービス経由の書き込みが
        // 実際にGlobal側の同一実体に反映されることを検証し、同種の回帰を防ぐ。
        [Fact]
        public void StartMinimized_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = TestGlobals.Store.startMinimized;
            try
            {
                service.StartMinimized = !original;
                Assert.Equal(!original, TestGlobals.Store.startMinimized);

                TestGlobals.Store.startMinimized = original;
                Assert.Equal(original, service.StartMinimized);
            }
            finally
            {
                TestGlobals.Store.startMinimized = original;
            }
        }

        [Fact]
        public void MinimizeToTaskbar_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = TestGlobals.Store.minToTaskbar;
            try
            {
                service.MinimizeToTaskbar = !original;
                Assert.Equal(!original, TestGlobals.Store.minToTaskbar);
            }
            finally
            {
                TestGlobals.Store.minToTaskbar = original;
            }
        }

        [Fact]
        public void CloseMinimizes_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = TestGlobals.Store.closeMini;
            try
            {
                service.CloseMinimizes = !original;
                Assert.Equal(!original, TestGlobals.Store.closeMini);
            }
            finally
            {
                TestGlobals.Store.closeMini = original;
            }
        }

        [Fact]
        public void UseExclusiveMode_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = TestGlobals.Store.useExclusiveMode;
            try
            {
                service.UseExclusiveMode = !original;
                Assert.Equal(!original, TestGlobals.Store.useExclusiveMode);
            }
            finally
            {
                TestGlobals.Store.useExclusiveMode = original;
            }
        }

        [Fact]
        public void UdpServerSettings_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool originalUse = TestGlobals.Store.useUDPServ;
            int originalPort = TestGlobals.Store.udpServPort;
            string originalAddr = TestGlobals.Store.udpServListenAddress;
            try
            {
                service.UseUdpServer = !originalUse;
                Assert.Equal(!originalUse, TestGlobals.Store.useUDPServ);

                service.UdpServerPort = 27000;
                Assert.Equal(27000, TestGlobals.Store.udpServPort);

                service.UdpServerListenAddress = "192.168.1.1";
                Assert.Equal("192.168.1.1", TestGlobals.Store.udpServListenAddress);
            }
            finally
            {
                TestGlobals.Store.useUDPServ = originalUse;
                TestGlobals.Store.udpServPort = originalPort;
                TestGlobals.Store.udpServListenAddress = originalAddr;
            }
        }

        // Phase5-Step13-7 で追加されたウィンドウ位置・サイズ・列幅の永続化プロパティ。
        // 実体はGlobal.FormWidth等(m_Configへの薄い公開アクセサ)であり、全10種のColWidthは
        // 同一パターンのため代表として3種のみ検証する。
        [Fact]
        public void FormGeometry_SetAndGet_FiresSettingChangedEvent()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalWidth = TestGlobals.Store.formWidth;
            int originalHeight = TestGlobals.Store.formHeight;
            int originalX = TestGlobals.Store.formLocationX;
            int originalY = TestGlobals.Store.formLocationY;
            try
            {
                service.FormWidth = 1024;
                Assert.Equal(nameof(service.FormWidth), changedProperty);
                Assert.Equal(1024, TestGlobals.Store.formWidth);

                changedProperty = null;
                service.FormHeight = 768;
                Assert.Equal(nameof(service.FormHeight), changedProperty);
                Assert.Equal(768, TestGlobals.Store.formHeight);

                changedProperty = null;
                service.FormLocationX = 100;
                Assert.Equal(nameof(service.FormLocationX), changedProperty);
                Assert.Equal(100, TestGlobals.Store.formLocationX);

                changedProperty = null;
                service.FormLocationY = 200;
                Assert.Equal(nameof(service.FormLocationY), changedProperty);
                Assert.Equal(200, TestGlobals.Store.formLocationY);
            }
            finally
            {
                TestGlobals.Store.formWidth = originalWidth;
                TestGlobals.Store.formHeight = originalHeight;
                TestGlobals.Store.formLocationX = originalX;
                TestGlobals.Store.formLocationY = originalY;
            }
        }

        [Fact]
        public void ControllerListColumnWidths_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            int originalIndex = TestGlobals.Store.controllerIndexColWidth;
            int originalStatus = TestGlobals.Store.controllerStatusColWidth;
            int originalBattery = TestGlobals.Store.controllerBatteryColWidth;
            try
            {
                service.ControllerIndexColWidth = 55;
                Assert.Equal(55, TestGlobals.Store.controllerIndexColWidth);

                service.ControllerStatusColWidth = 66;
                Assert.Equal(66, TestGlobals.Store.controllerStatusColWidth);

                service.ControllerBatteryColWidth = 77;
                Assert.Equal(77, TestGlobals.Store.controllerBatteryColWidth);
            }
            finally
            {
                TestGlobals.Store.controllerIndexColWidth = originalIndex;
                TestGlobals.Store.controllerStatusColWidth = originalStatus;
                TestGlobals.Store.controllerBatteryColWidth = originalBattery;
            }
        }

        [Fact]
        public void Notifications_SwipeProfiles_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalNotif = Global.Notifications;
            bool originalSwipe = TestGlobals.Store.swipeProfiles;
            try
            {
                // デフォルト値(2)と衝突しないよう、元の値と異なる値を明示的に選ぶ
                int newNotif = originalNotif == 1 ? 0 : 1;
                service.Notifications = newNotif;
                Assert.Equal(nameof(service.Notifications), changedProperty);
                Assert.Equal(newNotif, Global.Notifications);

                changedProperty = null;
                service.SwipeProfiles = !originalSwipe;
                Assert.Equal(nameof(service.SwipeProfiles), changedProperty);
                Assert.Equal(!originalSwipe, TestGlobals.Store.swipeProfiles);
            }
            finally
            {
                Global.Notifications = originalNotif;
                TestGlobals.Store.swipeProfiles = originalSwipe;
            }
        }
    }
}