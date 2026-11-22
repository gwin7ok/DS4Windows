using System;
using System.IO;
using System.Linq;
using DS4Windows.Core.Profiles;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// メイン画面のプロファイルの取り込み・書き出し・削除（<see cref="ProfileSelectionCoordinator"/>）が、
    /// 窓なしで動くこと（偽の <c>IDialogService</c> と一時フォルダーで確かめる）。
    /// </summary>
    public class ProfileFileCommandsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ProfCmd_" + Guid.NewGuid().ToString("N"));
        private readonly TempPathService _path;
        private readonly FakeDialogService _dialogs = new FakeDialogService();
        private readonly DS4Windows.Core.Persistence.ProfileIdentityService _identity;

        public ProfileFileCommandsTests()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _path = new TempPathService(_dir);
            _identity = new DS4Windows.Core.Persistence.ProfileIdentityService(new TempPathService(Path.Combine(_dir, "Profiles")));
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private ProfileSelectionCoordinator Create()
        {
            return new ProfileSelectionCoordinator(
                TestHost.GetService<DS4Windows.UI.Abstractions.IEditorWindowFactory>(),
                TestHost.GetService<DS4Windows.Core.Abstractions.IProfileRepository>(),
                TestHost.GetService<DS4Windows.Processing.Abstractions.IProfileSlotApplier>(),
                TestHost.GetService<DS4Windows.Core.Abstractions.IAppSettingsService>(),
                TestHost.GetService<DS4Windows.Core.Abstractions.IEnvironmentService>(),
                TestHost.GetService<DS4Windows.Processing.Abstractions.IControllerSlotQuery>(),
                _path, _dialogs, _identity);
        }

        private ProfileList CreateList(params string[] names)
        {
            foreach (string name in names) File.WriteAllText(Path.Combine(_dir, "Profiles", name + ".xml"), "<x/>");
            var list = new ProfileList(_path);
            list.Refresh();
            return list;
        }

        [Fact]
        public void ImportProfile_CopiesTheChosenFileAndAddsItToTheList()
        {
            string source = Path.Combine(_dir, "Incoming.xml");
            File.WriteAllText(source, "<x/>");
            _dialogs.OpenFileAnswer = source;
            var list = CreateList();

            Create().ImportProfile(list);

            Assert.True(File.Exists(Path.Combine(_dir, "Profiles", "Incoming.xml")));
            Assert.Contains(list.ProfileListCol, p => p.Name == "Incoming");
        }

        private const string OldId = "11111111111111111111111111111111";
        private const string ImportedId = "22222222222222222222222222222222";

        private static string Xml(string marker, string id) => "<Profile><ProfileId>" + id + "</ProfileId><" + marker + "/></Profile>";

        private string Write(string folder, string name, string content)
        {
            string path = Path.Combine(folder, name + ".xml");
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public void ImportProfile_WhenTheSameNameExists_AndTheUserSaysNo_ChangesNothing()
        {
            var list = CreateList();
            string existing = Write(Path.Combine(_dir, "Profiles"), "Dup", Xml("old", OldId));
            string source = Write(_dir, "Dup", Xml("new", ImportedId));
            _dialogs.OpenFileAnswer = source;
            _dialogs.ThreeWayAnswer = DS4Windows.UI.Abstractions.DialogChoice.Second;

            Create().ImportProfile(list);

            Assert.Single(_dialogs.ThreeWayMessages);
            Assert.Contains("Dup", _dialogs.ThreeWayMessages[0]);
            Assert.Equal(Xml("old", OldId), File.ReadAllText(existing));
        }

        [Fact]
        public void ImportProfile_WhenTheDialogIsClosed_ChangesNothing()
        {
            var list = CreateList();
            string existing = Write(Path.Combine(_dir, "Profiles"), "Dup", Xml("old", OldId));
            _dialogs.OpenFileAnswer = Write(_dir, "Dup", Xml("new", ImportedId));
            _dialogs.ThreeWayAnswer = DS4Windows.UI.Abstractions.DialogChoice.Cancelled;

            Create().ImportProfile(list);

            Assert.Equal(Xml("old", OldId), File.ReadAllText(existing));
        }

        [Fact]
        public void ImportProfile_Overwrite_TakesTheImportedSettings_ButKeepsTheStoredId_AndLeavesTheSourceUntouched()
        {
            var list = CreateList();
            string existing = Write(Path.Combine(_dir, "Profiles"), "Dup", Xml("old", OldId));
            string sourceContent = Xml("new", ImportedId);
            string source = Write(_dir, "Dup", sourceContent);
            _dialogs.OpenFileAnswer = source;
            _dialogs.ThreeWayAnswer = DS4Windows.UI.Abstractions.DialogChoice.First;

            Create().ImportProfile(list);

            string saved = File.ReadAllText(existing);
            Assert.Contains("<new", saved);
            Assert.DoesNotContain("<old", saved);
            Assert.Equal(OldId, new DS4Windows.Core.Persistence.ProfileIdentityService(new TempPathService(Path.Combine(_dir, "Profiles"))).GetId("Dup"));
            Assert.Equal(sourceContent, File.ReadAllText(source));
            Assert.Single(list.ProfileListCol, p => p.Name == "Dup");
        }

        [Fact]
        public void ImportProfile_Rename_SavesUnderTheNewNameWithANewId_AndKeepsTheOriginal()
        {
            var list = CreateList();
            string existing = Write(Path.Combine(_dir, "Profiles"), "Dup", Xml("old", OldId));
            string source = Write(_dir, "Dup", Xml("new", ImportedId));
            _dialogs.OpenFileAnswer = source;
            _dialogs.ThreeWayAnswer = DS4Windows.UI.Abstractions.DialogChoice.Third;
            _dialogs.PromptAnswers.Enqueue("Dup");       // 重複: 入力へ戻る
            _dialogs.PromptAnswers.Enqueue("bad/name");   // 使えない文字: 入力へ戻る
            _dialogs.PromptAnswers.Enqueue("Fresh");

            Create().ImportProfile(list);

            Assert.Equal(3, _dialogs.PromptMessages.Count);
            Assert.Equal(2, _dialogs.Messages.Count);
            Assert.Equal(Xml("old", OldId), File.ReadAllText(existing));
            string id = new DS4Windows.Core.Persistence.ProfileIdentityService(new TempPathService(Path.Combine(_dir, "Profiles"))).GetId("Fresh");
            Assert.NotNull(id);
            Assert.NotEqual(OldId, id);
            Assert.NotEqual(ImportedId, id);
            Assert.Contains("<new", File.ReadAllText(Path.Combine(_dir, "Profiles", "Fresh.xml")));
            Assert.Contains(list.ProfileListCol, p => p.Name == "Fresh");
        }

        [Fact]
        public void ImportProfile_Rename_WhenThePromptIsCancelled_ChangesNothing()
        {
            var list = CreateList();
            Write(Path.Combine(_dir, "Profiles"), "Dup", Xml("old", OldId));
            _dialogs.OpenFileAnswer = Write(_dir, "Dup", Xml("new", ImportedId));
            _dialogs.ThreeWayAnswer = DS4Windows.UI.Abstractions.DialogChoice.Third;

            Create().ImportProfile(list);

            Assert.Single(Directory.GetFiles(Path.Combine(_dir, "Profiles")));
        }

        [Fact]
        public void ImportProfile_NewName_KeepsTheImportedId()
        {
            var list = CreateList();
            _dialogs.OpenFileAnswer = Write(_dir, "Incoming", Xml("new", ImportedId));

            Create().ImportProfile(list);

            Assert.Equal(ImportedId, new DS4Windows.Core.Persistence.ProfileIdentityService(new TempPathService(Path.Combine(_dir, "Profiles"))).GetId("Incoming"));
        }

        [Fact]
        public void ImportProfile_NewName_WhenTheImportedIdBelongsToAnotherProfile_GetsANewId()
        {
            var list = CreateList();
            Write(Path.Combine(_dir, "Profiles"), "Other", Xml("old", ImportedId));
            _dialogs.OpenFileAnswer = Write(_dir, "Incoming", Xml("new", ImportedId));

            Create().ImportProfile(list);

            var identity = new DS4Windows.Core.Persistence.ProfileIdentityService(new TempPathService(Path.Combine(_dir, "Profiles")));
            Assert.Equal(ImportedId, identity.GetId("Other"));
            string id = identity.GetId("Incoming");
            Assert.NotNull(id);
            Assert.NotEqual(ImportedId, id);
        }

        [Fact]
        public void ImportProfile_FromTheProfilesFolderItself_DoesNotAsk()
        {
            var list = CreateList();
            string inPlace = Path.Combine(_dir, "Profiles", "Same.xml");
            File.WriteAllText(inPlace, "<x/>");
            _dialogs.OpenFileAnswer = inPlace;

            Create().ImportProfile(list);

            Assert.Empty(_dialogs.Confirmations);
            Assert.Contains(list.ProfileListCol, p => p.Name == "Same");
        }

        [Fact]
        public void ImportProfile_WhenCancelled_ChangesNothing()
        {
            _dialogs.OpenFileAnswer = null;
            var list = CreateList();

            Create().ImportProfile(list);

            Assert.Empty(list.ProfileListCol);
        }

        [Fact]
        public void ExportProfile_CopiesTheSelectedProfileToTheChosenFile()
        {
            var list = CreateList("Alpha");
            string target = Path.Combine(_dir, "Exported.xml");
            _dialogs.SaveFileAnswer = target;

            Create().ExportProfile(list, 0);

            Assert.True(File.Exists(target));
        }

        [Fact]
        public void ExportProfile_OffersTheProfileNameWithXmlAsTheDefaultFileName()
        {
            var list = CreateList("Alpha");
            _dialogs.SaveFileAnswer = null;

            Create().ExportProfile(list, 0);

            Assert.Equal("Alpha.xml", _dialogs.LastSaveOptions.FileName);
        }

        [Fact]
        public void DeleteProfile_WhenDeclined_KeepsTheFileAndTheListItem()
        {
            var list = CreateList("Beta");
            _dialogs.ConfirmAnswer = false;

            Create().DeleteProfile(list, 0);

            Assert.True(File.Exists(Path.Combine(_dir, "Profiles", "Beta.xml")));
            Assert.Single(list.ProfileListCol);
            Assert.Contains("Beta", _dialogs.Confirmations.Single());
        }

        [Fact]
        public void DeleteProfile_WhenAccepted_RemovesTheFileAndTheListItem()
        {
            var list = CreateList("Gamma");
            _dialogs.ConfirmAnswer = true;

            Create().DeleteProfile(list, 0);

            Assert.False(File.Exists(Path.Combine(_dir, "Profiles", "Gamma.xml")));
            Assert.Empty(list.ProfileListCol);
        }

        [Fact]
        public void WithNothingSelected_DoNothing()
        {
            var list = CreateList("Delta");
            var part = Create();

            part.ExportProfile(list, -1);
            part.DeleteProfile(list, -1);

            Assert.Empty(_dialogs.Confirmations);
            Assert.Single(list.ProfileListCol);
        }
    }
}
