using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W5b（Phase10-Step4d-Plan.md §4 W5、ユーザー決定: 案 1）: スロットごとのプロファイルの<b>保管を ID に</b>し、名前を ID から引く窓口（<see cref="ProfileSlotNames"/>）。
    /// 書くときは名前の控えと ID を保存し、読むときは ID から現在の名前を返す（名前を変えても、保管を書き換えなくてよい）。ID が引けないとき（削除、未登録）は名前の控え。
    /// 登録簿がなければ、従来の <c>string[]</c> と同じに動く。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW5bProfileSlotNamesTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;
        private readonly DateTime _old = DateTime.UtcNow.AddHours(-1);

        public Phase10Step4dW5bProfileSlotNamesTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4d_W5b_" + Guid.NewGuid().ToString("N"));
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

        // ---- 登録簿なし: 従来の配列と同じ ----

        [Fact]
        public void WithoutTheRegistry_ItBehavesLikeAStringArray()
        {
            var slots = new ProfileSlotNames(4);

            Assert.Equal(4, slots.Length);
            Assert.All(slots, name => Assert.Equal(string.Empty, name));

            slots[1] = "Second";
            slots[2] = null;

            Assert.Equal("Second", slots[1]);
            Assert.Null(slots[2]);
            Assert.Null(slots.IdAt(1));
            Assert.Equal(new[] { string.Empty, "Second", null, string.Empty }, slots.ToArray());
            Assert.Equal(slots.ToArray(), slots.ToList());
            Assert.Throws<IndexOutOfRangeException>(() => slots[4]);
        }

        // ---- 登録簿あり: 保管は ID、名前は引く ----

        [Fact]
        public void Writing_StoresTheId_AndReadingFollowsARenameWithoutAnyWrite()
        {
            string id = WriteProfile("Before");
            var slots = new ProfileSlotNames(2);
            slots.AttachIdentity(NewIdentity());

            slots[0] = "Before";
            Assert.Equal(id, slots.IdAt(0));

            File.Move(FileOf("Before"), FileOf("After"));   // 名前を変える（配列には何もしない）

            Assert.Equal("After", slots[0]);   // 現在の名前
            Assert.Equal(id, slots.IdAt(0));
        }

        [Fact]
        public void AnUnknownOrBlankName_IsKeptAsIs_WithoutAnId()
        {
            WriteProfile("Exists");
            var slots = new ProfileSlotNames(3);
            slots.AttachIdentity(NewIdentity());

            slots[0] = "NotCreatedYet";
            slots[1] = string.Empty;
            slots[2] = "(none)";

            Assert.Equal("NotCreatedYet", slots[0]);
            Assert.Null(slots.IdAt(0));
            Assert.Equal(string.Empty, slots[1]);
            Assert.Equal("(none)", slots[2]);
            Assert.Null(slots.IdAt(2));
        }

        [Fact]
        public void ADeletedProfile_KeepsTheLastName()
        {
            WriteProfile("Doomed");
            var slots = new ProfileSlotNames(1);
            slots.AttachIdentity(NewIdentity());
            slots[0] = "Doomed";

            File.Delete(FileOf("Doomed"));

            Assert.Equal("Doomed", slots[0]);   // これまでと同じ（最後の名前が残る）
            Assert.Null(slots.IdAt(0));         // 無効になった ID は外れる
        }

        [Fact]
        public void ANameSetBeforeTheProfileExisted_IsTiedToItLater()
        {
            var slots = new ProfileSlotNames(1);
            slots.AttachIdentity(NewIdentity());
            slots[0] = "Later";
            Assert.Null(slots.IdAt(0));

            string id = WriteProfile("Later");   // あとから作られた

            Assert.Equal("Later", slots[0]);
            Assert.Equal(id, slots.IdAt(0));     // 読むときに、名前から ID を引き直して結ぶ
            File.Move(FileOf("Later"), FileOf("Renamed"));
            Assert.Equal("Renamed", slots[0]);
        }

        [Fact]
        public void ARecreatedProfileOfTheSameName_IsTiedToTheNewOne()
        {
            string oldId = WriteProfile("Same");
            var slots = new ProfileSlotNames(1);
            slots.AttachIdentity(NewIdentity());
            slots[0] = "Same";
            File.Delete(FileOf("Same"));
            Assert.Equal("Same", slots[0]);   // ID が無効になり、外れる

            string newId = WriteProfile("Same");   // 同じ名前で作り直した（別の ID）

            Assert.Equal("Same", slots[0]);
            Assert.Equal(newId, slots.IdAt(0));
            Assert.NotEqual(oldId, newId);
        }

        [Fact]
        public void AttachingTheRegistry_ResolvesTheNamesAlreadyThere_AndDetachingGoesBackToNames()
        {
            string id = WriteProfile("Existing");
            var slots = new ProfileSlotNames(1);
            slots[0] = "Existing";
            Assert.Null(slots.IdAt(0));

            slots.AttachIdentity(NewIdentity());
            Assert.Equal(id, slots.IdAt(0));

            slots.AttachIdentity(null);
            Assert.Null(slots.IdAt(0));
            Assert.Equal("Existing", slots[0]);
        }

        // ---- BackingStore ----

        [Fact]
        public void TheBackingStore_TiesTheThreeSlotArraysToTheRegistry()
        {
            string id = WriteProfile("Shared");
            BackingStore store = PersistenceHarness.NewStore();
            store.AttachProfileIdentity(NewIdentity());

            store.profilePath[0] = "Shared";
            store.olderProfilePath[0] = "Shared";
            store.selectedProfile[0] = "Shared";
            File.Move(FileOf("Shared"), FileOf("Moved"));

            Assert.Equal("Moved", store.profilePath[0]);
            Assert.Equal("Moved", store.olderProfilePath[0]);
            Assert.Equal("Moved", store.selectedProfile[0]);
            Assert.Equal(id, store.profilePath.IdAt(0));
        }

        [Fact]
        public void TheContainer_TiesTheRegistryToTheBackingStore()
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            if (path == null) return;

            string source = File.ReadAllText(path);
            Assert.Contains("AttachProfileIdentity(service)", source);
            Assert.NotNull(TestHost.GetService<IProfileIdentityService>());
        }

        // ---- 排他 ----

        [Fact]
        public void WhenTheIndexIsFresh_ALookupDoesNotWaitForTheXmlLock()
        {
            string id = WriteProfile("Quick");
            Directory.SetLastWriteTimeUtc(_profiles, _old);
            IProfileIdentityService identity = NewIdentity();
            Assert.Equal("Quick", identity.GetName(id));   // 索引を作る

            using var holding = new ManualResetEventSlim(false);
            using var release = new ManualResetEventSlim(false);
            Task holder = Task.Run(() =>
            {
                lock (ProfileXmlStore.XmlIoLock)
                {
                    holding.Set();
                    release.Wait(TimeSpan.FromSeconds(10));
                }
            });
            Assert.True(holding.Wait(TimeSpan.FromSeconds(5)));

            try
            {
                // 別のスレッドが XML の排他を持っていても、索引が有効なら、待たずに引ける（長い XML の読み書きを、実行時の読み取りが待たない）
                Task<string> lookup = Task.Run(() => identity.GetName(id));
                Assert.True(lookup.Wait(TimeSpan.FromSeconds(3)), "索引が有効なのに、XML の排他を待った");
                Assert.Equal("Quick", lookup.Result);
            }
            finally
            {
                release.Set();
                holder.Wait(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public void ConcurrentReadsAndWrites_DoNotThrow()
        {
            string id = WriteProfile("Busy");
            var slots = new ProfileSlotNames(4);
            slots.AttachIdentity(NewIdentity());

            Parallel.For(0, 4, worker =>
            {
                for (int i = 0; i < 200; i++)
                {
                    slots[worker] = i % 2 == 0 ? "Busy" : "Elsewhere";
                    string read = slots[worker];
                    Assert.NotNull(read);
                }
            });

            Assert.NotNull(id);
        }
    }
}
