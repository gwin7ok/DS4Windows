using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2b-2: <see cref="WindowFocuser"/> の手順（探索→応答なし→復元→前面確認→素の前面化→入力の結び付け→Alt の擬似入力）を、
    /// 偽の <see cref="IWindowSystem"/> で確かめる。Win32 の実装（<see cref="Win32WindowSystem"/>）は、列挙が例外なく動くことだけを確かめる。
    /// </summary>
    public class Phase12Step2W2b2WindowFocuserTests
    {
        private static readonly IntPtr A = new IntPtr(100);
        private static readonly IntPtr B = new IntPtr(200);

        private class FakeWindowSystem : IWindowSystem
        {
            public List<WindowCandidate> Windows = new List<WindowCandidate>();
            public List<string> Calls = new List<string>();
            public HashSet<IntPtr> Minimized = new HashSet<IntPtr>();
            public HashSet<IntPtr> Hung = new HashSet<IntPtr>();
            public IntPtr Foreground = IntPtr.Zero;
            public bool ForegroundAfterRestore;

            /// <summary>この回数目の TrySetForeground で成功する（0＝成功しない）。呼び出しは通しで数える。</summary>
            public int SucceedOnAttempt;
            public int Attempts;
            public int AttachDisposed;
            public bool ThrowOnEnumerate;

            public IReadOnlyList<WindowCandidate> EnumerateWindows()
            {
                Calls.Add("Enumerate");
                if (ThrowOnEnumerate) throw new InvalidOperationException("boom");
                return Windows;
            }

            public CapturedWindow GetWindowUnderCursor() => null;

            public bool IsMinimized(IntPtr window) => Minimized.Contains(window);

            public void Restore(IntPtr window)
            {
                Calls.Add("Restore");
                Minimized.Remove(window);
                if (ForegroundAfterRestore) Foreground = window;
            }

            public bool IsHung(IntPtr window) => Hung.Contains(window);

            public bool IsForeground(IntPtr window) => Foreground == window;

            public bool TrySetForeground(IntPtr window)
            {
                Attempts++;
                Calls.Add("TrySetForeground");
                if (SucceedOnAttempt != 0 && Attempts >= SucceedOnAttempt)
                {
                    Foreground = window;
                    return true;
                }

                return false;
            }

            public IDisposable AttachInput(IntPtr window)
            {
                Calls.Add("Attach");
                return new Detach(this);
            }

            public void SendAltUp() => Calls.Add("AltUp");

            public void SendAltTap() => Calls.Add("AltTap");

            public void BringToTop(IntPtr window) => Calls.Add("BringToTop");

            private class Detach : IDisposable
            {
                private readonly FakeWindowSystem owner;
                public Detach(FakeWindowSystem owner) { this.owner = owner; }
                public void Dispose() { owner.AttachDisposed++; owner.Calls.Add("Detach"); }
            }
        }

        private static FakeWindowSystem Windows(params WindowCandidate[] windows)
        {
            var system = new FakeWindowSystem();
            system.Windows.AddRange(windows);
            return system;
        }

        private static WindowCandidate Game(IntPtr handle, string title = "test game window")
            => new WindowCandidate(handle, "c:\\games\\game.exe", title);

        private static WindowFocusResult Focus(FakeWindowSystem system, string path, string title)
            => new WindowFocuser(system).Focus(new WindowTargetMatcher(path, title));

        [Fact]
        public void Focus_EmptyTarget_ReturnsInvalidTarget_WithoutEnumerating()
        {
            var system = Windows(Game(A));

            Assert.Equal(WindowFocusResult.InvalidTarget, Focus(system, "", null));
            Assert.Empty(system.Calls);
        }

        [Fact]
        public void Focus_NoMatchingWindow_ReturnsNotFound()
        {
            var system = Windows(Game(A));

            Assert.Equal(WindowFocusResult.NotFound, Focus(system, "c:\\other\\app.exe", null));
            Assert.DoesNotContain("TrySetForeground", system.Calls);
        }

        [Fact]
        public void Focus_UsesSameMatchingRulesAsAutoProfile_PrefixSuffixContains()
        {
            Assert.Equal(WindowFocusResult.Focused, FocusWith("^c:\\games", null));
            Assert.Equal(WindowFocusResult.Focused, FocusWith("game.exe$", null));
            Assert.Equal(WindowFocusResult.Focused, FocusWith(null, "*game win"));
            Assert.Equal(WindowFocusResult.Focused, FocusWith("C:/Games/Game.EXE", "Test Game Window"));
            Assert.Equal(WindowFocusResult.NotFound, FocusWith("c:\\games\\game.exe", "other title"));

            WindowFocusResult FocusWith(string path, string title)
            {
                var system = Windows(Game(A));
                system.SucceedOnAttempt = 1;
                return Focus(system, path, title);
            }
        }

        [Fact]
        public void Focus_MultipleMatches_UsesFirstInZOrder()
        {
            var system = Windows(Game(A, "first"), Game(B, "second"));
            system.SucceedOnAttempt = 1;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.Equal(A, system.Foreground);
        }

        [Fact]
        public void Focus_HungWindow_ReturnsFailed_WithoutRestoreOrActivate()
        {
            var system = Windows(Game(A));
            system.Hung.Add(A);
            system.Minimized.Add(A);

            Assert.Equal(WindowFocusResult.Failed, Focus(system, "c:\\games\\game.exe", null));
            Assert.DoesNotContain("Restore", system.Calls);
            Assert.DoesNotContain("TrySetForeground", system.Calls);
        }

        [Fact]
        public void Focus_AlreadyForeground_ReturnsFocused_WithoutActivating()
        {
            var system = Windows(Game(A));
            system.Foreground = A;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.DoesNotContain("TrySetForeground", system.Calls);
        }

        [Fact]
        public void Focus_MinimizedWindow_IsRestoredBeforeActivation()
        {
            var system = Windows(Game(A));
            system.Minimized.Add(A);
            system.SucceedOnAttempt = 1;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.True(system.Calls.IndexOf("Restore") < system.Calls.IndexOf("TrySetForeground"));
        }

        [Fact]
        public void Focus_MinimizedWindowThatBecomesForegroundOnRestore_DoesNotNeedActivation()
        {
            var system = Windows(Game(A));
            system.Minimized.Add(A);
            system.ForegroundAfterRestore = true;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.DoesNotContain("TrySetForeground", system.Calls);
        }

        [Fact]
        public void Focus_PlainSetForegroundSucceeds_DoesNotAttachOrSendKeys()
        {
            var system = Windows(Game(A));
            system.SucceedOnAttempt = 1;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.DoesNotContain("Attach", system.Calls);
            Assert.DoesNotContain("AltUp", system.Calls);
            Assert.DoesNotContain("AltTap", system.Calls);
            Assert.Contains("BringToTop", system.Calls);
        }

        [Fact]
        public void Focus_SucceedsOnThirdAttempt_AttachesOnceSendsAltUpOnceAndDetaches()
        {
            var system = Windows(Game(A));
            system.SucceedOnAttempt = 3;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.Equal(1, system.Calls.Count(c => c == "Attach"));
            Assert.Equal(1, system.Calls.Count(c => c == "AltUp"));
            Assert.DoesNotContain("AltTap", system.Calls);
            Assert.Equal(3, system.Attempts);
            Assert.Equal(1, system.AttachDisposed);
            Assert.Contains("BringToTop", system.Calls);
        }

        [Fact]
        public void Focus_AllAttemptsFail_SendsAltTapTwice_ThenFails_AndDetaches()
        {
            var system = Windows(Game(A));
            system.SucceedOnAttempt = 0;

            Assert.Equal(WindowFocusResult.Failed, Focus(system, "c:\\games\\game.exe", null));
            // 素の 1 回＋結び付け後の 5 回＋Alt の後の 1 回
            Assert.Equal(7, system.Attempts);
            Assert.Equal(2, system.Calls.Count(c => c == "AltTap"));
            Assert.Equal(1, system.AttachDisposed);
            Assert.DoesNotContain("BringToTop", system.Calls);
        }

        [Fact]
        public void Focus_SucceedsOnlyAfterAltTap_ReturnsFocused()
        {
            var system = Windows(Game(A));
            system.SucceedOnAttempt = 7;

            Assert.Equal(WindowFocusResult.Focused, Focus(system, "c:\\games\\game.exe", null));
            Assert.Equal(2, system.Calls.Count(c => c == "AltTap"));
            Assert.Contains("BringToTop", system.Calls);
        }

        [Fact]
        public void Focus_WindowSystemThrows_ReturnsFailed()
        {
            var system = Windows(Game(A));
            system.ThrowOnEnumerate = true;

            Assert.Equal(WindowFocusResult.Failed, Focus(system, "c:\\games\\game.exe", null));
        }

        [Fact]
        public void Focus_NullTarget_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new WindowFocuser(Windows()).Focus(null));
        }

        [Fact]
        public void Win32WindowSystem_EnumerateWindows_DoesNotThrow_AndReturnsNormalizedValues()
        {
            var system = new Win32WindowSystem();

            IReadOnlyList<WindowCandidate> windows = system.EnumerateWindows();

            Assert.NotNull(windows);
            foreach (WindowCandidate w in windows)
            {
                Assert.Equal(w.Title.ToLower(), w.Title);
                Assert.Equal(w.ProcessPath.ToLower(), w.ProcessPath);
                Assert.DoesNotContain('/', w.ProcessPath);
            }
        }
    }
}
