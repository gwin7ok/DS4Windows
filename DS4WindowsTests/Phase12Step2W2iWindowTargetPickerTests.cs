using System;
using System.Collections.Generic;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2i（R-4）: ターゲットマークのドロップで、カーソル位置のウィンドウから対象（フルパスとタイトル全文）を取り出す処理。
    /// マウス操作と Win32 の取得そのものは、実機で確認する。
    /// </summary>
    public class Phase12Step2W2iWindowTargetPickerTests
    {
        private const int OwnProcessId = 4242;

        private sealed class FakeWindowSystem : IWindowSystem
        {
            public CapturedWindow Under;

            public CapturedWindow GetWindowUnderCursor() => Under;

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
            public bool MoveCursorTo(int x, int y) => throw new NotSupportedException();
            public bool TryGetForegroundWindowOrigin(out int x, out int y) => throw new NotSupportedException();
        }

        private static WindowTargetPicker NewPicker(CapturedWindow under)
            => new WindowTargetPicker(new FakeWindowSystem { Under = under }, OwnProcessId);

        [Fact]
        public void TryPickUnderCursor_ReturnsTheFullPathAndTheFullTitle_WithTheOriginalCase()
        {
            WindowTargetPicker picker = NewPicker(new CapturedWindow(new IntPtr(1), 100, "C:\\Windows\\System32\\Notepad.exe", "Untitled - Notepad"));

            Assert.True(picker.TryPickUnderCursor(out string path, out string title));
            Assert.Equal("C:\\Windows\\System32\\Notepad.exe", path);
            Assert.Equal("Untitled - Notepad", title);
        }

        [Fact]
        public void TryPickUnderCursor_WithNoWindow_ReturnsFalse_AndEmptyValues()
        {
            WindowTargetPicker picker = NewPicker(null);

            Assert.False(picker.TryPickUnderCursor(out string path, out string title));
            Assert.Equal(string.Empty, path);
            Assert.Equal(string.Empty, title);
        }

        [Fact]
        public void TryPickUnderCursor_OnOurOwnWindow_ReturnsFalse()
        {
            WindowTargetPicker picker = NewPicker(new CapturedWindow(new IntPtr(1), OwnProcessId, "C:\\app\\DS4Windows.exe", "DS4Windows"));

            Assert.False(picker.TryPickUnderCursor(out string path, out string title));
            Assert.Equal(string.Empty, path);
            Assert.Equal(string.Empty, title);
        }

        [Fact]
        public void TryPickUnderCursor_WithATitleButNoPath_ReturnsTheTitleOnly()
        {
            WindowTargetPicker picker = NewPicker(new CapturedWindow(new IntPtr(1), 100, null, "Protected Window"));

            Assert.True(picker.TryPickUnderCursor(out string path, out string title));
            Assert.Equal(string.Empty, path);
            Assert.Equal("Protected Window", title);
        }

        [Fact]
        public void TryPickUnderCursor_WithAPathButNoTitle_ReturnsThePathOnly()
        {
            WindowTargetPicker picker = NewPicker(new CapturedWindow(new IntPtr(1), 100, "C:\\x\\a.exe", null));

            Assert.True(picker.TryPickUnderCursor(out string path, out string title));
            Assert.Equal("C:\\x\\a.exe", path);
            Assert.Equal(string.Empty, title);
        }

        [Fact]
        public void TryPickUnderCursor_WithNeitherPathNorTitle_ReturnsFalse()
        {
            WindowTargetPicker picker = NewPicker(new CapturedWindow(new IntPtr(1), 100, null, null));

            Assert.False(picker.TryPickUnderCursor(out _, out _));
        }

        [Fact]
        public void TheConstructor_RejectsANullWindowSystem()
        {
            Assert.Throws<ArgumentNullException>(() => new WindowTargetPicker(null, OwnProcessId));
        }

        [Fact]
        public void Win32WindowSystem_GetWindowUnderCursor_DoesNotThrow()
        {
            // 実機の画面に依存するため、結果（null か、取れたウィンドウ）は問わず、呼び出しが失敗しないことだけを確かめる
            var exception = Record.Exception(() => new Win32WindowSystem().GetWindowUnderCursor());

            Assert.Null(exception);
        }
    }
}
