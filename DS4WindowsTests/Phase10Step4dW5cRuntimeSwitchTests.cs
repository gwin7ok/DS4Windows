using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W5c（Phase10-Step4d-Plan.md §4 W5、ユーザー決定: 案 1 の範囲 1・2）: 実行時の切り替え・復帰の経路を ID に。
    /// (1) 一時プロファイルの名前の保管は、プロファイルの固定 ID（名前を読み書きする窓口）。名前を変えても、読み出す名前は現在の名前になる。
    /// (2) SA／Controls の Load Profile は、実行の直前に、切り替え先の ID から現在の名前を引く（SA の定義・束縛を作り直さなくても、名前変更に追従する）。
    /// ID がない・引けないときは、従来どおり名前（<c>details</c>）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW5cRuntimeSwitchTests : IDisposable
    {
        private const int Device = MappingCharacterizationScope.Device;

        private readonly string _dir;
        private readonly string _profiles;

        public Phase10Step4dW5cRuntimeSwitchTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4d_W5c_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
            TestHost.GetService<IProfileSettingsService>().SetUseTempProfile(Device, false);
            try { Directory.Delete(_dir, true); } catch { }
        }

        private IProfileIdentityService NewIdentity() => new ProfileIdentityService(new PathService(_dir));

        private string FileOf(string name) => Path.Combine(_profiles, name + ".xml");

        private string WriteProfile(string name)
        {
            string id = Guid.NewGuid().ToString("N");
            File.WriteAllText(FileOf(name), "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId></DS4Windows>");
            return id;
        }

        // ---- (1) 一時プロファイルの名前 ----

        [Fact]
        public void TheTempProfileName_FollowsARename_WhenTheRegistryIsAttached()
        {
            WriteProfile("TempBefore");
            ProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService(PersistenceHarness.NewStore());
            settings.AttachProfileIdentity(NewIdentity());

            settings.SetTempProfileName(1, "TempBefore");
            File.Move(FileOf("TempBefore"), FileOf("TempAfter"));

            Assert.Equal("TempAfter", settings.GetTempProfileName(1));
        }

        [Fact]
        public void TheTempProfileName_IsPlainNames_WithoutTheRegistry()
        {
            ProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService(PersistenceHarness.NewStore());

            Assert.Equal(string.Empty, settings.GetTempProfileName(0));
            settings.SetTempProfileName(0, "Anything");
            Assert.Equal("Anything", settings.GetTempProfileName(0));
            settings.SetTempProfileName(0, null);
            Assert.Equal(string.Empty, settings.GetTempProfileName(0));
            Assert.Equal(string.Empty, settings.GetTempProfileName(-1));   // 範囲外は空（従来どおり）
            Assert.Equal(string.Empty, settings.GetTempProfileName(99));
        }

        [Fact]
        public void TheContainer_TiesTheRegistryToTheTempProfileName()
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            if (path == null) return;

            Assert.Contains("ProfileSettingsService)?.AttachProfileIdentity(service)", File.ReadAllText(path));
        }

        // ---- (2) Load Profile の実行時の解決 ----

        private ProfileApplicationService NewApplicationService(IProfileIdentityService identity)
        {
            IProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService();
            return TestRepositoryFactory.CreateProfileApplicationService(settings, () => null,
                TestRepositoryFactory.CreateProfileRepository(settings), new EnvironmentService(), profileIdentity: identity);
        }

        [Fact]
        public void TheSwitchTarget_IsTheCurrentName_WhenTheProfileWasRenamedAfterTheActionWasLoaded()
        {
            string id = WriteProfile("OldName");
            var action = new SpecialAction("Switch", "Cross", "Profile", "OldName") { profileId = id };
            ProfileApplicationService service = NewApplicationService(NewIdentity());

            File.Move(FileOf("OldName"), FileOf("NewName"));   // SA を読み込んだあとの名前変更（SA を作り直さない）

            Assert.Equal("NewName", service.ResolveTarget(action));
            Assert.Equal("NewName", action.details);   // 定義の名前も直る
        }

        [Fact]
        public void TheSwitchTarget_IsTheName_WithoutAnIdOrAMatch_OrWithoutTheRegistry()
        {
            string id = WriteProfile("Present");
            ProfileApplicationService withRegistry = NewApplicationService(NewIdentity());

            Assert.Equal("Typed", withRegistry.ResolveTarget(new SpecialAction("S", "Cross", "Profile", "Typed")));   // ID なし
            Assert.Equal("Missing", withRegistry.ResolveTarget(new SpecialAction("S", "Cross", "Profile", "Missing") { profileId = Guid.NewGuid().ToString("N") }));   // 引けない ID
            Assert.Equal("Fallback", NewApplicationService(null).ResolveTarget(new SpecialAction("S", "Cross", "Profile", "Fallback") { profileId = id }));   // 登録簿なし
        }

        [Fact]
        public void AControlsLoadProfile_CarriesTheTargetsId_ToTheSyntheticAction()
        {
            using var t = new ControlsKeyTestScope();
            string id = Guid.NewGuid().ToString("N");
            t.MapSpecial(DS4Controls.L3, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "Target", ProfileId = id });
            t.MapSpecial(DS4Controls.R3, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "NoId" });

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            Assert.Equal(id, bindings.Single(b => b.Source.name == $"Synthetic_Special_{Device}_{(int)DS4Controls.L3}").Source.profileId);
            Assert.Null(bindings.Single(b => b.Source.name == $"Synthetic_Special_{Device}_{(int)DS4Controls.R3}").Source.profileId);
        }

        [Fact]
        public void TheApplyPath_UsesTheResolvedTarget()
        {
            // ApplyFromAction は、解決した名前で切り替える（details をそのまま使わない）
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileApplicationService.cs");
            if (path == null) return;

            string source = File.ReadAllText(path);
            int start = source.IndexOf("public void ApplyFromAction(", StringComparison.Ordinal);
            int end = source.IndexOf("public bool RestoreFromAction(", start, StringComparison.Ordinal);
            string body = source.Substring(start, end - start);

            Assert.Contains("string target = ResolveTarget(action);", body);
            Assert.DoesNotContain("ApplyCore(deviceIndex, action.details", body);
        }
    }
}
