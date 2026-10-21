using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W3（Phase10-Step4d-Plan.md §3.2・§4 W3）: 保存する参照を、ID を主・名前を併記にする。
    /// 対象は、プロファイルが使う SA（<c>&lt;ProfileActions&gt;</c>＋<c>&lt;ProfileActionIds&gt;</c>）、SA の Load Profile の切り替え先（<c>Actions.xml</c> の <c>ProfileId</c>）、
    /// Controls の Load Profile の切り替え先（<c>&lt;Control&gt;&lt;Special&gt;</c> の <c>ProfileId</c>）。読み込みでは、ID が見つかれば現在の名前に引き直し（ID が勝つ）、
    /// ない・見つからないときは名前のまま。実行時の内部（<c>profileActions</c> ほか）は名前のまま。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW3ReferenceIdTests
    {
        private const int Slot = PersistenceHarness.Slot;

        private static string NewId() => Guid.NewGuid().ToString("N");

        private static IProfileIdentityService Identity(PersistenceGoldenContext ctx) => new ProfileIdentityService(new PathService(ctx.AppData));

        private static void WriteProfileFile(PersistenceGoldenContext ctx, string name, string id)
            => File.WriteAllText(Path.Combine(ctx.AppData, "Profiles", name + ".xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId></DS4Windows>");

        // ---- <ProfileActions> + <ProfileActionIds> ----

        private static string SaveProfileWith(PersistenceGoldenContext ctx, BackingStore store, string name)
        {
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).SaveProfile(Slot, name));
            return File.ReadAllText(Path.Combine(ctx.AppData, "Profiles", name + ".xml"));
        }

        private static BackingStore LoadProfileWith(PersistenceGoldenContext ctx, string xml, string name, params SpecialAction[] actions)
        {
            string path = Path.Combine(ctx.AppData, "Profiles", name + ".xml");
            File.WriteAllText(path, xml);
            BackingStore store = PersistenceHarness.NewStore();
            store.actions.AddRange(actions);
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).LoadProfile(Slot, false, ctx.Control, path, false, false));
            return store;
        }

        [Fact]
        public void TheProfile_SavesTheActionIds_NextToTheNames_InTheSameOrder()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            var a = new SpecialAction("ActionA", "Cross", "Macro", "65");
            var b = new SpecialAction("ActionB", "Circle", "Macro", "66");
            store.actions.Add(a);
            store.actions.Add(b);
            store.profileActions[Slot] = new List<string> { "ActionB", "ActionA" };

            string xml = SaveProfileWith(ctx, store, "Step4dW3Save");

            Assert.Contains("<ProfileActions>ActionB/ActionA</ProfileActions>", xml);   // 名前の一覧は、古いバージョンのために残す
            Assert.Contains($"<ProfileActionIds>{b.id}/{a.id}</ProfileActionIds>", xml);
        }

        [Fact]
        public void TheProfile_WritesNoIds_WhenNoneOfTheActionsIsKnown()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.profileActions[Slot] = new List<string> { "Unknown1", "Unknown2" };

            string xml = SaveProfileWith(ctx, store, "Step4dW3NoIds");

            Assert.Contains("<ProfileActions>Unknown1/Unknown2</ProfileActions>", xml);
            Assert.DoesNotContain("ProfileActionIds", xml);
        }

        [Fact]
        public void AMissingAction_GetsAnEmptyId_AmongTheKnownOnes()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            var known = new SpecialAction("Known", "Cross", "Macro", "65");
            store.actions.Add(known);
            store.profileActions[Slot] = new List<string> { "Gone", "Known" };

            string xml = SaveProfileWith(ctx, store, "Step4dW3Mixed");

            Assert.Contains($"<ProfileActionIds>/{known.id}</ProfileActionIds>", xml);
        }

        private static string Xml(string actions, string ids)
            => "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileActions>" + actions + "</ProfileActions>" +
               (ids == null ? string.Empty : "<ProfileActionIds>" + ids + "</ProfileActionIds>") + "</DS4Windows>";

        [Fact]
        public void TheIdWins_WhenAnActionWasRenamed_AfterTheProfileWasSaved()
        {
            using var ctx = new PersistenceGoldenContext();
            var renamed = new SpecialAction("NewName", "Cross", "Macro", "65");

            BackingStore loaded = LoadProfileWith(ctx, Xml("OldName/Other", renamed.id + "/" + NewId()), "Step4dW3Renamed", renamed);

            Assert.Equal(new[] { "NewName", "Other" }, loaded.profileActions[Slot].ToArray());   // 古い名前でも、ID から現在の名前に引き直す
        }

        [Fact]
        public void TheNames_AreUsed_WhenThereAreNoIds_TheCountDiffers_OrAnIdIsNotFound()
        {
            using var ctx = new PersistenceGoldenContext();
            var known = new SpecialAction("Known", "Cross", "Macro", "65");

            BackingStore noIds = LoadProfileWith(ctx, Xml("A/B", null), "Step4dW3None", known);
            BackingStore wrongCount = LoadProfileWith(ctx, Xml("A/B", known.id), "Step4dW3Count", known);
            BackingStore notFound = LoadProfileWith(ctx, Xml("A/Known", NewId() + "/" + NewId()), "Step4dW3NotFound", known);

            Assert.Equal(new[] { "A", "B" }, noIds.profileActions[Slot].ToArray());
            Assert.Equal(new[] { "A", "B" }, wrongCount.profileActions[Slot].ToArray());
            Assert.Equal(new[] { "A", "Known" }, notFound.profileActions[Slot].ToArray());
        }

        [Fact]
        public void TwoNamesThatResolveToTheSameAction_AreListedOnce()
        {
            using var ctx = new PersistenceGoldenContext();
            var action = new SpecialAction("Current", "Cross", "Macro", "65");

            BackingStore loaded = LoadProfileWith(ctx, Xml("Current/OldAlias", action.id + "/" + action.id), "Step4dW3Dup", action);

            Assert.Equal(new[] { "Current" }, loaded.profileActions[Slot].ToArray());
        }

        // ---- SA の Load Profile の切り替え先 ----

        private sealed class ActionsRig : IDisposable
        {
            public readonly PersistenceGoldenContext Context = new PersistenceGoldenContext();
            public readonly BackingStore Store = PersistenceHarness.NewStore();
            public readonly string Path;
            public readonly IProfileIdentityService Identity;
            public readonly SpecialActionsXmlSerializer Xml;

            public ActionsRig(bool withIdentity = true)
            {
                Path = System.IO.Path.Combine(Context.AppData, "Actions.xml");
                Store.m_Actions = Path;
                Identity = withIdentity ? new ProfileIdentityService(new PathService(Context.AppData)) : null;
                Xml = new SpecialActionsXmlSerializer(Store, Identity);
            }

            public void Dispose() => Context.Dispose();

            public XmlNode Node(string name)
            {
                var doc = new XmlDocument();
                doc.Load(Path);
                return doc.SelectSingleNode($"/Actions/Action[@Name='{name}']");
            }

            public BackingStore Reload()
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.m_Actions = Path;
                Assert.True(new SpecialActionsXmlSerializer(store, Identity).LoadActions());
                return store;
            }
        }

        [Fact]
        public void ALoadProfileAction_SavesTheTargetsId_NextToItsName()
        {
            using var rig = new ActionsRig();
            string id = NewId();
            WriteProfileFile(rig.Context, "Target", id);

            rig.Xml.SaveActionNew("Switch", "Cross", 3, "Target", false);

            XmlNode node = rig.Node("Switch");
            Assert.Equal("Target", node.SelectSingleNode("Details").InnerText);   // 名前は残す
            Assert.Equal(id, node.SelectSingleNode("ProfileId").InnerText);
        }

        [Fact]
        public void ALoadProfileAction_FollowsTheTargetsRename_WithoutAnyReplaceCall()
        {
            using var rig = new ActionsRig();
            WriteProfileFile(rig.Context, "Target", NewId());
            rig.Xml.SaveActionNew("Switch", "Cross", 3, "Target", false);

            string profiles = Path.Combine(rig.Context.AppData, "Profiles");
            File.Move(Path.Combine(profiles, "Target.xml"), Path.Combine(profiles, "Renamed.xml"));   // プロファイルの名前変更（ID はファイルの中）
            BackingStore loaded = rig.Reload();

            Assert.Equal("Renamed", loaded.actions.Single(a => a.name == "Switch").details);   // ID から現在の名前に引き直す（ID が勝つ）
        }

        [Fact]
        public void ALoadProfileAction_KeepsTheName_WhenTheTargetIsGone()
        {
            using var rig = new ActionsRig();
            WriteProfileFile(rig.Context, "Target", NewId());
            rig.Xml.SaveActionNew("Switch", "Cross", 3, "Target", false);
            File.Delete(Path.Combine(rig.Context.AppData, "Profiles", "Target.xml"));

            BackingStore loaded = rig.Reload();

            Assert.Equal("Target", loaded.actions.Single(a => a.name == "Switch").details);
        }

        [Fact]
        public void AnOldFileWithoutTheTargetsId_GetsItOnLoad_AndIsSavedOnce()
        {
            using var rig = new ActionsRig();
            string id = NewId();
            WriteProfileFile(rig.Context, "Target", id);
            File.WriteAllText(rig.Path, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>" +
                "<Action Name=\"Switch\"><Trigger>Cross</Trigger><Type>Profile</Type><Details>Target</Details></Action></Actions>");

            rig.Reload();

            Assert.Equal(id, rig.Node("Switch").SelectSingleNode("ProfileId").InnerText);
            string before = File.ReadAllText(rig.Path);
            rig.Reload();
            Assert.Equal(before, File.ReadAllText(rig.Path));   // 2 回目は書き換えない
        }

        [Fact]
        public void WithoutTheRegistry_NoTargetIdIsWritten()
        {
            using var rig = new ActionsRig(withIdentity: false);
            WriteProfileFile(rig.Context, "Target", NewId());

            rig.Xml.SaveActionNew("Switch", "Cross", 3, "Target", false);

            Assert.Null(rig.Node("Switch").SelectSingleNode("ProfileId"));
        }

        [Fact]
        public void OnlyTheProfileType_HasATargetId()
        {
            using var rig = new ActionsRig();
            WriteProfileFile(rig.Context, "Target", NewId());

            rig.Xml.SaveActionNew("Macro", "Cross", 1, "65", false);

            Assert.Null(rig.Node("Macro").SelectSingleNode("ProfileId"));
        }

        // ---- Controls の Load Profile の切り替え先 ----

        private static void MapProfile(BackingStore store, string profileName, string profileId = null)
        {
            DS4ControlSettings dcs = store.GetDS4CSetting(Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = profileName, ProfileId = profileId };
        }

        [Fact]
        public void AControlsLoadProfile_SavesTheTargetsId_AndFollowsTheRename()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = NewId();
            WriteProfileFile(ctx, "Target", id);
            IProfileIdentityService identity = Identity(ctx);

            BackingStore store = PersistenceHarness.NewStore();
            MapProfile(store, "Target");
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store, null, null, identity).SaveProfile(Slot, "Step4dW3Controls"));
            string path = Path.Combine(ctx.AppData, "Profiles", "Step4dW3Controls.xml");

            var doc = new XmlDocument();
            doc.Load(path);
            Assert.Equal("Target", doc.SelectSingleNode("DS4Windows/Control/Special/Cross/ProfileName").InnerText);   // 名前は残す
            Assert.Equal(id, doc.SelectSingleNode("DS4Windows/Control/Special/Cross/ProfileId").InnerText);

            // 切り替え先のプロファイルの名前を変えて、保存したプロファイルを読み込む
            File.Move(Path.Combine(ctx.AppData, "Profiles", "Target.xml"), Path.Combine(ctx.AppData, "Profiles", "Renamed.xml"));
            BackingStore loaded = PersistenceHarness.NewStore();
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(loaded, null, null, identity).LoadProfile(Slot, false, ctx.Control, path, false, false));

            Assert.Equal("Renamed", loaded.GetDS4CSetting(Slot, DS4Controls.Cross).special.ProfileName);   // ID が勝つ
            Assert.Equal(id, loaded.GetDS4CSetting(Slot, DS4Controls.Cross).special.ProfileId);
        }

        [Fact]
        public void AControlsLoadProfile_KeepsTheName_WithoutTheRegistry_OrWhenTheTargetIsGone()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            MapProfile(store, "Missing", NewId());   // 見つからない ID（削除済み）

            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store, null, null, Identity(ctx)).SaveProfile(Slot, "Step4dW3Gone"));
            string path = Path.Combine(ctx.AppData, "Profiles", "Step4dW3Gone.xml");
            BackingStore loaded = PersistenceHarness.NewStore();
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(loaded, null, null, Identity(ctx)).LoadProfile(Slot, false, ctx.Control, path, false, false));
            Assert.Equal("Missing", loaded.GetDS4CSetting(Slot, DS4Controls.Cross).special.ProfileName);

            // 登録簿なし: ID は書かず、名前だけで読み書きする（従来どおり）
            BackingStore plain = PersistenceHarness.NewStore();
            MapProfile(plain, "Missing");
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(plain).SaveProfile(Slot, "Step4dW3Plain"));
            var doc = new XmlDocument();
            doc.Load(Path.Combine(ctx.AppData, "Profiles", "Step4dW3Plain.xml"));
            Assert.Null(doc.SelectSingleNode("DS4Windows/Control/Special/Cross/ProfileId"));
        }

        [Fact]
        public void TheControlsItem_CopiesTheProfileId()
        {
            // 保存・読み込みの往復は、AControlsLoadProfile_SavesTheTargetsId_AndFollowsTheRename が確かめる
            string id = NewId();
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "P", ProfileId = id };

            Assert.Equal(id, item.Clone().ProfileId);
        }

        // ---- 配線 ----

        [Fact]
        public void TheStartup_AssignsTheProfileIdsBefore_TheActionsAreLoaded()
        {
            string app = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (app == null) return;

            string source = File.ReadAllText(app);
            int ensure = source.IndexOf("_profileIdentityService.EnsureAllPersisted()", StringComparison.Ordinal);
            int load = source.IndexOf("_specialActionRepository.LoadActions()", StringComparison.Ordinal);
            Assert.True(ensure > 0 && load > ensure, "プロファイルの ID の割り当てが、SA の読み込みより前にない（Load Profile の切り替え先を ID で解決するため）");
        }
    }
}
