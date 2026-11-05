using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Output;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{

    public class Step10FourTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（薄い委譲・本体が Global にあるもの）
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            // 10-4-2a
            // ApplyProfile: 実装本体が Global にある（Phase7 で Mapping の instance 化とあわせて移す。TODO 付き）
            ["DS4Windows/Processing/Services/ProfileApplicationService.cs"] = new[] { "ApplyProfile" },
            // 実行中アクション状態のクリアを含む Global 側の実装への薄い委譲
            ["DS4Windows/Core/Persistence/SpecialActionRepository.cs"] =
                new[] { "CreateStdActions", "SaveAction", "RemoveAction", "SaveActions" },
            // 10-4-2b
            // 薄い委譲（IProfileRepository の契約の実体。Global 側に実装がある）と、読み込み前のクリア対象
            ["DS4Windows/Core/Persistence/ProfileRepository.cs"] = new[]
            {
                "ProfilePath", "OlderProfilePath", "SelectedProfile", "LinkedProfileUI", "SelectedProfileChanged",
                "RaiseSelectedProfileChanged", "changeLinkedProfile", "removeLinkedProfile", "SaveLinkedProfiles",
                "ProfileActions", "CacheExtraProfileInfo", "CacheProfileCustomsFlags", "LoadTempProfile", "store",
                "LoadLinkedProfiles", "LoadBlankDevProfile", "containsLinkedProfile", "getLinkedProfile", "loggedInvalidActions",
            },
            ["DS4Windows/Processing/Services/ProfileActionProvider.cs"] = new string[0],
            ["DS4Windows/Core/Profiles/AutoProfileHolder.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Processing/Services/AutoProfileService.cs"] = new[] { "autoProfileSwitchNotifyChoice" },
            // 10-4-2c: 薄い委譲（実装本体が Global にあるもの）と、不変の定数表
            // 10-4-3: 実行時配列の実体は Phase7-Step6 W1 で OutputSlotService へ移した。残るのは配列の長さの定数だけ
            ["DS4Windows/Output/Services/OutputSlotService.cs"] = new[] { "TEST_PROFILE_ITEM_COUNT" },
            ["DS4Windows/Core/Persistence/OutputSlotStore.cs"] = new string[0],
            ["DS4Windows/Output/OutputSlotManager.cs"] = new string[0],
            ["DS4Windows/Output/OutputSlotPersist.cs"] = new[] { "exeversion" },
            // 10-4-4: Actions/*（ServiceProviderHolder は D4-4 により Phase7/Phase8 へ温存。MAX_DS4_CONTROLLER_COUNT は定数）
            ["DS4Windows/Processing/Actions/MacroAction.cs"] = new string[0],
            ["DS4Windows/Processing/Actions/ProfileSwitchAction.cs"] = new string[0],
            ["DS4Windows/Processing/Actions/LaunchProcessAction.cs"] = new string[0],
            ["DS4Windows/Processing/Actions/DefaultMacroPlayer.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Processing/Actions/DefaultActionManager.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Actions/ActionManager.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Core/Settings/ProfileSettingsService.cs"] =
                new[] { "RefreshActionAlias", "RefreshExtrasButtons", "defaultButtonMapping", "reverseX360ButtonMapping" },
        };

        private static string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null) return null;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers()
        {
            foreach (var entry in Allowed)
            {
                string text = ReadWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"\bGlobal\.(\w+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(n => !entry.Value.Contains(n)).Distinct().ToList();
                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void SpecialActionRepository_RequiresStoreAndPathService()
        {
            // Phase14b-Step14（D14）: 旧 引数の SyntheticInputState・IManagedActionManager は、リセットの契約 1 つにまとめた
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(null, new PathService(), TestRepositoryFactory.CreateActionsXml(), TestActions.RuntimeReset));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()), null, TestRepositoryFactory.CreateActionsXml(), TestActions.RuntimeReset));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()), new PathService(), null, TestActions.RuntimeReset));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()), new PathService(), TestRepositoryFactory.CreateActionsXml(), null));
        }

        [Fact]
        public void ProfileRepository_ProfileActionProvider_AutoProfileHolder_RequireDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            var store = TestRepositoryFactory.CreateProfileXmlStore(config);
            var linkedXml = TestRepositoryFactory.CreateLinkedProfilesXml(config);
            var actionsXml = TestRepositoryFactory.CreateActionsXml(config);
            var legacyXml = TestRepositoryFactory.CreateLegacyProfileXml(config);
            var path = new PathService();
            IProfileLoadTarget control = new LazyProfileLoadTarget();   // Phase13-Step6 6-e: 旧 Func<ControlService>

            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(null, settings, store, path, control, linkedXml, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(null, new DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer(config), TestRepositoryFactory.CreateAppSettingsXml(config), TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, null, TestRepositoryFactory.CreateAppSettingsXml(config), TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, new DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer(config), null, TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, new DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer(config), TestRepositoryFactory.CreateAppSettingsXml(config), null));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, null, linkedXml, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, null, store, path, control, linkedXml, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, null, path, control, linkedXml, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, null, control, linkedXml, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, null, actionsXml, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, linkedXml, null, legacyXml, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, linkedXml, actionsXml, null, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new ProfileActionProvider(null));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Core.Profiles.AutoProfileHolder(null, new EnvironmentService()));
        }

        [Fact]
        public void ProfileSettingsService_RequiresAllDependencies()
        {
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            var env = new EnvironmentService();
            IDeviceStateAccessor control = new DS4Windows.Processing.Services.DeviceSlotState();

            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(null, env, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, null, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, env, null));
        }

        [Fact]
        public void ProfileSettingsService_GetRumbleBoost_UsesInjectedControlProvider()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()), null, new DS4Windows.Processing.Services.DeviceSlotState());
            // コントローラーが無い場合も、注入された関数だけを参照して例外なく値を返す（Program.rootHub を参照しない）
            var ex = Record.Exception(() => settings.GetRumbleBoost(0));
            Assert.Null(ex);
        }

        [Fact]
        public void AutoProfileService_RequiresAllDependencies()
        {
            var path = new PathService();
            var holder = new DS4Windows.Core.Profiles.AutoProfileHolder(path, new EnvironmentService());
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var app = TestRepositoryFactory.CreateProfileApplicationService(settings,
                new LazyProfileLoadTarget(),
                TestRepositoryFactory.CreateProfileRepository(settings), new EnvironmentService());
            var repo = TestRepositoryFactory.CreateProfileRepository(settings);
            var env = new EnvironmentService();
            var appSettings = TestRepositoryFactory.CreateAppSettingsService();
            IServiceLifecycle lifecycle = TestHost.GetService<IServiceLifecycle>();
            IDeviceStateAccessor deviceState = new DS4Windows.Processing.Services.DeviceSlotState();
            IProcessInspector inspector = new NullProcessInspector();

            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(null, app, settings, inspector, env, appSettings, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, null, settings, inspector, env, appSettings, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, null, inspector, env, appSettings, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, null, env, appSettings, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, null, appSettings, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, null, repo, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, null, lifecycle, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, null, deviceState, TestGlobals.Store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, lifecycle, deviceState, null));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, lifecycle, null, TestGlobals.Store));
        }

        [Fact]
        public void AutoProfileHolder_LoadsFromInjectedPath()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-2b-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                // 設定ファイルが無い場合は空で生成される（注入されたパスを参照し、Global.appdatapath を参照しない）
                var holder = new DS4Windows.Core.Profiles.AutoProfileHolder(new PathService(dir), new EnvironmentService());
                Assert.Empty(holder.AutoProfileColl);
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void OutputSlot_Services_RequireAllDependencies()
        {
            var store = new OutputSlotStore(new PathService());
            IVirtualPadService virtualPad = new FakeVirtualPadService(null);   // Phase13-Step7 7-d: 旧 Func<OutputSlotManager>／Func<IVirtualPadService>
            var types = new DS4Windows.Output.Services.OutputDeviceTypeState();

            Assert.Throws<ArgumentNullException>(() => new OutputSlotStore(null));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotManager(null, 4));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(null, store, types));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(virtualPad, null, types));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(virtualPad, store, null));
        }

        [Fact]
        public void OutputSlotStore_Save_UsesInjectedPath()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-3-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var store = new OutputSlotStore(new PathService(dir));
                var manager = TestRepositoryFactory.CreateOutputSlotManager();

                Assert.True(store.Save(manager)); // 書き込み成功時は true を返す（Phase6-Step10-4-Plan.md §26 で是正）
                Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, "OutputSlots.xml")));
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void OutputSlotManager_ReadsViGEmBusVersionFromProvider()
        {
            int calls = 0;
            var manager = new OutputSlotManager(() => { calls++; return new Version(1, 0, 0, 0); }, 4);
            Assert.NotNull(manager);
            Assert.Equal(0, calls); // 生成時には読まない（割り当て時に都度読み取る）
        }

        [Fact]
        public void Actions_RequireAllDependencies()
        {
            var sa = new SpecialAction("a", "Cross", "Key", "1", 0);
            var player = new DefaultMacroPlayer(new MockVirtualKBM(), TestHost.GetService<IMapping>());
            var launcher = new DefaultProcessLauncher();
            var switcher = new DefaultProfileSwitcher();

            Assert.Throws<ArgumentNullException>(() => new MacroAction(sa, (IMacroPlayer)null));
            Assert.Throws<ArgumentNullException>(() => new ProfileSwitchAction(sa, (IProfileSwitcher)null));
            Assert.Throws<ArgumentNullException>(() => new LaunchProcessAction(sa, null));
            Assert.Throws<ArgumentNullException>(() => new DefaultMacroPlayer(null, TestHost.GetService<IMapping>()));
            Assert.Throws<ArgumentNullException>(() => new DefaultMacroPlayer(new MockVirtualKBM(), null));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionManager(null));
        }

        [Fact]
        public void MainWindow_And_ControlService_UseOnlyConstGlobalMembers()
        {
            var allowed = new Dictionary<string, string[]>
            {
                // 定数のみ（RESOURCES_PREFIX/TEST_PROFILE_INDEX/MAX_DS4_CONTROLLER_COUNT）
                ["DS4Windows/UI/MainWindow.xaml.cs"] = new[] { "RESOURCES_PREFIX", "TEST_PROFILE_INDEX" },
                ["DS4Windows/Processing/ControlService.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT", "TEST_PROFILE_INDEX" },
            };

            foreach (var entry in allowed)
            {
                string text = ReadWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"Global.(w+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(n => !entry.Value.Contains(n)).Distinct().ToList();
                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");

                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        // Phase10-Step4d W7: 以前ここにあった、ReplaceLinkedProfileName／AutoProfileHolder.ReplaceProfileName／MainWindow の追従の検査は、撤去したメンバーのため削除した。
        // 名前変更の共通の経路（SyncProfileListAndControllers）を通ることの検査は、Phase10Step4dW7FollowUpTests に移した

        [Fact]
        public void OutputSlotService_PluginAndUnplug_AreRejectedWithoutControlService_AndRaiseNoEvent()
        {
            // ControlService が無い（ホストが無い）場合は、何もできないため false を返し、変更の通知も出さない
            // Phase13-Step7 7-d: 操作先（スロット管理）が無い仮想パッド窓口。旧: virtualPadProvider が null を返す
            var service = new OutputSlotService(new FakeVirtualPadService(null),
                new OutputSlotStore(new PathService()), new DS4Windows.Output.Services.OutputDeviceTypeState());
            int fired = 0;
            service.OutputSlotChanged += (s, e) => fired++;

            Assert.False(service.PluginSlot(0, OutContType.X360));
            Assert.False(service.UnplugSlot(0));
            Assert.False(service.PluginSlot(-1, OutContType.DS4));
            Assert.False(service.UnplugSlot(OutputSlotService.MAX_SLOTS));
            Assert.Equal(0, fired);
        }

        [Fact]
        public void OutputSlotOperations_GoThroughOutputSlotService_NotControlServiceDirectly()
        {
            foreach (string file in new[]
            {
                "DS4Windows/UI/ViewModels/CurrentOutDeviceViewModel.cs",
                "DS4Windows/UI/OutputSlotManagerControl.xaml.cs",
                "DS4Windows/UI/MainWindow.xaml.cs",
            })
            {
                string text = ReadWithoutComments(file);
                if (text == null) continue;
                Assert.DoesNotContain("AttachUnboundOutDev(", text);
                Assert.DoesNotContain("DetachUnboundOutDev(", text);
            }

            string vm = ReadWithoutComments("DS4Windows/UI/ViewModels/CurrentOutDeviceViewModel.cs");
            if (vm != null)
            {
                Assert.Contains("outputSlotService.PluginSlot(", vm);
                Assert.Contains("outputSlotService.UnplugSlot(", vm);
            }
        }

        [Fact]
        public void ProfileApplicationService_RequiresAllDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            IProfileLoadTarget control = new LazyProfileLoadTarget();   // Phase13-Step6 6-e: 旧 Func<ControlService>
            var repo = TestRepositoryFactory.CreateProfileRepository(settings, null, null);
            var env = new EnvironmentService();

            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(null, control, repo, env));
            // Phase7-Step10b W3: IDeviceStateService は ProfileApplicationService の依存ではなくなった
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, null, repo, env));
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, control, null, env));
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, control, repo, null));
        }

        [Fact]
        public void ProfileApplicationService_SlotLimit_FollowsEnvironment_NotFixedFour()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repo = TestRepositoryFactory.CreateProfileRepository(settings);
            var env = new EnvironmentService();
            var service = TestRepositoryFactory.CreateProfileApplicationService(settings, new LazyProfileLoadTarget(), repo, env);
            int limit = env.ControllerSlotLimit;

            // 上限（範囲外）は拒否され、何も変更されない（Phase8-Step6 W3: ClearPendingRestore の削除に伴い、保留中の復帰先の検査は外した）
            Assert.False(service.RestoreFromAction(limit));
            Assert.False(service.RestoreFromAction(-1));
            Assert.False(service.ApplyProfile(limit, "NoSuchProfile"));
            Assert.False(service.ApplyProfile(-1, "NoSuchProfile"));
        }
    }
}
