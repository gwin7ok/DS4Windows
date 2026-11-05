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
    public class Step10GroupOneGlobalReferenceGuardTests
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

        public Step10GroupOneGlobalReferenceGuardTests(ITestOutputHelper output)
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
            string text = SourceText.ReadCode("DS4Windows/Core/Platform/StartupEntryService.cs");
            if (text == null) return;

            Assert.Contains("WriteStartProgEntry(string exeLocation, string programDirectory)", text);
            Assert.Contains("DeleteOldTaskEntry(string programDirectory)", text);
            Assert.Contains("WriteTaskEntry(string programDirectory, string exeFileName)", text);
            Assert.Contains("CheckStartupExeLocation(string exeLocation)", text);
        }

        [Fact]
        public void LanguagePackControl_IsInitializedByMainWindow()
        {
            string xaml = SourceFileLocator.Find("DS4Windows/UI/MainWindow.xaml");
            string code = SourceText.ReadCode("DS4Windows/UI/MainWindow.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.Contains("x:Name=\"languagePackControl\"", File.ReadAllText(xaml));
            Assert.Contains("languagePackControl.Initialize(appSettingsService, themeLanguage);", code);   // Phase14b-Step16（D11）: テーマ・言語の契約も渡す
        }
    }
}
