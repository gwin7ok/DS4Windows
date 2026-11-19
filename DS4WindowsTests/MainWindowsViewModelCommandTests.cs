#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Platform;
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
            var lifecycle = new FakeServiceLifecycle();
            return (new MainWindowsViewModel(new EnvironmentService(), new PathService(), lifecycle), lifecycle);
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
            Assert.Throws<ArgumentNullException>(() => new MainWindowsViewModel(new EnvironmentService(), new PathService(), null!));
        }
    }
}
