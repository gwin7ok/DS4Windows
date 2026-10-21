using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W5d（Phase10-Step4d-Plan.md §4 W5、ユーザー決定: 案 1）: 自動プロファイルのルール（<c>AutoProfileEntity.ProfileNames</c>）と、
    /// リンクプロファイル（<c>BackingStore.linkedProfiles</c>、画面表示用の <c>linkedProfileUI</c>）の、実行時の保持を ID に。名前を読み書きする窓口で、保管は固定 ID と名前の控え。
    /// プロファイルの名前を変えても、読み出す名前は現在の名前になる。登録簿がなければ、従来の配列・辞書と同じに動く。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW5dTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;

        public Phase10Step4dW5dTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4d_W5d_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
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

        // ---- LinkedProfileMap ----

        [Fact]
        public void TheLinkedProfileMap_WorksLikeADictionary_WithoutTheRegistry()
        {
            var map = new LinkedProfileMap();

            map["A1"] = "One";
            map["B2"] = "Two";
            map["A1"] = "OneAgain";

            Assert.Equal(2, map.Count);
            Assert.Equal("OneAgain", map["A1"]);
            Assert.True(map.ContainsKey("B2"));
            Assert.False(map.ContainsKey("C3"));
            Assert.True(map.TryGetValue("B2", out string two));
            Assert.Equal("Two", two);
            Assert.False(map.TryGetValue("C3", out string none));
            Assert.Null(none);
            Assert.Throws<KeyNotFoundException>(() => map["C3"]);

            // 列挙・Keys は写し。列挙中に変えてもよい
            foreach (string key in map.Keys) map[key] = map[key] + "!";
            foreach (KeyValuePair<string, string> pair in map) map.Remove(pair.Key);

            Assert.Equal(0, map.Count);
        }

        [Fact]
        public void TheLinkedProfileMap_FollowsARename_AndKeepsTheLastNameOfADeletedProfile()
        {
            WriteProfile("Linked");
            var map = new LinkedProfileMap();
            map.AttachIdentity(NewIdentity());
            map["0123456789AB"] = "Linked";
            map["BA9876543210"] = "NotThere";

            File.Move(FileOf("Linked"), FileOf("Renamed"));

            Assert.Equal("Renamed", map["0123456789AB"]);
            Assert.Equal("NotThere", map["BA9876543210"]);
            Assert.Equal("Renamed", map.ToDictionary()["0123456789AB"]);
            Assert.Equal("Renamed", map.Single(p => p.Key == "0123456789AB").Value);

            File.Delete(FileOf("Renamed"));
            Assert.Equal("Renamed", map["0123456789AB"]);   // 削除されたら、最後の名前
        }

        [Fact]
        public void AttachingTheRegistry_ResolvesTheLinksAlreadyThere()
        {
            WriteProfile("Existing");
            var map = new LinkedProfileMap();
            map["0123456789AB"] = "Existing";

            map.AttachIdentity(NewIdentity());
            File.Move(FileOf("Existing"), FileOf("Moved"));

            Assert.Equal("Moved", map["0123456789AB"]);
        }

        // ---- BackingStore / ProfileRepository ----

        [Fact]
        public void TheBackingStore_TiesTheLinksAndTheirDisplayValuesToTheRegistry()
        {
            WriteProfile("Shared");
            BackingStore store = PersistenceHarness.NewStore();
            store.AttachProfileIdentity(NewIdentity());
            store.linkedProfiles["0123456789AB"] = "Shared";
            store.linkedProfileUI[2] = "Shared";

            File.Move(FileOf("Shared"), FileOf("Moved"));

            Assert.Equal("Moved", store.linkedProfiles["0123456789AB"]);
            Assert.Equal("Moved", store.linkedProfileUI[2]);
        }

        [Fact]
        public void TheRepository_ReturnsTheCurrentLinkedName_WithoutAnyReplaceCall()
        {
            WriteProfile("RepoLinked");
            BackingStore store = PersistenceHarness.NewStore();
            store.AttachProfileIdentity(NewIdentity());
            ProfileRepository repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService(store),
                TestRepositoryFactory.CreateProfileXmlStore(store), new PathService(_dir), null, store);
            repository.ChangeLinkedProfile("01:23:45:67:89:AB", "RepoLinked");
            Assert.Equal("RepoLinked", repository.GetLinkedProfile("01:23:45:67:89:AB"));

            File.Move(FileOf("RepoLinked"), FileOf("RepoRenamed"));

            Assert.Equal("RepoRenamed", repository.GetLinkedProfile("01:23:45:67:89:AB"));   // ReplaceLinkedProfileName を呼ばなくても追従する
            Assert.True(repository.ContainsLinkedProfile("01:23:45:67:89:AB"));
            repository.RemoveLinkedProfile("01:23:45:67:89:AB");
            Assert.False(repository.ContainsLinkedProfile("01:23:45:67:89:AB"));
        }

        // ---- 自動プロファイル ----

        [Fact]
        public void AnAutoProfileRule_FollowsARename_OnceItIsInTheHolder()
        {
            using var ctx = new PersistenceGoldenContext();
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            string file = Path.Combine(ctx.AppData, "Profiles", "RuleTarget.xml");
            File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + Guid.NewGuid().ToString("N") + "</ProfileId></DS4Windows>");
            var holder = new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService(), identity);
            var rule = new AutoProfileEntity(@"C:\p10s4d\w5d.exe", "Title");
            rule.ProfileNames[0] = "RuleTarget";   // ホルダーに入れる前に設定した名前も、入るときに ID に結ばれる
            holder.AutoProfileColl.Add(rule);

            File.Move(file, Path.Combine(ctx.AppData, "Profiles", "RuleRenamed.xml"));

            Assert.Equal("RuleRenamed", rule.ProfileNames[0]);   // AutoProfileService が読む名前が、現在の名前になる
        }

        [Fact]
        public void AnAutoProfileRule_OutsideTheHolder_OrWithoutTheRegistry_KeepsPlainNames()
        {
            var alone = new AutoProfileEntity(@"C:\p10s4d\w5d2.exe", "Title");
            alone.ProfileNames[0] = "Plain";
            Assert.Equal("Plain", alone.ProfileNames[0]);
            Assert.Equal(string.Empty, alone.ProfileNames[1]);
            Assert.Equal(ControllerLimits.MaxControllerCount, alone.ProfileNames.Length);

            using var ctx = new PersistenceGoldenContext();
            var holder = new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService());   // 登録簿なし
            var rule = new AutoProfileEntity(@"C:\p10s4d\w5d3.exe", "Title");
            rule.ProfileNames[0] = "NoRegistry";
            holder.AutoProfileColl.Add(rule);
            Assert.Equal("NoRegistry", rule.ProfileNames[0]);
        }

        [Fact]
        public void TheAutoProfileRules_LoadedFromTheFile_AreTiedToTheRegistry()
        {
            using var ctx = new PersistenceGoldenContext();
            IProfileIdentityService identity = new ProfileIdentityService(new PathService(ctx.AppData));
            string targetFile = Path.Combine(ctx.AppData, "Profiles", "FromFile.xml");
            File.WriteAllText(targetFile, "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + Guid.NewGuid().ToString("N") + "</ProfileId></DS4Windows>");

            var first = new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService(), identity);
            var rule = new AutoProfileEntity(@"C:\p10s4d\w5d4.exe", "Title");
            rule.ProfileNames[1] = "FromFile";
            first.AutoProfileColl.Add(rule);
            Assert.True(first.Save(Path.Combine(ctx.AppData, "Auto Profiles.xml")));

            var second = new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService(), identity);   // ファイルから読み込む
            File.Move(targetFile, Path.Combine(ctx.AppData, "Profiles", "FromFileRenamed.xml"));

            Assert.Equal("FromFileRenamed", second.AutoProfileColl.Single().ProfileNames[1]);
        }
    }
}
