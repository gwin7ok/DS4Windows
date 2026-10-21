using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W2（Phase10-Step4d-Plan.md §3・§4 W2）: SA の固定 ID。<c>Actions.xml</c> の <c>Action</c> の <c>Id</c> 属性（GUID の N 形式）に保存する。
    /// 編集では保ち、名前の変更（古い名前を削除して作り直す経路）でも引き継ぎ、新規は新しい ID を振る。読み込みでは、ID のない・不正な・重複した SA に新しい ID を割り当てて、
    /// <c>Actions.xml</c> へ 1 回保存する（変更がなければ書き換えない）。参照（<c>&lt;ProfileActions&gt;</c> ほか）は、まだ名前のまま。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW2ActionIdTests
    {
        private sealed class Rig : IDisposable
        {
            public readonly PersistenceGoldenContext Context = new PersistenceGoldenContext();
            public readonly BackingStore Store = PersistenceHarness.NewStore();
            public readonly SpecialActionsXmlSerializer Xml;
            public readonly string Path;

            public Rig()
            {
                Path = System.IO.Path.Combine(Context.AppData, "Actions.xml");
                Store.m_Actions = Path;
                Xml = new SpecialActionsXmlSerializer(Store);
            }

            public void Dispose() => Context.Dispose();

            public XmlDocument Load()
            {
                var doc = new XmlDocument();
                doc.Load(Path);
                return doc;
            }

            public string IdInFile(string name)
                => Load().SelectSingleNode($"/Actions/Action[@Name='{name}']")?.Attributes["Id"]?.Value;

            public BackingStore Reload()
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.m_Actions = Path;
                Assert.True(new SpecialActionsXmlSerializer(store).LoadActions());
                return store;
            }
        }

        private static bool IsId(string id) => Guid.TryParseExact(id, "N", out _);

        // ---- 保存 ----

        [Fact]
        public void ANewAction_IsSavedWithAnId_AndEachActionHasItsOwn()
        {
            using var rig = new Rig();

            rig.Xml.SaveActionNew("First", "Cross", 1, "65", false);
            rig.Xml.SaveActionNew("Second", "Circle", 1, "66", false);

            string first = rig.IdInFile("First"), second = rig.IdInFile("Second");
            Assert.True(IsId(first));
            Assert.True(IsId(second));
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void TheIds_SurviveSavingAndReloading()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("First", "Cross", 1, "65", false);
            rig.Xml.SaveActionNew("Second", "Circle", 1, "66", false);

            BackingStore reloaded = rig.Reload();

            Assert.Equal(rig.IdInFile("First"), reloaded.actions.Single(a => a.name == "First").id);
            Assert.Equal(rig.IdInFile("Second"), reloaded.actions.Single(a => a.name == "Second").id);
        }

        [Fact]
        public void EditingAnAction_KeepsItsId()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("Edit", "Cross", 1, "65", false);
            string id = rig.IdInFile("Edit");

            rig.Xml.SaveActionNew("Edit", "Circle", 1, "66/67", true);

            Assert.Equal(id, rig.IdInFile("Edit"));
            Assert.Equal("Circle", rig.Load().SelectSingleNode("/Actions/Action[@Name='Edit']/Trigger").InnerText);
        }

        [Fact]
        public void RenamingAnAction_KeepsItsId_WhenTheCallerPassesIt()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("OldName", "Cross", 1, "65", false);
            string id = rig.IdInFile("OldName");

            // 編集画面の名前変更: 古い名前を削除して、新しい名前で作り直す（保存済みの SA の ID を渡す）
            rig.Xml.RemoveAction("OldName");
            rig.Xml.SaveActionNew("NewName", "Cross", 1, "65", false, actionId: id);

            Assert.Null(rig.IdInFile("OldName"));
            Assert.Equal(id, rig.IdInFile("NewName"));
        }

        [Fact]
        public void RenamingWithoutPassingTheId_GivesANewOne_AndABadIdIsIgnored()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("OldName", "Cross", 1, "65", false);
            string id = rig.IdInFile("OldName");
            rig.Xml.RemoveAction("OldName");

            rig.Xml.SaveActionNew("NoId", "Cross", 1, "65", false);
            rig.Xml.SaveActionNew("BadId", "Cross", 1, "65", false, actionId: "not-a-guid");

            Assert.NotEqual(id, rig.IdInFile("NoId"));
            Assert.True(IsId(rig.IdInFile("BadId")));
        }

        // ---- 読み込み ----

        private const string LegacyActions =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>" +
            "<Action Name=\"A\"><Trigger>Cross</Trigger><Type>Macro</Type><Details>65</Details></Action>" +
            "<Action Name=\"B\"><Trigger>Circle</Trigger><Type>Macro</Type><Details>66</Details></Action>" +
            "</Actions>";

        [Fact]
        public void AFileWithoutIds_GetsThemOnLoad_AndIsSavedOnce()
        {
            using var rig = new Rig();
            File.WriteAllText(rig.Path, LegacyActions);

            BackingStore loaded = rig.Reload();

            string a = rig.IdInFile("A"), b = rig.IdInFile("B");
            Assert.True(IsId(a));
            Assert.True(IsId(b));
            Assert.NotEqual(a, b);
            Assert.Equal(a, loaded.actions.Single(x => x.name == "A").id);   // メモリとファイルが同じ
            Assert.Equal("65", rig.Load().SelectSingleNode("/Actions/Action[@Name='A']/Details").InnerText);   // 内容は保たれる
        }

        [Fact]
        public void LoadingAFileThatHasIds_DoesNotRewriteIt()
        {
            using var rig = new Rig();
            File.WriteAllText(rig.Path, LegacyActions);
            rig.Reload();   // ID が書かれる
            string before = File.ReadAllText(rig.Path);
            DateTime stamp = File.GetLastWriteTimeUtc(rig.Path);

            rig.Reload();

            Assert.Equal(before, File.ReadAllText(rig.Path));
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(rig.Path));
        }

        [Fact]
        public void ADuplicateId_GetsANewOneForTheLaterAction_AndAnInvalidIdIsReplaced()
        {
            using var rig = new Rig();
            string id = Guid.NewGuid().ToString("N");
            File.WriteAllText(rig.Path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions>" +
                $"<Action Name=\"First\" Id=\"{id}\"><Trigger>Cross</Trigger><Type>Macro</Type><Details>65</Details></Action>" +
                $"<Action Name=\"Second\" Id=\"{id}\"><Trigger>Circle</Trigger><Type>Macro</Type><Details>66</Details></Action>" +
                "<Action Name=\"Third\" Id=\"nope\"><Trigger>Square</Trigger><Type>Macro</Type><Details>67</Details></Action>" +
                "</Actions>");

            rig.Reload();

            Assert.Equal(id, rig.IdInFile("First"));        // ファイルの順で先のものが保つ
            Assert.NotEqual(id, rig.IdInFile("Second"));
            Assert.True(IsId(rig.IdInFile("Second")));
            Assert.True(IsId(rig.IdInFile("Third")));
            Assert.Equal(3, new[] { rig.IdInFile("First"), rig.IdInFile("Second"), rig.IdInFile("Third") }.Distinct().Count());
        }

        [Fact]
        public void TheDefaultActionOfANewInstall_HasAnId()
        {
            using var rig = new Rig();   // Actions.xml がない

            BackingStore loaded = rig.Reload();

            string id = rig.IdInFile("Disconnect Controller");
            Assert.True(IsId(id));
            Assert.Equal(id, loaded.actions.Single(a => a.name == "Disconnect Controller").id);
        }

        [Fact]
        public void AnOldReaderIgnoresTheIdAttribute()
        {
            // 新しい書式のファイルは、Id 属性を含む。古いバージョン相当の読み方（Name・Trigger・Type・Details だけを読む）でも、読める
            using var rig = new Rig();
            rig.Xml.SaveActionNew("Compat", "Cross", 1, "65", false);

            XmlNode node = rig.Load().SelectSingleNode("/Actions/Action[@Name='Compat']");

            Assert.Equal("Cross", node.SelectSingleNode("Trigger").InnerText);
            Assert.Equal("Macro", node.SelectSingleNode("Type").InnerText);
            Assert.Equal("65", node.SelectSingleNode("Details").InnerText);
        }

        // ---- 編集画面 ----

        [Fact]
        public void TheEditor_CarriesTheSavedActionsId_ToTheActionItWrites()
        {
            var saved = new SpecialAction("Saved", "Cross", "Macro", "65");
            var vm = new SpecialActEditorViewModel(0, saved, new RecordingSpecialActionRepository());
            var written = new SpecialAction("null", "null", "null", "null");

            vm.SetAction(written);

            Assert.Equal(saved.id, written.id);
        }

        [Fact]
        public void ANewActionInTheEditor_KeepsTheNewIdItWasCreatedWith()
        {
            var vm = new SpecialActEditorViewModel(0, null, new RecordingSpecialActionRepository());
            var written = new SpecialAction("null", "null", "null", "null");
            string created = written.id;

            vm.SetAction(written);

            Assert.Equal(created, written.id);
            Assert.True(IsId(written.id));
        }

        [Fact]
        public void EveryTypeEditor_PassesTheActionsIdToTheRepository()
        {
            // 8 か所の保存の呼び出しが、actionId: action.id を渡す（名前の変更で ID が変わらないようにするため）
            string folder = SourceFileLocator.Find("DS4Windows/DS4Forms/ViewModels/SpecialActions/MacroViewModel.cs");
            if (folder == null) return;

            string dir = Path.GetDirectoryName(folder);
            int calls = 0, withId = 0;
            foreach (string file in Directory.GetFiles(dir, "*.cs"))
            {
                string text = File.ReadAllText(file);
                int index = 0;
                while ((index = text.IndexOf("specialActionRepository.SaveAction(", index, StringComparison.Ordinal)) >= 0)
                {
                    calls++;
                    int end = text.IndexOf(");", index, StringComparison.Ordinal);
                    if (text.Substring(index, end - index).Contains("actionId: action.id")) withId++;
                    index = end;
                }
            }

            Assert.Equal(8, calls);
            Assert.Equal(calls, withId);
        }
    }
}
