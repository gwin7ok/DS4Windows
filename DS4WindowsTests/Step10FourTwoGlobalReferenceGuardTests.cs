using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-2: クラスタ 1（プロファイル系サービス）の Global／AppHost／Program.rootHub 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-4-Plan.md</c> §13）。10-4-2b／2c で、対象のファイルを追加していく。
    /// </summary>
    internal class NullProcessInspector : IProcessInspector
    {
        public bool IsProcessRunning(string exePath) => false;
        public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
        {
            processPath = string.Empty;
            windowTitle = string.Empty;
            return false;
        }
    }

    public class Step10FourTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（薄い委譲・本体が Global にあるもの）
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            // 10-4-2a
            // ApplyProfile: 実装本体が Global にある（Phase7 で Mapping の instance 化とあわせて移す。TODO 付き）
            ["DS4Windows/DS4Control/Services/ProfileApplicationService.cs"] = new[] { "ApplyProfile" },
            // 実行中アクション状態のクリアを含む Global 側の実装への薄い委譲
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] =
                new[] { "CreateStdActions", "SaveAction", "RemoveAction", "SaveActions" },
            // 10-4-2b
            // 薄い委譲（IProfileRepository の契約の実体。Global 側に実装がある）と、読み込み前のクリア対象
            ["DS4Windows/DS4Control/Services/ProfileRepository.cs"] = new[]
            {
                "ProfilePath", "OlderProfilePath", "SelectedProfile", "LinkedProfileUI", "SelectedProfileChanged",
                "RaiseSelectedProfileChanged", "changeLinkedProfile", "removeLinkedProfile", "SaveLinkedProfiles",
                "ProfileActions", "CacheExtraProfileInfo", "CacheProfileCustomsFlags", "LoadTempProfile", "store",
                "LoadLinkedProfiles", "LoadBlankDevProfile", "containsLinkedProfile", "getLinkedProfile", "loggedInvalidActions",
            },
            ["DS4Windows/DS4Control/Services/ProfileActionProvider.cs"] = new string[0],
            ["DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/DS4Control/Services/AutoProfileService.cs"] = new[] { "autoProfileSwitchNotifyChoice", "CreateAutoProfiles" },
            // 10-4-2c: 薄い委譲（実装本体が Global にあるもの）と、不変の定数表
            ["DS4Windows/DS4Control/Services/ProfileSettingsService.cs"] =
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
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(null, new PathService()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new BackingStore(), null));
        }

        [Fact]
        public void ProfileRepository_ProfileActionProvider_AutoProfileHolder_RequireDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var store = new ProfileXmlStore();
            var path = new PathService();
            Func<ControlService> control = () => null;

            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(null, store, path, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(settings, null, path, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(settings, store, null, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(settings, store, path, null));
            Assert.Throws<ArgumentNullException>(() => new ProfileActionProvider(null));
            Assert.Throws<ArgumentNullException>(() => new DS4WinWPF.AutoProfileHolder(null));
        }

        [Fact]
        public void ProfileSettingsService_RequiresAllDependencies()
        {
            var store = new BackingStore();
            var env = new EnvironmentService();
            Func<ControlService> control = () => null;

            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(null, env, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, null, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, env, null));
        }

        [Fact]
        public void ProfileSettingsService_GetRumbleBoost_UsesInjectedControlProvider()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(), null, () => null);
            // コントローラーが無い場合も、注入された関数だけを参照して例外なく値を返す（Program.rootHub を参照しない）
            var ex = Record.Exception(() => settings.GetRumbleBoost(0));
            Assert.Null(ex);
        }

        [Fact]
        public void AutoProfileService_RequiresAllDependencies()
        {
            var path = new PathService();
            var holder = new DS4WinWPF.AutoProfileHolder(path);
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var app = new ProfileApplicationService(settings,
                new DeviceStateService(), () => null,
                TestRepositoryFactory.CreateProfileRepository(settings));
            var repo = TestRepositoryFactory.CreateProfileRepository(settings);
            var env = new EnvironmentService();
            var appSettings = new DS4Windows.Services.AppSettingsService();
            Func<ControlService> control = () => null;
            IProcessInspector inspector = new NullProcessInspector();

            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(null, app, settings, inspector, env, appSettings, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, null, settings, inspector, env, appSettings, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, null, inspector, env, appSettings, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, null, env, appSettings, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, null, appSettings, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, null, repo, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, null, control));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, null));
        }

        [Fact]
        public void AutoProfileHolder_LoadsFromInjectedPath()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-2b-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                // 設定ファイルが無い場合は空で生成される（注入されたパスを参照し、Global.appdatapath を参照しない）
                var holder = new DS4WinWPF.AutoProfileHolder(new PathService(dir));
                Assert.Empty(holder.AutoProfileColl);
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void ProfileApplicationService_RequiresAllDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var state = new DeviceStateService();
            Func<ControlService> control = () => null;
            var repo = TestRepositoryFactory.CreateProfileRepository(settings, null, null);

            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(null, state, control, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, null, control, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, state, null, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, state, control, null));
        }
    }
}
