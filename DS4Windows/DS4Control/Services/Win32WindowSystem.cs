using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase12-Step2 2b-2: <see cref="IWindowSystem"/> の実装（Win32 の P/Invoke）。手順の判断は <see cref="WindowFocuser"/> が持ち、
    /// ここは個々の操作だけを行う。パスは <c>QueryFullProcessImageName</c>（<c>PROCESS_QUERY_LIMITED_INFORMATION</c>）で求めるため、
    /// 管理者権限のプロセスのウィンドウでも、パスで照合できる（前面化は、権限が足りなければ失敗する）。
    /// </summary>
    public class Win32WindowSystem : IWindowSystem
    {
        private const int SW_RESTORE = 9;
        private const int GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int DWMWA_CLOAKED = 14;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const int ForegroundSettleMs = 10;

        public IReadOnlyList<WindowCandidate> EnumerateWindows()
        {
            var windows = new List<WindowCandidate>();
            var pathByProcess = new Dictionary<uint, string>();
            var text = new StringBuilder(1024);

            EnumWindowsProc callback = (hWnd, lParam) =>
            {
                try
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    if (GetWindowTextLength(hWnd) == 0) return true;
                    if (IsCloaked(hWnd)) return true;

                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0) return true;

                    text.Clear();
                    GetWindowText(hWnd, text, text.Capacity);
                    string title = text.ToString().ToLower();

                    GetWindowThreadProcessId(hWnd, out uint processId);
                    if (!pathByProcess.TryGetValue(processId, out string path))
                    {
                        path = GetProcessPath(processId);
                        pathByProcess[processId] = path;
                    }

                    windows.Add(new WindowCandidate(hWnd, path, title));
                }
                catch
                {
                    // 1 つのウィンドウの情報が取れなくても、列挙は続ける
                }

                return true;
            };

            EnumWindows(callback, IntPtr.Zero);
            GC.KeepAlive(callback);
            return windows;
        }

        public bool IsMinimized(IntPtr window) => IsIconic(window);

        public void Restore(IntPtr window) => ShowWindow(window, SW_RESTORE);

        public bool IsHung(IntPtr window) => IsHungAppWindow(window);

        public bool IsForeground(IntPtr window)
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            // 所有されたウィンドウ（ダイアログなど）が前面のときも、対象が前面化されたものとみなす（所有者のウィンドウ自身は前面になれない）
            return foreground == window || GetWindow(foreground, GW_OWNER) == window;
        }

        public bool TrySetForeground(IntPtr window)
        {
            SetForegroundWindow(window);
            Thread.Sleep(ForegroundSettleMs);
            return IsForeground(window);
        }

        public IDisposable AttachInput(IntPtr window)
        {
            return new AttachedInput(GetForegroundWindow(), window);
        }

        public void SendAltUp() => keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        public void SendAltTap()
        {
            keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void BringToTop(IntPtr window) => BringWindowToTop(window);

        public CapturedWindow GetWindowUnderCursor()
        {
            if (!GetCursorPos(out POINT point)) return null;

            IntPtr window = WindowFromPoint(point);
            if (window == IntPtr.Zero) return null;

            IntPtr root = GetAncestor(window, GA_ROOT);
            if (root != IntPtr.Zero) window = root;

            var text = new StringBuilder(1024);
            GetWindowText(window, text, text.Capacity);
            GetWindowThreadProcessId(window, out uint processId);

            return new CapturedWindow(window, (int)processId, GetProcessPathRaw(processId), text.ToString());
        }

        private static bool IsCloaked(IntPtr hWnd)
        {
            try
            {
                return DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        private static string GetProcessPath(uint processId)
            => GetProcessPathRaw(processId).Replace('/', '\\').ToLower();

        /// <summary>元の大文字小文字のままのフルパス（照合用の正規化は <see cref="GetProcessPath"/>）。</summary>
        private static string GetProcessPathRaw(uint processId)
        {
            IntPtr process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (process == IntPtr.Zero) return string.Empty;

            try
            {
                var buffer = new StringBuilder(1024);
                int size = buffer.Capacity;
                if (!QueryFullProcessImageName(process, 0, buffer, ref size)) return string.Empty;
                return buffer.ToString();
            }
            finally
            {
                CloseHandle(process);
            }
        }

        /// <summary>入力の処理の結び付け。Dispose で、結んだものだけを外す。</summary>
        private sealed class AttachedInput : IDisposable
        {
            private readonly uint currentThread;
            private readonly uint foregroundThread;
            private readonly uint targetThread;
            private bool currentToForeground;
            private bool foregroundToTarget;

            public AttachedInput(IntPtr foregroundWindow, IntPtr targetWindow)
            {
                currentThread = GetCurrentThreadId();
                foregroundThread = foregroundWindow != IntPtr.Zero ? GetWindowThreadProcessId(foregroundWindow, out _) : 0;
                targetThread = GetWindowThreadProcessId(targetWindow, out _);

                if (foregroundThread != 0 && foregroundThread != currentThread)
                    currentToForeground = AttachThreadInput(currentThread, foregroundThread, true);

                if (foregroundThread != 0 && targetThread != 0 && foregroundThread != targetThread)
                    foregroundToTarget = AttachThreadInput(foregroundThread, targetThread, true);
            }

            public void Dispose()
            {
                if (foregroundToTarget)
                {
                    AttachThreadInput(foregroundThread, targetThread, false);
                    foregroundToTarget = false;
                }

                if (currentToForeground)
                {
                    AttachThreadInput(currentThread, foregroundThread, false);
                    currentToForeground = false;
                }
            }
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private const uint GA_ROOT = 2;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsHungAppWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
    }
}
