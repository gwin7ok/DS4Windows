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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;
using DS4Windows.Composition;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI;

namespace DS4Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public partial class App : Application
    {
        private Thread controlThread = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        // Phase14b-Step17（決定 D10）: 旧 public static HttpClient requestClient（更新の確認・ドライバーのダウンロード）は、DI の Singleton の HttpClient にした
        private bool skipSave;
        private bool runShutdown;
        private bool exitApp;
        private DS4Windows.Core.Platform.IpcMemoryMap _ipc = null!;   // CLI との通信の窓口（ホストの構築後に解決する）
        private readonly SingleInstanceGuard _singleInstance = new SingleInstanceGuard();
        // 起動前（-driverinstall の分岐を含む）は、ブートストラップの BackingStore で作ったもの。ホストの構築後は、コンテナの Singleton に置き換える
        private DS4Windows.UI.Services.AppThemeAndCultureService _themeCulture = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private ITimer collectTimer = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        public static LoggerHolder logHolder = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない

        // Phase6-Step7（決定1＝A）: Post-Host（明示的なホスト構築と CreateControlService の後）で使う DI サービス。
        // App は WPF の Application でコンストラクタ注入できないため、Composition Root として
        // InitializePostHostServices() で一度だけ解決して保持する。Pre-Host 領域（ホスト構築より前）では null。
        private IAppSettingsService _appSettingsService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IPathService _pathService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IEnvironmentService _environmentService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IProfileRepository _profileRepository = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IProfileIdentityService _profileIdentityService = null!;   // Phase10-Step4d W1
        private ISpecialActionRepository _specialActionRepository = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IAppearanceSettingsService _appearanceSettingsService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IDeviceStateService _deviceStateService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private IOutputSlotService _outputSlotService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        // Phase7-Step5 W4: 旧 AppHost.GetService／ServiceProviderHolder の直接呼び出しの置き換え先（InitializePostHostServices で解決）
        private DS4Windows.Core.Logging.LogSettingsApplierProxy _logSettingsApplierProxy = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない

        // Phase7-Step5 W2（Q1＝A）: 旧 Program.rootHub。ControlService は CreateControlService の別スレッドで解決して代入し、
        // 呼び出し元がスレッドの終了を待ってから読む（以降は LoggerHolder の生成、起動後の処理、未処理例外の処理、終了処理が使う）。
        // 別スレッド（未処理例外、セッション終了）からも読まれるため volatile にする。
        private volatile DS4Windows.Processing.Abstractions.IRuntimeBootstrapper _runtimeBootstrapper = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private volatile DS4Windows.Processing.Abstractions.IServiceLifecycle _lifecycle = null!;   // Phase13-Step9: 旧 ControlService.running の転送
        private DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch _runtimeFeatures = null!;   // Phase13-Step10 10-c: 旧 ControlService.CheckHidHidePresence の転送

        // Phase7-Step4 W4（D3＝S1）: コンテナの構築より前に作るブートストラップのサービス。Pre-Host の起動処理（保存場所の決定、
        // ViGEmBus 情報の取得）で使い、CreateHost に渡して、コンテナへ同じインスタンスを登録する。
        // 状態は PathService／EnvironmentService の static（プロセス全体で 1 つ）のため、どのインスタンスでも同じ値を見る。
        private DS4Windows.Core.Platform.PathService _bootstrapPathService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        private DS4Windows.Core.Platform.EnvironmentService _bootstrapEnvironmentService = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない
        // Phase7-Step10d W5（Q3＝A）: 旧 Global.store（静的）。コンテナの構築より前に作り、CreateHost に渡して、コンテナへ同じインスタンスを登録する
        // （起動直後のログのローテーションと最小ログレベル、-driverinstall 分岐の言語・テーマの読み取りが使う）。
        private DS4Windows.Core.Settings.BackingStore _bootstrapBackingStore = null!; // 起動の流れ（OnStartup）で設定する。それまでは使われない

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ★ 最優先で AUMID とレジストリを初期化（ポータブル通知対策）
            DS4Windows.UI.Services.AppNotificationRegistration.Initialize();

            runShutdown = true;
            skipSave = true;

            // Phase9 9-F F-1b（docs-forDIMG/MadeByAgent/Archive/Phase9-F-Plan.md §5）: 起動の手順を、順に呼ぶだけにした（各手順は、下のメソッド。順序と、早期の終了の条件は、従来のまま。
            // 呼び出しの順序は 対応するテスト が守る）
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
            string? selectedLanguage = null;

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

        // 設定の場所を決められたか（決められなかったときは、従来どおり、ローテーションも行わない）
        private bool _bootstrapConfigLocationFound;

        private static T ResolvePostHostService<T>() where T : class
        {
            return TryResolvePostHostService<T>()
                ?? throw new InvalidOperationException($"{typeof(T).Name} could not be resolved from AppHost.");
        }

        // ホストが未構築（早期終了）のときは null を返す。終了の手順だけが使う
        private static T? TryResolvePostHostService<T>() where T : class
        {
            return AppHost.GetService<T>();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!Current.Dispatcher.CheckAccess())
            {
                Exception? exp = e.ExceptionObject as Exception;
                AppLogger.LogError($"Thread App Crashed with message {exp!.Message}"); // 未処理の例外は Exception（そうでなければ従来どおり NullReferenceException）
                AppLogger.LogError(exp.ToString());
                //LogManager.Flush();
                //LogManager.Shutdown();
                if (e.IsTerminating)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _lifecycle?.PrepareAbort();
                        CleanShutdown();
                    });
                }
            }
            else
            {
                Exception? exp = e.ExceptionObject as Exception;
                if (e.IsTerminating)
                {
                    AppLogger.LogError($"Thread Crashed with message {exp!.Message}"); // 未処理の例外は Exception（そうでなければ従来どおり NullReferenceException）
                    AppLogger.LogError(exp.ToString());

                    _lifecycle?.PrepareAbort();
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

        private void CheckOptions(ArgumentParser parser)
        {
            StartupMode mode = StartupModeSelector.Select(parser);
            if (mode == StartupMode.ArgumentError)
            {
                runShutdown = false;
                exitApp = true;
                Current.Shutdown(1);
            }
            else if (mode == StartupMode.DriverInstall)
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
                var driverInstallSettings = (DS4Windows.Core.Abstractions.IAppSettingsService)driverInstallHost.Services
                    .GetService(typeof(DS4Windows.Core.Abstractions.IAppSettingsService))!; // 登録済み（null なら従来どおり NullReferenceException）
                bool readAppConfig = driverInstallSettings.Load();
                if (readAppConfig)
                {
                    // Have app use selected culture
                    _themeCulture.SetUICulture(_bootstrapBackingStore.Appearance.useLang);
                    _themeCulture.ApplyLanguageSetting(_bootstrapBackingStore.Appearance.useLang);
                    DS4Windows.Core.Settings.AppThemeChoice themeChoice = _bootstrapBackingStore.Appearance.useCurrentTheme;
                    _themeCulture.ChangeTheme(_bootstrapBackingStore.Appearance.useCurrentTheme, false);
                }

                CreateBaseThread();
                DS4Windows.UI.WelcomeDialog dialog = new DS4Windows.UI.WelcomeDialog(
                    new DS4Windows.UI.Services.DriverEnvironmentAdapter(_bootstrapEnvironmentService),
                    (System.Net.Http.HttpClient)driverInstallHost.Services.GetService(typeof(System.Net.Http.HttpClient))!);   // 登録済み（Phase14b-Step17（D10））
                dialog.ShowDialog();
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (mode == StartupMode.ReenableDevice)
            {
                // Phase7-Step4 W4（Q4＝A）: 第 1 層の静的な InputDeviceRegistry.ReEnableDevice の直接呼び出しをやめ、IInputDeviceRegistry の契約を通す。
                // この分岐はホストを構築しない昇格した補助プロセスのため、状態を持たないアダプタを直接作る。
                // 差: デバイスのインスタンス ID が空のときは、アダプタが何もせず戻る（旧: 例外で終了していた。不正な起動引数の場合のみ）。
                DS4Windows.Processing.Abstractions.IInputDeviceRegistry reEnableRegistry = new DS4Windows.Processing.Services.Devices.InputDeviceRegistryAdapter();
                reEnableRegistry.ReEnableDevice(parser.DeviceInstanceId!); // この経路は、機器の指定がある引数のときだけ来る（従来どおり）
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (mode == StartupMode.RunTask)
            {
                // Phase14b-Step15（D12）: ホストを構築しない補助プロセスのため、起点が実装を直接作る（旧: 静的 StartupMethods.LaunchOldTask）
                new DS4Windows.Core.Platform.StartupEntryService().LaunchOldTask();
                runShutdown = false;
                exitApp = true;
                Current.Shutdown();
            }
            else if (mode == StartupMode.LegacyCommand)
            {
                // コマンドの送信は、コンソール種別の DS4WindowsCli.exe が行う（GUI アプリのこの実行ファイルは cmd.exe に待たれず、結果の順序が崩れるため）。
                // 古い書き方（DS4Windows.exe -command ...）で起動されたときは、何もせず終了する。
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
                // startup-order safety net only, and 'new InputDeviceRegistryAdapter()' here
                // is the same adapter class already registered, not a second implementation.
                // Phase7-Step5 W4: 解決の窓口（ResolvePostHostService）を通す。解決できなければ InvalidOperationException（従来と同じ）
                _runtimeBootstrapper = ResolvePostHostService<DS4Windows.Processing.Abstractions.IRuntimeBootstrapper>();
                _runtimeBootstrapper.Initialize();
                _lifecycle = ResolvePostHostService<DS4Windows.Processing.Abstractions.IServiceLifecycle>();

                collectTimer = TimeProvider.System.CreateTimer(GarbageTask, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

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
                collectTimer = TimeProvider.System.CreateTimer(GarbageTask, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            });
            controlThread.Priority = ThreadPriority.Normal;
            controlThread.IsBackground = true;
            controlThread.Start();
            while (controlThread.IsAlive)
                Thread.SpinWait(500);
        }

        private void GarbageTask(object? state)
        {
            GC.Collect(0, GCCollectionMode.Forced, false);
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

        /// <summary>
        /// 終了の手順を行う。手順そのものは <see cref="AppShutdownSequence"/>（順序は同クラスのテストが固定）。
        /// ホスト構築前の早期終了では、まだ解決していない協力者は <c>null</c> のまま渡す（その手順は何もしない）。
        /// </summary>
        private void CleanShutdown()
        {
            if (!runShutdown) return;

            new AppShutdownSequence(
                _runtimeBootstrapper, _lifecycle,
                skipSave ? (Action?)null : () => _appSettingsService?.Save(),
                _ipc, TryResolvePostHostService<DS4Windows.Processing.Actions.IControllerRegistry>(), ControllerLimits.MaxControllerCount,
                resetTimerResolution: () => DS4Windows.Core.Utilities.Util.TimeEndPeriod(1),
                stopSingleInstance: _singleInstance.Stop,
                disposeHost: AppHost.Dispose,
                flushLogs: () => { LogManager.Flush(); LogManager.Shutdown(); }).Run();
        }
    }
}