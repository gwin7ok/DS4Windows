using System;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;
using Xunit;
using DS4Windows.Processing.Services.Slots;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイルに結んだ「起動するプログラム」の起動（<see cref="ProfileProgramLauncher"/>）。
    /// 設定がないとき・すでに動いているときは起動せず、動いていなければ、5 秒後に 1 回だけ起動する。待ちは仮想時間で進める。
    /// </summary>
    public class ProfileProgramLauncherTests
    {
        private const string Program = @"C:\Tools\Overlay\overlay.exe";

        private sealed class FakeInspector : IProcessInspector
        {
            public bool Running;
            public int Checks;
            public bool IsProcessRunning(string exePath) { Checks++; return Running; }
            public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
            {
                processPath = string.Empty;
                windowTitle = string.Empty;
                return false;
            }
        }

        private sealed class RecordingLauncher : IProcessLauncher
        {
            public readonly System.Collections.Generic.List<string> Started = new System.Collections.Generic.List<string>();
            public readonly ManualResetEventSlim Signal = new ManualResetEventSlim();
            public bool Throw;
            public void Launch(string filePath) { }
            public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden) { }
            public void LaunchWithWorkingDirectory(string filePath)
            {
                lock (Started) { Started.Add(filePath); }
                Signal.Set();
                if (Throw) throw new InvalidOperationException("fail");
            }
        }

        private static (ProfileProgramLauncher Launcher, FakeInspector Inspector, RecordingLauncher Process, ManualTimeProvider Time) Create(string program)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            settings.BasicSlot(0).LaunchProgram = program;
            var inspector = new FakeInspector();
            var process = new RecordingLauncher();
            var time = new ManualTimeProvider();
            return (new ProfileProgramLauncher(settings, inspector, process, time), inspector, process, time);
        }

        // 待ちのタイマーが作られる（Task.Run の中）まで、少し待つ
        private static void WaitForTimer(ManualTimeProvider time)
        {
            for (int i = 0; i < 200 && time.ActiveTimerCount == 0; i++) Thread.Sleep(10);
        }

        [Fact]
        public void NoProgram_DoesNothing_AndDoesNotEvenLookAtTheProcesses()
        {
            var t = Create(string.Empty);
            t.Launcher.LaunchIfNotRunning(0);
            Assert.Equal(0, t.Inspector.Checks);
            Assert.Equal(0, t.Time.ActiveTimerCount);
            Assert.Empty(t.Process.Started);
        }

        [Fact]
        public void OutOfRangeSlot_DoesNothing()
        {
            var t = Create(Program);
            t.Launcher.LaunchIfNotRunning(-1);
            t.Launcher.LaunchIfNotRunning(10_000);
            Assert.Equal(0, t.Inspector.Checks);
        }

        [Fact]
        public void AlreadyRunning_DoesNotLaunch()
        {
            var t = Create(Program);
            t.Inspector.Running = true;
            t.Launcher.LaunchIfNotRunning(0);
            Thread.Sleep(50);
            Assert.Equal(1, t.Inspector.Checks);
            Assert.Equal(0, t.Time.ActiveTimerCount);
            Assert.Empty(t.Process.Started);
        }

        [Fact]
        public void NotRunning_LaunchesOnce_AfterTheFiveSecondDelay_NotBefore()
        {
            var t = Create(Program);
            t.Launcher.LaunchIfNotRunning(0);
            WaitForTimer(t.Time);

            t.Time.Advance(TimeSpan.FromSeconds(4.9));
            Thread.Sleep(50);
            Assert.Empty(t.Process.Started);

            t.Time.Advance(TimeSpan.FromSeconds(0.2));
            Assert.True(t.Process.Signal.Wait(5000));
            Assert.Equal(new[] { Program }, t.Process.Started.ToArray());
        }

        [Fact]
        public void ALaunchFailure_IsSwallowed()
        {
            var t = Create(Program);
            t.Process.Throw = true;
            t.Launcher.LaunchIfNotRunning(0);
            WaitForTimer(t.Time);
            t.Time.Advance(ProfileProgramLauncher.LaunchDelay);
            Assert.True(t.Process.Signal.Wait(5000));
            Thread.Sleep(50);   // 例外が漏れて、テストプロセスを落とさないこと
            Assert.Single(t.Process.Started);
        }

        [Fact]
        public void Constructor_RequiresAllDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            var inspector = new FakeInspector();
            var process = new RecordingLauncher();
            var time = new ManualTimeProvider();
            Assert.Throws<ArgumentNullException>(() => new ProfileProgramLauncher(null, inspector, process, time));
            Assert.Throws<ArgumentNullException>(() => new ProfileProgramLauncher(settings, null, process, time));
            Assert.Throws<ArgumentNullException>(() => new ProfileProgramLauncher(settings, inspector, null, time));
            Assert.Throws<ArgumentNullException>(() => new ProfileProgramLauncher(settings, inspector, process, null));
        }
    }
}
