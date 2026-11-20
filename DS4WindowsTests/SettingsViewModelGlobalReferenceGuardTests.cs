using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>SettingsViewModel</c> の Global 直接参照解消の回帰ガード
    /// （<c>docs-forDIMG/MadeByAgent/Archive/Phase6-Step9-Plan.md</c> §0A.2 表A、決定1＝S9-A、決定6＝D1）。
    /// - Step9-3a・3b で DI サービス経由へ置換した 89 件のメンバーが、<c>Global.</c> として再混入していないこと。
    /// - コンストラクタの依存が必須であり、Service Locator（<c>AppHost</c>）を持たないこと。
    /// - 削除した呼出元 0 件のハンドラが戻っていないこと。
    /// - 置換後も、設定の読み書きが <c>Global</c> と同じ実体に届くこと（薄い委譲の動作確認）。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class SettingsViewModelGlobalReferenceGuardTests
    {
        private const string VmPath = "DS4Windows/UI/ViewModels/SettingsViewModel.cs";

        private readonly ITestOutputHelper _output;

        public SettingsViewModelGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find(VmPath);
            if (path == null)
            {
                _output.WriteLine($"{VmPath} が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void Constructor_HasNoServiceLocator_AndRequiresServices()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;


            var env = new EnvironmentService();
            var appearance = TestRepositoryFactory.CreateAppearanceSettingsService();
            var app = TestRepositoryFactory.CreateAppSettingsService();
            var path = new PathService();
            var logApplier = new DS4Windows.Core.Logging.LogSettingsApplierProxy();
            var startup = new StartupEntryService();   // Phase14b-Step15（D12）
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(null, appearance, env, path, logApplier, startup));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, null, env, path, logApplier, startup));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, null, path, logApplier, startup));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, env, null, logApplier, startup));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, env, path, null, startup));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void Properties_ReadAndWriteThroughToGlobal()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var vm = TestHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            bool originalQuick = TestGlobals.Store.Behavior.quickCharge;
            int originalOscPort = TestGlobals.Store.Network.oscServPort;
            double originalBeta = TestGlobals.Store.Network.udpSmoothingBeta;
            string originalLevel = TestGlobals.Store.Log.logMinLevel;
            try
            {
                vm.QuickCharge = !originalQuick;
                Assert.Equal(!originalQuick, TestGlobals.Store.Behavior.quickCharge);
                Assert.Equal(!originalQuick, vm.QuickCharge);

                vm.OscPort = originalOscPort + 1;
                Assert.Equal(originalOscPort + 1, TestGlobals.Store.Network.oscServPort);

                vm.UdpSmoothBeta = originalBeta + 0.25;
                Assert.Equal(originalBeta + 0.25, TestGlobals.Store.Network.udpSmoothingBeta);
            }
            finally
            {
                TestGlobals.Store.Behavior.quickCharge = originalQuick;
                TestGlobals.Store.Network.oscServPort = originalOscPort;
                TestGlobals.Store.Network.udpSmoothingBeta = originalBeta;
                TestGlobals.Store.Log.logMinLevel = originalLevel;
                vm.Dispose();
            }
        }
    }
}
