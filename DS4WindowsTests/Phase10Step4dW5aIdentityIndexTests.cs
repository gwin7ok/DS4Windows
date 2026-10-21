using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W5a（Phase10-Step4d-Plan.md §4 W5）: 登録簿（<see cref="IProfileIdentityService"/>）の<b>メモリの索引</b>。実行時に頻繁に引くため、
    /// ID ↔ 名前を索引から引く。索引の鮮度は、プロファイルのフォルダーの最終更新時刻で判定する（追加・削除・名前変更・コピーは、次の検索で反映される）。
    /// フォルダーの時刻が 1 秒以内に新しいあいだは、時刻の粗さで直後の変更を見逃さないよう、毎回作り直す。挙動（引数・戻り値）は W1 から変わらない（W1 のテストが守る）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW5aIdentityIndexTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;
        private readonly DateTime _old = DateTime.UtcNow.AddHours(-1);

        public Phase10Step4dW5aIdentityIndexTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4d_W5a_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private IProfileIdentityService NewService() => new ProfileIdentityService(new PathService(_dir));

        private string FileOf(string name) => Path.Combine(_profiles, name + ".xml");

        private static string NewId() => Guid.NewGuid().ToString("N");

        private string WriteProfile(string name, string id = null)
        {
            id ??= NewId();
            File.WriteAllText(FileOf(name), "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId><Other>1</Other></DS4Windows>");
            return id;
        }

        // フォルダーの更新時刻を、十分に古くする（索引を信用できる状態にする）
        private void MakeFolderOld() => Directory.SetLastWriteTimeUtc(_profiles, _old);   // 同じ時刻に戻す（索引は、時刻が同じあいだ使われる）

        // ---- フォルダーの変更を拾う ----

        [Fact]
        public void ARename_IsSeenByTheNextLookup()
        {
            string id = WriteProfile("Before");
            IProfileIdentityService service = NewService();
            Assert.Equal("Before", service.GetName(id));

            File.Move(FileOf("Before"), FileOf("After"));

            Assert.Equal("After", service.GetName(id));   // フォルダーの時刻が新しいあいだは、毎回作り直す
            Assert.Equal(id, service.GetId("After"));
            Assert.Null(service.GetId("Before"));
        }

        [Fact]
        public void AnAddedAndADeletedFile_AreSeenByTheNextLookup()
        {
            string kept = WriteProfile("Kept");
            string gone = WriteProfile("Gone");
            IProfileIdentityService service = NewService();
            Assert.Equal("Gone", service.GetName(gone));

            File.Delete(FileOf("Gone"));
            string added = WriteProfile("Added");

            Assert.Null(service.GetName(gone));
            Assert.Equal("Added", service.GetName(added));
            Assert.Equal("Kept", service.GetName(kept));
        }

        [Fact]
        public void TheLookups_AreByNameIgnoringCase_AndByIdExactly()
        {
            string id = WriteProfile("MixedCase");
            IProfileIdentityService service = NewService();

            Assert.Equal(id, service.GetId("mixedcase"));
            Assert.Equal("MixedCase", service.GetName(id));        // ファイル名のとおりの名前を返す
            Assert.Null(service.GetName(id.ToUpperInvariant()));    // ID は大文字小文字を区別する（GUID の N 形式は小文字）
        }

        // ---- 索引を使う（フォルダーが変わっていないあいだ）----

        [Fact]
        public void WhenTheFolderIsUnchanged_TheIndexIsUsed_NotTheFiles()
        {
            string id = WriteProfile("Cached");
            MakeFolderOld();
            IProfileIdentityService service = NewService();
            Assert.Equal("Cached", service.GetName(id));   // 索引を作る

            // フォルダーの時刻を変えずに、ファイルの中身だけを書き換える（外からの手編集）。索引を使うあいだは、反映されない
            string other = NewId();
            File.WriteAllText(FileOf("Cached"), "<DS4Windows><ProfileId>" + other + "</ProfileId></DS4Windows>");
            MakeFolderOld();

            Assert.Equal("Cached", service.GetName(id));
            Assert.Null(service.GetName(other));

            service.Refresh();   // 明示的に作り直す

            Assert.Null(service.GetName(id));
            Assert.Equal("Cached", service.GetName(other));
        }

        [Fact]
        public void WhenTheFolderChangesAgain_TheIndexIsRebuilt()
        {
            string id = WriteProfile("First");
            MakeFolderOld();
            IProfileIdentityService service = NewService();
            Assert.Equal("First", service.GetName(id));

            string added = WriteProfile("Second");   // フォルダーの時刻が新しくなる
            Assert.Equal("Second", service.GetName(added));
        }

        [Fact]
        public void TheLookupsAreFast_WhenTheFolderIsUnchanged()
        {
            string id = null;
            for (int i = 0; i < 40; i++)
            {
                string made = WriteProfile("P" + i);
                if (i == 20) id = made;
            }

            MakeFolderOld();
            IProfileIdentityService service = NewService();
            Assert.Equal("P20", service.GetName(id));   // 索引を作る

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
            {
                Assert.Equal("P20", service.GetName(id));
                Assert.Equal(id, service.GetId("p20"));
            }

            // 40 ファイルを毎回読むと、4000 回で数十秒かかる。索引なら、はるかに短い（余裕を見て 2 秒）
            Assert.True(watch.ElapsedMilliseconds < 2000, "索引を使っていない可能性がある: " + watch.ElapsedMilliseconds + "ms");
        }

        // ---- 自分の書き込みは、その場で索引に入る ----

        [Fact]
        public void EnsurePersisted_PutsTheNewIdIntoTheIndex_Immediately()
        {
            File.WriteAllText(FileOf("NoId"), "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><Other>1</Other></DS4Windows>");
            MakeFolderOld();
            IProfileIdentityService service = NewService();
            Assert.Null(service.GetId("NoId"));   // 索引を作る（ID なし）

            string id = service.EnsurePersisted("NoId");
            MakeFolderOld();   // 書き込みで、フォルダーの時刻が変わっていても、古く戻す（索引を使う状態）

            Assert.NotNull(id);
            Assert.Equal(id, service.GetId("NoId"));
            Assert.Equal("NoId", service.GetName(id));

            var doc = new XmlDocument();
            doc.Load(FileOf("NoId"));
            Assert.Equal(id, doc.SelectSingleNode("DS4Windows/ProfileId").InnerText);
        }

        [Fact]
        public void EnsureAllPersisted_RebuildsTheIndex_WithTheWrittenIds()
        {
            WriteProfile("A");
            File.WriteAllText(FileOf("B"), "<DS4Windows><Other>1</Other></DS4Windows>");   // ID なし
            IProfileIdentityService service = NewService();

            Assert.Empty(service.EnsureAllPersisted());

            string idB = service.GetId("B");
            Assert.NotNull(idB);
            Assert.Equal("B", service.GetName(idB));
            Assert.NotNull(service.GetId("A"));
        }

        // ---- 保存場所が変わったとき ----

        [Fact]
        public void AChangeOfTheProfilesFolder_RebuildsTheIndex()
        {
            string id = WriteProfile("InFirst");
            MakeFolderOld();
            var paths = new SwitchablePathService(_dir);
            IProfileIdentityService service = new ProfileIdentityService(paths);
            Assert.Equal("InFirst", service.GetName(id));

            string otherDir = Path.Combine(_dir, "Other");
            Directory.CreateDirectory(Path.Combine(otherDir, "Profiles"));
            string otherId = NewId();
            File.WriteAllText(Path.Combine(otherDir, "Profiles", "InSecond.xml"), "<DS4Windows><ProfileId>" + otherId + "</ProfileId></DS4Windows>");
            Directory.SetLastWriteTimeUtc(Path.Combine(otherDir, "Profiles"), DateTime.UtcNow.AddHours(-1));
            paths.Use(otherDir);

            Assert.Null(service.GetName(id));
            Assert.Equal("InSecond", service.GetName(otherId));
        }

        // ProfilesPath を切り替えられる、テスト用のパス窓口
        private sealed class SwitchablePathService : PathService, IPathService
        {
            private string _root;

            public SwitchablePathService(string root) : base(root) { _root = root; }

            public void Use(string root) => _root = root;

            public new string ProfilesPath => Path.Combine(_root, "Profiles");
        }
    }
}
