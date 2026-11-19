#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// コマンドの共通の窓口 <see cref="CommandGuard"/> の規則: 実行中は CanExecute が false、再入は拒む、例外は外へ出さず記録して通知、
    /// 取り消しは失敗ではない、CanExecuteChanged が発火する。ViewModel のコマンドはこの窓口を通して作る（旧 RelayCommand の観点を引き継ぐ）。
    /// </summary>
    public class CommandGuardTests
    {
        [Fact]
        public async Task CanExecute_IsFalseWhileRunning_AndChangedIsRaised()
        {
            var gate = new TaskCompletionSource();
            var command = CommandGuard.Create(() => gate.Task);
            int raised = 0;
            command.CanExecuteChanged += (_, _) => raised++;

            Assert.True(command.CanExecute(null));
            Task running = command.ExecuteAsync(null);
            Assert.False(command.CanExecute(null));
            gate.SetResult();
            await running;
            Assert.True(command.CanExecute(null));
            Assert.True(raised >= 2);
        }

        [Fact]
        public async Task Reentry_WhileRunning_IsRefused()
        {
            var gate = new TaskCompletionSource();
            int calls = 0;
            var command = CommandGuard.Create(() => { calls++; return gate.Task; });
            Task first = command.ExecuteAsync(null);
            Task second = command.ExecuteAsync(null);
            await second;
            Assert.Equal(1, calls);
            gate.SetResult();
            await first;

            await command.ExecuteAsync(null);
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task Exception_IsReportedAndNeverEscapes()
        {
            Exception? reported = null;
            var command = CommandGuard.Create(async () => { await Task.Yield(); throw new InvalidOperationException("boom"); }, onError: e => reported = e);
            await command.ExecuteAsync(null);
            Assert.IsType<InvalidOperationException>(reported);
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void Exception_ThrownSynchronously_AlsoNeverEscapes()
        {
            Exception? reported = null;
            var command = CommandGuard.Create(() => throw new InvalidOperationException("sync"), onError: e => reported = e);
            ((ICommand)command).Execute(null);
            Assert.IsType<InvalidOperationException>(reported);
        }

        [Fact]
        public async Task FailingErrorHandler_DoesNotEscape()
        {
            var command = CommandGuard.Create(() => throw new InvalidOperationException("x"), onError: _ => throw new ArgumentException("handler"));
            await command.ExecuteAsync(null);
        }

        [Fact]
        public async Task Cancellation_IsNotAFailure()
        {
            bool reported = false;
            var command = CommandGuard.Create(() => throw new OperationCanceledException(), onError: _ => reported = true);
            await command.ExecuteAsync(null);
            Assert.False(reported);
        }

        [Fact]
        public async Task Parameter_IsPassed_AndCanExecutePredicateIsUsed()
        {
            string? seen = null;
            var command = CommandGuard.Create<string>(p => { seen = p; return Task.CompletedTask; }, p => p != "no");
            Assert.False(command.CanExecute("no"));
            Assert.True(command.CanExecute("yes"));
            await command.ExecuteAsync("yes");
            Assert.Equal("yes", seen);
        }

        [Fact]
        public void Sync_ExceptionIsReported_AndCanExecuteChangedCanBeRaised()
        {
            Exception? reported = null;
            bool enabled = false;
            var command = CommandGuard.CreateSync(() => throw new InvalidOperationException("s"), () => enabled, onError: e => reported = e);
            int raised = 0;
            command.CanExecuteChanged += (_, _) => raised++;
            Assert.False(command.CanExecute(null));
            enabled = true;
            command.NotifyCanExecuteChanged();
            Assert.Equal(1, raised);
            command.Execute(null);
            Assert.IsType<InvalidOperationException>(reported);
        }
    }
}
