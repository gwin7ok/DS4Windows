using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;
using DS4Windows.Composition;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Utilities;
using DS4Windows.UI.Abstractions;

namespace DS4Windows
{
    /// <summary>起動の流れ（ホストの構築の前後の準備、初回の設定、設定の読み込み）。</summary>
    public partial class App
    {
        /// <summary>
        /// Phase9 9-F F-1b: ブートストラップのサービスと、設定の場所の決定（Pre-Host。旧 Application_Startup の先頭の部分）。
        /// ログのローテーションと起動時の診断ログは、単一起動の確認の後（<see cref="RotateLogsAndLogStartupDiagnostics"/>）。
        /// </summary>
        private void RunPreHostBootstrap(StartupEventArgs e)
        {
            // Phase7-Step4 W4: ブートストラップのサービスを、コンテナの構築より前に作る
            _bootstrapEnvironmentService = new DS4Windows.Core.Platform.EnvironmentService();
            _bootstrapBackingStore = new DS4Windows.Core.Settings.BackingStore(_bootstrapEnvironmentService);
            _bootstrapPathService = new DS4Windows.Core.Platform.PathService(config: _bootstrapBackingStore);
            _themeCulture = new DS4Windows.UI.Services.AppThemeAndCultureService(_bootstrapBackingStore);

            // Locate config location as early as possible
            try
            {
                _bootstrapPathService.FindConfigLocation();
                _bootstrapConfigLocationFound = true;
            }
            catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[App:178] ignored {caught.GetType().Name}: {caught.Message}"); }
        }

        /// <summary>
        /// ログのローテーション（決定的な起動時のローテーション）と、起動時の診断ログ。
        /// 変更（Phase9 9-F、ユーザー指示 2026-10-23）: 単一起動の確認（<see cref="EnsureSingleInstance"/>）の<b>後</b>に行う。以前は確認の前に行っていたため、
        /// 2 つ目のインスタンスを起動すると、動いている 1 つ目のインスタンスのログが、アーカイブへ回されていた。別のインスタンスが動いていれば、ここへは来ない
        /// （ログのファイルに、何も書かない）。
        /// </summary>
        private void RotateLogsAndLogStartupDiagnostics(StartupEventArgs e)
        {
            try
            {
                if (!_bootstrapConfigLocationFound) throw new InvalidOperationException("config location not found");

                // Perform deterministic startup rotation
                // （Phase7-Step4 W4: appdatapath は null の可能性がある（初回起動）ため、AppDataPath（null のとき実行フォルダ）ではなく状態そのものを渡す）
                LogRotator.PerformStartupRotation(DS4Windows.Core.Platform.PathService.AppDataPathState, _bootstrapBackingStore.Log.logMaxArchiveFiles);

                // Immediately apply bootstrap minimum log level so early logging honors user-configured min level
                try
                {
                    DS4Windows.Core.Logging.LoggerHolder.ApplyBootstrapMinLogLevel(_bootstrapBackingStore.Log.logMinLevel);
                }
                catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[App:205] ignored {caught.GetType().Name}: {caught.Message}"); }
            }
            catch { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }

            try
            {
                // Detailed diagnostics at Debug level
                // Phase9 9-F F-1a: 文言は StartupDiagnostics（文言とレベル、順序は変えない）
                AppLogger.LogInfo(StartupDiagnostics.CultureMessage());
                AppLogger.LogInfo(StartupDiagnostics.DefaultCultureMessage());

                // Log command-line args for Updater-related diagnostic
                try
                {
                    AppLogger.LogInfo(StartupDiagnostics.ArgsMessage(e.Args));
                }
                catch { /* 診断の出力の失敗は無視してよい */ }

                string langDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Lang");
                AppLogger.LogInfo(StartupDiagnostics.LangFolderMessage(langDir));
                if (System.IO.Directory.Exists(langDir))
                {
                    try
                    {
                        AppLogger.LogInfo(StartupDiagnostics.LangSubdirsMessage(langDir));
                    }
                    catch (Exception ex) { AppLogger.LogError($"LangDirList exception: {ex}"); }
                }
            }
            catch (Exception) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
        }

        /// <summary>
        /// Phase9 9-F F-1b: 昇格した更新の完了モード（--complete-install）の処理。完了モードなら、処理して終了コードで終了する（戻らない）。通常の起動なら false。
        /// </summary>
        private bool TryHandleCompleteInstall(StartupEventArgs e)
        {
            try
            {
                // Phase9 9-F F-1a: 引数の読み取りは CompleteInstallArguments
                if (CompleteInstallArguments.TryParse(e.Args, out string? src, out string? tgt))
                {
                    // We are in elevated completion mode — perform move/copy and exit
                    int exit = DS4Windows.Core.Updater.UpdaterElevatedHelper.CompleteInstall(src, tgt);
                    // Ensure logs flushed then exit
                    try { NLog.LogManager.Flush(); }
                    catch (Exception caught)
                    {
                        /* 失敗しても処理を続ける（呼び出し元に影響させない） */
                        AppLogger.LogDebug($"[App:250] ignored {caught.GetType().Name}: {caught.Message}");
                    }
                    Environment.Exit(exit);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong here, log and continue with normal startup
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error handling --complete-install args");
            }
            return false;
        }

        /// <summary>
        /// Phase9 9-F F-1b: プロセスの優先度（High）、IO の優先度、ページの優先度（Normal）の設定。
        /// </summary>
        private void SetProcessPriorities()
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass =
                    ProcessPriorityClass.High;
            }
            catch (Exception caught) when (caught is System.ComponentModel.Win32Exception or System.InvalidOperationException or System.NotSupportedException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ } // Ignore problems raising the priority.

            // Force Normal IO Priority
            IntPtr ioPrio = new IntPtr(2);
            DS4Windows.Core.Utilities.Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                DS4Windows.Core.Utilities.Util.PROCESS_INFORMATION_CLASS.ProcessIoPriority, ref ioPrio, 4);

            // Force Normal Page Priority
            IntPtr pagePrio = new IntPtr(5);
            DS4Windows.Core.Utilities.Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                DS4Windows.Core.Utilities.Util.PROCESS_INFORMATION_CLASS.ProcessPagePriority, ref pagePrio, 4);
        }

        /// <summary>
        /// Phase9 9-F F-1b: 単一起動の確認。別のインスタンスが動いていれば、そちらへ通知して、このインスタンスの終了を要求し、false を返す。
        /// </summary>
        private bool EnsureSingleInstance()
        {
            if (_singleInstance.TryNotifyExistingInstance())
            {
                runShutdown = false;
                Current.Shutdown();    // Quit temp instance
                return false;
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: タイマーの精度、キーボードのリピート設定、ViGEmBus の情報、単一起動の通知用イベントと一時ワーカー（Pre-Host の最後）。
        /// </summary>
        private void InitializeProcessWideSettings()
        {
            // Allow sleep time durations less than 16 ms
            DS4Windows.Core.Utilities.Util.TimeBeginPeriod(1);

            // Load keyboard repeat settings from user registry so synthetic repeat matches OS behaviour
            try
            {
                DS4Windows.Core.Platform.KeyboardSettings.LoadFromRegistry();
            }
            catch (Exception caught) when (caught is System.Security.SecurityException or System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }

            // Retrieve info about installed ViGEmBus device if found
            // Phase7-Step4 W1／W4: 調べ物と状態は EnvironmentService（状態は static）が持つ。ブートストラップのインスタンスを使う。
            _bootstrapEnvironmentService.RefreshViGEmBusInfo();

            // 2 つ目のインスタンスからの知らせを待ち、来たら画面を前面へ出す（画面は UI スレッドで操作する）
            _singleInstance.StartListening(() => Dispatcher.BeginInvoke((Action)(() =>
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            })));
        }

        /// <summary>
        /// Phase9 9-F F-1b: ホストの構築、ControlService の作成、Post-Host のサービスの解決（Pre-Host 領域の終わり）。
        /// </summary>
        private void CreateHostExplicitly(ArgumentParser parser)
        {
            // Phase6-Step7: ここまでが Pre-Host 領域（明示的なホスト構築より前に実行される起動処理）。
            // この領域と CheckOptions（-driverinstall 分岐を含む）の Global 参照（起動時ログローテーション・
            // 設定の場所の決定・ViGEmBus 情報など）は、ブートストラップ処理として意図的に静的のまま温存する
            // （docs-forDIMG/MadeByAgent/Archive/Phase6-Step7-Plan.md §1.2・§2.5）。Phase7-Step4 W2: -driverinstall 分岐は、設定の読込の前に、
            // CheckOptions の中で CreateHost(config, parser) を明示的に呼ぶ（暗黙のホスト構築はなくなった）。
            // DI サービスは、下の CreateControlService の直後に InitializePostHostServices で解決する。
            // フェーズ0-3: AppHost正式ルート
            try
            {
                var host = AppHost.CreateHost(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), parser,
                    _bootstrapPathService, _bootstrapEnvironmentService, _bootstrapBackingStore);
                AppLogger.LogInfo("AppHost.CreateHost() called successfully (Phase 0-3 verification)");
                AppLogger.LogInfo($"[DI] Host creation path: {AppHost.CreationNote}");
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"AppHost verification call failed (expected if no full config): {ex.Message}");
            }

        }

        /// <summary>
        /// Phase9 9-F F-1b: Profiles.xml の読み込み（ログの設定を使うため、LoggerHolder の作成より前）と、初回起動で選んだ言語の再適用・保存。読み込めたかを返す。
        /// </summary>
        private bool LoadAppSettings(bool firstRun, string? selectedLanguage)
        {
            // Load Profiles.xml BEFORE creating LoggerHolder so log settings are available
            bool readAppConfig = _appSettingsService.Load();

            // Re-apply selected language after Load() and save to Profiles.xml
            if (firstRun && !string.IsNullOrEmpty(selectedLanguage))
            {
                _themeCulture.ApplyLanguageSetting(selectedLanguage);
                _appSettingsService.Save();
            }
            return readAppConfig;
        }

        /// <summary>
        /// Phase9 9-F F-1b: LoggerHolder の作成、例外ハンドラーの登録、環境・ビルド日時のログ。
        /// </summary>
        private void ConfigureLoggingAndDiagnostics(bool firstRun, bool readAppConfig)
        {
            logHolder = new LoggerHolder(_appSettingsService);
            // Phase6-Step10-1-5（決定D7＝A）: 設定画面がログ設定の変更を反映する窓口（ILogSettingsApplier）へ、生成した LoggerHolder を接続する
            _logSettingsApplierProxy.Attach(logHolder);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string? version = _environmentService.ApplicationVersion;
            // Log process and path information at INFO level now that logger is configured
            string? processExe = null;
            try { processExe = Process.GetCurrentProcess().MainModule?.FileName; }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException || ex is NotSupportedException)
            {
                // モジュールを読めない環境（権限など）では、ファイル名なしでログを出す。
            }
            AppLogger.LogInfo($"Process exe: {processExe}");
            AppLogger.LogInfo($"AppDomain BaseDirectory: {AppContext.BaseDirectory}");
            AppLogger.LogInfo($"Current working directory: {Environment.CurrentDirectory}");
            // Phase9 9-F F-1a: 文言は StartupDiagnostics（文言と順序は変えない）
            foreach (string environmentMessage in StartupDiagnostics.EnvironmentMessages(version, _pathService.ExecutablePath,
                DS4Windows.Core.Utilities.Util.GetOSProductName(), DS4Windows.Core.Utilities.Util.GetOSReleaseId()))
            {
                AppLogger.LogInfo(environmentMessage);
            }
            AppLogger.LogInfo("Logger created");

            // Startup summary: DS4Windows.exe（アセンブリ）に埋め込まれたビルド日時とコミットを読む（csproj の EmbedBuildStamp）
            try
            {
                string? stamp = BuildStampReader.FromAssembly(typeof(App).Assembly);
                if (stamp != null)
                {
                    // Phase9 9-F F-1a: 文言づくりは BuildStampReader（解釈中の例外は、これまでどおり握りつぶす）
                    string? buildMessage = BuildStampReader.BuildLogMessage(stamp);
                    if (buildMessage != null) AppLogger.LogInfo(buildMessage);
                }
                else
                {
                    AppLogger.LogInfo("Build summary not found in the executable");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"Startup summary read failed: {ex}");
            }

            if (!firstRun && !readAppConfig)
            {
                AppLogger.LogInfo($@"Profiles.xml not read at location ${_pathService.AppDataPath}\Profiles.xml. Using default app settings");
            }
        }

        /// <summary>
        /// Phase9 9-F F-1b: メイン画面の生成と表示、IPC の準備、HidHide の確認、出力スロットの読み込み、LateChecks（起動の最後）。
        /// </summary>
        private void ShowMainWindowAndFinish(ArgumentParser parser)
        {
            // Phase7-Step5 W5（Q4＝A）: メイン画面の生成の窓口を、生成の直前に解決する（窓口が受け取る多数のサービスの生成が
            // 設定の読込より前にならないよう、InitializePostHostServices では解決しない。旧: MainWindow のコンストラクター内で解決していた）
            DS4Windows.UI.MainWindow window = ResolvePostHostService<IMainWindowFactory>().Create(parser);
            MainWindow = window;
            window.IsInitialShow = true;
            window.Show();
            // Diagnostic: verify culture remains applied after window shown
            AppLogger.LogDebug($"After Show(): LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            window.IsInitialShow = false;

            // Set up hooks for IPC command calls
            HwndSource? source = PresentationSource.FromVisual(window) as HwndSource;
            _ipc.PublishWindowClassName(source!.Handle); // WPF のウィンドウは HwndSource を持つ（null なら従来どおり NullReferenceException）

            window.CheckMinStatus();

            bool runningAsAdmin = _environmentService.IsAdministrator();
            AppLogger.LogUser(AppLogLevel.Info, runningAsAdmin ? "Log_App_RunningAsAdmin" : "Log_App_RunningAsUser");

            new DriverStatusReporter(_environmentService).RefreshAndLog();
            if (_environmentService.HidHideInstalled)
            {
                _runtimeFeatures.CheckHidHidePresence();
            }

            // Phase7-Step5 W3（Q9＝A）: 契約（IOutputSlotService）経由に切り替えた。旧 ControlService.LoadPermanentSlotsConfig と同じ処理
            // （OutputSlotPersist.ReadConfig を、同じスロット管理と保存先で呼ぶ）
            _outputSlotService.LoadOutputSlots();
            window.LateChecks(parser);
        }

        /// <summary>
        /// Phase6-Step7（決定1＝A）: Post-Host で使う DI サービスを一度だけ解決してフィールドに保持する。
        /// 明示的なホスト構築と CreateControlService の直後に呼ぶ。解決できない場合は、CreateControlService と同じく
        /// 例外で起動を止める（ホスト構築の失敗は、従来から起動失敗である）。
        /// </summary>
        private void InitializePostHostServices()
        {
            _post = PostHostServices.Resolve();
            _themeCulture = _post.ThemeCulture;
            // 静的なロガー（AppLogger.LogToTray）が使う通知サービスを、DI のシングルトンに設定する
            AppLogger.NotificationService = _post.Notifications;

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] App.InitializePostHostServices: Post-Host services resolved");
        }
    }
}
