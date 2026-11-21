#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 外部コンポーネントの導入の流れ（ダウンロード → 起動 → 終了の監視 → 導入の確認）を、
    /// 偽の HTTP・偽の起動・偽の時計で固定する。実際にドライバーは入れない。
    /// </summary>
    public class ExternalComponentInstallerTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ExtInstTests_" + Guid.NewGuid().ToString("N"));

        public ExternalComponentInstallerTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* 一時フォルダーの後始末。残っても害はない */ }
        }

        private sealed class FakeHandler(HttpStatusCode status) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) });
        }

        private sealed class FakeLauncher : IInstallerProcessLauncher
        {
            public string? StartedPath;
            public bool StartFails;
            public int ExitAfterChecks = 1;
            public int Checks;

            public Func<bool>? Start(string filePath)
            {
                if (StartFails) return null;
                StartedPath = filePath;
                return () => ++Checks >= ExitAfterChecks;
            }
        }

        private sealed class Collector : IProgress<InstallProgress>
        {
            public readonly List<InstallProgress> Items = new List<InstallProgress>();
            public void Report(InstallProgress value) { lock (Items) Items.Add(value); }
        }

        private ExternalComponentInstaller Create(HttpStatusCode status, FakeLauncher launcher, TimeProvider time) =>
            new ExternalComponentInstaller(new HttpClient(new FakeHandler(status)), launcher, time, _dir);

        private static List<InstallStage> Stages(Collector c)
        {
            lock (c.Items)
                return c.Items.Where(i => i.Stage != InstallStage.Downloading).Select(i => i.Stage).ToList();
        }

        private static ExternalComponentSpec Spec(Func<bool> isInstalled) =>
            new ExternalComponentSpec("setup.exe", "http://example.invalid/setup.exe", isInstalled);

        [Fact]
        public async Task Success_StartsTheDownloadedFile_WaitsForExit_ChecksInstalled_AndDeletesIt()
        {
            var launcher = new FakeLauncher { ExitAfterChecks = 3 };
            var time = new ManualTimeProvider();
            var progress = new Collector();
            int installedChecks = 0;

            Task run = Create(HttpStatusCode.OK, launcher, time).InstallAsync(Spec(() => { installedChecks++; return true; }), progress, CancellationToken.None);
            while (!run.IsCompleted)
            {
                time.Advance(ExternalComponentInstaller.PollInterval);
                await Task.Delay(5);
            }
            await run;

            Assert.Equal(Path.Combine(_dir, "setup.exe"), launcher.StartedPath);
            Assert.Equal(new[] { InstallStage.Installing, InstallStage.Completed }, Stages(progress));
            Assert.Equal(1, installedChecks);
            Assert.False(File.Exists(Path.Combine(_dir, "setup.exe")));
            Assert.False(File.Exists(Path.Combine(_dir, "setup.exe.tmp")));
        }

        [Fact]
        public async Task InstallerExitedButNotInstalled_ReportsFailed()
        {
            var progress = new Collector();

            await Create(HttpStatusCode.OK, new FakeLauncher(), TimeProvider.System).InstallAsync(Spec(() => false), progress, CancellationToken.None);

            Assert.Equal(new[] { InstallStage.Installing, InstallStage.Failed }, Stages(progress));
        }

        [Fact]
        public async Task DownloadFailure_ReportsFailed_AndDoesNotStartAnything()
        {
            var launcher = new FakeLauncher();
            var progress = new Collector();

            await Create(HttpStatusCode.NotFound, launcher, TimeProvider.System).InstallAsync(Spec(() => true), progress, CancellationToken.None);

            Assert.Null(launcher.StartedPath);
            Assert.Equal(new[] { InstallStage.Failed }, Stages(progress));
        }

        [Fact]
        public async Task LauncherFailure_ReportsFailed()
        {
            var progress = new Collector();

            await Create(HttpStatusCode.OK, new FakeLauncher { StartFails = true }, TimeProvider.System).InstallAsync(Spec(() => true), progress, CancellationToken.None);

            Assert.Equal(new[] { InstallStage.Failed }, Stages(progress));
        }

        [Fact]
        public async Task StaleFilesFromAPreviousRun_AreRemovedBeforeDownloading()
        {
            File.WriteAllText(Path.Combine(_dir, "setup.exe"), "old");
            File.WriteAllText(Path.Combine(_dir, "setup.exe.tmp"), "old");
            var progress = new Collector();

            await Create(HttpStatusCode.OK, new FakeLauncher(), TimeProvider.System).InstallAsync(Spec(() => true), progress, CancellationToken.None);

            Assert.Equal(new[] { InstallStage.Installing, InstallStage.Completed }, Stages(progress));
        }

        [Fact]
        public async Task Cancelled_WhileWaiting_ReportsNothingMore()
        {
            var launcher = new FakeLauncher { ExitAfterChecks = int.MaxValue };
            var progress = new Collector();
            using var cts = new CancellationTokenSource();

            Task run = Create(HttpStatusCode.OK, launcher, TimeProvider.System).InstallAsync(Spec(() => true), progress, cts.Token);
            await Task.Delay(300);
            cts.Cancel();
            await run;

            Assert.Equal(new[] { InstallStage.Installing }, Stages(progress));
        }
    }
}
