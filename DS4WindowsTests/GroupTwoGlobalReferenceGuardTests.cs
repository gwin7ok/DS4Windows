using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-2: グループ2（プロファイル・マッピング系）の View／ViewModel の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-2-Plan.md</c>、<c>Phase6-Step10-Plan.md</c> §0A.3）。Step10-2 の各マイクロステップで、
    /// 置換済みのファイルを本クラスの一覧へ追加していく。
    /// - 置換済みのファイルに、温存項目（定数・不変の静的表）以外の <c>Global.</c> が再混入していないこと。
    /// - <c>AppHost</c>（Service Locator）・<c>Program.rootHub</c> を持たないこと。
    /// - コンストラクタの依存が必須であり、null を拒否すること。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class GroupTwoGlobalReferenceGuardTests
    {

        private readonly ITestOutputHelper _output;

        public GroupTwoGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ControllerListViewModel_RequiresAllServices()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repo = TestRepositoryFactory.CreateProfileRepository();
            var app = TestRepositoryFactory.CreateAppSettingsService();

            // 依存の null 検査は、機器の窓口を使う処理より前に行われる
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, null, null, repo, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, null, settings, null, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, null, settings, repo, null));
        }

        [Fact]
        public void AutoProfilesViewModel_RequiresAllServices()
        {
            // 依存の null 検査は、AutoProfileHolder を使う処理より前に行われる（holder／list は null で検証できる）
            var app = TestRepositoryFactory.CreateAppSettingsService();

            Assert.Throws<ArgumentNullException>(() => new AutoProfilesViewModel(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new AutoProfilesViewModel(null, null, app, null, null, new EnvironmentService()));
        }

        [Fact]
        public void RenameProfileViewModel_RequiresPathService_AndUsesItsAppDataPath()
        {
            Assert.Throws<ArgumentNullException>(() => new RenameProfileViewModel(null));

            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Phase6Step10_Rename_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "Profiles"));
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Profiles", "Existing.xml"), "x");
                var vm = new RenameProfileViewModel(new PathService(dir));

                vm.ProfileName = "Existing";
                Assert.True(vm.ProfileFileExists());

                vm.ProfileName = "Missing";
                Assert.False(vm.ProfileFileExists());
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void EditorWindowFactory_RequiresAllServices()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var slots = TestRepositoryFactory.CreateOutputSlotService();
            var path = new PathService();
            var rd = TestHost.GetService<DS4Windows.Processing.Abstractions.IControllerReadingsSource>();
            var mr = TestHost.GetService<DS4Windows.Processing.Abstractions.IMacroRecordingState>();
            var lt = new LazyProfileLoadTarget();   // Phase13-Step6 6-e: プリセットの読込先の窓口
            var loader = TestRepositoryFactory.CreateProfilePresetLoader();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            // Phase7-Step5 W5: CreateProfileEditor のために、アプリ設定・プロファイルリポジトリ・プロファイル切替の 3 つを追加した
            var app = TestRepositoryFactory.CreateAppSettingsService();
            var profiles = TestRepositoryFactory.CreateProfileRepository(settings);
            var switcher = new DS4Windows.Processing.Actions.DefaultProfileSwitcher();
            var vm = new NullViewModelFactory();
            var windowSystem = new DS4Windows.Core.Platform.Win32WindowSystem();
            var mouseHook = new DS4Windows.Core.Platform.LowLevelMouseHook();
            var rf = TestHost.GetService<DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch>();   // Phase13-Step10 10-d
            var sc = TestHost.GetService<DS4Windows.Processing.Abstractions.IControllerSlotCommands>();

            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, null, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, null, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, null, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, null, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, null, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, null, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, null, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, null, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, null, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, null, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, null, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, null, windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, null, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), null, mouseHook, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, null, rf, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, null, sc));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.Services.EditorWindowFactory(TestLightbar.State, settings, slots, path, vm, repo, loader, rd, mr, lt, app, profiles, switcher, new DS4Windows.Core.Platform.EnvironmentService(), windowSystem, mouseHook, rf, null));
        }

        private class NullViewModelFactory : IViewModelFactory
        {
            public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1) => null;
            public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool repeatable = false, int targetDevice = -1) => null;
            public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null) => null;
            public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList) => null;
            public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device) => null;
            public SettingsViewModel CreateSettingsViewModel() => null;
            public ControllerListViewModel CreateControllerListViewModel(ProfileList profileListHolder) => null;
            public CurrentOutDeviceViewModel CreateCurrentOutDeviceViewModel() => null;
            public ChangelogViewModel CreateChangelogViewModel() => null;
            public RenameProfileViewModel CreateRenameProfileViewModel() => null;
            public PresetOptionViewModel CreatePresetOptionViewModel() => null;
            public FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions) => null;
            public UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion) => null;
            public ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions) => null;
            public TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex) => null;
            public LanguagePackViewModel CreateLanguagePackViewModel() => null;
            public AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo) => null;
            public MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType) => null;
            public LogViewModel CreateLogViewModel() => null;
            public TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher) => null;
            public BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice) => null;
            public ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target) => null;
            public DS4Windows.UI.ViewModels.SpecialActions.MacroViewModel CreateMacroViewModel() => null;
            public DS4Windows.UI.ViewModels.SpecialActions.LaunchProgramViewModel CreateLaunchProgramViewModel() => null;
            public DS4Windows.UI.ViewModels.SpecialActions.LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList) => null;
            public DS4Windows.UI.ViewModels.SpecialActions.PressKeyViewModel CreatePressKeyViewModel(int deviceIndex) => null;
            public DS4Windows.UI.ViewModels.SpecialActions.SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex) => null;
            public DS4Windows.UI.ViewModels.SpecialActions.CheckBatteryViewModel CreateCheckBatteryViewModel() => null;
            public DS4Windows.UI.ViewModels.SpecialActions.MultiActButtonViewModel CreateMultiActButtonViewModel() => null;
            public DS4Windows.UI.ViewModels.SpecialActions.FocusWindowViewModel CreateFocusWindowViewModel() => null;
        }

        [Fact]
        public void PresetAndMappingViewModels_RequireServices()
        {
            Assert.Throws<ArgumentNullException>(() => new PresetOptionViewModel(null, null));
            Assert.Throws<ArgumentNullException>(() => new PresetOptionViewModel(TestRepositoryFactory.CreateProfilePresetLoader(), null));
            Assert.Throws<ArgumentNullException>(() => new MappingListViewModel(0, OutContType.X360, null));
            Assert.Throws<ArgumentNullException>(() => new TouchButtonUserControlViewModel(0, null));
        }

        [Fact]
        public void BindingAndRecordViewModels_RequireProfileSettingsService()
        {
            Assert.Throws<ArgumentNullException>(() => new BindingWindowViewModel(TestLightbar.State, 0, new DS4ControlSettings(DS4Controls.Cross), null, TestRepositoryFactory.CreateOutputSlotService(), new DS4Windows.Core.Platform.EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new BindingWindowViewModel(TestLightbar.State, 0, new DS4ControlSettings(DS4Controls.Cross), TestRepositoryFactory.CreateProfileSettingsService(), null, new DS4Windows.Core.Platform.EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new RecordBoxViewModel(TestLightbar.State, 0, new DS4ControlSettings(DS4Controls.Cross), true, null, new DS4Windows.Core.Platform.EnvironmentService(), null));
        }

        [Fact]
        public void ControllerRegDeviceOptsViewModel_RequiresAppSettingsAndSlotQuery()
        {
            var app = TestRepositoryFactory.CreateAppSettingsService();

            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, app));
        }
    }
}
