#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.Core.Logging;

namespace DS4Windows.UI.ViewModels.Util
{
    /// <summary>
    /// 画面のコマンドを作る共通の窓口。ViewModel のコマンドは、すべてここを通して作る。
    /// <list type="bullet">
    /// <item>例外は本体の外へ出さない。ツールキットの <c>AsyncRelayCommand</c> は、<c>ICommand.Execute</c>（XAML のバインディングが呼ぶ形）で、
    /// 例外を同期コンテキストへ再スローする（<c>async void</c> と同じで、WPF ではアプリが落ちる）。ここで受けて <see cref="AppLogger"/> へ記録し、
    /// <c>onError</c> があれば呼ぶ（利用者への通知は、呼び出し側が <c>IDialogService</c> で決める）。キャンセルは失敗として扱わない。</item>
    /// <item>実行中の再入は、<c>CanExecute</c>（ボタンの無効化）に加えて、ここでも拒む。ツールキットは <c>Execute</c>／<c>ExecuteAsync</c> では再入を拒まないため、
    /// ホットキー・トレイ・コードから呼ばれても二重に走らない。</item>
    /// </list>
    /// </summary>
    public static class CommandGuard
    {
        /// <summary>引数のない非同期のコマンド。実行中は <c>CanExecute</c> が false になる。</summary>
        public static AsyncRelayCommand Create(Func<Task> body, Func<bool>? canExecute = null, string name = "Command", Action<Exception>? onError = null)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            int running = 0;
            Func<Task> guarded = async () =>
            {
                if (Interlocked.Exchange(ref running, 1) == 1) return;
                try
                {
                    await body().ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // 取り消しは失敗ではない（記録も通知もしない）
                    return;
                }
                catch (Exception ex)
                {
                    Report(name, ex, onError);
                }
                finally
                {
                    Volatile.Write(ref running, 0);
                }
            };
            return canExecute == null
                ? new AsyncRelayCommand(guarded, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler)
                : new AsyncRelayCommand(guarded, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        }

        /// <summary>引数をとる非同期のコマンド（<c>CommandParameter</c>）。</summary>
        public static AsyncRelayCommand<T> Create<T>(Func<T?, Task> body, Predicate<T?>? canExecute = null, string name = "Command", Action<Exception>? onError = null)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            int running = 0;
            Func<T?, Task> guarded = async parameter =>
            {
                if (Interlocked.Exchange(ref running, 1) == 1) return;
                try
                {
                    await body(parameter).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // 取り消しは失敗ではない（記録も通知もしない）
                    return;
                }
                catch (Exception ex)
                {
                    Report(name, ex, onError);
                }
                finally
                {
                    Volatile.Write(ref running, 0);
                }
            };
            return canExecute == null
                ? new AsyncRelayCommand<T>(guarded, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler)
                : new AsyncRelayCommand<T>(guarded, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        }

        /// <summary>同期のコマンド。例外は記録して続ける。</summary>
        public static RelayCommand CreateSync(Action body, Func<bool>? canExecute = null, string name = "Command", Action<Exception>? onError = null)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            Action guarded = () =>
            {
                try
                {
                    body();
                }
                catch (Exception ex)
                {
                    Report(name, ex, onError);
                }
            };
            return canExecute == null ? new RelayCommand(guarded) : new RelayCommand(guarded, canExecute);
        }

        /// <summary>引数をとる同期のコマンド。</summary>
        public static RelayCommand<T> CreateSync<T>(Action<T?> body, Predicate<T?>? canExecute = null, string name = "Command", Action<Exception>? onError = null)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            Action<T?> guarded = parameter =>
            {
                try
                {
                    body(parameter);
                }
                catch (Exception ex)
                {
                    Report(name, ex, onError);
                }
            };
            return canExecute == null ? new RelayCommand<T>(guarded) : new RelayCommand<T>(guarded, canExecute);
        }

        private static void Report(string name, Exception ex, Action<Exception>? onError)
        {
            AppLogger.LogError($"[Command] {name} failed: {ex}");
            if (onError == null) return;
            try
            {
                onError(ex);
            }
            catch (Exception notifyFailure)
            {
                // 通知の失敗で、コマンドの呼び出し元（画面）を落とさない
                AppLogger.LogError($"[Command] {name} error handler failed: {notifyFailure.Message}");
            }
        }
    }
}
