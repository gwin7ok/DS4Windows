using System;
using System.Runtime.InteropServices;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// Win32 の WM_COPYDATA で渡す構造体（CLI の <c>-command</c> を、動いているインスタンスへ送る IPC）。送る側は <c>App</c>、受ける側は <c>MainWindow</c>。
    /// Phase14b-Step16: 旧 <c>App</c> のネスト型 <c>App.COPYDATASTRUCT</c>。UI から起点の <c>App</c> を参照しないため、トップレベルの型にした（配置・名前・レイアウトは同じ）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
}
