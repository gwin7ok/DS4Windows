using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// Phase12-Step3 3e（R-3）: <see cref="IMouseInputHook"/> の実装（Win32 の <c>WH_MOUSE_LL</c>）。
    /// フックは、<see cref="Start"/> を呼んだスレッドのメッセージループで呼ばれるため、イベントもそのスレッド（UI スレッド）で起こる。
    /// DS4Windows 自身が送るマウス入力（タッチパッドのマウス操作など）も、そのまま通知する（記録の対象にするかは、呼び出し側が決める）。
    /// ログは Log-Policy.md に従う（旧 API。汎用 Log への集約は Phase13b）。
    /// </summary>
    public sealed class LowLevelMouseHook : IMouseInputHook, IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private readonly HookProc _proc;
        private IntPtr _hook = IntPtr.Zero;

        public event Action<int, int> PointerMoved;
        public event Action<int, bool, int, int> ButtonChanged;

        public LowLevelMouseHook()
        {
            // ガベージコレクションで回収されないよう、フィールドで持つ
            _proc = HookCallback;
        }

        public bool IsRunning => _hook != IntPtr.Zero;

        public bool Start()
        {
            if (_hook != IntPtr.Zero) return true;

            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
            {
                _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(module?.ModuleName), 0);
            }

            if (_hook == IntPtr.Zero)
            {
                AppLogger.LogWarn($"[MouseHook] Failed to start: error={Marshal.GetLastWin32Error()}");
                return false;
            }

            if (AppLogger.IsDebugEnabled) AppLogger.LogDebug("[MouseHook] Started");
            return true;
        }

        public void Stop()
        {
            if (_hook == IntPtr.Zero) return;

            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
            if (AppLogger.IsDebugEnabled) AppLogger.LogDebug("[MouseHook] Stopped");
        }

        public void Dispose() => Stop();

        /// <summary>
        /// フックのメッセージを、マクロのボタンの値と押下・離上へ変換する。ボタン以外（移動など）は false。
        /// <paramref name="mouseData"/> の上位 16 ビットは、X ボタンの番号（1＝4 番目、2＝5 番目）。
        /// </summary>
        public static bool TryTranslateButton(int message, uint mouseData, out int macroValue, out bool isDown)
        {
            macroValue = 0;
            isDown = false;
            switch (message)
            {
                case WM_LBUTTONDOWN: macroValue = 256; isDown = true; return true;
                case WM_LBUTTONUP: macroValue = 256; return true;
                case WM_RBUTTONDOWN: macroValue = 257; isDown = true; return true;
                case WM_RBUTTONUP: macroValue = 257; return true;
                case WM_MBUTTONDOWN: macroValue = 258; isDown = true; return true;
                case WM_MBUTTONUP: macroValue = 258; return true;
                case WM_XBUTTONDOWN:
                case WM_XBUTTONUP:
                    uint button = mouseData >> 16;
                    if (button != 1 && button != 2) return false;
                    macroValue = 258 + (int)button;
                    isDown = message == WM_XBUTTONDOWN;
                    return true;
                default:
                    return false;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    int message = wParam.ToInt32();
                    MSLLHOOKSTRUCT data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    if (message == WM_MOUSEMOVE)
                    {
                        PointerMoved?.Invoke(data.pt.X, data.pt.Y);
                    }
                    else if (TryTranslateButton(message, data.mouseData, out int macroValue, out bool isDown))
                    {
                        ButtonChanged?.Invoke(macroValue, isDown, data.pt.X, data.pt.Y);
                    }
                }
                catch (Exception ex)
                {
                    // ハンドラーの例外で、Windows のマウス操作を止めない
                    AppLogger.LogWarn($"[MouseHook] Handler error: {ex.GetType().Name}: {ex.Message}");
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
