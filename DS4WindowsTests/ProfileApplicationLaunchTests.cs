using System;
using System.Collections.Generic;
using System.IO;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using Xunit;
using DS4Windows.Processing.Services.Slots;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイルの適用のあとの、「起動するプログラム」の起動の呼び出し（<see cref="IProfileLoadTarget.LaunchProfileProgram"/>）。
    /// 起動の指定（<c>launchProgram</c>）が真で、適用が成功したときだけ、そのスロットで 1 回呼ぶ。読み込み自体は起動しない。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ProfileApplicationLaunchTests : IDisposable
    {
        private const int Slot = 0;
        private const string Profile = "LaunchTest_Profile";

        private sealed class RecordingTarget : IProfileLoadTarget
        {
            private readonly IProfileLoadTarget _inner;
            public readonly List<int> Launched = new List<int>();
            public RecordingTarget(IProfileLoadTarget inner) { _inner = inner; }
            public IConfigurableDevice GetController(int ind) => _inner.GetController(ind);
            public ITouchPadGyroToggles GetTouchPad(int ind) => _inner.GetTouchPad(ind);
            public bool HasOutputDevice(int ind) => _inner.HasOutputDevice(ind);
            public void PluginOutDev(int index, IConfigurableDevice device) => _inner.PluginOutDev(index, device);
            public void UnplugOutDev(int index, IConfigurableDevice device, bool immediate = false, bool force = false) => _inner.UnplugOutDev(index, device, immediate, force);
            public void CheckProfileOptions(int ind, IConfigurableDevice device, bool startUp = false) => _inner.CheckProfileOptions(ind, device, startUp);
            public void LaunchProfileProgram(int ind) => Launched.Add(ind);
            public void StartTPOff(int deviceID) => _inner.StartTPOff(deviceID);
            public void PreLoadReset(int ind) => _inner.PreLoadReset(ind);
        }

        private readonly string _dir;
        private readonly string _originalAppData;
        private readonly RecordingTarget _target;
        private readonly ProfileApplicationService _service;
        private readonly ProfileRepository _repo;
        private readonly string _origProfilePath;
        private readonly string _origOlder;
        private readonly string _origSelected;

        public ProfileApplicationLaunchTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "LaunchTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _originalAppData = TestGlobals.AppDataPath;
            TestGlobals.AppDataPath = _dir;

            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.InitializeRuntime();
            _target = new RecordingTarget(TestHost.GetService<IProfileLoadTarget>());
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            _repo = TestRepositoryFactory.CreateProfileRepository(settings);
            _service = TestRepositoryFactory.CreateProfileApplicationService(settings, _target, _repo, new EnvironmentService());

            _origProfilePath = _repo.ProfilePath[Slot];
            _origOlder = _repo.OlderProfilePath[Slot];
            _origSelected = _repo.SelectedProfile[Slot];
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml().SaveProfile(Slot, Profile));
        }

        public void Dispose()
        {
            _repo.ProfilePath[Slot] = _origProfilePath;
            _repo.OlderProfilePath[Slot] = _origOlder;
            _repo.SelectedProfile[Slot] = _origSelected;
            TestGlobals.ProfileSettingsServiceInstance.SetUseTempProfile(Slot, false);
            TestGlobals.ProfileSettingsServiceInstance.SetTempProfileName(Slot, string.Empty);
            TestGlobals.AppDataPath = _originalAppData;
            try { Directory.Delete(_dir, true); } catch { }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void TheProgram_IsLaunchedOnce_OnlyWhenRequested(bool isTemp, bool launchProgram)
        {
            Assert.True(_service.ApplyProfileWithoutHalt(Slot, Profile, isTemp, launchProgram, ProfileChangeSource.Manual, "prolog"));
            Assert.Equal(launchProgram ? new[] { Slot } : Array.Empty<int>(), _target.Launched.ToArray());
        }

        [Fact]
        public void AFailedApply_DoesNotLaunch()
        {
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, "LaunchTest_DoesNotExist", false, true, ProfileChangeSource.Manual, "prolog"));
            Assert.Empty(_target.Launched);
        }
    }
}
