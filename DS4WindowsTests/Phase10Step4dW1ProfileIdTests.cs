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
    /// Phase10-Step4d W1（Phase10-Step4d-Plan.md §3・§4 W1）: プロファイルの固定 ID。プロファイル XML のルート直下の先頭に <c>&lt;ProfileId&gt;</c>（GUID の N 形式）を保存し、
    /// 上書き保存では保ち、別名で保存・新規作成では新しい ID を振る。名前の変更（ファイル名の変更）では変わらない。
    /// 登録簿（<see cref="IProfileIdentityService"/>）は、起動時に、ID のないファイルへ ID を書き、ファイルのコピーで重複した ID に新しい ID を振って書く。参照は、まだ名前のまま。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW1ProfileIdTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;

        public Phase10Step4dW1ProfileIdTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4d_W1_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private IProfileIdentityService NewService() => new ProfileIdentityService(new PathService(_dir));

        private string FileOf(string name) => Path.Combine(_profiles, name + ".xml");

        private void WriteProfile(string name, string id = null, string body = "<Other>1</Other>")
        {
            string idNode = id == null ? string.Empty : "<ProfileId>" + id + "</ProfileId>";
            File.WriteAllText(FileOf(name), "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows>" + idNode + body + "</DS4Windows>");
        }

        private static string NewId() => Guid.NewGuid().ToString("N");

        private string ReadId(string name)
        {
            var doc = new XmlDocument();
            doc.Load(FileOf(name));
            return doc.SelectSingleNode("DS4Windows/ProfileId")?.InnerText;
        }

        // ---- 保存 ----

        [Fact]
        public void ASavedProfile_HasAValidId_AsTheFirstChildOfTheRoot()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "Step4dW1First");

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode first = doc.DocumentElement.FirstChild;

            Assert.Equal("ProfileId", first.Name);
            Assert.True(Guid.TryParseExact(first.InnerText, "N", out _), "ID が GUID の N 形式でない: " + first.InnerText);
        }

        [Fact]
        public void OverwritingAProfile_KeepsItsId_AndSavingUnderANewName_GetsANewOne()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();

            string first = IdOf(PersistenceHarness.SaveProfile(ctx, store, "Step4dW1Keep"));
            string overwritten = IdOf(PersistenceHarness.SaveProfile(ctx, store, "Step4dW1Keep"));
            string other = IdOf(PersistenceHarness.SaveProfile(ctx, store, "Step4dW1Other"));

            Assert.Equal(first, overwritten);
            Assert.NotEqual(first, other);
        }

        private static string IdOf(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectSingleNode("DS4Windows/ProfileId").InnerText;
        }

        [Fact]
        public void AFileThatAlreadyHasAnId_KeepsItWhenTheSlotIsSavedOverIt()
        {
            using var ctx = new PersistenceGoldenContext();
            string id = NewId();
            File.WriteAllText(Path.Combine(ctx.AppData, "Profiles", "Step4dW1Existing.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId></DS4Windows>");

            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "Step4dW1Existing");

            Assert.Equal(id, IdOf(xml));
        }

        [Fact]
        public void ARenamedProfile_KeepsItsId_BecauseTheIdIsInsideTheFile()
        {
            string id = NewId();
            WriteProfile("Before", id);

            File.Move(FileOf("Before"), FileOf("After"));

            Assert.Equal(id, NewService().GetId("After"));
            Assert.Equal("After", NewService().GetName(id));
            Assert.Null(NewService().GetName(NewId()));
        }

        [Fact]
        public void AProfileWithAnId_StillLoads()
        {
            using var ctx = new PersistenceGoldenContext();
            string saved = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "Step4dW1Load");

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, saved, "Step4dW1Loaded");

            Assert.NotNull(loaded);
        }

        // ---- 起動時の割り当て ----

        [Fact]
        public void EnsureAllPersisted_WritesAnIdToEveryProfileWithoutOne_AndKeepsTheRestOfTheFile()
        {
            string keep = NewId();
            WriteProfile("NoId1", null, "<Other>one</Other>");
            WriteProfile("NoId2", null, "<Other>two</Other>");
            WriteProfile("HasId", keep, "<Other>three</Other>");

            IReadOnlyList<string> failed = NewService().EnsureAllPersisted();

            Assert.Empty(failed);
            Assert.Equal(keep, ReadId("HasId"));
            string a = ReadId("NoId1"), b = ReadId("NoId2");
            Assert.True(Guid.TryParseExact(a, "N", out _));
            Assert.True(Guid.TryParseExact(b, "N", out _));
            Assert.NotEqual(a, b);

            var doc = new XmlDocument();
            doc.Load(FileOf("NoId1"));
            Assert.Equal("one", doc.SelectSingleNode("DS4Windows/Other").InnerText);
            Assert.Equal("ProfileId", doc.DocumentElement.FirstChild.Name);   // ルート直下の先頭
        }

        [Fact]
        public void EnsureAllPersisted_IsStable_TheSecondRunChangesNothing()
        {
            WriteProfile("A");
            WriteProfile("B");
            NewService().EnsureAllPersisted();
            string a = ReadId("A"), b = ReadId("B");
            DateTime stamp = File.GetLastWriteTimeUtc(FileOf("A"));

            IReadOnlyList<string> failed = NewService().EnsureAllPersisted();

            Assert.Empty(failed);
            Assert.Equal(a, ReadId("A"));
            Assert.Equal(b, ReadId("B"));
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(FileOf("A")));   // 書き換えない
        }

        [Fact]
        public void ACopiedProfile_GetsANewId_AndTheOlderFileKeepsTheOriginal()
        {
            string id = NewId();
            WriteProfile("Original", id);
            WriteProfile("Copy", id);
            File.SetCreationTimeUtc(FileOf("Original"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetCreationTimeUtc(FileOf("Copy"), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Empty(NewService().EnsureAllPersisted());

            Assert.Equal(id, ReadId("Original"));
            Assert.NotEqual(id, ReadId("Copy"));
            Assert.True(Guid.TryParseExact(ReadId("Copy"), "N", out _));
        }

        [Fact]
        public void WhenTheCreationTimesAreTheSame_TheFirstNameKeepsTheId()
        {
            string id = NewId();
            DateTime same = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            WriteProfile("b_second", id);
            WriteProfile("a_first", id);
            File.SetCreationTimeUtc(FileOf("b_second"), same);
            File.SetCreationTimeUtc(FileOf("a_first"), same);

            NewService().EnsureAllPersisted();

            Assert.Equal(id, ReadId("a_first"));
            Assert.NotEqual(id, ReadId("b_second"));
        }

        [Fact]
        public void AnUnwritableOrBrokenFile_IsReported_AndTheOthersAreStillDone()
        {
            WriteProfile("Fine");
            WriteProfile("ReadOnly");
            File.WriteAllText(FileOf("Broken"), "<DS4Windows><unclosed>");
            File.SetAttributes(FileOf("ReadOnly"), FileAttributes.ReadOnly);
            try
            {
                IReadOnlyList<string> failed = NewService().EnsureAllPersisted();

                Assert.Equal(new[] { "Broken", "ReadOnly" }, failed.OrderBy(x => x).ToArray());
                Assert.True(Guid.TryParseExact(ReadId("Fine"), "N", out _));
            }
            finally
            {
                File.SetAttributes(FileOf("ReadOnly"), FileAttributes.Normal);
            }
        }

        [Fact]
        public void WithoutAProfilesFolder_NothingHappens()
        {
            Directory.Delete(_profiles, true);

            Assert.Empty(NewService().EnsureAllPersisted());
        }

        // ---- 検索と解決 ----

        [Fact]
        public void EnsurePersisted_WritesAnIdOnce_AndReturnsNullForAMissingFile()
        {
            WriteProfile("Old");
            IProfileIdentityService service = NewService();

            string id = service.EnsurePersisted("Old");

            Assert.True(Guid.TryParseExact(id, "N", out _));
            Assert.Equal(id, ReadId("Old"));
            Assert.Equal(id, service.EnsurePersisted("Old"));   // 2 回目は同じ ID
            Assert.Null(service.EnsurePersisted("Missing"));
            Assert.Null(service.EnsurePersisted(" "));
        }

        [Fact]
        public void TryResolve_FindsTheProfileById_ThenFallsBackToTheName()
        {
            string id = NewId();
            WriteProfile("Renamed", id);
            WriteProfile("OnlyByName");
            IProfileIdentityService service = NewService();

            Assert.True(service.TryResolve(id, "OldName", out string byId));      // ID が優先。名前が古くても見つかる
            Assert.Equal("Renamed", byId);

            Assert.True(service.TryResolve(NewId(), "OnlyByName", out string byName));   // ID が見つからなければ名前
            Assert.Equal("OnlyByName", byName);

            Assert.True(service.TryResolve(null, "OnlyByName", out string noId));
            Assert.Equal("OnlyByName", noId);

            Assert.False(service.TryResolve(NewId(), "Missing", out string none));
            Assert.Null(none);
        }

        [Fact]
        public void AnIdOfTheWrongFormat_IsIgnored()
        {
            WriteProfile("Bad", "not-a-guid");

            Assert.Null(NewService().GetId("Bad"));
            Assert.Null(NewService().GetName("not-a-guid"));

            // 形式が不正な ID は「ID なし」として、起動時に正しい ID に置き換える
            Assert.Empty(NewService().EnsureAllPersisted());
            Assert.True(Guid.TryParseExact(ReadId("Bad"), "N", out _));
        }

        // ---- 配線 ----

        [Fact]
        public void TheService_IsRegistered_AndTheStartupCallsItBeforeTheMainWindowIsCreated()
        {
            Assert.NotNull(TestHost.GetService<IProfileIdentityService>());

            string app = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (app == null) return;

            string source = File.ReadAllText(app);
            int ensure = source.IndexOf("_profileIdentityService.EnsureAllPersisted()", StringComparison.Ordinal);
            int window = source.IndexOf("ResolvePostHostService<IMainWindowFactory>().Create(parser)", StringComparison.Ordinal);
            Assert.True(ensure > 0 && window > ensure, "プロファイルの ID の割り当てが、メイン画面の生成より前にない");
        }
    }
}
