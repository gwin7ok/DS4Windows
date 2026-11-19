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
            var (vm, lifecycle, _, _, _, _, _, _) = CreateAll();
            return (vm, lifecycle);
        }

        private static (MainWindowsViewModel Vm, FakeServiceLifecycle Lifecycle, FakeShellLauncher Launcher, FakeDialogService Dialogs, FakeShellNavigator Navigator, FakeChangelogService Changelog, FakeRuntimeFeatureSwitch Features, FakeProcessPriorityControl Priority) CreateAll()
        {
            var lifecycle = new FakeServiceLifecycle();
            var launcher = new FakeShellLauncher();
            var dialogs = new FakeDialogService();
            var navigator = new FakeShellNavigator();
            var changelog = new FakeChangelogService();
            var features = new FakeRuntimeFeatureSwitch();
            var priority = new FakeProcessPriorityControl();
            var policy = new UpdateCheckPolicy(changelog, TestRepositoryFactory.CreateAppSettingsService());
            var vm = new MainWindowsViewModel(new EnvironmentService(), new PathService(), lifecycle, launcher, dialogs, policy, features, priority) { Navigator = navigator };
            return (vm, lifecycle, launcher, dialogs, navigator, changelog, features, priority);
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
            Assert.Throws<ArgumentNullException>(() => new MainWindowsViewModel(new EnvironmentService(), new PathService(), null!, new FakeShellLauncher(), new FakeDialogService(), new UpdateCheckPolicy(new FakeChangelogService(), TestRepositoryFactory.CreateAppSettingsService()), new FakeRuntimeFeatureSwitch(), new FakeProcessPriorityControl()));
        }

        [Fact]
        public void LauncherCommands_AskTheLauncher_WithTheExpectedTargets()
        {
            var (vm, _, launcher, dialogs, _, _, _, _) = CreateAll();

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
            var (vm, _, _, _, navigator, _, _, _) = CreateAll();

            vm.ShowAboutCommand.Execute(null);
            vm.ShowChangelogCommand.Execute(null);
            vm.ShowDeviceOptionsCommand.Execute(null);

            Assert.Equal(new[] { "About", "Changelog", "DeviceOptions" }, navigator.Shown);
        }

        [Fact]
        public void NavigationCommands_WithoutANavigator_DoNothing()
        {
            var (vm, _, _, _, _, _, _, _) = CreateAll();
            vm.Navigator = null;
            vm.ShowAboutCommand.Execute(null);
        }

        [Fact]
        public async Task CheckUpdates_WhenNewerExists_OpensTheUpdater()
        {
            var (vm, _, _, dialogs, navigator, changelog, _, _) = CreateAll();
            changelog.Newer = true;
            changelog.Latest = new Version(9, 9, 9);

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "Updater:9.9.9" }, navigator.Shown);
            Assert.Empty(dialogs.Messages);
        }

        [Fact]
        public async Task CheckUpdates_WhenUpToDate_ShowsAMessage()
        {
            var (vm, _, _, dialogs, navigator, _, _, _) = CreateAll();

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Empty(navigator.Shown);
            Assert.Single(dialogs.Messages);
        }

        [Fact]
        public async Task CheckUpdates_WhenTheCheckFails_ShowsAFailureMessage_AndDoesNotEscape()
        {
            var (vm, _, _, dialogs, navigator, changelog, _, _) = CreateAll();
            changelog.Throw = true;

            await vm.CheckUpdatesCommand.ExecuteAsync(null);

            Assert.Empty(navigator.Shown);
            Assert.Single(dialogs.Messages);
        }

        [Fact]
        public void ProfileListCommands_AreGatedBySelection_ExceptNewAndImport()
        {
            var (vm, _, _, _, navigator, _, _, _) = CreateAll();
            Assert.True(vm.NewProfileCommand.CanExecute(null));
            Assert.True(vm.ImportProfileCommand.CanExecute(null));
            Assert.False(vm.EditProfileCommand.CanExecute(null));
            Assert.False(vm.DeleteProfileCommand.CanExecute(null));
            Assert.False(vm.RenameProfileCommand.CanExecute(null));
            Assert.False(vm.DuplicateProfileCommand.CanExecute(null));
            Assert.False(vm.ExportProfileCommand.CanExecute(null));

            int raised = 0;
            vm.EditProfileCommand.CanExecuteChanged += (_, _) => raised++;
            vm.SelectedProfileIndex = 2;

            Assert.Equal(1, raised);
            Assert.True(vm.EditProfileCommand.CanExecute(null));
            Assert.True(vm.ExportProfileCommand.CanExecute(null));

            vm.NewProfileCommand.Execute(null);
            vm.EditProfileCommand.Execute(null);
            vm.DeleteProfileCommand.Execute(null);
            vm.RenameProfileCommand.Execute(null);
            vm.DuplicateProfileCommand.Execute(null);
            vm.ImportProfileCommand.Execute(null);
            vm.ExportProfileCommand.Execute(null);
            Assert.Equal(new[] { "NewProfile", "EditProfile", "DeleteProfile", "RenameProfile", "DuplicateProfile", "ImportProfile", "ExportProfile" }, navigator.Shown);
        }

        [Fact]
        public void DeviceProfileCommands_PassTheDeviceIndex()
        {
            var (vm, _, _, _, navigator, _, _, _) = CreateAll();

            vm.EditDeviceProfileCommand.Execute(1);
            vm.NewDeviceProfileCommand.Execute(3);

            Assert.Equal(new[] { "EditDeviceProfile:1", "NewDeviceProfile:3" }, navigator.Shown);
        }

        [Fact]
        public void FeatureToggles_PassTheCheckedState()
        {
            var (vm, _, _, _, navigator, _, features, _) = CreateAll();

            vm.SetOscListenerCommand.Execute(true);
            vm.SetOscSenderCommand.Execute(false);
            vm.SetSwipeTouchCommand.Execute(true);
            vm.SetRunAtStartPanelCommand.Execute(true);
            vm.ExportLogCommand.Execute(null);
            vm.ClearLogCommand.Execute(null);

            Assert.Equal(new[] { "OscListener:True", "OscSender:False" }, features.Calls);
            Assert.Equal(new[] { "Hotkeys:True", "RunStartPanel:True", "ExportLog", "ClearLog" }, navigator.Shown);
        }

        [Fact]
        public async Task UdpServer_TurnOn_OpensThePortBeforeTheMotionFeed_AndTurnOffReverses()
        {
            var (vm, _, _, _, _, _, features, _) = CreateAll();

            await vm.SetUdpServerCommand.ExecuteAsync(true);
            await vm.SetUdpServerCommand.ExecuteAsync(false);

            Assert.Equal(new[] { "Udp:True", "Motion:True", "Motion:False", "Udp:False" }, features.Calls);
        }

        [Fact]
        public async Task DriverSetup_StopsTheServiceOnlyWhenRunning_ThenRunsTheInstallerElevated_AndRefreshes()
        {
            var (vm, lifecycle, launcher, _, navigator, _, _, _) = CreateAll();
            lifecycle.Running = true;

            await vm.DriverSetupCommand.ExecuteAsync(null);

            Assert.Equal(1, lifecycle.StopCalls);
            Assert.False(vm.IsServiceBusy);
            Assert.Single(launcher.Calls);
            Assert.StartsWith("RunElevatedAndWait:", launcher.Calls[0]);
            Assert.EndsWith("-driverinstall", launcher.Calls[0]);
            Assert.Equal(new[] { "RefreshDriverCheck" }, navigator.Shown);

            lifecycle.Running = false;
            await vm.DriverSetupCommand.ExecuteAsync(null);
            Assert.Equal(1, lifecycle.StopCalls);
        }

        [Fact]
        public void ApplyProcessPriority_AppliesTheChosenClass()
        {
            var (vm, _, _, dialogs, _, _, _, priority) = CreateAll();
            int index = DS4Windows.Core.Platform.ProcessPriorityOptions.IndexOf(System.Diagnostics.ProcessPriorityClass.AboveNormal);

            int effective = vm.ApplyProcessPriority(index);

            Assert.Equal(index, effective);
            Assert.Equal(new[] { System.Diagnostics.ProcessPriorityClass.AboveNormal }, priority.Applied);
            Assert.Empty(dialogs.Messages);
        }

        [Fact]
        public void ApplyProcessPriority_OutOfRange_ChangesNothing()
        {
            var (vm, _, _, _, _, _, _, priority) = CreateAll();

            Assert.Equal(99, vm.ApplyProcessPriority(99));
            Assert.Equal(-1, vm.ApplyProcessPriority(-1));
            Assert.Empty(priority.Applied);
        }

        [Fact]
        public void ShowLogMessageCommand_AsksTheNavigator()
        {
            var (vm, _, _, _, navigator, _, _, _) = CreateAll();

            vm.ShowLogMessageCommand.Execute(null);

            Assert.Equal(new[] { "LogMessage" }, navigator.Shown);
        }
    }
}
