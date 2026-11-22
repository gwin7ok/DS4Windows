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

        public ProfileFileCommandsTests()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _path = new TempPathService(_dir);
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
                _path, _dialogs);
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

        [Fact]
        public void ImportProfile_WhenTheSameNameExists_AsksBeforeOverwriting_AndKeepsTheOldFileIfDeclined()
        {
            var list = CreateList("Dup");
            string existing = Path.Combine(_dir, "Profiles", "Dup.xml");
            File.WriteAllText(existing, "<old/>");
            string source = Path.Combine(_dir, "Dup.xml");
            File.WriteAllText(source, "<new/>");
            _dialogs.OpenFileAnswer = source;
            _dialogs.ConfirmAnswer = false;

            Create().ImportProfile(list);

            Assert.Single(_dialogs.Confirmations);
            Assert.Contains("Dup", _dialogs.Confirmations[0]);
            Assert.Equal("<old/>", File.ReadAllText(existing));
        }

        [Fact]
        public void ImportProfile_WhenTheSameNameExists_OverwritesAfterTheUserAgrees_WithoutDuplicatingTheListEntry()
        {
            var list = CreateList("Dup");
            string existing = Path.Combine(_dir, "Profiles", "Dup.xml");
            File.WriteAllText(existing, "<old/>");
            string source = Path.Combine(_dir, "Dup.xml");
            File.WriteAllText(source, "<new/>");
            _dialogs.OpenFileAnswer = source;
            _dialogs.ConfirmAnswer = true;

            Create().ImportProfile(list);

            Assert.Single(_dialogs.Confirmations);
            Assert.Equal("<new/>", File.ReadAllText(existing));
            Assert.Single(list.ProfileListCol, p => p.Name == "Dup");
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
