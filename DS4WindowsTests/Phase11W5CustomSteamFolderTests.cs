using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W5（Issue #4 の 3、Q3）: 設定画面の「カスタム Steam フォルダー」に、存在しないパスを入れたときの扱い。
    /// 旧: <c>SettingsViewModel.CustomSteamFolder</c> の setter は、存在するフォルダーか空文字のときだけ保存し、
    /// それ以外は何も言わずに捨てていた。TextBox には入力が残るため、保存されたように見える（Issue #4 の 3 の実体）。
    /// 復元後: 値は保存し、警告（GUI ログの warning。ステータス行に目立つ色で出る）を出す。入力を黙って捨てない。
    /// 値を使う側（<c>AutoProfiles</c>）は <c>Directory.Exists</c> で確かめ、無ければ既定の Steam フォルダーへ戻るため、保存しても害はない。
    /// </summary>
    public class Phase11W5CustomSteamFolderTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly object _gate = new object();
        private readonly List<DebugEventArgs> _guiLog = new List<DebugEventArgs>();
        private readonly string _existingFolder;
        private readonly string _missingFolder;
        private readonly SettingsViewModel _vm;
        private readonly IAppSettingsService _service;

        public Phase11W5CustomSteamFolderTests(ITestOutputHelper output)
        {
            _output = output;
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            string unique = "Phase11W5Steam_" + Guid.NewGuid().ToString("N");
            _existingFolder = Path.Combine(Path.GetTempPath(), unique + "_exists");
            _missingFolder = Path.Combine(Path.GetTempPath(), unique + "_missing");
            Directory.CreateDirectory(_existingFolder);

            _service = TestRepositoryFactory.CreateAppSettingsService(store: new BackingStore(new EnvironmentService()));
            _vm = SettingsViewModelTestFactory.Create(_service);
            AppLogger.GuiLog += OnGuiLog;
        }

        public void Dispose()
        {
            AppLogger.GuiLog -= OnGuiLog;
            _vm.Dispose();
            try { Directory.Delete(_existingFolder, true); } catch { }
        }

        private void OnGuiLog(object sender, DebugEventArgs e)
        {
            lock (_gate) { _guiLog.Add(e); }
        }

        /// <summary>このテストの一時フォルダーを名指しする GUI ログ（並行して動く他のテストのログを除く）</summary>
        private DebugEventArgs[] LogsAbout(string folder)
        {
            lock (_gate) { return _guiLog.Where(e => e.Data != null && e.Data.Contains(folder)).ToArray(); }
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void ANonExistentFolder_IsKept_AndWarned()
        {
            _vm.CustomSteamFolder = _missingFolder;

            // 入力を黙って捨てない（旧: 保存されず、何も言わなかった）
            Assert.Equal(_missingFolder, _service.CustomSteamFolder);
            Assert.Equal(_missingFolder, _vm.CustomSteamFolder);

            // 警告を出す: GUI ログの warning。入力したパスを含める
            DebugEventArgs[] logs = LogsAbout(_missingFolder);
            Assert.Single(logs);
            Assert.True(logs[0].Warning, "警告（warning）として出すこと");
            _output.WriteLine(logs[0].Data);
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void AnExistingFolder_IsKept_WithoutAWarning()
        {
            _vm.CustomSteamFolder = _existingFolder;

            Assert.Equal(_existingFolder, _service.CustomSteamFolder);
            Assert.Empty(LogsAbout(_existingFolder));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void AnEmptyValue_ClearsTheFolder_WithoutAWarning()
        {
            _vm.CustomSteamFolder = _existingFolder;

            _vm.CustomSteamFolder = string.Empty;

            Assert.Equal(string.Empty, _service.CustomSteamFolder);
            // 空に戻すときは、警告しない（空文字は「指定なし」。旧コードも空文字は保存していた）。
            // 警告の文言に空のパスを入れた文は、どこにも出ていない
            string warningForEmpty = string.Format(
                DS4Windows.Translations.Strings.ResourceManager.GetString("CustomSteamFolderNotFound", CultureInfo.CurrentUICulture),
                string.Empty);
            lock (_gate) { Assert.DoesNotContain(_guiLog, e => e.Data == warningForEmpty); }
            Assert.Empty(LogsAbout(_existingFolder));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void ANullValue_IsStoredAsEmpty()
        {
            _vm.CustomSteamFolder = _existingFolder;

            _vm.CustomSteamFolder = null;

            Assert.Equal(string.Empty, _service.CustomSteamFolder);
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void TheSameNonExistentFolder_IsNotWarnedAgain()
        {
            _vm.CustomSteamFolder = _missingFolder;
            _vm.CustomSteamFolder = _missingFolder;

            Assert.Single(LogsAbout(_missingFolder));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void ChangingFromAMissingFolderToAnExistingOne_ReplacesTheValue()
        {
            _vm.CustomSteamFolder = _missingFolder;

            _vm.CustomSteamFolder = _existingFolder;

            Assert.Equal(_existingFolder, _service.CustomSteamFolder);
            Assert.Empty(LogsAbout(_existingFolder));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void TheWarningText_ExistsInEnglishAndJapanese_WithTheFolderPlaceholder()
        {
            const string key = "CustomSteamFolderNotFound";
            var resources = DS4Windows.Translations.Strings.ResourceManager;

            string english = resources.GetString(key, CultureInfo.InvariantCulture);
            string japanese = resources.GetString(key, new CultureInfo("ja"));

            Assert.False(string.IsNullOrWhiteSpace(english), "英語（既定）のリソースがない");
            Assert.False(string.IsNullOrWhiteSpace(japanese), "日本語のリソースがない");
            Assert.NotEqual(english, japanese);
            Assert.Contains("{0}", english);
            Assert.Contains("{0}", japanese);
            // 日本語の文言に、かな・漢字が含まれる（英語のコピーではない）
            Assert.Matches(@"[぀-ヿ一-鿿]", japanese);
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void TheDesignerAccessor_ReturnsTheResourceText()
        {
            Assert.Equal(
                DS4Windows.Translations.Strings.ResourceManager.GetString("CustomSteamFolderNotFound", CultureInfo.CurrentUICulture),
                DS4Windows.Translations.Strings.CustomSteamFolderNotFound);
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void TheConsumer_StillChecksTheFolderExists_SoSavingAMissingFolderIsHarmless()
        {
            string path = SourceFileLocator.Find("DS4Windows/UI/AutoProfiles.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("AutoProfiles.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            text = Regex.Replace(text, @"\s+", " ");

            // 使う側が、存在を確かめてから使う（無ければ既定の Steam フォルダーへ）。この確認を外す変更は、保存を許す前提を壊す
            Assert.Contains("if (appSettingsService.UseCustomSteamFolder && Directory.Exists(appSettingsService.CustomSteamFolder))", text);
        }
    }
}
