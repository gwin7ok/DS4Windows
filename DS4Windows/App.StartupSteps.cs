#nullable enable
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Utilities;

namespace DS4Windows
{
    /// <summary>
    /// 起動の手順の実装。呼ぶ順序と早期の終了は <see cref="AppStartupSequence"/> が決め、ここは、手順の間で受け渡す状態
    /// （起動引数、パーサー、初回起動か、選んだ言語、設定を読めたか）を持って、既存の private メソッドへ渡すだけ。
    /// </summary>
    public partial class App : IAppStartupSteps
    {
        private StartupEventArgs _startupEventArgs = null!;   // Application_Startup が、手順を始める前に設定する
        private readonly ArgumentParser _argumentParser = new ArgumentParser();
        private bool _firstRun;
        private string? _selectedLanguage;
        private bool _readAppConfig;

        private StartupEventArgs _startupArgs { set => _startupEventArgs = value; }

        void IAppStartupSteps.RunPreHostBootstrap() => RunPreHostBootstrap(_startupEventArgs);

        bool IAppStartupSteps.TryHandleCompleteInstall() => TryHandleCompleteInstall(_startupEventArgs);

        void IAppStartupSteps.ParseAndHandleOptions()
        {
            _argumentParser.Parse(_startupEventArgs.Args);
            CheckOptions(_argumentParser);
        }

        bool IAppStartupSteps.ExitRequested => exitApp;

        void IAppStartupSteps.SetProcessPriorities() => SetProcessPriorities();

        bool IAppStartupSteps.EnsureSingleInstance() => EnsureSingleInstance();

        void IAppStartupSteps.RotateLogsAndLogStartupDiagnostics() => RotateLogsAndLogStartupDiagnostics(_startupEventArgs);

        void IAppStartupSteps.InitializeProcessWideSettings() => InitializeProcessWideSettings();

        // R-6: Windows の設定（1 画面ずつ／行数）に合わせる。最初の画面ができる前に、1 回だけ登録する
        void IAppStartupSteps.RegisterPageScrollWheel() => DS4Windows.UI.PageScrollWheelHandler.Register();

        void IAppStartupSteps.CreateHost() => CreateHostExplicitly(_argumentParser);

        void IAppStartupSteps.CreateControlService() => CreateControlService(_argumentParser);

        void IAppStartupSteps.InitializePostHostServices()
        {
            InitializePostHostServices();
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        void IAppStartupSteps.ReadFirstRunFlag() => _firstRun = _appSettingsService.FirstRun;

        bool IAppStartupSteps.RunFirstRunLanguageSelection() => RunFirstRunLanguageSelection(_firstRun, ref _selectedLanguage);

        bool IAppStartupSteps.RunFirstRunSaveLocationAndSkeleton() => RunFirstRunSaveLocationAndSkeleton(_firstRun);

        void IAppStartupSteps.EnsureMacrosFolder() => EnsureMacrosFolder();

        void IAppStartupSteps.LoadAppSettings() => _readAppConfig = LoadAppSettings(_firstRun, _selectedLanguage);

        void IAppStartupSteps.ConfigureLoggingAndDiagnostics() => ConfigureLoggingAndDiagnostics(_firstRun, _readAppConfig);

        void IAppStartupSteps.RunFirstLaunchSetup() => RunFirstLaunchSetup(_firstRun);

        void IAppStartupSteps.PrepareProfilesCultureAndTheme() => PrepareProfilesCultureAndTheme();

        void IAppStartupSteps.ShowMainWindowAndFinish() => ShowMainWindowAndFinish(_argumentParser);
    }
}
