using DS4Windows.Output.Abstractions;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Output;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using DS4Windows.UI.Services;

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
            // Phase14b-Step14（D14）: 旧 引数（new SyntheticInputState()、TestActions.Manager）を、同じ組み合わせのリセットの実装にまとめて渡す
            return new SpecialActionRepository(store ?? TestGlobals.Store, pathService ?? new PathService(),
                CreateActionsXml(store), new SpecialActionRuntimeReset(new SyntheticInputState(), TestActions.Manager));
        }

        /// <summary>Phase7-Step10c W2: Actions.xml の保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Core.Persistence.SpecialActionsXmlSerializer CreateActionsXml(BackingStore store = null,
            IProfileIdentityService profileIdentity = null)
            => new DS4Windows.Core.Persistence.SpecialActionsXmlSerializer(store ?? TestGlobals.Store, TestActions.RuntimeReset, profileIdentity);

        /// <summary>Phase7-Step10c W2: リンクプロファイルの保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer CreateLinkedProfilesXml(BackingStore store = null)
            => new DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer(store ?? TestGlobals.Store);

        /// <summary>Phase7-Step10c W2: ProfileXmlStore（コントローラー固有設定の保存・読込を含む）。</summary>
        public static ProfileXmlStore CreateProfileXmlStore(BackingStore store = null)
            => new ProfileXmlStore(store ?? TestGlobals.Store, new DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer(store ?? TestGlobals.Store),
                CreateAppSettingsXml(store), CreateLegacyProfileXml(store));

        /// <summary>Phase7-Step10c W4: プロファイル（旧形式の XML）の保存・読込（旧: BackingStore のメソッド）。</summary>
        public static DS4Windows.Core.Persistence.LegacyProfileXmlSerializer CreateLegacyProfileXml(BackingStore store = null,
            IProfileSettingsService settings = null, IPathService pathService = null, IProfileIdentityService profileIdentity = null)
            => new DS4Windows.Core.Persistence.LegacyProfileXmlSerializer(TestLightbar.State, store ?? TestGlobals.Store, pathService ?? new PathService(),
                settings ?? TestGlobals.ProfileSettingsServiceInstance, new EnvironmentService(), profileIdentity);

        /// <summary>Phase7-Step10c W3: アプリ設定（Profiles.xml）の保存・読込（旧: BackingStore.Load／Save）。</summary>
        public static DS4Windows.Core.Persistence.AppSettingsXmlSerializer CreateAppSettingsXml(BackingStore store = null,
            IPathService pathService = null, IEnvironmentService environment = null)
            => new DS4Windows.Core.Persistence.AppSettingsXmlSerializer(store ?? TestGlobals.Store, pathService ?? new PathService(), environment ?? new EnvironmentService());

        public static ProfileRepository CreateProfileRepository(IProfileSettingsService settings = null,
            IProfileXmlStore xmlStore = null, IPathService pathService = null, IProfileLoadTarget loadTarget = null,
            BackingStore store = null)
        {
            return new ProfileRepository(
                store ?? TestGlobals.Store,
                settings ?? TestGlobals.ProfileSettingsServiceInstance,
                xmlStore ?? CreateProfileXmlStore(store),
                pathService ?? new PathService(),
                loadTarget ?? new LazyProfileLoadTarget(),   // Phase13-Step6 6-e: 旧 Func<ControlService>（遅延）。ホストから初回使用時に解決する
                CreateLinkedProfilesXml(store), CreateActionsXml(store), CreateLegacyProfileXml(store, settings, pathService), new EnvironmentService());
        }

        public static ProfileSettingsService CreateProfileSettingsService(BackingStore store = null,
            IEnvironmentService environmentService = null, IDeviceStateAccessor deviceState = null)
        {
            return new ProfileSettingsService(store ?? TestGlobals.Store,
                environmentService ?? new EnvironmentService(),
                deviceState ?? new DeviceSlotState());
        }

        public static OutputSlotManager CreateOutputSlotManager()
        {
            return new OutputSlotManager(() => EnvironmentService.ViGEmBusVersionState, new EnvironmentService().ControllerSlotLimit);
        }

        public static OutputSlotService CreateOutputSlotService(OutputSlotManager manager = null,
            IOutputSlotStore store = null, ControlService control = null)
        {
            // Phase7-Step5 W2: Program.rootHub を削除したため、control を省略したときは ControlService なし（従来のテストの既定と同じ）
            ControlService ctl = control;
            OutputSlotManager mgr = manager ?? (ctl != null ? TestHost.GetService<IVirtualPadService>().OutputSlotManager : null) ?? CreateOutputSlotManager();
            return new OutputSlotService(
                new FakeVirtualPadService(mgr, ctl != null ? TestHost.GetService<IVirtualPadService>() : null),   // Phase13-Step7 7-d: 旧 Func<OutputSlotManager>／Func<IVirtualPadService>。抜き差しは VirtualPadService（Phase9 E-4）
                store ?? new OutputSlotStore(new PathService()),
                new DS4Windows.Output.Services.OutputDeviceTypeState());   // Phase13-Step7 7-b: 出力種別の配列の持ち主
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
            IServiceLifecycle lifecycle = null, IDeviceStateAccessor deviceState = null, BackingStore store = null)
        {
            var service = new AutoProfileService(holder, profileAppService, profileSettings, processInspector,
                environmentService ?? new EnvironmentService(),
                appSettings ?? CreateAppSettingsService(),
                profileRepository ?? CreateProfileRepository(profileSettings),
                lifecycle ?? TestHost.GetService<IServiceLifecycle>(),   // 実体（Running=false）。停止中のサービスとして扱う
                deviceState ?? new DeviceSlotState(),
                store ?? TestGlobals.Store);
            // 旧実装は ControlService が未解決（テスト）のとき「全スロット接続」とみなしていた。既存テストの前提を保つため、既定は全スロット接続（個別のテストは SlotConnectedProbe で差し替える）
            service.SlotConnectedProbe = _ => true;
            return service;
        }

        public static ProfilePresetLoader CreateProfilePresetLoader(BackingStore store = null,
            IProfileSettingsService profileSettings = null)
        {
            return new ProfilePresetLoader(store ?? TestGlobals.Store, profileSettings ?? TestGlobals.ProfileSettingsServiceInstance,
                new DS4Windows.Core.Persistence.ProfilePresets(store ?? TestGlobals.Store));
        }

        public static ProfileApplicationService CreateProfileApplicationService(IProfileSettingsService settings,
            IProfileLoadTarget control, IProfileRepository profileRepository,
            IEnvironmentService environmentService, IProfileXmlStore xmlStore = null,
            IProfileActionProvider actionProvider = null, BackingStore store = null,
            IKeyButtonControllerRegistry keyButtonControllers = null,
            DS4Windows.Processing.Actions.IActionRegistry actionRegistry = null,
            Func<DS4Windows.Processing.Actions.IActionBindingBuilder> bindingBuilderProvider = null,
            IProfileIdentityService profileIdentity = null)
        {
            return new ProfileApplicationService(settings, control, profileRepository, environmentService,
                xmlStore ?? CreateProfileXmlStore(store),
                actionProvider ?? CreateProfileActionProvider(store ?? TestGlobals.Store),
                store ?? TestGlobals.Store,
                keyButtonControllers ?? TestHost.GetService<IKeyButtonControllerRegistry>(),
                actionRegistry ?? TestHost.GetService<DS4Windows.Processing.Actions.IActionRegistry>(),
                bindingBuilderProvider ?? (() => TestHost.GetService<DS4Windows.Processing.Actions.IActionBindingBuilder>()),
                TestActions.Manager,
                profileIdentity);
        }

        public static ProfileSlotApplier CreateProfileSlotApplier(IProfileApplicationService profileApplication = null,
            DS4Windows.Processing.Actions.ITriggerDispatchEngine engine = null)
        {
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            return new ProfileSlotApplier(profileApplication ?? CreateProfileApplicationService(settings,
                new LazyProfileLoadTarget(), CreateProfileRepository(settings), new EnvironmentService()), new EnvironmentService(),
                engine ?? TestHost.GetService<DS4Windows.Processing.Actions.ITriggerDispatchEngine>());
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
