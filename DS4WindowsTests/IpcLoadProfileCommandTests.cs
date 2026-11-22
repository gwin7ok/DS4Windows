#nullable enable
using System;
using System.IO;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// IPC コマンド <c>loadprofile</c>・<c>loadtempprofile</c>（<see cref="IpcCommandProcessor"/>）の振る舞い。
    /// 一時プロファイルも通常と同じ入口（<c>ApplyProfile</c>。束縛の差し替えを含む）で適用し、接続中のスロットだけを対象にする。
    /// プロファイルのファイルがなければ、何もしない。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class IpcLoadProfileCommandTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "IpcLoad_" + Guid.NewGuid().ToString("N"));

        public IpcLoadProfileCommandTests()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            File.WriteAllText(Path.Combine(_dir, "Profiles", "Game.xml"), "<DS4Windows />");
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch (IOException) { /* 一時フォルダーの後始末の失敗は無視してよい */ }
        }

        private sealed class Rig
        {
            public readonly RecordingProxy Application, Host;
            public readonly IProfileRepository Repository;
            public readonly IpcCommandProcessor Processor;
            public readonly IIpcCommandHost HostFake;

            public Rig(string dir, bool connected, int profileIndex)
            {
                var settings = TestRepositoryFactory.CreateProfileSettingsService();
                Repository = TestRepositoryFactory.CreateProfileRepository(settings);
                var application = RecordingProxy.Create<IProfileApplicationService>();
                Application = RecordingProxy.Of(application);
                Application.Returns["ApplyProfile"] = true;
                HostFake = RecordingProxy.Create<IIpcCommandHost>();
                Host = RecordingProxy.Of(HostFake);
                Host.Returns["FindProfileIndex"] = profileIndex;
                Host.Returns["GetProfileName"] = "Game";
                if (connected) Host.Returns["GetControllerDevice"] = RecordingProxy.Create<IControllerSlotView>();

                Processor = new IpcCommandProcessor(RecordingProxy.Create<IServiceLifecycle>(), RecordingProxy.Create<IOutputSlotService>(),
                    Repository, settings, application, RecordingProxy.Create<IProfileLoadTarget>(), new TempPathService(dir),
                    new DS4Windows.Processing.Services.Devices.DeviceSlotState(), RecordingProxy.Create<IVirtualPadService>(),
                    RecordingProxy.Create<IIpcResultWriter>());
            }

            public object?[]? LastApply => Application.ArgsOf("ApplyProfile").LastOrDefault();
        }

        [Fact]
        public void ATempProfile_GoesThroughTheNormalApplyEntry_AsATemporaryProfile()
        {
            var rig = new Rig(_dir, connected: true, profileIndex: 0);

            rig.Processor.Execute(rig.HostFake, new[] { "loadtempprofile", "1", "Game" });

            Assert.NotNull(rig.LastApply);
            Assert.Equal(0, rig.LastApply![0]);   // 番号は 1 始まり → スロット 0
            Assert.Equal("Game", rig.LastApply[1]);
            Assert.Equal(true, rig.LastApply[2]);   // 一時プロファイル
            Assert.Equal(true, rig.LastApply[3]);   // 起動するプログラムも起動する
        }

        [Fact]
        public void ANormalProfile_IsAppliedNow_WhenTheControllerIsConnected()
        {
            var rig = new Rig(_dir, connected: true, profileIndex: 0);

            rig.Processor.Execute(rig.HostFake, new[] { "loadprofile", "2", "Game" });

            Assert.Equal(1, rig.LastApply![0]);
            Assert.Equal(false, rig.LastApply[2]);
        }

        [Fact]
        public void ANormalProfile_OnAnEmptySlot_OnlyPresetsThePathForLater()
        {
            var rig = new Rig(_dir, connected: false, profileIndex: 0);
            string? before = rig.Repository.ProfilePath[1];
            try
            {
                rig.Processor.Execute(rig.HostFake, new[] { "loadprofile", "2", "Game" });

                Assert.Null(rig.LastApply);
                Assert.Equal("Game", rig.Repository.ProfilePath[1]);
            }
            finally
            {
                rig.Repository.ProfilePath[1] = before;
            }
        }

        [Fact]
        public void ATempProfile_OnAnEmptySlot_IsNotApplied()
        {
            var rig = new Rig(_dir, connected: false, profileIndex: 0);

            rig.Processor.Execute(rig.HostFake, new[] { "loadtempprofile", "1", "Game" });

            Assert.Null(rig.LastApply);
        }

        [Fact]
        public void AMissingProfileFile_DoesNothing()
        {
            var rig = new Rig(_dir, connected: true, profileIndex: 0);

            rig.Processor.Execute(rig.HostFake, new[] { "loadtempprofile", "1", "NoSuchProfile" });
            rig.Processor.Execute(rig.HostFake, new[] { "loadprofile", "1", "NoSuchProfile" });

            Assert.Null(rig.LastApply);
        }
    }
}
