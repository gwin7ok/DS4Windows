using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Composition;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W5（Q1・Q3・Q8・Q9＝A）: <c>Global</c> を削除し、<see cref="BackingStore"/> をコンストラクター注入で生成する形にしたことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10dW5GlobalRemovedGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10dW5GlobalRemovedGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void TheGlobalType_AndFile_AreGone()
        {
            Assert.Null(typeof(BackingStore).Assembly.GetType("DS4Windows.Global"));
            Assert.Null(SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs"));
        }

        [Fact]
        public void NoSource_UsesTheStaticGlobalDirective_OrAGlobalMemberInCode()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = SourceFileLocator.RootFromAnchor(anchor);
            // ログ文言の文字列（LanguageSelectDialog／LanguagePackViewModel の "Global.UseLang"）は、ログ内容の維持のために残してあり、参照ではない
            var logLiteral = new Regex("\"[^\"\\r\\n]*Global\\.UseLang[^\"\\r\\n]*\"");
            var offenders = Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Select(f => (file: f, text: logLiteral.Replace(StripComments(File.ReadAllText(f)), "\"\"")))
                .Where(x => Regex.IsMatch(x.text, @"using\s+static\s+(DS4Windows\.)?Global\s*;")
                         || Regex.IsMatch(x.text, @"(?<![\w])(?:DS4Windows\.)?Global\.\w+"))
                .Select(x => Path.GetFileName(x.file))
                .ToArray();
            Assert.True(offenders.Length == 0, "Global の参照が残っている: " + string.Join(", ", offenders));
        }

        [Fact]
        public void BackingStore_RequiresTheEnvironmentService()
        {
            Assert.Throws<ArgumentNullException>(() => new BackingStore(null));
        }

        [Fact]
        public void ProfileSettings_FirstServiceWins_AndASetterOverrides()
        {
            var env = new EnvironmentService();
            var store = new BackingStore(env);

            var first = new ProfileSettingsService(store, env, new DS4Windows.Processing.Services.DeviceSlotState());
            var second = new ProfileSettingsService(store, env, new DS4Windows.Processing.Services.DeviceSlotState());
            Assert.Same(first, store.ProfileSettings);

            store.ProfileSettings = second;
            Assert.Same(second, store.ProfileSettings);
        }

        [Fact]
        public void ProfileSettings_WithoutAService_Throws()
        {
            // Phase14b-Step9（決定 D8）: 旧「登録がないときはフォールバックを 1 回だけ作る」は撤去した。組み立ての誤りとして例外にする
            var store = new BackingStore(new EnvironmentService());
            Assert.Throws<InvalidOperationException>(() => store.ProfileSettings);
        }

        [Fact]
        public void RefreshActionAlias_UsesTheServicesOwnKbmMapping()
        {
            var env = new EnvironmentService();
            var store = new BackingStore(env);
            var service = new ProfileSettingsService(store, env, new DS4Windows.Processing.Services.DeviceSlotState());

            var setting = new DS4ControlSettings(DS4Controls.Cross);
            setting.action.actionAlias = 99;
            // Key 以外のアクション種別は、別名を 0 に戻すだけ（KBM のマッピングは使わない）
            service.RefreshActionAlias(setting);
            Assert.Equal(0u, setting.action.actionAlias);
        }

        [Fact]
        public void DistanceProfiles_IsTheStoresOwnArray()
        {
            var env = new EnvironmentService();
            var store = new BackingStore(env);
            var service = new ProfileSettingsService(store, env, new DS4Windows.Processing.Services.DeviceSlotState());
            Assert.Same(store.distanceProfiles, service.DistanceProfiles);
        }

        [Fact]
        public void PathService_WithoutAStore_CannotDecideTheSaveLocation_ButReadsPaths()
        {
            var service = new PathService();
            Assert.NotNull(service.AppDataPath);
            Assert.Throws<InvalidOperationException>(() => service.SetSaveLocation(@"C:\Phase7Step10dW5"));
            Assert.Throws<InvalidOperationException>(() => service.FindConfigLocation());
        }

        [Fact]
        public void Registration_UsesTheGivenStore_AndOtherwiseCreatesItsOwn()
        {
            var env = new EnvironmentService();
            var given = new BackingStore(env);

            var withGiven = new ServiceCollection();
            withGiven.RegisterServices(given);
            using (var provider = withGiven.BuildServiceProvider())
            {
                Assert.Same(given, provider.GetRequiredService<BackingStore>());
            }

            var withoutGiven = new ServiceCollection();
            withoutGiven.RegisterServices();
            using (var provider = withoutGiven.BuildServiceProvider())
            {
                var created = provider.GetRequiredService<BackingStore>();
                Assert.NotNull(created);
                Assert.NotSame(given, created);
            }
        }

        [Fact]
        public void TheBootstrapStore_IsCreatedFirst_AndPassedToTheHost()
        {
            string path = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            int store = text.IndexOf("_bootstrapBackingStore = new DS4Windows.Core.Settings.BackingStore(", StringComparison.Ordinal);
            int pathService = text.IndexOf("_bootstrapPathService = new DS4Windows.Core.Platform.PathService(config: _bootstrapBackingStore);", StringComparison.Ordinal);
            int rotation = text.IndexOf("PerformStartupRotation(", StringComparison.Ordinal);
            Assert.True(store >= 0 && pathService > store && rotation > pathService, "BackingStore → PathService → ログのローテーションの順であること");
            Assert.Equal(2, Regex.Matches(text, @"_bootstrapEnvironmentService,\s*_bootstrapBackingStore\)").Count);
        }

        [Fact]
        public void LightBar_TakesTheProfileSettingsAsAnArgument()
        {
            var method = typeof(DS4LightBar).GetMethod("updateLightBar");
            Assert.NotNull(method);
            // Phase14c-Step4（R-05 の前半）: 強制表示の状態（旧: 静的な配列）も引数で受ける
            Assert.Equal(new[] { "device", "deviceNum", "profileSettings", "overrides" }, method.GetParameters().Select(p => p.Name).ToArray());
        }
    }
}
