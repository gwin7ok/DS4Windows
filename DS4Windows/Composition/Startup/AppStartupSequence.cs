#nullable enable
namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// アプリの起動の順序と、早期の終了の条件。
    /// 順序には理由がある: ホストの作成 → 入力の制御の作成 → 起動後のサービスの解決（前の結果を次が使う）、
    /// 設定の読み込みはサービスの解決の後、プロファイルの ID と SA の読み込みは最初の画面の前、マウスホイールの登録は最初の画面の前。
    /// 手順の中身は <see cref="IAppStartupSteps"/> の実装（<c>App</c>）にあり、ここでは呼ぶ順序と、続けるかどうかだけを決める。
    /// </summary>
    public static class AppStartupSequence
    {
        public static void Run(IAppStartupSteps steps)
        {
            steps.RunPreHostBootstrap();

            if (steps.TryHandleCompleteInstall()) return;

            steps.ParseAndHandleOptions();
            if (steps.ExitRequested) return;

            steps.SetProcessPriorities();

            if (!steps.EnsureSingleInstance()) return;

            // ログのローテーションと診断ログは、単一起動の確認の後（別のインスタンスのログを、アーカイブへ回さないため）
            steps.RotateLogsAndLogStartupDiagnostics();

            steps.InitializeProcessWideSettings();
            steps.RegisterPageScrollWheel();

            steps.CreateHost();
            steps.CreateControlService();
            steps.InitializePostHostServices();

            steps.ReadFirstRunFlag();
            if (!steps.RunFirstRunLanguageSelection()) return;
            if (!steps.RunFirstRunSaveLocationAndSkeleton()) return;

            steps.EnsureMacrosFolder();
            steps.LoadAppSettings();
            steps.ConfigureLoggingAndDiagnostics();
            steps.RunFirstLaunchSetup();

            steps.PrepareProfilesCultureAndTheme();
            steps.ShowMainWindowAndFinish();
        }
    }
}
