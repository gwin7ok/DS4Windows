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
    /// Phase10-Step4d W4（Phase10-Step4d-Plan.md §3.2・§4 W4）: スロットの選択（<c>Profiles.xml</c> の Controller1〜8）、リンクプロファイル（<c>LinkedProfiles.xml</c>）、
    /// 自動プロファイル（<c>Auto Profiles.xml</c>）の参照を、ID を主・名前を併記にする。読み込みでは、ID が見つかれば現在の名前に引き直し（ID が勝つ）、
    /// ない・見つからないときは名前のまま。実行時の内部（<c>ProfilePath</c> ほか）は名前のまま。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW4ReferenceIdTests
    {
        private static string NewId() => Guid.NewGuid().ToString("N");

        private static IProfileIdentityService Identity(PersistenceGoldenContext ctx) => new ProfileIdentityService(new PathService(ctx.AppData));

        private static string WriteProfileFile(PersistenceGoldenContext ctx, string name)
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

        private static XmlDocument LoadXml(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }

        // ---- スロットの選択（Profiles.xml）----

        private static AppSettingsXmlSerializer AppSettings(BackingStore store, IProfileIdentityService identity)
            => new AppSettingsXmlSerializer(store, new PathService(), new EnvironmentService(), identity);

        [Fact]
        public void TheSlotSelection_SavesTheProfileIds_NextToTheNames_AndFollowsTheRename()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = WriteProfileFile(ctx, "Target");
            IProfileIdentityService identity = Identity(ctx);

            BackingStore store = PersistenceHarness.NewStore();
            store.m_Profile = Path.Combine(ctx.AppData, "Profiles.xml");
            store.selectedProfile[0] = "Target";
            store.selectedProfile[1] = "NoSuchProfile";
            Assert.True(AppSettings(store, identity).Save());

            XmlDocument doc = LoadXml(store.m_Profile);
            Assert.Equal("Target", doc.SelectSingleNode("Profile/Controller1").InnerText);   // 名前は残す
            string[] ids = doc.SelectSingleNode("Profile/ControllerProfileIds").InnerText.Split('/');
            Assert.Equal(8, ids.Length);
            Assert.Equal(id, ids[0]);
            Assert.Equal(string.Empty, ids[1]);   // 見つからないプロファイルは、名前だけ

            Rename(ctx, "Target", "Renamed");
            BackingStore loaded = PersistenceHarness.NewStore();
            loaded.m_Profile = store.m_Profile;
            Assert.True(AppSettings(loaded, identity).Load());

            Assert.Equal("Renamed", loaded.profilePath[0]);   // ID が勝つ
            Assert.Equal("Renamed", loaded.olderProfilePath[0]);
            Assert.Equal("Renamed", loaded.selectedProfile[0]);
            Assert.Equal("NoSuchProfile", loaded.profilePath[1]);
        }

        [Fact]
        public void TheSlotSelection_WithoutTheRegistry_WritesNoIds_AndAnOldFileLoadsByName()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfileFile(ctx, "Target");

            BackingStore store = PersistenceHarness.NewStore();
            store.m_Profile = Path.Combine(ctx.AppData, "Profiles.xml");
            store.selectedProfile[0] = "Target";
            Assert.True(AppSettings(store, null).Save());
            Assert.Null(LoadXml(store.m_Profile).SelectSingleNode("Profile/ControllerProfileIds"));

            // ID のない古いファイルは、登録簿があっても、名前のまま読む
            BackingStore loaded = PersistenceHarness.NewStore();
            loaded.m_Profile = store.m_Profile;
            Assert.True(AppSettings(loaded, Identity(ctx)).Load());
            Assert.Equal("Target", loaded.profilePath[0]);
        }

        // ---- リンクプロファイル（LinkedProfiles.xml）----

        [Fact]
        public void TheLinkedProfiles_SaveTheIdAsAnAttribute_AndFollowTheRename()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = WriteProfileFile(ctx, "Target");
            IProfileIdentityService identity = Identity(ctx);

            BackingStore store = PersistenceHarness.NewStore();
            store.m_linkedProfiles = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            var serializer = new LinkedProfilesXmlSerializer(store, identity);
            Assert.True(serializer.createLinkedProfiles());
            store.linkedProfiles["0123456789AB"] = "Target";
            store.linkedProfiles["BA9876543210"] = "Missing";
            Assert.True(serializer.SaveLinkedProfiles());

            XmlDocument doc = LoadXml(store.m_linkedProfiles);
            XmlNode linked = doc.SelectSingleNode("/LinkedControllers/MAC0123456789AB");
            Assert.Equal("Target", linked.InnerText);                  // 中身（名前）は従来のまま
            Assert.Equal(id, linked.Attributes["Id"].Value);
            Assert.Null(doc.SelectSingleNode("/LinkedControllers/MACBA9876543210").Attributes["Id"]);   // 見つからないプロファイルは、名前だけ

            Rename(ctx, "Target", "Renamed");
            BackingStore loaded = PersistenceHarness.NewStore();
            loaded.m_linkedProfiles = store.m_linkedProfiles;
            Assert.True(new LinkedProfilesXmlSerializer(loaded, identity).LoadLinkedProfiles());

            Assert.Equal("Renamed", loaded.linkedProfiles["0123456789AB"]);   // ID が勝つ
            Assert.Equal("Missing", loaded.linkedProfiles["BA9876543210"]);
        }

        [Fact]
        public void TheLinkedProfiles_WithoutTheRegistry_WriteTheOldFormat_AndAnOldFileLoadsByName()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfileFile(ctx, "Target");

            BackingStore store = PersistenceHarness.NewStore();
            store.m_linkedProfiles = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            var plain = new LinkedProfilesXmlSerializer(store);
            Assert.True(plain.createLinkedProfiles());
            store.linkedProfiles["0123456789AB"] = "Target";
            Assert.True(plain.SaveLinkedProfiles());
            Assert.Null(LoadXml(store.m_linkedProfiles).SelectSingleNode("/LinkedControllers/MAC0123456789AB").Attributes["Id"]);

            BackingStore loaded = PersistenceHarness.NewStore();
            loaded.m_linkedProfiles = store.m_linkedProfiles;
            Assert.True(new LinkedProfilesXmlSerializer(loaded, Identity(ctx)).LoadLinkedProfiles());
            Assert.Equal("Target", loaded.linkedProfiles["0123456789AB"]);
        }

        // ---- 自動プロファイル（Auto Profiles.xml）----

        private static AutoProfileHolder NewHolder(PersistenceGoldenContext ctx, IProfileIdentityService identity)
            => new AutoProfileHolder(new PathService(ctx.AppData), new EnvironmentService(), identity);

        [Fact]
        public void TheAutoProfiles_SaveTheProfileIds_NextToTheNames_AndFollowTheRename()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = WriteProfileFile(ctx, "Target");
            IProfileIdentityService identity = Identity(ctx);
            string file = Path.Combine(ctx.AppData, "Auto Profiles.xml");

            AutoProfileHolder holder = NewHolder(ctx, identity);
            var entity = new AutoProfileEntity(@"C:\p10s4d\w4.exe", "Title");
            entity.ProfileNames[0] = "Target";
            entity.ProfileNames[1] = "Missing";
            entity.ProfileNames[2] = AutoProfileEntity.NONE_STRING;
            holder.AutoProfileColl.Add(entity);
            Assert.True(holder.Save(file));

            XmlNode program = LoadXml(file).SelectSingleNode("/Programs/Program");
            Assert.Equal("Target", program.SelectSingleNode("Controller1").InnerText);   // 名前は残す
            string[] ids = program.SelectSingleNode("ControllerProfileIds").InnerText.Split('/');
            Assert.Equal(id, ids[0]);
            Assert.Equal(string.Empty, ids[1]);
            Assert.Equal(string.Empty, ids[2]);

            Rename(ctx, "Target", "Renamed");
            AutoProfileEntity loaded = NewHolder(ctx, identity).AutoProfileColl.Single();

            Assert.Equal("Renamed", loaded.ProfileNames[0]);   // ID が勝つ
            Assert.Equal("Missing", loaded.ProfileNames[1]);
            Assert.Equal(AutoProfileEntity.NONE_STRING, loaded.ProfileNames[2]);
        }

        [Fact]
        public void TheAutoProfiles_WithoutTheRegistry_WriteNoIds_AndAnOldFileLoadsByName()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfileFile(ctx, "Target");
            string file = Path.Combine(ctx.AppData, "Auto Profiles.xml");

            AutoProfileHolder plain = NewHolder(ctx, null);
            var entity = new AutoProfileEntity(@"C:\p10s4d\w4b.exe", "Title");
            entity.ProfileNames[0] = "Target";
            plain.AutoProfileColl.Add(entity);
            Assert.True(plain.Save(file));
            Assert.Null(LoadXml(file).SelectSingleNode("/Programs/Program/ControllerProfileIds"));

            Assert.Equal("Target", NewHolder(ctx, Identity(ctx)).AutoProfileColl.Single().ProfileNames[0]);
        }

        // ---- 配線 ----

        [Fact]
        public void TheContainer_GivesTheRegistryToTheThreeSerializers()
        {
            // ServiceRegistration は、これらを具象クラスの Singleton として登録する。省略可能な引数に、登録済みの登録簿が渡る（渡らないと、ID を書かず・使わない）
            Assert.NotNull(TestHost.GetService<IProfileIdentityService>());
            Assert.NotNull(TestHost.GetService<AppSettingsXmlSerializer>());
            Assert.NotNull(TestHost.GetService<LinkedProfilesXmlSerializer>());
            Assert.NotNull(TestHost.GetService<AutoProfileHolder>());

            foreach (string relative in new[]
            {
                "DS4Windows/DS4Control/Services/AppSettingsXmlSerializer.cs",
                "DS4Windows/DS4Control/Services/LinkedProfilesXmlSerializer.cs",
                "DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs",
            })
            {
                string path = SourceFileLocator.Find(relative);
                if (path == null) continue;
                Assert.Contains("IProfileIdentityService", File.ReadAllText(path));
            }
        }
    }
}
