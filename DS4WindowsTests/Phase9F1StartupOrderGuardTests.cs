using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-F F-1b（Phase9-F-Plan.md §5）: <c>App.Application_Startup</c> を手順ごとのメソッドに分けた後も、呼び出しの順序と、
    /// 手順の中の安全弁の順序（ログの設定の前に設定を読む、保存の許可は接続フラグのリセットの後、など）が変わらないことを、ソースの照合で守る。
    /// </summary>
    public class Phase9F1StartupOrderGuardTests
    {
        private static string Read()
        {
            string path = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            return path == null ? null : File.ReadAllText(path);
        }

        private static string BodyOf(string text, string signature)
        {
            int start = text.IndexOf(signature, StringComparison.Ordinal);
            Assert.True(start >= 0, signature + " がない");
            int open = text.IndexOf('{', start);
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text.Substring(open, i - open + 1);
            }

            throw new InvalidOperationException("閉じ括弧がない: " + signature);
        }

        private static void AssertOrder(string body, params string[] tokens)
        {
            int at = -1;
            foreach (string token in tokens)
            {
                int next = body.IndexOf(token, at + 1, StringComparison.Ordinal);
                Assert.True(next > at, token + " が、期待の順序にない");
                at = next;
            }
        }

        [Fact]
        public void TheStartup_CallsTheStepsInTheOriginalOrder()
        {
            string text = Read();
            if (text == null) return;

            string body = BodyOf(text, "private void Application_Startup(");
            AssertOrder(body,
                "AppNotificationRegistration.Initialize()",
                "runShutdown = true;",
                "skipSave = true;",
                "RunPreHostBootstrap(e);",
                "new ArgumentParser()",
                "TryHandleCompleteInstall(e)",
                "parser.Parse(e.Args);",
                "CheckOptions(parser);",
                "if (exitApp)",
                "SetProcessPriorities();",
                "EnsureSingleInstance()",
                "InitializeProcessWideSettings();",
                "BuildHostAndServices(parser);",
                "bool firstRun = _appSettingsService.FirstRun;",
                "RunFirstRunLanguageSelection(",
                "RunFirstRunSaveLocationAndSkeleton(",
                "EnsureMacrosFolder();",
                "LoadAppSettings(",
                "ConfigureLoggingAndDiagnostics(",
                "RunFirstLaunchSetup(",
                "PrepareProfilesCultureAndTheme();",
                "ShowMainWindowAndFinish(parser);");
        }

        [Fact]
        public void TheEarlyExits_StillStopTheStartup()
        {
            string text = Read();
            if (text == null) return;

            string body = BodyOf(text, "private void Application_Startup(");
            // 早期の終了（完了モード、CheckOptions の終了要求、別のインスタンス、初回起動の中止）は、Application_Startup から return する
            Assert.Matches(@"if \(TryHandleCompleteInstall\(e\)\)\s*\{\s*return;", body);
            Assert.Matches(@"if \(exitApp\)\s*\{\s*return;", body);
            Assert.Matches(@"if \(!EnsureSingleInstance\(\)\)\s*\{\s*return;", body);
            Assert.Matches(@"if \(!RunFirstRunLanguageSelection\(firstRun, ref selectedLanguage\)\)\s*\{\s*return;", body);
            Assert.Matches(@"if \(!RunFirstRunSaveLocationAndSkeleton\(firstRun\)\)\s*\{\s*return;", body);
        }

        [Fact]
        public void TheStepsKeepTheirSafetyOrder()
        {
            string text = Read();
            if (text == null) return;

            // Pre-Host: 設定の場所 → ログのローテーション → 診断ログ
            AssertOrder(BodyOf(text, "private void RunPreHostBootstrap("),
                "new DS4Windows.EnvironmentService()", "FindConfigLocation()", "PerformStartupRotation(", "StartupDiagnostics.CultureMessage()");

            // 単一起動の通知は、ホストの構築より前
            AssertOrder(BodyOf(text, "private void InitializeProcessWideSettings("),
                "timeBeginPeriod(1)", "LoadFromRegistry()", "RefreshViGEmBusInfo()", "new EventWaitHandle(", "CreateTempWorkerThread();");

            // ホストの構築 → ControlService → Post-Host のサービス
            AssertOrder(BodyOf(text, "private void BuildHostAndServices("),
                "AppHost.CreateHost(", "CreateControlService(parser);", "InitializePostHostServices();");

            // 設定の読み込みは、LoggerHolder の作成より前
            AssertOrder(BodyOf(text, "private bool LoadAppSettings("), "_appSettingsService.Load()", "_appSettingsService.Save()");
            AssertOrder(BodyOf(text, "private void ConfigureLoggingAndDiagnostics("),
                "new LoggerHolder(", "_logSettingsApplierProxy.Attach(logHolder);", "DispatcherUnhandledException +=", "UnhandledException +=", "StartupDiagnostics.EnvironmentMessages(", "BuildStampReader.BuildLogMessage(");

            // 保存の許可（skipSave = false）は、接続フラグのリセットの後
            AssertOrder(BodyOf(text, "private void RunFirstLaunchSetup("), "ResetConnectionFlags();", "skipSave = false;");

            // プロファイルの固定 ID → SA の読み込み → 言語 → テーマ → リンクされたプロファイル（メイン画面の生成の前）
            AssertOrder(BodyOf(text, "private void PrepareProfilesCultureAndTheme("),
                "EnsureAllPersisted();", "LoadActions()", "SetUICulture(", "ApplyLanguageSetting(", "ChangeTheme(", "LoadLinkedProfiles();");

            // メイン画面 → IPC → 出力スロット → LateChecks
            AssertOrder(BodyOf(text, "private void ShowMainWindowAndFinish("),
                "IMainWindowFactory", "window.Show();", "CreateIPCClassNameMMF(", "window.CheckMinStatus();", "CheckHidHidePresence()", "LoadOutputSlots();", "window.LateChecks(parser);");
        }
    }
}
