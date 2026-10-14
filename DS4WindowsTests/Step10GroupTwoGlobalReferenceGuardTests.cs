using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

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
    public class Step10GroupTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（温存項目）
        private static readonly Dictionary<string, string[]> AllowedGlobalMembers = new Dictionary<string, string[]>
        {
            // Step10-2-2
            ["DS4Windows/DS4Forms/ViewModels/ControllersViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs"] = new[] { "RESOURCES_PREFIX" },
            ["DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ControllerRegisterOptionsWindow.xaml.cs"] = new string[0],
        };

        private readonly ITestOutputHelper _output;

        public Step10GroupTwoGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers()
        {
            foreach (var entry in AllowedGlobalMembers)
            {
                string text = ReadSourceWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"\bGlobal\.(\w+)")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(name => !entry.Value.Contains(name))
                    .Distinct()
                    .ToList();

                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");
            }
        }

        [Fact]
        public void ReplacedFiles_DoNotUseServiceLocatorOrRootHubFallbacks()
        {
            foreach (string file in AllowedGlobalMembers.Keys)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void ControllersViewModel_RequiresAllServices()
        {
            var settings = new ProfileSettingsService();
            var repo = new ProfileRepository();

            Assert.Throws<ArgumentNullException>(() => new ControllersViewModel(null, settings, repo));
            Assert.Throws<ArgumentNullException>(() => new ControllersViewModel(new DeviceStateService(), null, repo));
            Assert.Throws<ArgumentNullException>(() => new ControllersViewModel(new DeviceStateService(), settings, null));
        }

        [Fact]
        public void ControllerListViewModel_RequiresAllServices()
        {
            var settings = new ProfileSettingsService();
            var repo = new ProfileRepository();
            var app = new DS4Windows.Services.AppSettingsService();

            // 依存の null 検査は、ControlService を使う処理より前に行われる
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, null, repo, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, settings, null, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, settings, repo, null));
        }

        [Fact]
        public void ControllerRegDeviceOptsViewModel_RequiresAppSettingsAndXmlStore()
        {
            var app = new DS4Windows.Services.AppSettingsService();
            var xmlStore = new ProfileXmlStore();

            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, null, xmlStore));
            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, app, null));
        }
    }
}
