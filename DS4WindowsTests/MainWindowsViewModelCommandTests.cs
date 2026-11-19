#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Platform;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// メイン画面の ViewModel のコマンド: 開始・停止の分岐（動作中なら停止、止まっていれば開始）、忙しい間の再実行の拒否、
    /// 通知までボタンを押せないこと、設定の変更に伴う再起動（停止→開始。操作中のチェックは無効）、失敗してもボタンが戻ること。
    /// </summary>
    public class MainWindowsViewModelCommandTests
    {
        private static (MainWindowsViewModel Vm, FakeServiceLifecycle Lifecycle) Create()
        {
            var (vm, lifecycle, _, _, _, _) = CreateAll();
            return (vm, lifecycle);
        }

        private static (MainWindowsViewModel Vm, FakeServiceLifecycle Lifecycle, FakeShellLauncher Launcher, FakeDialogService Dialogs, FakeShellNavigator Navigator, FakeChangelogService Changelog) CreateAll()
        {
            var lifecycle = new FakeServiceLifecycle();
            var launcher = new FakeShellLauncher();
            var dialogs = new FakeDialogService();
            var navigator = new FakeShellNavigator();
            var changelog = new FakeChangelogService();
            var policy = new UpdateCheckPolicy(changelog, TestRepositoryFactory.CreateAppSettingsService());
            var vm = new MainWindowsViewModel(new EnvironmentService(), new PathService(), lifecycle, launcher, dialogs, policy) { Navigator = navigator };
            return (vm, lifecycle, launcher, dialogs, navigator, changelog);
        }

        [Fact]
        public async Task Toggle_StartsWhenStopped_AndStopsWithImmediateUnplugWhenRunning()
        {
            var (vm, lifecycle) = Create();

            await vm.ToggleServiceCommand.ExecuteAsync(null);
            Assert.Equal(1, lifecycle.StartCalls);
            Assert.Equal(0, lifecycle.StopCalls);
            vm.NotifyServiceStateChanged();

            await vm.ToggleServiceCommand.ExecuteAsync(null);
            Assert.Equal(1, lifecycle.StopCalls);
            Assert.True(lifecycle.LastStopImmediateUnplug);
        }

        [Fact]
        public async Task Toggle_StaysBusyUntilTheServiceStateChangeIsNotified()
        {
            var (vm, _) = Create();
            Assert.True(vm.ToggleServiceCommand.CanExecute(null));

            await vm.ToggleServiceCommand.ExecuteAsync(null);
            Assert.True(vm.IsServiceBusy);
            Assert.False(vm.ToggleServiceCommand.CanExecute(null));

            vm.NotifyServiceStateChanged();
            Assert.False(vm.IsServiceBusy);
            Assert.True(vm.ToggleServiceCommand.CanExecute(null));
        }

        [Fact]
        public async Task Toggle_WhileRunning_IsRefused()
        {
            var (vm, lifecycle) = Create();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            lifecycle.OnCall = () => { entered.Set(); release.Wait(5000); };

            Task first = vm.ToggleServiceCommand.ExecuteAsync(null);
            Assert.True(entered.Wait(5000));
            await vm.ToggleServiceCommand.ExecuteAsync(null);
            release.Set();
            await first;

            Assert.Equal(1, lifecycle.StartCalls);
        }

        [Fact]
        public async Task Toggle_WhenTheServiceThrows_ReleasesTheButton_AndDoesNotEscape()
        {
            var (vm, lifecycle) = Create();
            lifecycle.OnCall = () => throw new InvalidOperationException("driver");

            await vm.ToggleServiceCommand.ExecuteAsync(null);

            Assert.False(vm.IsServiceBusy);
            Assert.True(vm.ToggleServiceCommand.CanExecute(null));
        }

        [Fact]
        public async Task RestartForHideDs4_StopsThenStarts_AndRestoresTheControls()
        {
            var (vm, lifecycle) = Create();
            bool disabledDuring = false;
            lifecycle.OnCall = () => disabledDuring |= !vm.HideDs4ToggleEnabled && vm.HideXboxToggleEnabled;

            await vm.RestartForHideDs4Command.ExecuteAsync(null);

            Assert.Equal(1, lifecycle.StopCalls);
            Assert.Equal(1, lifecycle.StartCalls);
            Assert.False(lifecycle.LastStopImmediateUnplug);
            Assert.True(disabledDuring);
            Assert.True(vm.HideDs4ToggleEnabled);
            Assert.False(vm.IsServiceBusy);
        }

        [Fact]
        public async Task RestartForHideXbox_DisablesOnlyTheXboxCheck_AndRestoresAfterFailure()
        {
            var (vm, lifecycle) = Create();
            bool xboxOnly = false;
            lifecycle.OnCall = () =>
            {
                xboxOnly |= !vm.HideXboxToggleEnabled && vm.HideDs4ToggleEnabled;
                throw new InvalidOperationException("x");
            };

            await vm.RestartForHideXboxCommand.ExecuteAsync(null);

            Assert.True(xboxOnly);
            Assert.True(vm.HideXboxToggleEnabled);
            Assert.False(vm.IsServiceBusy);
        }

        [Fact]
        public void Constructor_RequiresTheLifecycle()
        {
            Assert.Throws<ArgumentNullException>(() => new MainWindowsViewModel(new EnvironmentService(), new PathService(), null!, new FakeShellLauncher(), new FakeDialogService(), new UpdateCheckPolicy(new FakeChangelogService(), TestRepositoryFactory.CreateAppSettingsService())));
        }

        [Fact]
        public void LauncherCommands_AskTheLauncher_WithTheExpectedTargets()
        {
            var (vm, _, launcher, dialogs, _, _) = CreateAll();

            vm.OpenProfilesFolderCommand.Execute(null);
            vm.OpenControlPanelCommand.Execute(null);
            vm.OpenGamepadTesterCommand.Execute(null);
            vm.OpenXInputCheckerCommand.Execute(null);
            vm.ExplainCustomExeNameCommand.Execute(null);

            Assert.EndsWith("Profiles", launcher.Calls[0]);
            Assert.StartsWith("OpenPath:", launcher.Calls[0]);
            Assert.Equal("RunProgram:control joy.cpl", launcher.Calls[1]);
            Assert.Equal("OpenUrl:https://gamepad-tester.com/", launcher.Calls[2]);
            Assert.Contains("XInputChecker.exe", launcher.Calls[3]);
            Assert.Single(dialogs.Messages);
        }

        [Fact]
        public void NavigationCommands_AskTheNavigator()
        {
            var (vm, _, _, _, navigator, _) = CreateAll();

            vm.ShowAboutCommand.Execute(null);
            vm.ShowChangelogCommand.Execute(null);
            vm.ShowDeviceOptionsCommand.Execute(null);

            Assert.Equal(new[] { "About", "Changelog", "DeviceOptions" }, navigator.Shown);
        }

        [Fact]
        public void NavigationCommands_WithoutANavigator_DoNothing()
        {
            var (vm, _, _, _, _, _) = CreateAll();
            vm.Navigator = null;
            vm.ShowAboutCommand.Execute(null);
        }

        [Fact]
        public async Task CheckUpdates_WhenNewerExists_OpensTheUpdater()
        {
            var (vm, _, _, dialogs, navigator, changelog) = CreateAll();
            changelog.Newer = true;
            changelog.Latest = new Version(9, 9, 9);

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "Updater:9.9.9" }, navigator.Shown);
            Assert.Empty(dialogs.Messages);
        }

        [Fact]
        public async Task CheckUpdates_WhenUpToDate_ShowsAMessage()
        {
            var (vm, _, _, dialogs, navigator, _) = CreateAll();

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Empty(navigator.Shown);
            Assert.Single(dialogs.Messages);
        }

        [Fact]
        public async Task CheckUpdates_WhenTheCheckFails_ShowsAFailureMessage_AndDoesNotEscape()
        {
            var (vm, _, _, dialogs, navigator, changelog) = CreateAll();
            changelog.Throw = true;

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Empty(navigator.Shown);
            Assert.Single(dialogs.Messages);
        }
    }
}
