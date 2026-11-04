using System;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.UI.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase12-Step2 2i（R-4）: ターゲットマークをドロップした位置のウィンドウから、対象の指定（実行ファイルのフルパスとタイトル全文）を取り出す。
    /// DS4Windows 自身のウィンドウは対象にしない（自分自身を前面に出す割り当てを作らないため）。マウス操作は画面の部品（<c>WindowTargetEditor</c>）が受け持つ。
    /// </summary>
    public sealed class WindowTargetPicker
    {
        private readonly IWindowSystem windowSystem;
        private readonly int ownProcessId;

        public WindowTargetPicker(IWindowSystem windowSystem, int ownProcessId)
        {
            this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            this.ownProcessId = ownProcessId;
        }

        /// <summary>今のマウスカーソルの位置のウィンドウから、パスとタイトルを取る。取れない（ウィンドウなし、自分自身、パスもタイトルも空）ときは false。</summary>
        public bool TryPickUnderCursor(out string path, out string title)
        {
            path = string.Empty;
            title = string.Empty;

            CapturedWindow window = windowSystem.GetWindowUnderCursor();
            if (window == null || window.ProcessId == ownProcessId) return false;
            if (string.IsNullOrEmpty(window.ProcessPath) && string.IsNullOrEmpty(window.Title)) return false;

            path = window.ProcessPath;
            title = window.Title;
            return true;
        }
    }
}
