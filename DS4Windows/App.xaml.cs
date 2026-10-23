/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DS4WinWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public partial class App : Application
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string sClass, string sWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private Thread controlThread;
        public static HttpClient requestClient;
        private bool skipSave;
        private bool runShutdown;
        private bool exitApp;
        private Thread testThread;
        private bool exitComThread = false;
        private const string SingleAppComEventName = "{a52b5b20-d9ee-4f32-8518-307fa14aa0c6}";
        private EventWaitHandle threadComEvent = null;
        private Timer collectTimer;
        public static LoggerHolder logHolder;

        // Phase6-Step7（決定1＝A）: Post-Host（明示的なホスト構築と CreateControlService の後）で使う DI サービス。
        // App は WPF の Application でコンストラクタ注入できないため、Composition Root として
        // InitializePostHostServices() で一度だけ解決して保持する。Pre-Host 領域（ホスト構築より前）では null。
        private IAppSettingsService _appSettingsService;
        private IPathService _pathService;
        private IEnvironmentService _environmentService;
        private IProfileRepository _profileRepository;
        private IProfileIdentityService _profileIdentityService;   // Phase10-Step4d W1
        private ISpecialActionRepository _specialActionRepository;
        private IAppearanceSettingsService _appearanceSettingsService;
        private IDeviceStateService _deviceStateService;
        private IOutputSlotService _outputSlotService;
        // Phase7-Step5 W4: 旧 AppHost.GetService／ServiceProviderHolder の直接呼び出しの置き換え先（InitializePostHostServices で解決）
        private DS4Windows.Services.LogSettingsApplierProxy _logSettingsApplierProxy;
        private DS4Windows.Actions.IControllerRegistry _controllerRegistry;

        // Phase7-Step5 W2（Q1＝A）: 旧 Program.rootHub。ControlService は CreateControlService の別スレッドで解決して代入し、
        // 呼び出し元がスレッドの終了を待ってから読む（以降は LoggerHolder の生成、起動後の処理、未処理例外の処理、終了処理が使う）。
        // 別スレッド（未処理例外、セッション終了）からも読まれるため volatile にする。
        private volatile DS4Windows.ControlService _controlService;

        // Phase7-Step4 W4（D3＝S1）: コンテナの構築より前に作るブートストラップのサービス。Pre-Host の起動処理（保存場所の決定、
        // ViGEmBus 情報の取得）で使い、CreateHost に渡して、コンテナへ同じインスタンスを登録する。
        // 状態は PathService／EnvironmentService の static（プロセス全体で 1 つ）のため、どのインスタンスでも同じ値を見る。
        private DS4Windows.PathService _bootstrapPathService;
        private DS4Windows.EnvironmentService _bootstrapEnvironmentService;
        // Phase7-Step10d W5（Q3＝A）: 旧 Global.store（静的）。コンテナの構築より前に作り、CreateHost に渡して、コンテナへ同じインスタンスを登録する
        // （起動直後のログのローテーションと最小ログレベル、-driverinstall 分岐の言語・テーマの読み取りが使う）。
        private DS4Windows.BackingStore _bootstrapBackingStore;

        private MemoryMappedFile ipcClassNameMMF = null; // MemoryMappedFile for inter-process communication used to hold className of DS4Form window
        private MemoryMappedFile ipcResultDataMMF = null; // MemoryMappedFile for inter-process communication used to exchange string result data between cmdline client process and the background running DS4Windows app

        private static Dictionary<DS4Windows.AppThemeChoice, string> themeLocs = new
            Dictionary<DS4Windows.AppThemeChoice, string>()
        {
            [DS4Windows.AppThemeChoice.Default] = "DS4Forms/Themes/DefaultTheme.xaml",
            [DS4Windows.AppThemeChoice.Light] = "DS4Forms/Themes/DefaultTheme.xaml",
            [DS4Windows.AppThemeChoice.Dark] = "DS4Forms/Themes/DarkTheme.xaml",
        };

        public event EventHandler ThemeChanged;

        /// <summary>
        /// Apply culture settings to both Global and WPF LocalizeDictionary
        /// </summary>
        private void ApplyLanguageSetting(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
                return;

            try
            {
                try
                {
                    // Ensure Logs folder exists under application base directory so initial NLog writes succeed
                    string baseLogs = Path.Combine(AppContext.BaseDirectory, "Logs");
                    if (!Directory.Exists(baseLogs)) Directory.CreateDirectory(baseLogs);
                }
                catch { }
                AppLogger.LogDebug($"ApplyLanguageSetting requested: {cultureCode}");
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureCode);
                // 旧 TODO(Phase6-Step7 決定4＝K)（解消済み）: このメソッドは Post-Host だけでなく、Pre-Host の -driverinstall 分岐
                // （CheckOptions。DI サービスのフィールドはまだ null）と、App の外（LanguagePackControl → ApplyLanguageSettingPublic）
                // からも呼ばれる共用ヘルパーのため、DI 経由の二重経路を作らない。SetCulture はスレッドのカルチャを設定するだけの静的ユーティリティ。
                // （Phase7c W3: 当時は Global を直接使っていた。Global は Phase7-Step10d で削除済みで、現在は Global を参照しない。）
                // Phase7-Step10d W5: Global は削除した。ブートストラップの BackingStore（コンテナの Singleton と同一）へ書く。
                _bootstrapBackingStore.useLang = cultureCode;
                DS4Windows.UiCulture.Set(cultureCode);

                // Ensure thread and default thread cultures are set so UI localization applies
                try
                {
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
                catch { }

                // Enable WPFLocalizeExtension to apply culture to current thread resources
                try { LocalizeDictionary.Instance.SetCurrentThreadCulture = true; } catch { }
                LocalizeDictionary.Instance.Culture = culture;

                AppLogger.LogDebug($"ApplyLanguageSetting applied: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            }
            catch
            {
                // Skip if culture cannot be set
            }
        }

        /// <summary>
        /// Public wrapper for ApplyLanguageSetting to allow access from other components
        /// </summary>
        public void ApplyLanguageSettingPublic(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
            {
                // 「No (English UI)」（カルチャ名が空）の選択。ApplyLanguageSetting は空文字を無視して何も変えないため、
                // 起動時に UseLang が空のときと同じ処理（SetUICulture。空文字はインバリアントカルチャ＝既定の英語 UI）を
                // 実行時にも行う。これまでは、再起動するまで UI が切り替わらなかった。
                // Global.UseLang は呼び出し元（LanguagePackViewModel.ChangeLanguagePack）が設定済み。
                SetUICulture(string.Empty);
                return;
            }

            ApplyLanguageSetting(cultureCode);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ★ 最優先で AUMID とレジストリを初期化（ポータブル通知対策）
            DS4Windows.AppNotificationRegistration.Initialize();

            runShutdown = true;
            skipSave = true;

            // Phase9 9-F F-1b（Phase9-F-Plan.md §5）: 起動の手順を、順に呼ぶだけにした（各手順は、下のメソッド。順序と、早期の終了の条件は、従来のまま。
            // 呼び出しの順序は Phase9F1StartupOrderGuardTests が守る）
            RunPreHostBootstrap(e);

            ArgumentParser parser = new ArgumentParser();
            // Early check: handle elevated 'complete install' mode before normal startup
            if (TryHandleCompleteInstall(e))
            {
                return;
            }

            parser.Parse(e.Args);
            CheckOptions(parser);

            if (exitApp)
            {
                return;
            }

            SetProcessPriorities();

            if (!EnsureSingleInstance())
            {
                return;   // 別のインスタンスが動いている（そちらへ通知して、この一時的なインスタンスは終了する）
            }

            // ログのローテーションと診断ログは、単一起動の確認の後（別のインスタンスのログを、アーカイブへ回さないため）
            RotateLogsAndLogStartupDiagnostics(e);

            InitializeProcessWideSettings();

            BuildHostAndServices(parser);

            // FindConfigLocation was already called earlier to enable rotation as early as possible
            bool firstRun = _appSettingsService.FirstRun;
            string selectedLanguage = null;

            if (!RunFirstRunLanguageSelection(firstRun, ref selectedLanguage))
            {
                return;
            }

            if (!RunFirstRunSaveLocationAndSkeleton(firstRun))
            {
                return;
            }

            EnsureMacrosFolder();

            bool readAppConfig = LoadAppSettings(firstRun, selectedLanguage);

            ConfigureLoggingAndDiagnostics(firstRun, readAppConfig);

            RunFirstLaunchSetup(firstRun);

            PrepareProfilesCultureAndTheme();

            ShowMainWindowAndFinish(parser);
        }

        /// <summary>
        /// Phase9 9-F F-1b: ブートストラップのサービスと、設定の場所の決定（Pre-Host。旧 Application_Startup の先頭の部分）。
        /// ログのローテーションと起動時の診断ログは、単一起動の確認の後（<see cref="RotateLogsAndLogStartupDiagnostics"/>）。
        /// </summary>
        private void RunPreHostBootstrap(StartupEventArgs e)
        {
            // Phase7-Step4 W4: ブートストラップのサービスを、コンテナの構築より前に作る
            _bootstrapEnvironmentService = new DS4Windows.EnvironmentService();
            _bootstrapBackingStore = new DS4Windows.BackingStore(_bootstrapEnvironmentService);
            _bootstrapPathService = new DS4Windows.PathService(config: _bootstrapBackingStore);

            // Locate config location as early as possible
            try
            {
                _bootstrapPathService.FindConfigLocation();
                _bootstrapConfigLocationFound = true;
            }
            catch { }
        }

        // 設定の場所を決められたか（決められなかったときは、従来どおり、ローテーションも行わない）
        private bool _bootstrapConfigLocationFound;

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
                LogRotator.PerformStartupRotation(DS4Windows.PathService.AppDataPathState, _bootstrapBackingStore.logMaxArchiveFiles);

                // Immediately apply bootstrap minimum log level so early logging honors user-configured min level
                try
                {
                    DS4WinWPF.LoggerHolder.ApplyBootstrapMinLogLevel(_bootstrapBackingStore.logMinLevel);
                }
                catch { }
            }
            catch { }

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
                catch { }

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
            catch (Exception) { }
        }

        /// <summary>
        /// Phase9 9-F F-1b: 昇格した更新の完了モード（--complete-install）の処理。完了モードなら、処理して終了コードで終了する（戻らない）。通常の起動なら false。
        /// </summary>
        private bool TryHandleCompleteInstall(StartupEventArgs e)
        {
            try
            {
                // Phase9 9-F F-1a: 引数の読み取りは CompleteInstallArguments
                if (CompleteInstallArguments.TryParse(e.Args, out string src, out string tgt))
                {
                    // We are in elevated completion mode — perform move/copy and exit
                    int exit = DS4WinWPF.UpdaterElevatedHelper.CompleteInstall(src, tgt);
                    // Ensure logs flushed then exit
                    try { NLog.LogManager.Flush(); } catch { }
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
            catch { } // Ignore problems raising the priority.

            // Force Normal IO Priority
            IntPtr ioPrio = new IntPtr(2);
            DS4Windows.Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                DS4Windows.Util.PROCESS_INFORMATION_CLASS.ProcessIoPriority, ref ioPrio, 4);

            // Force Normal Page Priority
            IntPtr pagePrio = new IntPtr(5);
            DS4Windows.Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                DS4Windows.Util.PROCESS_INFORMATION_CLASS.ProcessPagePriority, ref pagePrio, 4);
        }

        /// <summary>
        /// Phase9 9-F F-1b: 単一起動の確認。別のインスタンスが動いていれば、そちらへ通知して、このインスタンスの終了を要求し、false を返す。
        /// </summary>
        private bool EnsureSingleInstance()
        {
            // another instance is already running if TryOpenExisting returns true.
            try
            {
                if (EventWaitHandleAcl.TryOpenExisting(SingleAppComEventName,
                System.Security.AccessControl.EventWaitHandleRights.Synchronize |
                System.Security.AccessControl.EventWaitHandleRights.Modify,
                out EventWaitHandle tempComEvent))
                {
                    tempComEvent.Set();  // signal the other instance.
                    tempComEvent.Close();

                    runShutdown = false;
                    Current.Shutdown();    // Quit temp instance
                    return false;
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                // Ignore exception
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: タイマーの精度、キーボードのリピート設定、ViGEmBus の情報、単一起動の通知用イベントと一時ワーカー（Pre-Host の最後）。
        /// </summary>
        private void InitializeProcessWideSettings()
        {
            // Allow sleep time durations less than 16 ms
            DS4Windows.Util.timeBeginPeriod(1);

            // Load keyboard repeat settings from user registry so synthetic repeat matches OS behaviour
            try
            {
                DS4Windows.KeyboardSettings.LoadFromRegistry();
            }
            catch { }

            // R-6: マウスホイールを、Windows の設定（1 画面ずつ／行数）に合わせる。最初の画面ができる前に、1 回だけ登録する
            DS4WinWPF.DS4Forms.PageScrollWheelHandler.Register();

            // Retrieve info about installed ViGEmBus device if found
            // Phase7-Step4 W1／W4: 調べ物と状態は EnvironmentService（状態は static）が持つ。ブートストラップのインスタンスを使う。
            _bootstrapEnvironmentService.RefreshViGEmBusInfo();

            // Create the Event handle
            threadComEvent = new EventWaitHandle(false, EventResetMode.ManualReset, SingleAppComEventName);
            CreateTempWorkerThread();
        }

        /// <summary>
        /// Phase9 9-F F-1b: ホストの構築、ControlService の作成、Post-Host のサービスの解決（Pre-Host 領域の終わり）。
        /// </summary>
        private void BuildHostAndServices(ArgumentParser parser)
        {
            // Phase6-Step7: ここまでが Pre-Host 領域（明示的なホスト構築より前に実行される起動処理）。
            // この領域と CheckOptions（-driverinstall 分岐を含む）の Global 参照（起動時ログローテーション・
            // 設定の場所の決定・ViGEmBus 情報など）は、ブートストラップ処理として意図的に静的のまま温存する
            // （Phase6-Step7-Plan.md §1.2・§2.5）。Phase7-Step4 W2: -driverinstall 分岐は、設定の読込の前に、
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

            CreateControlService(parser);
            InitializePostHostServices();
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        /// <summary>
        /// Phase9 9-F F-1b: 初回起動の言語の選択（ダイアログ）。選ばずに閉じられたら、終了を要求して false を返す。
        /// </summary>
        private bool RunFirstRunLanguageSelection(bool firstRun, ref string selectedLanguage)
        {
            // On first run, show language selection dialog first
            if (firstRun)
            {
                DS4Forms.LanguageSelectDialog langDialog = new DS4Forms.LanguageSelectDialog(_appSettingsService);
                langDialog.ShowDialog();
                if (!langDialog.ChoiceMade)
                {
                    runShutdown = false;
                    Current.Shutdown();
                    return false;
                }

                // Store selected language and apply it immediately for dialogs
                selectedLanguage = langDialog.SelectedCulture;
                ApplyLanguageSetting(selectedLanguage);
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: 初回起動の保存先の選択と、設定フォルダの骨格の作成。選ばなかった、または作れなかったときは、終了を要求して false を返す。
        /// </summary>
        private bool RunFirstRunSaveLocationAndSkeleton(bool firstRun)
        {
            // Could not find unique profile location; does not exist or multiple places.
            // Advise user to specify where DS4Windows should save its configuation files
            // and profiles
            if (firstRun)
            {
                DS4Forms.SaveWhere savewh =
                    new DS4Forms.SaveWhere(_pathService.HasMultipleSaveLocations, _pathService, _environmentService);
                savewh.ShowDialog();
                if (!savewh.ChoiceMade)
                {
                    runShutdown = false;
                    Current.Shutdown();
                    return false;
                }
            }

            // Exit if base configuration could not be generated
            if (firstRun && !CreateConfDirSkeleton())
            {
                MessageBox.Show($"Cannot create config folder structure in {_pathService.AppDataPath}. Exiting",
                    "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(1);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: マクロの保存・読込ダイアログの初期フォルダ（Macros）を、毎回の起動で確認する。
        /// </summary>
        private void EnsureMacrosFolder()
        {
            // マクロの保存・読込ダイアログの初期フォルダ（RecordBox が <保存場所>\Macros を指定する）。
            // CreateConfDirSkeleton は初回起動時しか呼ばれないため、既存の環境にも作られるよう毎回確認する。
            // 作成できなくても（書き込み権限がない場合など）起動は続ける。
            try
            {
                Directory.CreateDirectory(Path.Combine(_pathService.AppDataPath, "Macros"));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Phase9 9-F F-1b: Profiles.xml の読み込み（ログの設定を使うため、LoggerHolder の作成より前）と、初回起動で選んだ言語の再適用・保存。読み込めたかを返す。
        /// </summary>
        private bool LoadAppSettings(bool firstRun, string selectedLanguage)
        {
            // Load Profiles.xml BEFORE creating LoggerHolder so log settings are available
            bool readAppConfig = _appSettingsService.Load();

            // Re-apply selected language after Load() and save to Profiles.xml
            if (firstRun && !string.IsNullOrEmpty(selectedLanguage))
            {
                ApplyLanguageSetting(selectedLanguage);
                _appSettingsService.Save();
            }
            return readAppConfig;
        }

        /// <summary>
        /// Phase9 9-F F-1b: LoggerHolder の作成、例外ハンドラーの登録、環境・ビルド日時のログ。
        /// </summary>
        private void ConfigureLoggingAndDiagnostics(bool firstRun, bool readAppConfig)
        {
            logHolder = new LoggerHolder(_controlService, _appSettingsService);
            // Phase6-Step10-1-5（決定D7＝A）: 設定画面がログ設定の変更を反映する窓口（ILogSettingsApplier）へ、生成した LoggerHolder を接続する
            _logSettingsApplierProxy.Attach(logHolder);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string version = _environmentService.ApplicationVersion;
            // Log process and path information at INFO level now that logger is configured
            try { AppLogger.LogInfo($"Process exe: {Process.GetCurrentProcess().MainModule?.FileName}"); } catch { }
            try { AppLogger.LogInfo($"AppDomain BaseDirectory: {AppContext.BaseDirectory}"); } catch { }
            try { AppLogger.LogInfo($"Current working directory: {Environment.CurrentDirectory}"); } catch { }
            // Phase9 9-F F-1a: 文言は StartupDiagnostics（文言と順序は変えない）
            foreach (string environmentMessage in StartupDiagnostics.EnvironmentMessages(version, _pathService.ExecutablePath,
                DS4Windows.Util.GetOSProductName(), DS4Windows.Util.GetOSReleaseId()))
            {
                AppLogger.LogInfo(environmentMessage);
            }
            AppLogger.LogInfo("Logger created");

            // Startup summary: read from build_timestamp.txt in the executable folder
            try
            {
                string fp = Path.Combine(AppContext.BaseDirectory, "build_timestamp.txt");
                fp = Path.GetFullPath(fp);
                if (File.Exists(fp))
                {
                    // Phase9 9-F F-1a: 文言づくりは BuildStampReader（解釈中の例外は、これまでどおり握りつぶす）
                    string buildMessage = BuildStampReader.BuildLogMessage(File.ReadAllText(fp));
                    if (buildMessage != null) AppLogger.LogInfo(buildMessage);
                }
                else
                {
                    try { AppLogger.LogInfo("Build summary file not found in the executable folder"); } catch { }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"Startup summary read failed: {ex}");
            }

            if (!firstRun && !readAppConfig)
            {
                AppLogger.LogInfo($@"Profiles.xml not read at location ${_pathService.AppDataPath}\Profiles.xml. Using default app settings");
            }
        }

        /// <summary>
        /// Phase9 9-F F-1b: 初回起動のデバイスの選択と既定の設定の作成、接続フラグのリセット。最後に skipSave を解除する（保存を許可する）。
        /// </summary>
        private void RunFirstLaunchSetup(bool firstRun)
        {
            // Ask user which devices the mapper should attempt to open when detected.
            // Currently only support DS4 by default to avoid extra complications from
            // Steam Input
            if (firstRun)
            {
                ApplyLanguageSetting(_appSettingsService.UseLang);
                DS4Forms.FirstLaunchUtilWindow firstLaunchUtilWin =
                    new DS4Forms.FirstLaunchUtilWindow(_appSettingsService.DeviceOptions);
                firstLaunchUtilWin.ShowDialog();
                _appSettingsService.Save();
            }

            if (firstRun)
            {
                AppLogger.LogInfo("No config found. Creating default config");
                AttemptSave();

                _profileRepository.SaveAsProfile(0, "Default");
                ProfileSlotNames profilePath = _profileRepository.ProfilePath;
                ProfileSlotNames olderProfilePath = _profileRepository.OlderProfilePath;
                for (int i = 0; i < DS4Windows.ControlService.MAX_DS4_CONTROLLER_COUNT; i++)
                {
                    profilePath[i] = olderProfilePath[i] = "Default";
                }

                AppLogger.LogInfo("Default config created");
            }

            // Reset first connection flags at startup
            _deviceStateService.ResetConnectionFlags();

            skipSave = false;
        }

        /// <summary>
        /// Phase9 9-F F-1b: プロファイルの固定 ID、SA の読み込み、言語とテーマの適用、リンクされたプロファイルの読み込み（メイン画面の生成の前）。
        /// </summary>
        private void PrepareProfilesCultureAndTheme()
        {
            // Phase10-Step4d W1（Q4 の変更、2026-10-21）: すべてのプロファイルのファイルに固定 ID を書く（ID がないものに割り当て、ファイルのコピーで重複したものに付け替える）。
            // SA の読み込み（下の LoadActions。Load Profile の切り替え先を ID で解決する。W3）と、プロファイルをスロットへ読み込む前（メイン画面の生成の前）に行う。
            // 書けなかったファイル（読み取り専用など）は、参照を保存する直前に改めて試す
            _profileIdentityService.EnsureAllPersisted();

            // Phase6-Step7（決定2＝L2）: SpecialActionRepository.LoadActions は Step7-1 で Global.LoadActions と同じ動作
            // （Actions.xml がなければ既定アクションを作って true）に是正済み。
            if (!_specialActionRepository.LoadActions())
            {
                _specialActionRepository.CreateStandardActions();
            }

            // Have app use selected culture
            SetUICulture(_appSettingsService.UseLang);
            ApplyLanguageSetting(_appSettingsService.UseLang);
            AppLogger.LogInfo($"Effective UI culture after initialization: CurrentUICulture={Thread.CurrentThread.CurrentUICulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, UseLang={_appSettingsService.UseLang}");

            DS4Windows.AppThemeChoice themeChoice = _appearanceSettingsService.UseCurrentTheme;
            ChangeTheme(_appearanceSettingsService.UseCurrentTheme, false);
            // Diagnostic: log culture state after applying theme to detect theme-induced localization regressions
            AppLogger.LogDebug($"Post-ChangeTheme: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");

            _profileRepository.LoadLinkedProfiles();
        }

        /// <summary>
        /// Phase9 9-F F-1b: メイン画面の生成と表示、IPC の準備、HidHide の確認、出力スロットの読み込み、LateChecks（起動の最後）。
        /// </summary>
        private void ShowMainWindowAndFinish(ArgumentParser parser)
        {
            // Phase7-Step5 W5（Q4＝A）: メイン画面の生成の窓口を、生成の直前に解決する（窓口が受け取る多数のサービスの生成が
            // 設定の読込より前にならないよう、InitializePostHostServices では解決しない。旧: MainWindow のコンストラクター内で解決していた）
            DS4Forms.MainWindow window = ResolvePostHostService<IMainWindowFactory>().Create(parser);
            MainWindow = window;
            window.IsInitialShow = true;
            window.Show();
            // Diagnostic: verify culture remains applied after window shown
            AppLogger.LogDebug($"After Show(): LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            window.IsInitialShow = false;

            // Set up hooks for IPC command calls
            HwndSource source = PresentationSource.FromVisual(window) as HwndSource;
            CreateIPCClassNameMMF(source.Handle);

            window.CheckMinStatus();

            bool runningAsAdmin = _environmentService.IsAdministrator();
            _controlService.LogDebug($"Running as {(runningAsAdmin ? "Admin" : "User")}");

            if (_environmentService.HidHideInstalled)
            {
                _controlService.CheckHidHidePresence();
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
            _appSettingsService = ResolvePostHostService<IAppSettingsService>();
            _pathService = ResolvePostHostService<IPathService>();
            _environmentService = ResolvePostHostService<IEnvironmentService>();
            _profileRepository = ResolvePostHostService<IProfileRepository>();
            _profileIdentityService = ResolvePostHostService<IProfileIdentityService>();
            _specialActionRepository = ResolvePostHostService<ISpecialActionRepository>();
            _appearanceSettingsService = ResolvePostHostService<IAppearanceSettingsService>();
            _deviceStateService = ResolvePostHostService<IDeviceStateService>();
            _outputSlotService = ResolvePostHostService<IOutputSlotService>();
            // Phase7-Step5 W3（Q7＝A）: 静的なロガー（AppLogger.LogToTray）が使う通知サービスを、DI のシングルトンに設定する
            AppLogger.NotificationService = ResolvePostHostService<INotificationService>();
            _logSettingsApplierProxy = ResolvePostHostService<DS4Windows.Services.LogSettingsApplierProxy>();
            _controllerRegistry = ResolvePostHostService<DS4Windows.Actions.IControllerRegistry>();

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] App.InitializePostHostServices: Post-Host services resolved");
        }

        private static T ResolvePostHostService<T>() where T : class
        {
            return AppHost.GetService<T>()
                ?? throw new InvalidOperationException($"{typeof(T).Name} could not be resolved from AppHost.");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!Current.Dispatcher.CheckAccess())
            {
                Exception exp = e.ExceptionObject as Exception;
                AppLogger.LogError($"Thread App Crashed with message {exp.Message}");
                AppLogger.LogError(exp.ToString());
                //LogManager.Flush();
                //LogManager.Shutdown();
                if (e.IsTerminating)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _controlService?.PrepareAbort();
                        CleanShutdown();
                    });
                }
            }
            else
            {
                Exception exp = e.ExceptionObject as Exception;
                if (e.IsTerminating)
                {
                    AppLogger.LogError($"Thread Crashed with message {exp.Message}");
                    AppLogger.LogError(exp.ToString());

                    _controlService?.PrepareAbort();
                    CleanShutdown();
                }
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //Debug.WriteLine("App Crashed");
            //Debug.WriteLine(e.Exception.StackTrace);
            AppLogger.LogError($"Thread Crashed with message {e.Exception.Message}");
            AppLogger.LogError(e.Exception.ToString());
            //LogManager.Flush();
            //LogManager.Shutdown();
        }

        private bool CreateConfDirSkeleton()
        {
            bool result = true;
            try
            {
                string appDataPath = _pathService.AppDataPath;
                Directory.CreateDirectory(appDataPath);
                Directory.CreateDirectory(appDataPath + @"\Profiles\");
                Directory.CreateDirectory(appDataPath + @"\Logs\");
                // Macros フォルダは、毎回の起動で Application_Startup が作成する（初回起動時に限らないため）
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }


            return result;
        }

        private void AttemptSave()
        {
            if (!_appSettingsService.Save()) //if can't write to file
            {
                if (MessageBox.Show("Cannot write at current location\nCopy Settings to appdata?", "DS4Windows",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string roamingAppDataPath = _pathService.RoamingAppDataPath;
                        // Global.exedirpath と同じ値（ジャンクション解決済みの実行ファイルの親フォルダ）。
                        // IPathService.ExecutableDirectory（AppContext.BaseDirectory）とは値が異なりうるため使わない。
                        string exeDirPath = Path.GetDirectoryName(_pathService.ExecutablePath);
                        Directory.CreateDirectory(roamingAppDataPath);
                        File.Copy(exeDirPath + "\\Profiles.xml",
                            roamingAppDataPath + "\\Profiles.xml");
                        File.Copy(exeDirPath + "\\Auto Profiles.xml",
                            roamingAppDataPath + "\\Auto Profiles.xml");
                        Directory.CreateDirectory(roamingAppDataPath + "\\Profiles");
                        foreach (string s in Directory.GetFiles(exeDirPath + "\\Profiles"))
                        {
                            File.Copy(s, roamingAppDataPath + "\\Profiles\\" + Path.GetFileName(s));
                        }
                    }
                    catch { }
                    MessageBox.Show("Copy complete, please relaunch DS4Windows and remove settings from Program Directory",
                        "DS4Windows");
                }
                else
                {
                    MessageBox.Show("DS4Windows cannot edit settings here, This will now close",
                        "DS4Windows");
                }

                // 旧 TODO(Phase6-Step7 決定3＝P1)（解消済み。当時の経緯）: IPathService.AppDataPath のセッターは、保存先の状態を変えない孤立実装だったため、
                // 置き換え先にならず、保存先の状態を直接書き換えていた。
                // Phase7-Step4 W4: 保存先の所有を PathService へ移した。状態そのものを null に戻す（旧 Global.appdatapath = null と同じ）。
                // （Phase7c W3: 本行の TODO は Phase7-Step4 W4 で解消済みのため、TODO 表記を外した。）
                DS4Windows.PathService.AppDataPathState = null;
                skipSave = true;
                Current.Shutdown();
                return;
            }
        }

        private void CheckOptions(ArgumentParser parser)
        {
            if (parser.HasErrors)
            {
                runShutdown = false;
                exitApp = true;
                Current.Shutdown(1);
            }
            else if (parser.Driverinstall)
            {
                // Retrieve info about installed ViGEmBus device if found.
                // Might not be needed here
                _bootstrapEnvironmentService.RefreshViGEmBusInfo();

                // Load DS4Windows config if it exists
                _bootstrapPathService.FindConfigLocation();

                // Phase7-Step4 W2: 旧 Global.Load() は、中の AppHost.GetService が、起動引数なしでホストを暗黙に作っていた
                // （K7-1 の残存経路）。ここで、起動引数つきで明示的にホストを作り、設定は IAppSettingsService で読む。
                var driverInstallHost = AppHost.CreateHost(
                    new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), parser,
                    _bootstrapPathService, _bootstrapEnvironmentService, _bootstrapBackingStore);
                var driverInstallSettings = (DS4Windows.DI.IAppSettingsService)driverInstallHost.Services
                    .GetService(typeof(DS4Windows.DI.IAppSettingsService));
                bool readAppConfig = driverInstallSettings.Load();
                if (readAppConfig)
                {
                    // Have app use selected culture
                    SetUICulture(_bootstrapBackingStore.useLang);
                    ApplyLanguageSetting(_bootstrapBackingStore.useLang);
                    DS4Windows.AppThemeChoice themeChoice = _bootstrapBackingStore.useCurrentTheme;
                    ChangeTheme(_bootstrapBackingStore.useCurrentTheme, false);
                }

                CreateBaseThread();
                DS4Forms.WelcomeDialog dialog = new DS4Forms.WelcomeDialog(
                    _bootstrapEnvironmentService);
                dialog.ShowDialog();
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (parser.ReenableDevice)
            {
                // Phase7-Step4 W4（Q4＝A）: 第 1 層の静的な DS4Devices.reEnableDevice の直接呼び出しをやめ、IDs4DeviceRegistry の契約を通す。
                // この分岐はホストを構築しない昇格した補助プロセスのため、状態を持たないアダプタを直接作る。
                // 差: デバイスのインスタンス ID が空のときは、アダプタが何もせず戻る（旧: 例外で終了していた。不正な起動引数の場合のみ）。
                DS4Windows.Services.IDs4DeviceRegistry reEnableRegistry = new DS4Windows.Services.Ds4DeviceRegistryAdapter();
                reEnableRegistry.ReEnableDevice(parser.DeviceInstanceId);
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (parser.Runtask)
            {
                StartupMethods.LaunchOldTask();
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (parser.Command)
            {
                IntPtr hWndDS4WindowsForm = IntPtr.Zero;
                hWndDS4WindowsForm = FindWindow(ReadIPCClassNameMMF(), "DS4Windows");
                if (hWndDS4WindowsForm != IntPtr.Zero)
                {
                    bool bDoSendMsg = true;
                    bool bWaitResultData = false;
                    bool bOwnsMutex = false;
                    Mutex ipcSingleTaskMutex = null;
                    EventWaitHandle ipcNotifyEvent = null;

                    COPYDATASTRUCT cds;
                    cds.lpData = IntPtr.Zero;

                    try
                    {
                        if (parser.CommandArgs.ToLower().StartsWith("query."))
                        {
                            // Query.device# (1..4) command returns a string result via memory mapped file. The cmd is sent to the background DS4Windows
                            // process (via WM_COPYDATA wnd msg), then this client process waits for the availability of the result and prints it to console output pipe.
                            // Use mutex obj to make sure that concurrent client calls won't try to write and read the same MMF result file at the same time.
                            ipcSingleTaskMutex = new Mutex(false, "DS4Windows_IPCResultData_SingleTaskMtx");
                            try
                            {
                                bOwnsMutex = ipcSingleTaskMutex.WaitOne(10000);
                            }
                            catch (AbandonedMutexException)
                            {
                                bOwnsMutex = true;
                            }

                            if (bOwnsMutex)
                            {
                                // This process owns the inter-process sync mutex obj. Let's proceed with creating the output MMF file and waiting for a result.
                                bWaitResultData = true;
                                CreateIPCResultDataMMF();
                                ipcNotifyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DS4Windows_IPCResultData_ReadyEvent");
                            }
                            else
                                // If the mtx failed then something must be seriously wrong. Cannot do anything in that case because MMF file may be modified by concurrent processes.
                                bDoSendMsg = false;
                        }

                        if (bDoSendMsg)
                        {
                            cds.dwData = IntPtr.Zero;
                            cds.cbData = parser.CommandArgs.Length;
                            cds.lpData = Marshal.StringToHGlobalAnsi(parser.CommandArgs);
                            SendMessage(hWndDS4WindowsForm, DS4Forms.MainWindow.WM_COPYDATA, IntPtr.Zero, ref cds);

                            if (bWaitResultData)
                                Console.WriteLine(WaitAndReadIPCResultDataMMF(ipcNotifyEvent));
                        }
                    }
                    finally
                    {
                        // Release the result MMF file in the client process before releasing the mtx and letting other client process to proceed with the same MMF file
                        if (ipcResultDataMMF != null) ipcResultDataMMF.Dispose();
                        ipcResultDataMMF = null;

                        // If this was "Query.xxx" cmdline client call then release the inter-process mutex and let other concurrent clients to proceed (if there are anyone waiting for the MMF result file)
                        if (bOwnsMutex && ipcSingleTaskMutex != null)
                            ipcSingleTaskMutex.ReleaseMutex();

                        if (cds.lpData != IntPtr.Zero)
                            Marshal.FreeHGlobal(cds.lpData);
                    }
                }

                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
        }

        private void CreateControlService(ArgumentParser parser)
        {
            controlThread = new Thread(() =>
            {
                // Phase 3 Followup Step F-2: registry comes from AppHost (registered in
                // ServiceRegistration). AppHost.CreateHost() already ran before this method
                // is called, so GetService should not be null; the null-coalesce is a
                // startup-order safety net only, and 'new Ds4DeviceRegistryAdapter()' here
                // is the same adapter class already registered, not a second implementation.
                // Phase7-Step5 W4: 解決の窓口（ResolvePostHostService）を通す。解決できなければ InvalidOperationException（従来と同じ）
                _controlService = ResolvePostHostService<DS4Windows.ControlService>();

                requestClient = new HttpClient();
                requestClient.DefaultRequestHeaders.Add("User-Agent", "DS4Windows");
                collectTimer = new Timer(GarbageTask, null, 30000, 30000);

            });
            controlThread.Priority = ThreadPriority.Normal;
            controlThread.IsBackground = true;
            controlThread.Start();
            while (controlThread.IsAlive)
                Thread.SpinWait(500);
        }

        private void CreateBaseThread()
        {
            controlThread = new Thread(() =>
            {
                requestClient = new HttpClient();
                requestClient.DefaultRequestHeaders.Add("User-Agent", "DS4Windows");
                collectTimer = new Timer(GarbageTask, null, 30000, 30000);
            });
            controlThread.Priority = ThreadPriority.Normal;
            controlThread.IsBackground = true;
            controlThread.Start();
            while (controlThread.IsAlive)
                Thread.SpinWait(500);
        }

        private void GarbageTask(object state)
        {
            GC.Collect(0, GCCollectionMode.Forced, false);
        }

        private void CreateTempWorkerThread()
        {
            testThread = new Thread(SingleAppComThread_DoWork);
            testThread.Priority = ThreadPriority.Lowest;
            testThread.IsBackground = true;
            testThread.Start();
        }

        private void SingleAppComThread_DoWork()
        {
            while (!exitComThread)
            {
                // check for a signal.
                if (threadComEvent.WaitOne())
                {
                    threadComEvent.Reset();
                    // The user tried to start another instance. We can't allow that,
                    // so bring the other instance back into view and enable that one.
                    // That form is created in another thread, so we need some thread sync magic.
                    if (!exitComThread)
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            MainWindow.Show();
                            MainWindow.WindowState = WindowState.Normal;
                            MainWindow.Activate();
                        }));
                    }
                }
            }
        }

        public void CreateIPCClassNameMMF(IntPtr hWnd)
        {
            if (ipcClassNameMMF != null) return; // Already holding a handle to MMF file. No need to re-write the data

            try
            {
                StringBuilder wndClassNameStr = new StringBuilder(128);
                if (GetClassName(hWnd, wndClassNameStr, wndClassNameStr.Capacity) != 0 && wndClassNameStr.Length > 0)
                {
                    byte[] buffer = ASCIIEncoding.ASCII.GetBytes(wndClassNameStr.ToString());

                    ipcClassNameMMF = MemoryMappedFile.CreateNew("DS4Windows_IPCClassName.dat", 128);
                    MemoryMappedViewAccessor ipcClassNameMMA_Now = ipcClassNameMMF.CreateViewAccessor(0, buffer.Length);
                    ipcClassNameMMA_Now.WriteArray(0, buffer, 0, buffer.Length);
                    ipcClassNameMMA_Now?.Dispose();
                    // The MMF file is alive as long this process holds the file handle open
                }
            }
            catch (Exception)
            {
                /* Eat all exceptions because errors here are not fatal for DS4Win */
            }
        }

        private string ReadIPCClassNameMMF()
        {
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mma = null;

            try
            {
                byte[] buffer = new byte[128];
                mmf = MemoryMappedFile.OpenExisting("DS4Windows_IPCClassName.dat");
                mma = mmf.CreateViewAccessor(0, 128);
                mma.ReadArray(0, buffer, 0, buffer.Length);
                return ASCIIEncoding.ASCII.GetString(buffer);
            }
            catch (Exception)
            {
                // Eat all exceptions
            }
            finally
            {
                if (mma != null) mma.Dispose();
                if (mmf != null) mmf.Dispose();
            }

            return null;
        }

        private void CreateIPCResultDataMMF()
        {
            // Cmdline client process calls this to create the MMF file used in inter-process-communications. The background DS4Windows process
            // uses WriteIPCResultDataMMF method to write a command result and the client process reads the result from the same MMF file.
            if (ipcResultDataMMF != null) return; // Already holding a handle to MMF file. No need to re-write the data

            try
            {
                ipcResultDataMMF = MemoryMappedFile.CreateNew("DS4Windows_IPCResultData.dat", 256);
                // The MMF file is alive as long this process holds the file handle open
            }
            catch (Exception)
            {
                /* Eat all exceptions because errors here are not fatal for DS4Win */
            }
        }

        private string WaitAndReadIPCResultDataMMF(EventWaitHandle ipcNotifyEvent)
        {
            if (ipcResultDataMMF != null)
            {
                // Wait until the inter-process-communication (IPC) result data is available and read the result
                try
                {
                    // Wait max 10 secs and if the result is still not available then timeout and return "empty" result
                    if (ipcNotifyEvent == null || ipcNotifyEvent.WaitOne(10000))
                    {
                        int strNullCharIdx;
                        byte[] buffer = new byte[256];
                        // 不具合の修正（2026-10-23。Phase10-Step4d W3 の dd10cbb で混入）: 結果の MMF（ipcResultDataMMF）ではなく、クラス名の MMF（ipcClassNameMMF。
                        // クライアントのプロセスでは null）を読んでいたため、query の結果が常に空になっていた
                        MemoryMappedViewAccessor ipcResultDataMMA = ipcResultDataMMF.CreateViewAccessor(0, buffer.Length);
                        ipcResultDataMMA.ReadArray(0, buffer, 0, buffer.Length);
                        ipcResultDataMMA.Dispose();
                        strNullCharIdx = Array.FindIndex(buffer, byteVal => byteVal == 0);
                        return ASCIIEncoding.ASCII.GetString(buffer, 0, (strNullCharIdx <= 1 ? 1 : strNullCharIdx));
                    }
                }
                catch (Exception)
                {
                    /* Eat all exceptions because errors here are not fatal for DS4Win */
                }
            }

            return String.Empty;
        }

        public void WriteIPCResultDataMMF(string dataStr)
        {
            // The background DS4Windows process calls this method to write out the result of "-command QueryProfile.device#" command.
            // The cmdline client process reads the result from the DS4Windows_IPCResultData.dat MMF file and sends the result to console output pipe.
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mma = null;
            EventWaitHandle ipcNotifyEvent = null;

            try
            {
                ipcNotifyEvent = EventWaitHandle.OpenExisting("DS4Windows_IPCResultData_ReadyEvent");

                byte[] buffer = ASCIIEncoding.ASCII.GetBytes(dataStr);
                mmf = MemoryMappedFile.OpenExisting("DS4Windows_IPCResultData.dat");
                mma = mmf.CreateViewAccessor(0, 256);
                mma.WriteArray(0, buffer, 0, (buffer.Length >= 256 ? 256 : buffer.Length));
            }
            catch (Exception)
            {
                // Eat all exceptions
            }
            finally
            {
                if (mma != null) mma.Dispose();
                if (mmf != null) mmf.Dispose();

                if (ipcNotifyEvent != null) ipcNotifyEvent.Set();
            }
        }

        private void SetUICulture(string culture)
        {
            try
            {
                AppLogger.LogDebug($"SetUICulture requested: {culture}");
                //CultureInfo ci = new CultureInfo("ja");
                CultureInfo ci = CultureInfo.GetCultureInfo(culture);
                LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
                LocalizeDictionary.Instance.Culture = ci;
                // fixes the culture in threads
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                DS4WinWPF.Properties.Resources.Culture = ci;
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                AppLogger.LogDebug($"SetUICulture applied: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            }
            catch (CultureNotFoundException) { /* Skip setting culture that we cannot set */ }
        }

        public void ChangeTheme(DS4Windows.AppThemeChoice themeChoice,
            bool fireChanged = true)
        {
            if (themeChoice == DS4Windows.AppThemeChoice.Default)
            {
                Application.Current.Resources.MergedDictionaries.Clear();

                // 4.0.1: Default is the light theme (it used to follow the Windows apps theme mode, which made the
                // dark theme the default for many users). Choose Dark explicitly to use the dark theme.
                themeLocs.TryGetValue(DS4Windows.AppThemeChoice.Light, out string loc);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(loc, uriKind: UriKind.Relative) });

                if (fireChanged)
                {
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (themeLocs.TryGetValue(themeChoice, out string loc))
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(loc, uriKind: UriKind.Relative) });

                if (fireChanged)
                {
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (runShutdown)
            {
                AppLogger.LogInfo("Request App Shutdown");
                CleanShutdown();
            }
        }

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            AppLogger.LogInfo("User Session Ending");
            CleanShutdown();
        }

        private void CleanShutdown()
        {
            if (runShutdown)
            {
                if (_controlService != null)
                {
                    Task.Run(() =>
                    {
                        if (_controlService.running)
                        {
                            _controlService.Stop(immediateUnplug: true);
                            _controlService.ShutDown();
                        }
                    }).Wait();
                }

                if (!skipSave)
                {
                    // skipSave が false になるのは Application_Startup の Post-Host（InitializePostHostServices の後）だけなので、
                    // ここでは _appSettingsService は必ず解決済み。CleanShutdown はホスト構築前の早期終了・例外でも呼ばれるため、
                    // 念のため null 条件演算子で保護する（その場合は skipSave が true のままで、ここへは来ない）。
                    _appSettingsService?.Save();
                }

                // Reset timer
                DS4Windows.Util.timeEndPeriod(1);

                exitComThread = true;
                if (threadComEvent != null)
                {
                    threadComEvent.Set();  // signal the other instance.
                    while (testThread.IsAlive)
                        Thread.SpinWait(500);
                    threadComEvent.Close();
                }

                if (ipcClassNameMMF != null) ipcClassNameMMF.Dispose();

                // Dispose controllers registered in IControllerRegistry (if present)
                // Phase7-Step5 W4: ServiceProviderHolder.Provider の直接呼び出しをやめ、InitializePostHostServices で解決した
                // レジストリを使う（ホスト構築前の早期終了では null のため、何もしない）。
                try
                {
                    if (_controllerRegistry != null)
                    {
                        for (int d = 0; d < ControllerLimits.MaxControllerCount; d++)
                        {
                            try { _controllerRegistry.ClearControllersForDevice(d); } catch { }
                        }
                    }
                }
                catch { }

                // Phase7-Step4 W2: ホストを破棄する。ドライバー破棄の順序（防壁 5）を守るため、コントローラーの停止
                // （rootHub.Stop／ShutDown）と、レジストリの破棄の後に行う。Dispose の後は、GetService が暗黙にホストを
                // 作り直さない。例外は握る（AppHost.Dispose も握る）。ログのフラッシュより前に行い、破棄中のログも出力する。
                try
                {
                    AppHost.Dispose();
                }
                catch { }

                LogManager.Flush();
                LogManager.Shutdown();
            }
        }
    }
}