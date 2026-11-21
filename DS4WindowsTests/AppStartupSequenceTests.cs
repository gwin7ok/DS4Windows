#nullable enable
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Composition.Startup;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 起動の順序と早期の終了。各手順は記録するだけの偽物に差し替え、呼ばれた順序を挙動で確かめる。
    /// </summary>
    public class AppStartupSequenceTests
    {
        private sealed class RecordingSteps : IAppStartupSteps
        {
            public readonly List<string> Calls = new List<string>();
            public bool CompleteInstall, Exit, SingleInstance = true, LanguageOk = true, SaveLocationOk = true;

            private void Log(string name) => Calls.Add(name);
            public void RunPreHostBootstrap() => Log(nameof(RunPreHostBootstrap));
            public bool TryHandleCompleteInstall() { Log(nameof(TryHandleCompleteInstall)); return CompleteInstall; }
            public void ParseAndHandleOptions() => Log(nameof(ParseAndHandleOptions));
            public bool ExitRequested => Exit;
            public void SetProcessPriorities() => Log(nameof(SetProcessPriorities));
            public bool EnsureSingleInstance() { Log(nameof(EnsureSingleInstance)); return SingleInstance; }
            public void RotateLogsAndLogStartupDiagnostics() => Log(nameof(RotateLogsAndLogStartupDiagnostics));
            public void InitializeProcessWideSettings() => Log(nameof(InitializeProcessWideSettings));
            public void RegisterPageScrollWheel() => Log(nameof(RegisterPageScrollWheel));
            public void CreateHost() => Log(nameof(CreateHost));
            public void CreateControlService() => Log(nameof(CreateControlService));
            public void InitializePostHostServices() => Log(nameof(InitializePostHostServices));
            public void ReadFirstRunFlag() => Log(nameof(ReadFirstRunFlag));
            public bool RunFirstRunLanguageSelection() { Log(nameof(RunFirstRunLanguageSelection)); return LanguageOk; }
            public bool RunFirstRunSaveLocationAndSkeleton() { Log(nameof(RunFirstRunSaveLocationAndSkeleton)); return SaveLocationOk; }
            public void EnsureMacrosFolder() => Log(nameof(EnsureMacrosFolder));
            public void LoadAppSettings() => Log(nameof(LoadAppSettings));
            public void ConfigureLoggingAndDiagnostics() => Log(nameof(ConfigureLoggingAndDiagnostics));
            public void RunFirstLaunchSetup() => Log(nameof(RunFirstLaunchSetup));
            public void PrepareProfilesCultureAndTheme() => Log(nameof(PrepareProfilesCultureAndTheme));
            public void ShowMainWindowAndFinish() => Log(nameof(ShowMainWindowAndFinish));

            public bool Before(string first, string second)
            {
                int a = Calls.IndexOf(first), b = Calls.IndexOf(second);
                return a >= 0 && b > a;
            }
        }

        private static RecordingSteps Run(RecordingSteps steps)
        {
            AppStartupSequence.Run(steps);
            return steps;
        }

        [Fact]
        public void ANormalStart_RunsEveryStepOnce_EndingWithTheMainWindow()
        {
            var steps = Run(new RecordingSteps());

            Assert.Equal(steps.Calls.Distinct().Count(), steps.Calls.Count);   // どの手順も 1 回だけ
            Assert.Equal(nameof(IAppStartupSteps.RunPreHostBootstrap), steps.Calls.First());
            Assert.Equal(nameof(IAppStartupSteps.ShowMainWindowAndFinish), steps.Calls.Last());
            Assert.Equal(20, steps.Calls.Count);   // 手順の数（ExitRequested はプロパティで数えない）
        }

        [Theory]
        [InlineData(nameof(IAppStartupSteps.CreateHost), nameof(IAppStartupSteps.CreateControlService))]
        [InlineData(nameof(IAppStartupSteps.CreateControlService), nameof(IAppStartupSteps.InitializePostHostServices))]   // 解決は、入力の制御を作った直後
        [InlineData(nameof(IAppStartupSteps.InitializePostHostServices), nameof(IAppStartupSteps.ReadFirstRunFlag))]
        [InlineData(nameof(IAppStartupSteps.InitializePostHostServices), nameof(IAppStartupSteps.LoadAppSettings))]   // 設定の読み込みは、サービスの解決の後
        [InlineData(nameof(IAppStartupSteps.LoadAppSettings), nameof(IAppStartupSteps.ConfigureLoggingAndDiagnostics))]   // ログの設定は、読み込んだ設定を使う
        [InlineData(nameof(IAppStartupSteps.EnsureSingleInstance), nameof(IAppStartupSteps.RotateLogsAndLogStartupDiagnostics))]   // 別のインスタンスのログを、アーカイブへ回さない
        [InlineData(nameof(IAppStartupSteps.RegisterPageScrollWheel), nameof(IAppStartupSteps.ShowMainWindowAndFinish))]   // ホイールの登録は、最初の画面の前
        [InlineData(nameof(IAppStartupSteps.PrepareProfilesCultureAndTheme), nameof(IAppStartupSteps.ShowMainWindowAndFinish))]   // プロファイルの ID・SA の読み込みは、メイン画面の生成の前
        [InlineData(nameof(IAppStartupSteps.ParseAndHandleOptions), nameof(IAppStartupSteps.EnsureSingleInstance))]
        public void TheOrder_KeepsTheDependenciesBetweenSteps(string first, string second)
        {
            var steps = Run(new RecordingSteps());

            Assert.True(steps.Before(first, second), $"{first} は {second} より前でなければならない");
        }

        [Fact]
        public void ACompleteInstallRestart_StopsRightAfterHandlingIt()
        {
            var steps = Run(new RecordingSteps { CompleteInstall = true });

            Assert.Equal(new[] { nameof(IAppStartupSteps.RunPreHostBootstrap), nameof(IAppStartupSteps.TryHandleCompleteInstall) }, steps.Calls);
        }

        [Fact]
        public void AHelperLaunch_StopsAfterTheOptions_WithoutBuildingTheHost()
        {
            var steps = Run(new RecordingSteps { Exit = true });

            Assert.Equal(nameof(IAppStartupSteps.ParseAndHandleOptions), steps.Calls.Last());
            Assert.DoesNotContain(nameof(IAppStartupSteps.CreateHost), steps.Calls);
        }

        [Fact]
        public void ASecondInstance_StopsBeforeTouchingTheLogsOrTheHost()
        {
            var steps = Run(new RecordingSteps { SingleInstance = false });

            Assert.Equal(nameof(IAppStartupSteps.EnsureSingleInstance), steps.Calls.Last());
            Assert.DoesNotContain(nameof(IAppStartupSteps.RotateLogsAndLogStartupDiagnostics), steps.Calls);
        }

        [Fact]
        public void ACancelledFirstRunLanguage_StopsBeforeTheSaveLocationAndTheSettings()
        {
            var steps = Run(new RecordingSteps { LanguageOk = false });

            Assert.Equal(nameof(IAppStartupSteps.RunFirstRunLanguageSelection), steps.Calls.Last());
            Assert.DoesNotContain(nameof(IAppStartupSteps.LoadAppSettings), steps.Calls);
        }

        [Fact]
        public void ACancelledSaveLocation_StopsBeforeTheSettingsAreRead()
        {
            var steps = Run(new RecordingSteps { SaveLocationOk = false });

            Assert.Equal(nameof(IAppStartupSteps.RunFirstRunSaveLocationAndSkeleton), steps.Calls.Last());
            Assert.DoesNotContain(nameof(IAppStartupSteps.LoadAppSettings), steps.Calls);
        }
    }
}
