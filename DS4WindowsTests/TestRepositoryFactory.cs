using System;
using DS4WinWPF;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-2: 必須引数化したサービスのテスト用の生成ヘルパー。
    /// 既定では本番と同じく Global.store と PathService（Global.appdatapath をオンデマンドで参照）を渡す。
    /// </summary>
    internal static class TestRepositoryFactory
    {
        public static SpecialActionRepository CreateSpecialActionRepository(BackingStore store = null, IPathService pathService = null)
        {
            return new SpecialActionRepository(store ?? Global.store, pathService ?? new PathService());
        }

        public static ProfileRepository CreateProfileRepository(IProfileSettingsService settings = null,
            IProfileXmlStore xmlStore = null, IPathService pathService = null, Func<ControlService> control = null,
            BackingStore store = null)
        {
            return new ProfileRepository(
                store ?? Global.store,
                settings ?? Global.ProfileSettingsServiceInstance,
                xmlStore ?? Global.ProfileXmlStoreInstance,
                pathService ?? new PathService(),
                control ?? new Func<ControlService>(() => DS4WinWPF.AppHost.GetService<ControlService>()));
        }

        public static ProfileSettingsService CreateProfileSettingsService(BackingStore store = null,
            IEnvironmentService environmentService = null, Func<ControlService> control = null)
        {
            return new ProfileSettingsService(store ?? Global.store,
                environmentService ?? new EnvironmentService(),
                control ?? new Func<ControlService>(() => Program.rootHub));
        }

        public static DefaultActionFactory CreateDefaultActionFactory()
        {
            return new DefaultActionFactory(new DefaultMacroPlayer(new MockVirtualKBM()),
                new DefaultProcessLauncher(), new DefaultProfileSwitcher());
        }

        public static OutputSlotManager CreateOutputSlotManager()
        {
            return new OutputSlotManager(() => Global.vigemBusVersionInfo);
        }

        public static OutputSlotService CreateOutputSlotService(OutputSlotManager manager = null,
            IOutputSlotStore store = null, ControlService control = null)
        {
            // 従来（生成時に Program.rootHub から取得）と同じく、生成時点の値を保持する
            ControlService ctl = control ?? Program.rootHub;
            OutputSlotManager mgr = manager ?? ctl?.OutputslotMan ?? CreateOutputSlotManager();
            return new OutputSlotService(() => mgr,
                store ?? new OutputSlotStore(new PathService()),
                () => ctl);
        }

        public static ProfileActionProvider CreateProfileActionProvider(BackingStore store = null)
        {
            return new ProfileActionProvider(store ?? Global.store);
        }

        public static AppSettingsService CreateAppSettingsService(IProfileXmlStore xmlStore = null,
            IPathService pathService = null, BackingStore store = null)
        {
            return new AppSettingsService(store ?? Global.store, xmlStore, pathService);
        }

        public static AutoProfileHolder CreateAutoProfileHolder(IPathService pathService = null)
        {
            return new AutoProfileHolder(pathService ?? new PathService());
        }

        public static AutoProfileService CreateAutoProfileService(AutoProfileHolder holder,
            IProfileApplicationService profileAppService, IProfileSettingsService profileSettings,
            IProcessInspector processInspector, IEnvironmentService environmentService = null,
            IAppSettingsService appSettings = null, IProfileRepository profileRepository = null,
            Func<ControlService> control = null, BackingStore store = null)
        {
            return new AutoProfileService(holder, profileAppService, profileSettings, processInspector,
                environmentService ?? new EnvironmentService(),
                appSettings ?? CreateAppSettingsService(),
                profileRepository ?? CreateProfileRepository(profileSettings),
                control ?? new Func<ControlService>(() => Program.rootHub),
                store ?? Global.store);
        }

        public static ProfilePresetLoader CreateProfilePresetLoader(BackingStore store = null,
            IProfileSettingsService profileSettings = null)
        {
            return new ProfilePresetLoader(store ?? Global.store, profileSettings ?? Global.ProfileSettingsServiceInstance);
        }

        public static ProfileApplicationService CreateProfileApplicationService(IProfileSettingsService settings,
            IDeviceStateService deviceState, Func<ControlService> control, IProfileRepository profileRepository,
            IEnvironmentService environmentService, IProfileXmlStore xmlStore = null,
            IProfileActionProvider actionProvider = null, BackingStore store = null)
        {
            return new ProfileApplicationService(settings, deviceState, control, profileRepository, environmentService,
                xmlStore ?? Global.ProfileXmlStoreInstance,
                actionProvider ?? CreateProfileActionProvider(store ?? Global.store),
                store ?? Global.store);
        }

        public static AppearanceSettingsService CreateAppearanceSettingsService(BackingStore store = null)
        {
            return new AppearanceSettingsService(store ?? Global.store);
        }

        public static DeviceStateService CreateDeviceStateService(BackingStore store = null)
        {
            return new DeviceStateService(store ?? Global.store);
        }

        public static AppNotificationService CreateAppNotificationService(BackingStore store = null)
        {
            return new AppNotificationService(store ?? Global.store);
        }
    }
}
