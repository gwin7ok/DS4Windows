using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step3 3c: マクロのマウス移動のテスト用の <see cref="IWindowSystem"/>。ポインターの移動（<see cref="MoveCursorTo"/>）と前面ウィンドウの原点だけを持ち、
    /// 実際のカーソルは動かさない。ほかの操作は使われない（呼ばれたらテストの誤り）。
    /// </summary>
    public sealed class RecordingWindowSystem : IWindowSystem
    {
        public readonly List<(int X, int Y)> MovedTo = new List<(int X, int Y)>();
        public bool HasForegroundWindow = true;
        public (int X, int Y) ForegroundOrigin = (0, 0);
        public bool MoveResult = true;

        /// <summary>座標にあるウィンドウ（見つかったか、プロセスの ID、外枠の左上）。既定は、どこでも別のプロセスのウィンドウ（左上は 0, 0）。</summary>
        public Func<int, int, (bool Found, int ProcessId, int OriginX, int OriginY)> WindowAt = (x, y) => (true, 1234, 0, 0);

        public bool MoveCursorTo(int x, int y)
        {
            MovedTo.Add((x, y));
            return MoveResult;
        }

        public bool TryGetForegroundWindowOrigin(out int x, out int y)
        {
            x = ForegroundOrigin.X;
            y = ForegroundOrigin.Y;
            return HasForegroundWindow;
        }

        public bool TryGetWindowAtPoint(int x, int y, out int processId, out int originX, out int originY)
        {
            (bool found, int pid, int ox, int oy) = WindowAt(x, y);
            processId = pid;
            originX = ox;
            originY = oy;
            return found;
        }

        public IReadOnlyList<WindowCandidate> EnumerateWindows() => throw new NotSupportedException();
        public bool IsMinimized(IntPtr window) => throw new NotSupportedException();
        public void Restore(IntPtr window) => throw new NotSupportedException();
        public bool IsHung(IntPtr window) => throw new NotSupportedException();
        public bool IsForeground(IntPtr window) => throw new NotSupportedException();
        public bool TrySetForeground(IntPtr window) => throw new NotSupportedException();
        public IDisposable AttachInput(IntPtr window) => throw new NotSupportedException();
        public void SendAltUp() => throw new NotSupportedException();
        public void SendAltTap() => throw new NotSupportedException();
        public void BringToTop(IntPtr window) => throw new NotSupportedException();
        public bool MoveCursorToCenter(IntPtr window) => throw new NotSupportedException();
        public CapturedWindow GetWindowUnderCursor() => throw new NotSupportedException();
    }
}
