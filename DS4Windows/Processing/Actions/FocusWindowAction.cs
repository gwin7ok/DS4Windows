using System;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Phase12-Step2 2d（R-4）: FocusWindow（指定のウィンドウを前面へ出す）の出力。立ち上がりの 1 回だけ実行する。
    /// 対象は <c>sa.details</c>（プログラムのパス）と <c>sa.extra</c>（ウィンドウタイトル）で、Auto-Profile と同じ規則（<see cref="WindowTargetMatcher"/>）で特定する。
    /// 前面化は、確認の待ちを含み数十〜数百ミリ秒かかるため、入力処理のスレッドを止めないよう、別のスレッドで行う（<see cref="IWindowFocuser"/> は同期の部品）。
    /// 前の前面化が終わっていないうちの再実行は、重ねずに捨てる（押しっぱなしの連発や、連打で、前面化の試行が並ばないように）。
    /// 結果（見つからない・失敗）の警告ログは、<see cref="IWindowFocuser"/> が出す。ここでは、実行の要約を Debug で出す。
    /// </summary>
    public sealed class FocusWindowAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly IWindowFocuser _focuser;
        private readonly WindowTargetMatcher _target;
        private readonly Action<Action> _runInBackground;
        private int _running;   // 0＝待機中、1＝前面化の最中

        public FocusWindowAction(SpecialAction sa, IWindowFocuser focuser)
            : this(sa, focuser, work => Task.Run(work))
        {
        }

        /// <summary>テスト用: 別スレッドへ逃がす処理を差し替える（同じスレッドで実行して、結果をその場で確かめる）。</summary>
        public FocusWindowAction(SpecialAction sa, IWindowFocuser focuser, Action<Action> runInBackground)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _focuser = focuser ?? throw new ArgumentNullException(nameof(focuser));
            _runInBackground = runInBackground ?? throw new ArgumentNullException(nameof(runInBackground));
            _target = new WindowTargetMatcher(sa.details, sa.extra);
        }

        public string Id => _sa.name ?? "FocusWindow";

        public void Execute(IOutputContext ctx)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[WindowFocus] Skipped: previous focus is still running: id={Id}");
                return;
            }

            try
            {
                _runInBackground(() =>
                {
                    try
                    {
                        WindowFocusResult result = _focuser.Focus(_target, _sa.centerCursor);
                        if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[WindowFocus] Executed: id={Id}, path={_target.Path}, title={_target.Title}, result={result}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogWarn($"[WindowFocus] Unexpected error: id={Id}, {ex.GetType().Name}: {ex.Message}");
                    }
                    finally
                    {
                        Volatile.Write(ref _running, 0);
                    }
                });
            }
            catch (Exception ex)
            {
                // 別スレッドへ逃がす処理そのものが失敗したとき（通常は起きない）。次の実行を妨げないよう、印を戻す
                Volatile.Write(ref _running, 0);
                AppLogger.LogWarn($"[WindowFocus] Could not start the focus work: id={Id}, {ex.GetType().Name}: {ex.Message}");
            }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
