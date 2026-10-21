using System;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W7（Phase10-Step4d-Plan.md §4 W7、ユーザー決定: 案 A）: 名前変更の追従のコードのうち、実行時に不要になったもの（スロットの配列の書き換え、
    /// <c>ReplaceLinkedProfileName</c>、<c>AutoProfileHolder.ReplaceProfileName</c>）を撤去した。実行時の保持は、W5b〜W5d で、プロファイルの固定 ID を保管する窓口になっている。
    /// 保存先のファイルの名前と ID は、名前変更の直後に、保存し直して書き直す（<c>SaveLinkedProfiles</c>、<c>AutoProfileHolder.Save</c>、アプリ設定の保存）。
    /// SA の切り替え先は、ID から現在の名前に引き直して保存する（<c>RefreshProfileSwitchTargets</c>）。ID のない古いプロファイルのファイルのため、
    /// <c>ReplaceControlProfileSwitchTarget</c>（Controls）と <c>ReplaceActionReference</c>（SA の名前変更）は残す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW7FollowUpTests
    {
        private static string NewId() => Guid.NewGuid().ToString("N");

        private static string WriteProfile(PersistenceGoldenContext ctx, string name)
        {
            string id = NewId();
            File.WriteAllText(Path.Combine(ctx.AppData, "Profiles", name + ".xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId></DS4Windows>");
            return id;
        }

        private static void Rename(PersistenceGoldenContext ctx, string from, string to)
        {
            string folder = Path.Combine(ctx.AppData, "Profiles");
            File.Move(Path.Combine(folder, from + ".xml"), Path.Combine(folder, to + ".xml"));
        }

        // ---- 撤去したメンバー ----

        [Fact]
        public void TheRuntimeFollowUps_AreGone()
        {
            Assert.Null(typeof(IProfileRepository).GetMethod("ReplaceLinkedProfileName"));
            Assert.Null(typeof(IProfileRepository).GetMethod("ReplaceProfileSwitchTarget"));
            Assert.Null(typeof(AutoProfileHolder).GetMethod("ReplaceProfileName"));

            // 古いプロファイルのファイル（ID のない）のために残すもの
            Assert.NotNull(typeof(IProfileRepository).GetMethod("ReplaceControlProfileSwitchTarget"));
            Assert.NotNull(typeof(IProfileRepository).GetMethod("ReplaceActionReference"));
            Assert.NotNull(typeof(IProfileRepository).GetMethod("RemoveActionReference"));
        }

        // ---- 名前変更の直後に保存し直す ----

        [Fact]
        public void SavingTheLinkedProfiles_AfterARename_WritesTheNewNameWithTheSameId()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = WriteProfile(ctx, "LinkedBefore");
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            BackingStore store = PersistenceHarness.NewStore();
            store.AttachProfileIdentity(identity);
            store.m_linkedProfiles = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            var serializer = new LinkedProfilesXmlSerializer(store, identity);
            Assert.True(serializer.createLinkedProfiles());
            store.linkedProfiles["0123456789AB"] = "LinkedBefore";
            Assert.True(serializer.SaveLinkedProfiles());

            Rename(ctx, "LinkedBefore", "LinkedAfter");
            Assert.True(serializer.SaveLinkedProfiles());   // 名前変更の直後の保存（MainWindow が profileRepo.SaveLinkedProfiles() を呼ぶ）

            var doc = new XmlDocument();
            doc.Load(store.m_linkedProfiles);
            XmlNode node = doc.SelectSingleNode("/LinkedControllers/MAC0123456789AB");
            Assert.Equal("LinkedAfter", node.InnerText);            // ファイルの名前も新しくなる
            Assert.Equal(id, node.Attributes["Id"].Value);          // ID は同じ
        }

        [Fact]
        public void SavingTheAutoProfiles_AfterARename_WritesTheNewName_AndTheDefaultPathOverloadWorks()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = WriteProfile(ctx, "RuleBefore");
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            var holder = new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService(), identity);
            var rule = new AutoProfileEntity(@"C:\p10s4d\w7.exe", "Title");
            rule.ProfileNames[0] = "RuleBefore";
            holder.AutoProfileColl.Add(rule);

            Rename(ctx, "RuleBefore", "RuleAfter");
            Assert.True(holder.Save());   // 既定の保存先（Auto Profiles.xml）。MainWindow が Holder?.Save() を呼ぶ

            var doc = new XmlDocument();
            doc.Load(Path.Combine(ctx.AppData, "Auto Profiles.xml"));
            XmlNode program = doc.SelectSingleNode("/Programs/Program");
            Assert.Equal("RuleAfter", program.SelectSingleNode("Controller1").InnerText);
            Assert.StartsWith(id, program.SelectSingleNode("ControllerProfileIds").InnerText);
        }

        // ---- SA の切り替え先 ----

        private static string Details(string actionsPath, string name)
        {
            var doc = new XmlDocument();
            doc.Load(actionsPath);
            return doc.SelectSingleNode($"/Actions/Action[@Name='{name}']/Details").InnerText;
        }

        [Fact]
        public void RefreshProfileTargets_PullsTheCurrentNameFromTheId_AndSavesActionsXml()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfile(ctx, "SwitchBefore");
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            BackingStore store = PersistenceHarness.NewStore();
            store.m_Actions = Path.Combine(ctx.AppData, "Actions.xml");
            var serializer = new SpecialActionsXmlSerializer(store, identity);
            serializer.SaveActionNew("Switch", "Cross", 3, "SwitchBefore", false);
            SpecialAction action = store.actions.Single(a => a.name == "Switch");
            Assert.False(string.IsNullOrEmpty(action.profileId));

            Rename(ctx, "SwitchBefore", "SwitchAfter");
            int updated = serializer.RefreshProfileTargets("SwitchBefore", "SwitchAfter");

            Assert.Equal(1, updated);
            Assert.Equal("SwitchAfter", action.details);                               // メモリの details（編集画面が読む）
            Assert.Equal("SwitchAfter", Details(store.m_Actions, "Switch"));           // ファイルの名前も新しくなる
            Assert.Equal(0, serializer.RefreshProfileTargets("SwitchBefore", "SwitchAfter"));   // 2 回目は何も更新しない
        }

        [Fact]
        public void RefreshProfileTargets_FallsBackToTheOldName_ForAnActionWithoutAnId()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfile(ctx, "NewTarget");
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            BackingStore store = PersistenceHarness.NewStore();
            store.m_Actions = Path.Combine(ctx.AppData, "Actions.xml");
            var serializer = new SpecialActionsXmlSerializer(store, identity);
            var noId = new SpecialAction("NoId", "Cross", "Profile", "OldTarget");   // 切り替え先が存在せず、ID を引けなかった SA
            var other = new SpecialAction("Other", "Circle", "Profile", "Unrelated");
            store.actions.Add(noId);
            store.actions.Add(other);

            int updated = serializer.RefreshProfileTargets("oldtarget", "NewTarget");

            Assert.Equal(1, updated);
            Assert.Equal("NewTarget", noId.details);
            Assert.False(string.IsNullOrEmpty(noId.profileId));   // 新しい名前から ID を引けたので、割り当てる
            Assert.Equal("Unrelated", other.details);
        }

        // ---- 名前変更の共通の経路（ソースの検査）----

        [Fact]
        public void TheRenamePath_SavesTheFiles_ThroughTheCommonSyncPath()
        {
            string main = SourceFileLocator.Find("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            string editor = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (main == null || editor == null) return;

            string source = File.ReadAllText(main);
            int start = source.IndexOf("if (isRename)", StringComparison.Ordinal);
            int end = source.IndexOf("ProfileListHolder.Refresh();", start, StringComparison.Ordinal);
            string block = source.Substring(start, end - start);

            Assert.Contains("profileRepo.SaveLinkedProfiles();", block);
            Assert.Contains("_autoProfileService?.Holder?.Save();", block);
            Assert.Contains("appSettingsService.Save();", block);
            Assert.Contains("profileRepo.RefreshProfileSwitchTargets(oldProfile, newProfile);", block);
            Assert.Contains("profileRepo.ReplaceControlProfileSwitchTarget(oldProfile, newProfile);", block);   // 古いプロファイルのために残す
            Assert.DoesNotContain("ProfilePath[i] =", block);        // スロットの配列の書き換えは、窓口が追従するため、不要
            Assert.DoesNotContain("OlderProfilePath[i] =", block);
            Assert.DoesNotContain("ReplaceLinkedProfileName", block);
            Assert.DoesNotContain("ReplaceProfileName", block);

            // プロファイル一覧からの名前変更は、この共通の経路を通る
            Assert.Contains("SyncProfileListAndControllers(oldProfile: oldProfileName, newProfile: newProfileName)", source);
            // SA の名前変更は、古いプロファイルのファイルのため、ReplaceActionReference を残している
            Assert.Contains("profileRepository.ReplaceActionReference(oldName, newName)", File.ReadAllText(editor));
        }
    }
}
