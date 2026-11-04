using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// Phase12-Step2 2b-2（R-4）: <see cref="IWindowFocuser"/> の実装。手順は AutoHotkey の <c>SetForegroundWindowEx</c> を参考にした
    /// （Phase12-Step2-Plan.md §5b）:
    /// ①対象を探す（Z 順で最初に一致した 1 つ）→ ②応答なしなら失敗 → ③最小化なら復元 → ④すでに前面なら成功 →
    /// ⑤素の前面化を 1 回 → ⑥入力を結び付けて最大 5 回（初回の前に Alt の解放を 1 回送る）→ ⑦なお失敗なら Alt の押下・解放を 2 回送って再試行 →
    /// ⑧成功なら最上位へ。管理者権限のウィンドウなど、前面化できないものは、警告ログを出して <see cref="WindowFocusResult.Failed"/> を返す。
    /// <para>ログは Log-Policy.md に従う（旧 API。汎用 Log への集約は Phase13b）。</para>
    /// </summary>
    public class WindowFocuser : IWindowFocuser
    {
        private const int AttachedAttempts = 5;
        private const int AltTapRepeats = 2;

        private readonly IWindowSystem windowSystem;

        public WindowFocuser(IWindowSystem windowSystem)
        {
            this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
        }

        public WindowFocusResult Focus(WindowTargetMatcher target, bool centerCursor = false)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (target.IsEmpty)
            {
                AppLogger.LogWarn("[WindowFocus] Target is empty: neither path nor title is specified");
                return WindowFocusResult.InvalidTarget;
            }

            try
            {
                WindowCandidate found = Find(target);
                if (found == null)
                {
                    AppLogger.LogWarn($"[WindowFocus] Target window not found: path={target.Path}, title={target.Title}");
                    return WindowFocusResult.NotFound;
                }

                IntPtr window = found.Handle;
                if (windowSystem.IsHung(window))
                {
                    AppLogger.LogWarn($"[WindowFocus] Target window is not responding: path={found.ProcessPath}, title={found.Title}");
                    return WindowFocusResult.Failed;
                }

                // 復元は、すでに前面かの確認より先に行う（最小化されたままのウィンドウが「前面」と判定されるのを避ける）
                if (windowSystem.IsMinimized(window))
                {
                    windowSystem.Restore(window);
                    if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[WindowFocus] Restored minimized window: title={found.Title}");
                }

                if (windowSystem.IsForeground(window))
                {
                    if (centerCursor) MoveCursorToCenter(window, found);
                    return WindowFocusResult.Focused;
                }

                if (TryActivate(window))
                {
                    windowSystem.BringToTop(window);
                    if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[WindowFocus] Focused window: path={found.ProcessPath}, title={found.Title}");
                    if (centerCursor) MoveCursorToCenter(window, found);
                    return WindowFocusResult.Focused;
                }

                AppLogger.LogWarn($"[WindowFocus] Failed to bring the window to the foreground (elevated window or Windows restriction): path={found.ProcessPath}, title={found.Title}");
                return WindowFocusResult.Failed;
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"[WindowFocus] Unexpected error: {ex.GetType().Name}: {ex.Message}");
                return WindowFocusResult.Failed;
            }
        }

        /// <summary>前面化に成功したウィンドウの中央へ、マウスポインターを移動する。動かせなかったとき（最小化など）は、Debug ログだけ。</summary>
        private void MoveCursorToCenter(IntPtr window, WindowCandidate found)
        {
            bool moved = windowSystem.MoveCursorToCenter(window);
            if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[WindowFocus] Cursor moved to window center: moved={moved}, title={found.Title}");
        }

        private WindowCandidate Find(WindowTargetMatcher target)
        {
            foreach (WindowCandidate candidate in windowSystem.EnumerateWindows())
            {
                if (target.IsMatch(candidate.ProcessPath, candidate.Title)) return candidate;
            }

            return null;
        }

        /// <summary>手を段階的に強めて前面化を試す。成功したら true。</summary>
        private bool TryActivate(IntPtr window)
        {
            if (windowSystem.TrySetForeground(window)) return true;

            using (windowSystem.AttachInput(window))
            {
                for (int attempt = 0; attempt < AttachedAttempts; attempt++)
                {
                    if (attempt == 0) windowSystem.SendAltUp();
                    if (windowSystem.TrySetForeground(window)) return true;
                }

                for (int i = 0; i < AltTapRepeats; i++) windowSystem.SendAltTap();
                return windowSystem.TrySetForeground(window);
            }
        }
    }
}
