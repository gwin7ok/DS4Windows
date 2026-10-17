using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step3c W1: <c>Global.ApplyProfile</c>／<c>Global.ApplyProfileToSlot</c>／<c>Global.LoadTempProfile</c>／
    /// プリセット（<c>Global.LoadBlankDevProfile</c>）の、現行の結果を固定する特性化テスト。
    /// W3〜W4 でこれらの処理を <c>ProfileApplicationService</c>／<c>ProfilePresetLoader</c> へ移した後も、同じ結果になることを確かめる基準にする。
    /// 固定するもの: 適用後の状態（<c>ProfilePath</c>／<c>SelectedProfile</c>／<c>OlderProfilePath</c>）、一時プロファイルの状態（名前・使用・距離）、
    /// <c>SelectedProfileChanged</c> の発火（回数と引数）、失敗時に何も変えないこと、スロット・名前の検査。
    /// 静的な状態（<c>Global.appdatapath</c>、スロット 0 の状態、<c>Program.rootHub</c>）は退避して復元する。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step3cApplyProfileCharacterizationTests : IDisposable
    {
        private const int Slot = 0;
        private const string Normal = "Phase7Step3c_Normal";
        private const string Temp = "Phase7Step3c_Temp";
        private const string Distance = "Phase7Step3c_distance_Temp";
        private const string Missing = "Phase7Step3c_Missing";

        private readonly string _dir;
        private readonly string _originalAppData;
        private readonly ControlService _control;
        private readonly ControlService _originalRootHub;
        private readonly IProfileSettingsService _settings;
        private readonly ProfileRepository _repo;
        private readonly ProfileApplicationService _service;
        private readonly string _origProfilePath;
        private readonly string _origOlder;
        private readonly string _origSelected;
        private readonly bool _origUseTemp;
        private readonly string _origTempName;
        private readonly bool _origDistance;
        private readonly List<SelectedProfileChangedEventArgs> _events = new List<SelectedProfileChangedEventArgs>();
        private readonly EventHandler<SelectedProfileChangedEventArgs> _handler;

        public Phase7Step3cApplyProfileCharacterizationTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase7Step3c_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _originalAppData = Global.appdatapath;
            Global.appdatapath = _dir;

            DS4WinWPF.AppHost.CreateHost();
            _control = DS4WinWPF.AppHost.GetService<ControlService>();
            _originalRootHub = Program.rootHub;
            Program.rootHub = _control;
            _settings = Global.ProfileSettingsServiceInstance;
            // Phase7-Step3c W3: 移管先（ProfileApplicationService／ProfileRepository）。旧 Global と同じ状態を共有する
            _repo = TestRepositoryFactory.CreateProfileRepository(_settings);
            _service = TestRepositoryFactory.CreateProfileApplicationService(_settings,
                TestRepositoryFactory.CreateDeviceStateService(), () => _control, _repo, new EnvironmentService());

            _origProfilePath = Global.ProfilePath[Slot];
            _origOlder = Global.OlderProfilePath[Slot];
            _origSelected = Global.SelectedProfile[Slot];
            _origUseTemp = _settings.GetUseTempProfile(Slot);
            _origTempName = _settings.GetTempProfileName(Slot);
            _origDistance = Global.tempprofileDistance[Slot];

            // 読み込める有効なプロファイルを、保存の経路で作る（保存先は退避した appdatapath 配下）
            Assert.True(Global.store.SaveProfile(Slot, Normal));
            Assert.True(Global.store.SaveProfile(Slot, Temp));
            Assert.True(Global.store.SaveProfile(Slot, Distance));

            _handler = (s, e) => _events.Add(e);
            Global.SelectedProfileChanged += _handler;
        }

        public void Dispose()
        {
            Global.SelectedProfileChanged -= _handler;
            Global.ProfilePath[Slot] = _origProfilePath;
            Global.OlderProfilePath[Slot] = _origOlder;
            Global.SelectedProfile[Slot] = _origSelected;
            _settings.SetUseTempProfile(Slot, _origUseTemp);
            _settings.SetTempProfileName(Slot, _origTempName);
            Global.tempprofileDistance[Slot] = _origDistance;
            Program.rootHub = _originalRootHub;
            Global.appdatapath = _originalAppData;
            try { Directory.Delete(_dir, true); } catch { }
        }

        // viaService=false: 旧 Global.ApplyProfile／LoadTempProfile、true: 移管先（Halt なしの入口）。両方が同じ結果を示すこと
        private bool Apply(bool viaService, int slot, string name, bool isTemp, ProfileChangeSource source)
            => viaService
                ? _service.ApplyProfileWithoutHalt(slot, name, isTemp, false, source, "prolog")
                : Global.ApplyProfile(slot, name, isTemp, false, _control, source, "prolog");

        private bool LoadTemp(bool viaService, string name)
            => viaService
                ? _repo.LoadTempProfile(Slot, name, false, _control)
                : Global.LoadTempProfile(Slot, name, false, _control);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_Normal_UpdatesSelectionAndRaisesEventOnce(bool viaService)
        {
            Global.OlderProfilePath[Slot] = "OlderSentinel";
            Global.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = Apply(viaService, Slot, Normal, false, ProfileChangeSource.Manual);

            Assert.True(result);
            Assert.Equal(Normal, Global.ProfilePath[Slot]);
            Assert.Equal(Normal, Global.SelectedProfile[Slot]);
            Assert.Equal(Normal, Global.OlderProfilePath[Slot]);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);

            Assert.Single(_events);
            Assert.Equal(Slot, _events[0].DeviceIndex);
            Assert.Equal(Normal, _events[0].ProfileName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_Temp_KeepsOlderProfilePath_AndMarksTempState(bool viaService)
        {
            Global.OlderProfilePath[Slot] = "OlderSentinel";
            Global.ProfilePath[Slot] = "ProfilePathSentinel";

            bool result = Apply(viaService, Slot, Temp, true, ProfileChangeSource.MappingAction);

            Assert.True(result);
            // 一時プロファイルでは、復帰先の OlderProfilePath と通常の ProfilePath は更新されない
            Assert.Equal("OlderSentinel", Global.OlderProfilePath[Slot]);
            Assert.Equal("ProfilePathSentinel", Global.ProfilePath[Slot]);
            Assert.Equal(Temp, Global.SelectedProfile[Slot]);
            Assert.True(_settings.GetUseTempProfile(Slot));
            Assert.Equal(Temp, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);

            Assert.Single(_events);
            Assert.Equal(Temp, _events[0].ProfileName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_TempWithDistanceInName_SetsDistanceFlag(bool viaService)
        {
            bool result = Apply(viaService, Slot, Distance, true, ProfileChangeSource.MappingAction);

            Assert.True(result);
            Assert.True(Global.tempprofileDistance[Slot]);

            // 通常プロファイルを適用すると、一時プロファイルの状態と距離フラグが戻る
            Assert.True(Apply(viaService, Slot, Normal, false, ProfileChangeSource.Manual));
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_MissingProfile_ReturnsFalse_AndKeepsSelectionAndRaisesNothing(bool viaService)
        {
            Global.SelectedProfile[Slot] = "SelectedSentinel";
            Global.OlderProfilePath[Slot] = "OlderSentinel";

            bool result = Apply(viaService, Slot, Missing, false, ProfileChangeSource.Manual);

            Assert.False(result);
            Assert.Equal("SelectedSentinel", Global.SelectedProfile[Slot]);
            Assert.Equal("OlderSentinel", Global.OlderProfilePath[Slot]);
            Assert.Empty(_events);
            // 通常プロファイルでは、読み込みの前に ProfilePath が更新される（失敗しても戻さない。従来の動作）
            Assert.Equal(Missing, Global.ProfilePath[Slot]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_MissingTempProfile_ReturnsFalse_AndKeepsTempState(bool viaService)
        {
            _settings.SetUseTempProfile(Slot, false);
            _settings.SetTempProfileName(Slot, string.Empty);
            Global.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = Apply(viaService, Slot, Missing, true, ProfileChangeSource.MappingAction);

            Assert.False(result);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.Equal("SelectedSentinel", Global.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadTempProfile_Success_SetsTempState_AndDoesNotTouchSelection(bool viaService)
        {
            Global.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = LoadTemp(viaService, Temp);

            Assert.True(result);
            Assert.True(_settings.GetUseTempProfile(Slot));
            Assert.Equal(Temp, _settings.GetTempProfileName(Slot));
            Assert.Equal("SelectedSentinel", Global.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ApplyProfileToSlot_RejectsInvalidSlotOrName_AndDoesNotChangeState()
        {
            Global.SelectedProfile[Slot] = "SelectedSentinel";

            Assert.False(Global.ApplyProfileToSlot(-1, Normal, ProfileChangeSource.Manual));
            Assert.False(Global.ApplyProfileToSlot(ControlService.CURRENT_DS4_CONTROLLER_LIMIT, Normal, ProfileChangeSource.Manual));
            Assert.False(Global.ApplyProfileToSlot(Slot, "", ProfileChangeSource.Manual));
            Assert.False(Global.ApplyProfileToSlot(Slot, "   ", ProfileChangeSource.Manual));
            Assert.False(Global.ApplyProfileToSlot(Slot, null, ProfileChangeSource.Manual));

            Assert.Equal("SelectedSentinel", Global.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ApplyProfileWithoutHalt_RejectsInvalidSlotOrName_AndDoesNotChangeState()
        {
            Global.SelectedProfile[Slot] = "SelectedSentinel";

            Assert.False(_service.ApplyProfileWithoutHalt(-1, Normal));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, ""));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, "   "));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, null));

            Assert.Equal("SelectedSentinel", Global.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ApplyProfileToSlot_Valid_AppliesNormalProfile_AndRaisesEventOnce()
        {
            bool result = Global.ApplyProfileToSlot(Slot, Normal, ProfileChangeSource.ControlService);

            Assert.True(result);
            Assert.Equal(Normal, Global.ProfilePath[Slot]);
            Assert.Equal(Normal, Global.SelectedProfile[Slot]);
            Assert.Equal(Normal, Global.OlderProfilePath[Slot]);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Single(_events);
            Assert.Equal(Normal, _events[0].ProfileName);
        }

        [Fact]
        public void PresetLoadBlank_ResetsTempProfileState()
        {
            _settings.SetUseTempProfile(Slot, true);
            _settings.SetTempProfileName(Slot, "TempSentinel");
            Global.tempprofileDistance[Slot] = true;

            // Phase7-Step3c W2: 移管後は ProfilePresetLoader（旧 Global.LoadBlankDevProfile の処理）で同じ結果になる
            TestRepositoryFactory.CreateProfilePresetLoader().LoadBlank(Slot, false, _control, false, false);

            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);
        }
    }
}
