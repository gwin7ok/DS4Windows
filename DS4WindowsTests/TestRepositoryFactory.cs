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
        /// <summary>Phase7-Step8 W2: 切断時の後始末などで使う IMapping の遅延プロバイダー（コンテナの Singleton。ホスト構築時に Mapping を作らない）。</summary>
        public static Func<IMapping> MappingProvider => () => TestHost.GetService<IMapping>();

        public static SpecialActionRepository CreateSpecialActionRepository(BackingStore store = null, IPathService pathService = null)
        {
            return new SpecialActionRepository(store ?? TestGlobals.Store, pathService ?? new PathService(), new SyntheticInputState(),
                CreateActionsXml(store));
        }

        /// <summary>Phase7-Step10c W2: Actions.xml の保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Services.SpecialActionsXmlSerializer CreateActionsXml(BackingStore store = null,
            IProfileIdentityService profileIdentity = null)
            => new DS4Windows.Services.SpecialActionsXmlSerializer(store ?? TestGlobals.Store, profileIdentity);

        /// <summary>Phase7-Step10c W2: リンクプロファイルの保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Services.LinkedProfilesXmlSerializer CreateLinkedProfilesXml(BackingStore store = null)
            => new DS4Windows.Services.LinkedProfilesXmlSerializer(store ?? TestGlobals.Store);

        /// <summary>Phase7-Step10c W2: ProfileXmlStore（コントローラー固有設定の保存・読込を含む）。</summary>
        public static ProfileXmlStore CreateProfileXmlStore(BackingStore store = null)
            => new ProfileXmlStore(store ?? TestGlobals.Store, new DS4Windows.Services.ControllerConfigsXmlSerializer(store ?? TestGlobals.Store),
                CreateAppSettingsXml(store), CreateLegacyProfileXml(store));

        /// <summary>Phase7-Step10c W4: プロファイル（旧形式の XML）の保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Services.LegacyProfileXmlSerializer CreateLegacyProfileXml(BackingStore store = null,
            IProfileSettingsService settings = null, IPathService pathService = null, IProfileIdentityService profileIdentity = null)
            => new DS4Windows.Services.LegacyProfileXmlSerializer(store ?? TestGlobals.Store, pathService ?? new PathService(),
                settings ?? TestGlobals.ProfileSettingsServiceInstance, new EnvironmentService(), profileIdentity);

        /// <summary>Phase7-Step10c W3: アプリ設定（Profiles.xml）の保存・読込（旧: BackingStore.Load／Save）。</summary>
        public static DS4Windows.Services.AppSettingsXmlSerializer CreateAppSettingsXml(BackingStore store = null,
            IPathService pathService = null, IEnvironmentService environment = null)
            => new DS4Windows.Services.AppSettingsXmlSerializer(store ?? TestGlobals.Store, pathService ?? new PathService(), environment ?? new EnvironmentService());

        public static ProfileRepository CreateProfileRepository(IProfileSettingsService settings = null,
            IProfileXmlStore xmlStore = null, IPathService pathService = null, Func<ControlService> control = null,
            BackingStore store = null)
        {
            return new ProfileRepository(
                store ?? TestGlobals.Store,
                settings ?? TestGlobals.ProfileSettingsServiceInstance,
                xmlStore ?? CreateProfileXmlStore(store),
                pathService ?? new PathService(),
                control ?? new Func<ControlService>(() => TestHost.GetService<ControlService>()),
                CreateLinkedProfilesXml(store), CreateActionsXml(store), CreateLegacyProfileXml(store, settings, pathService), new EnvironmentService());
        }

        public static ProfileSettingsService CreateProfileSettingsService(BackingStore store = null,
            IEnvironmentService environmentService = null, Func<ControlService> control = null)
        {
            return new ProfileSettingsService(store ?? TestGlobals.Store,
                environmentService ?? new EnvironmentService(),
                control ?? new Func<ControlService>(() => null));
        }

        public static OutputSlotManager CreateOutputSlotManager()
        {
            return new OutputSlotManager(() => EnvironmentService.ViGEmBusVersionState, _ => SASteeringWheelEmulationAxisType.None, new EnvironmentService().ControllerSlotLimit);
        }

        public static OutputSlotService CreateOutputSlotService(OutputSlotManager manager = null,
            IOutputSlotStore store = null, ControlService control = null)
        {
            // Phase7-Step5 W2: Program.rootHub を削除したため、control を省略したときは ControlService なし（従来のテストの既定と同じ）
            ControlService ctl = control;
            OutputSlotManager mgr = manager ?? ctl?.OutputslotMan ?? CreateOutputSlotManager();
            return new OutputSlotService(() => mgr,
                store ?? new OutputSlotStore(new PathService()),
                () => ctl);
        }

        public static ProfileActionProvider CreateProfileActionProvider(BackingStore store = null)
        {
            return new ProfileActionProvider(store ?? TestGlobals.Store);
        }

        public static AppSettingsService CreateAppSettingsService(IProfileXmlStore xmlStore = null,
            IPathService pathService = null, BackingStore store = null)
        {
            return new AppSettingsService(store ?? TestGlobals.Store, xmlStore, pathService);
        }

        public static AutoProfileHolder CreateAutoProfileHolder(IPathService pathService = null)
        {
            return new AutoProfileHolder(pathService ?? new PathService(), new EnvironmentService());
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
                control ?? new Func<ControlService>(() => null),
                store ?? TestGlobals.Store);
        }

        public static ProfilePresetLoader CreateProfilePresetLoader(BackingStore store = null,
            IProfileSettingsService profileSettings = null)
        {
            return new ProfilePresetLoader(store ?? TestGlobals.Store, profileSettings ?? TestGlobals.ProfileSettingsServiceInstance,
                new DS4Windows.Services.ProfilePresets(store ?? TestGlobals.Store));
        }

        public static ProfileApplicationService CreateProfileApplicationService(IProfileSettingsService settings,
            Func<ControlService> control, IProfileRepository profileRepository,
            IEnvironmentService environmentService, IProfileXmlStore xmlStore = null,
            IProfileActionProvider actionProvider = null, BackingStore store = null,
            IKeyButtonControllerRegistry keyButtonControllers = null,
            DS4Windows.Actions.IActionRegistry actionRegistry = null,
            Func<DS4Windows.Actions.IActionBindingBuilder> bindingBuilderProvider = null)
        {
            return new ProfileApplicationService(settings, control, profileRepository, environmentService,
                xmlStore ?? CreateProfileXmlStore(store),
                actionProvider ?? CreateProfileActionProvider(store ?? TestGlobals.Store),
                store ?? TestGlobals.Store,
                keyButtonControllers ?? TestHost.GetService<IKeyButtonControllerRegistry>(),
                actionRegistry ?? TestHost.GetService<DS4Windows.Actions.IActionRegistry>(),
                bindingBuilderProvider ?? (() => TestHost.GetService<DS4Windows.Actions.IActionBindingBuilder>()));
        }

        public static ProfileSlotApplier CreateProfileSlotApplier(IProfileApplicationService profileApplication = null,
            DS4Windows.Actions.ITriggerDispatchEngine engine = null)
        {
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            return new ProfileSlotApplier(profileApplication ?? CreateProfileApplicationService(settings,
                () => null, CreateProfileRepository(settings), new EnvironmentService()), new EnvironmentService(),
                engine ?? TestHost.GetService<DS4Windows.Actions.ITriggerDispatchEngine>());
        }

        public static AppearanceSettingsService CreateAppearanceSettingsService(BackingStore store = null)
        {
            return new AppearanceSettingsService(store ?? TestGlobals.Store);
        }

        public static DeviceStateService CreateDeviceStateService(BackingStore store = null)
        {
            return new DeviceStateService(store ?? TestGlobals.Store);
        }

        public static AppNotificationService CreateAppNotificationService(BackingStore store = null)
        {
            return new AppNotificationService(store ?? TestGlobals.Store);
        }
    }
}
