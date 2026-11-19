#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// CommunityToolkit.Mvvm の振る舞いの記録（採用の根拠。共通の窓口 CommandGuard が必要な理由）。<c>AsyncRelayCommand</c> が、実行中は <c>CanExecute</c> を false にすること、
    /// 例外の既定の扱い（<c>FlowExceptionsToTaskScheduler</c> の有無）、キャンセルを確かめる。
    /// </summary>
    public class ToolkitAsyncCommandBehaviorTests
    {
        [Fact]
        public async Task CanExecute_IsFalseWhileRunning_AndRaisedOnChange()
        {
            var gate = new TaskCompletionSource();
            var command = new AsyncRelayCommand(() => gate.Task);
            int raised = 0;
            command.CanExecuteChanged += (_, _) => raised++;

            Assert.True(command.CanExecute(null));
            Task running = command.ExecuteAsync(null);
            Assert.False(command.CanExecute(null));
            Assert.True(command.IsRunning);
            gate.SetResult();
            await running;
            Assert.True(command.CanExecute(null));
            Assert.True(raised >= 2);
        }

        [Fact]
        public async Task ICommandExecute_WhileRunning_StillRunsAgain_SoCanExecuteMustGateTheButton()
        {
            // 観測: ツールキットは、実行中の再入を Execute／ExecuteAsync では拒まない。拒むのは CanExecute（ボタンの無効化）。
            // よって、バインディング以外（ホットキー・トレイ・コードからの呼び出し）では、呼ぶ側が CanExecute を確かめる規則が要る。
            var gate = new TaskCompletionSource();
            int calls = 0;
            var command = new AsyncRelayCommand(() => { calls++; return gate.Task; });
            Task first = command.ExecuteAsync(null);
            Assert.False(command.CanExecute(null));
            Task second = command.ExecuteAsync(null);
            Assert.Equal(2, calls);
            gate.SetResult();
            await first;
            await second;
        }

        [Fact]
        public async Task Exception_DefaultFlow_IsRethrownToTheAwaiter_AndRethrownOnTheContextWhenExecuted()
        {
            var command = new AsyncRelayCommand(async () => { await Task.Yield(); throw new InvalidOperationException("boom"); });
            // ExecuteAsync を await する側には届く（呼び出し側が包めば、アプリは落ちない）
            await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync(null));

            // ICommand.Execute（XAML のバインディングが呼ぶ形）は、既定では同期コンテキストへ再スローする。
            // 同期コンテキストが無いテストでは、スレッドプールで未処理になる。ここでは、その挙動を観測する。
            Exception? seen = null;
            var context = new CapturingContext(e => seen = e);
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var c2 = new AsyncRelayCommand(async () => { await Task.Yield(); throw new InvalidOperationException("boom2"); });
                ((System.Windows.Input.ICommand)c2).Execute(null);
                await context.WaitAsync();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            Assert.IsType<InvalidOperationException>(seen);
        }

        [Fact]
        public async Task Exception_WithFlowToTaskScheduler_DoesNotReachTheContext()
        {
            Exception? seen = null;
            var context = new CapturingContext(e => seen = e);
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var command = new AsyncRelayCommand(async () => { await Task.Yield(); throw new InvalidOperationException("boom3"); }, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
                ((System.Windows.Input.ICommand)command).Execute(null);
                await Task.Delay(100);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            Assert.Null(seen);
        }

        [Fact]
        public async Task Cancel_StopsTheRunningCommand()
        {
            var command = new AsyncRelayCommand(async ct => await Task.Delay(Timeout.Infinite, ct));
            Task running = command.ExecuteAsync(null);
            Assert.True(command.CanBeCanceled);
            command.Cancel();
            await Task.Delay(50);
            Assert.True(running.IsCanceled);
            Assert.False(command.IsRunning);
        }

        private sealed class CapturingContext : SynchronizationContext
        {
            private readonly Action<Exception> _onException;
            private readonly TaskCompletionSource _signal = new TaskCompletionSource();

            public CapturingContext(Action<Exception> onException) { _onException = onException; }

            public Task WaitAsync() => Task.WhenAny(_signal.Task, Task.Delay(1000));

            public override void Post(SendOrPostCallback d, object? state)
            {
                try { d(state); }
                catch (Exception e) { _onException(e); _signal.TrySetResult(); }
            }
        }
    }
}
