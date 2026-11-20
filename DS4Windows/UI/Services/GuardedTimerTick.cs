#nullable enable

using System;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 「止めて、処理して、始め直す」形のタイマー（<c>AutoReset = false</c>）の 1 回ぶんを包む。
    /// 処理が例外を出しても、始め直しを必ず行う（行わないと、タイマーが止まったままになり、
    /// スワイプでの切替や自動プロファイルが、以後ずっと動かなくなる）。
    /// </summary>
    public static class GuardedTimerTick
    {
        /// <param name="body">1 回ぶんの処理。</param>
        /// <param name="shouldRestart">終了のときなど、始め直してはいけない場合に偽を返す。</param>
        /// <param name="restart">タイマーの始め直し。</param>
        /// <param name="onError">処理の例外の受け口（ログ）。ここで例外を出さないこと。</param>
        public static void Run(Action body, Func<bool> shouldRestart, Action restart, Action<Exception> onError)
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                try { onError(ex); }
                catch { /* ログの失敗で、始め直しを妨げない */ }
            }
            finally
            {
                if (shouldRestart())
                {
                    restart();
                }
            }
        }
    }
}
