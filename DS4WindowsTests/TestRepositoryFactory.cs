using System;
using DS4WinWPF;
using DS4Windows;
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
            IProfileXmlStore xmlStore = null, IPathService pathService = null, Func<ControlService> control = null)
        {
            return new ProfileRepository(
                settings ?? Global.ProfileSettingsServiceInstance,
                xmlStore ?? Global.ProfileXmlStoreInstance,
                pathService ?? new PathService(),
                control ?? new Func<ControlService>(() => DS4WinWPF.AppHost.GetService<ControlService>()));
        }

        public static ProfileActionProvider CreateProfileActionProvider(BackingStore store = null)
        {
            return new ProfileActionProvider(store ?? Global.store);
        }

        public static AutoProfileHolder CreateAutoProfileHolder(IPathService pathService = null)
        {
            return new AutoProfileHolder(pathService ?? new PathService());
        }

        public static AutoProfileService CreateAutoProfileService(AutoProfileHolder holder,
            IProfileApplicationService profileAppService, IProfileSettingsService profileSettings,
            IProcessInspector processInspector, IEnvironmentService environmentService = null,
            IAppSettingsService appSettings = null, IProfileRepository profileRepository = null,
            Func<ControlService> control = null)
        {
            return new AutoProfileService(holder, profileAppService, profileSettings, processInspector,
                environmentService ?? new EnvironmentService(),
                appSettings ?? new AppSettingsService(),
                profileRepository ?? CreateProfileRepository(profileSettings),
                control ?? new Func<ControlService>(() => Program.rootHub));
        }
    }
}
