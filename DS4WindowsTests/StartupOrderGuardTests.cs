using System;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 手順の呼び出しの順序は <c>AppStartupSequence</c>（<c>AppStartupSequenceTests</c> が挙動で守る）。
    /// ここは、1 つの手順の中の安全弁の順序（ログの設定の前に設定を読む、保存の許可は接続フラグのリセットの後、など）を、ソースの照合で守る。
    /// </summary>
    public class StartupOrderGuardTests
    {
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
        public void TheStepsKeepTheirSafetyOrder()
        {
            string text = SourceText.Read("DS4Windows/App.xaml.cs");
            if (text == null) return;

            // Pre-Host: ブートストラップのサービス → 設定の場所
            AssertOrder(SourceText.Body(text, "private void RunPreHostBootstrap("),
                "new DS4Windows.Core.Platform.EnvironmentService()", "FindConfigLocation()");

            // ログのローテーション → 診断ログ（単一起動の確認の後。Phase9 9-F、ユーザー指示 2026-10-23）
            AssertOrder(SourceText.Body(text, "private void RotateLogsAndLogStartupDiagnostics("),
                "PerformStartupRotation(", "ApplyBootstrapMinLogLevel(", "StartupDiagnostics.CultureMessage()");

            // 単一起動の通知は、ホストの構築より前
            AssertOrder(SourceText.Body(text, "private void InitializeProcessWideSettings("),
                "TimeBeginPeriod(1)", "LoadFromRegistry()", "RefreshViGEmBusInfo()", "_singleInstance.StartListening(");

            // ホストの構築（ControlService の作成と Post-Host のサービスの解決は、起動の順序 AppStartupSequence の手順。AppStartupSequenceTests が守る）
            AssertOrder(SourceText.Body(text, "private void CreateHostExplicitly("),
                "AppHost.CreateHost(");

            // 設定の読み込みは、LoggerHolder の作成より前
            AssertOrder(SourceText.Body(text, "private bool LoadAppSettings("), "_appSettingsService.Load()", "_appSettingsService.Save()");
            AssertOrder(SourceText.Body(text, "private void ConfigureLoggingAndDiagnostics("),
                "new LoggerHolder(", "_logSettingsApplierProxy.Attach(logHolder);", "DispatcherUnhandledException +=", "UnhandledException +=", "StartupDiagnostics.EnvironmentMessages(", "BuildStampReader.BuildLogMessage(");

            // 保存の許可（skipSave = false）は、接続フラグのリセットの後
            AssertOrder(SourceText.Body(text, "private void RunFirstLaunchSetup("), "ResetConnectionFlags();", "skipSave = false;");

            // プロファイルの固定 ID → SA の読み込み（ProfileStartupLoader。ProfileStartupLoaderTests が守る）→ 言語 → テーマ → リンクされたプロファイル（メイン画面の生成の前）
            AssertOrder(SourceText.Body(text, "private void PrepareProfilesCultureAndTheme("),
                "ProfileStartupLoader(", "SetUICulture(", "ApplyLanguageSetting(", "ChangeTheme(", "LoadLinkedProfiles();");

            // ログのローテーションは、確認の後の 1 か所だけ（順序は AppStartupSequenceTests）。ほかの手順の中へ置かない
            Assert.Single(Regex.Matches(text, @"PerformStartupRotation\("));
            Assert.DoesNotContain("PerformStartupRotation(", SourceText.Body(text, "private void RunPreHostBootstrap("));
            Assert.DoesNotContain("PerformStartupRotation(", SourceText.Body(text, "private bool EnsureSingleInstance("));
            Assert.DoesNotContain("PerformStartupRotation(", SourceText.Body(text, "private void CheckOptions("));

            // メイン画面 → IPC → 出力スロット → LateChecks
            AssertOrder(SourceText.Body(text, "private void ShowMainWindowAndFinish("),
                "IMainWindowFactory", "window.Show();", "PublishWindowClassName(", "window.CheckMinStatus();", "CheckHidHidePresence()", "LoadOutputSlots();", "window.LateChecks(parser);");
        }
    }
}
