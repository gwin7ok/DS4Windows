using System;
using System.Collections.Generic;
using DS4Windows.Composition.Startup;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// アプリの終了の手順（<see cref="AppShutdownSequence"/>）の順序。偽物が呼ばれた順を記録して固定する
    /// （ドライバーの破棄の順序のガードレール: コントローラーの停止 → レジストリの破棄 → ホストの破棄 → ログのフラッシュ）。
    /// </summary>
    public class AppShutdownSequenceTests
    {
        private sealed class Recorder
        {
            public readonly List<string> Calls = new List<string>();
            public void Add(string call) { lock (Calls) Calls.Add(call); }
        }

        private sealed class FakeBootstrapper : IRuntimeBootstrapper
        {
            private readonly Recorder _r;
            public FakeBootstrapper(Recorder r) { _r = r; }
            public void Initialize() { }
            public void Shutdown() => _r.Add("bootstrapper.Shutdown");
        }

        private sealed class FakeLifecycle : IServiceLifecycle
        {
            private readonly Recorder _r;
            public FakeLifecycle(Recorder r, bool running) { _r = r; Running = running; }
            public bool Running { get; set; }
            public bool LoopControllers { get; set; }
            public bool InServiceTask { get; set; }
            public int ActiveControllers { get; set; }
            public bool Suspending { get; set; }
            public event EventHandler ServiceStarted { add { } remove { } }
            public event EventHandler PreServiceStop { add { } remove { } }
            public event EventHandler ServiceStopped { add { } remove { } }
            public event EventHandler RunningChanged { add { } remove { } }
            public event Action<InputDeviceCore, int> HotplugController { add { } remove { } }
            public bool Start(bool showlog = true) => true;
            public bool Stop(bool showlog = true, bool immediateUnplug = false)
            {
                _r.Add("lifecycle.Stop(immediateUnplug=" + immediateUnplug + ")");
                return true;
            }
            public bool HotPlug() => true;
            public void PrepareAbort() { }
        }

        private sealed class FakeRegistry : IControllerRegistry
        {
            private readonly Recorder _r;
            public bool ThrowOnDevice1 { get; set; }
            public FakeRegistry(Recorder r) { _r = r; }
            public void Register(string key, IDisposable controller) { }
            public void Unregister(string key) { }
            public IReadOnlyList<IDisposable> GetControllersForDevice(int device) => Array.Empty<IDisposable>();
            public void ClearControllersForDevice(int device)
            {
                _r.Add("registry.Clear(" + device + ")");
                if (ThrowOnDevice1 && device == 1) throw new InvalidOperationException("boom");
            }
        }

        private sealed class FakeDisposable : IDisposable
        {
            private readonly Recorder _r;
            public FakeDisposable(Recorder r) { _r = r; }
            public void Dispose() => _r.Add("ipc.Dispose");
        }

        private static AppShutdownSequence Create(Recorder r, bool running = true, bool save = true, bool throwOnDevice1 = false,
            Action disposeHost = null, bool withHost = true)
        {
            return new AppShutdownSequence(
                withHost ? new FakeBootstrapper(r) : null, withHost ? new FakeLifecycle(r, running) : null,
                save ? (Action)(() => r.Add("settings.Save")) : null,
                withHost ? new FakeDisposable(r) : null, withHost ? new FakeRegistry(r) { ThrowOnDevice1 = throwOnDevice1 } : null, 3,
                resetTimerResolution: () => r.Add("timer.Reset"),
                stopSingleInstance: () => r.Add("singleInstance.Stop"),
                disposeHost: disposeHost ?? (() => r.Add("host.Dispose")),
                flushLogs: () => r.Add("log.Flush"));
        }

        [Fact]
        public void TheSteps_RunInTheFixedOrder()
        {
            var r = new Recorder();
            Create(r).Run();

            Assert.Equal(new[]
            {
                "lifecycle.Stop(immediateUnplug=True)", "bootstrapper.Shutdown",
                "settings.Save", "timer.Reset", "singleInstance.Stop", "ipc.Dispose",
                "registry.Clear(0)", "registry.Clear(1)", "registry.Clear(2)",
                "host.Dispose", "log.Flush",
            }, r.Calls);
        }

        [Fact]
        public void WhenTheServiceIsNotRunning_TheControllersAreNotStopped_ButTheRestRuns()
        {
            var r = new Recorder();
            Create(r, running: false).Run();

            Assert.DoesNotContain("bootstrapper.Shutdown", r.Calls);
            Assert.DoesNotContain(r.Calls, c => c.StartsWith("lifecycle."));
            Assert.Equal("settings.Save", r.Calls[0]);
            Assert.Equal("log.Flush", r.Calls[r.Calls.Count - 1]);
        }

        [Fact]
        public void WhenSavingIsNotAllowed_TheSettingsAreNotSaved()
        {
            var r = new Recorder();
            Create(r, save: false).Run();

            Assert.DoesNotContain("settings.Save", r.Calls);
        }

        [Fact]
        public void AFailureWhileClearingOneDevice_DoesNotStopTheOthersOrTheHostDispose()
        {
            var r = new Recorder();
            Create(r, throwOnDevice1: true).Run();

            Assert.Contains("registry.Clear(2)", r.Calls);
            Assert.True(r.Calls.IndexOf("host.Dispose") > r.Calls.IndexOf("registry.Clear(2)"));
            Assert.Equal("log.Flush", r.Calls[r.Calls.Count - 1]);
        }

        [Fact]
        public void AFailureOfTheHostDispose_StillFlushesTheLogs()
        {
            var r = new Recorder();
            Create(r, disposeHost: () => throw new InvalidOperationException("boom")).Run();

            Assert.Equal("log.Flush", r.Calls[r.Calls.Count - 1]);
        }

        [Fact]
        public void BeforeTheHostIsBuilt_OnlyTheTimerTheSingleInstanceTheHostAndTheLogAreHandled()
        {
            var r = new Recorder();
            Create(r, save: false, withHost: false).Run();

            Assert.Equal(new[] { "timer.Reset", "singleInstance.Stop", "host.Dispose", "log.Flush" }, r.Calls);
        }
    }
}
