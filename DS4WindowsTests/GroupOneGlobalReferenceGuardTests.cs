using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-1: グループ1（起動・環境系）の View／ViewModel の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-1-Plan.md</c>、<c>Phase6-Step10-Plan.md</c> §0A.2）。
    /// - 置換済みのファイルに、温存項目（定数・Pre-Host の手動分岐）以外の <c>Global.</c> が再混入していないこと。
    /// - <c>AppHost</c>（Service Locator）・<c>Program.rootHub</c>・静的な <c>App.logHolder</c>／<c>App.Current</c> を持たないこと。
    /// - 削除した呼出元 0 件のメソッドが戻っていないこと。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class GroupOneGlobalReferenceGuardTests
    {

        // Service Locator・静的フォールバックを持たないこと（検査対象）
        private static readonly string[] NoServiceLocatorFiles =
        {
            "DS4Windows/UI/SaveWhere.xaml.cs",
            "DS4Windows/UI/LanguageSelectDialog.xaml.cs",
            "DS4Windows/UI/About.xaml.cs",
            "DS4Windows/UI/UpdaterWindow.xaml.cs",
            "DS4Windows/UI/WelcomeDialog.xaml.cs",
            "DS4Windows/UI/ViewModels/AboutViewModel.cs",
            "DS4Windows/UI/ViewModels/LogViewModel.cs",
            "DS4Windows/UI/ViewModels/UpdaterWindowViewModel.cs",
            "DS4Windows/UI/ViewModels/LanguagePackViewModel.cs",
            "DS4Windows/UI/ViewModels/SettingsViewModel.cs",
            "DS4Windows/UI/ViewModels/TrayIconViewModel.cs",
            "DS4Windows/Core/Platform/StartupEntryService.cs",
        };

        private readonly ITestOutputHelper _output;

        public GroupOneGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ReplacedFiles_DoNotUseServiceLocatorOrStaticFallbacks()
        {
            foreach (string file in NoServiceLocatorFiles)
            {
                string text = SourceText.ReadCode(file);
                if (text == null) continue;

                Assert.DoesNotContain("logHolder", text);
                Assert.DoesNotContain("App.Current", text);
                Assert.DoesNotContain("Application.Current", text);
            }
        }

        [Fact]
        public void StartupMethods_TakesPathsAsArguments()
        {
            // Phase14d-Step3: ソースの文字列ではなく、メソッドの引数（名前と型）を確かめる
            Type service = typeof(DS4Windows.Core.Platform.StartupEntryService);
            foreach ((string method, string[] parameters) in new[]
            {
                ("WriteStartProgEntry", new[] { "exeLocation", "programDirectory" }),
                ("DeleteOldTaskEntry", new[] { "programDirectory" }),
                ("WriteTaskEntry", new[] { "programDirectory", "exeFileName" }),
                ("CheckStartupExeLocation", new[] { "exeLocation" }),
            })
            {
                System.Reflection.ParameterInfo[] ps = service.GetMethods().Single(m => m.Name == method).GetParameters();
                Assert.Equal(parameters, ps.Select(p => p.Name).ToArray());
                Assert.All(ps, p => Assert.Equal(typeof(string), p.ParameterType));
            }
        }

        [Fact]
        public void LanguagePackControl_IsInitializedByMainWindow()
        {
            string xaml = SourceFileLocator.Find("DS4Windows/UI/MainWindow.xaml");
            string code = SourceText.ReadCode("DS4Windows/UI/MainWindow.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.Contains("x:Name=\"languagePackControl\"", File.ReadAllText(xaml));
            Assert.Contains("languagePackControl.Initialize(appSettingsService, viewModelFactory.CreateLanguagePackViewModel(), themeLanguage);", code);   // Phase14b-Step16（D11）: テーマ・言語の契約も渡す
        }
    }
}
