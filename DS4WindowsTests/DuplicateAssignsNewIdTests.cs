using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-8: アプリの「複製（Duplicate）」は、ファイルをコピーするだけで、コピー側が元と同じ ProfileId のままだった（再起動の補正まで）。
    /// 複製の直後に、コピー側へ新しい ID を振る（<see cref="IProfileIdentityService.AssignNewId"/>）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class Phase11W6DuplicateAssignsNewIdTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;
        private readonly ITestOutputHelper _output;

        public Phase11W6DuplicateAssignsNewIdTests(ITestOutputHelper output)
        {
            _output = output;
            _dir = Path.Combine(Path.GetTempPath(), "Phase11W6_Dup_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private IProfileIdentityService NewService() => new ProfileIdentityService(new PathService(_dir));

        private string FileOf(string name) => Path.Combine(_profiles, name + ".xml");

        private void WriteProfile(string name, string id, string body = "<Other>1</Other>")
        {
            File.WriteAllText(FileOf(name), "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><ProfileId>" + id + "</ProfileId>" + body + "</DS4Windows>");
        }

        private string ReadId(string name)
        {
            var doc = new XmlDocument();
            doc.Load(FileOf(name));
            return doc.SelectSingleNode("DS4Windows/ProfileId")?.InnerText;
        }

        [Fact]
        public void AssignNewId_GivesTheCopyANewId_AndKeepsTheOriginalAndTheOtherContent()
        {
            string originalId = Guid.NewGuid().ToString("N");
            WriteProfile("verifyC2", originalId, "<Other>keep</Other>");
            File.Copy(FileOf("verifyC2"), FileOf("verifyC3"));   // DupBox の File.Copy と同じ
            IProfileIdentityService service = NewService();

            string newId = service.AssignNewId("verifyC3");

            Assert.NotNull(newId);
            Assert.NotEqual(originalId, newId);
            Assert.True(Guid.TryParseExact(newId, "N", out _));
            Assert.Equal(newId, ReadId("verifyC3"));
            Assert.Equal(originalId, ReadId("verifyC2"));
            Assert.Contains("<Other>keep</Other>", File.ReadAllText(FileOf("verifyC3")));
        }

        [Fact]
        public void AssignNewId_UpdatesTheIndex_SoEachNameResolvesToItsOwnId()
        {
            string originalId = Guid.NewGuid().ToString("N");
            WriteProfile("verifyC2", originalId);
            IProfileIdentityService service = NewService();
            Assert.Equal(originalId, service.GetId("verifyC2"));

            File.Copy(FileOf("verifyC2"), FileOf("verifyC3"));
            string newId = service.AssignNewId("verifyC3");

            Assert.Equal(originalId, service.GetId("verifyC2"));
            Assert.Equal(newId, service.GetId("verifyC3"));
            Assert.Equal("verifyC2", service.GetName(originalId));
            Assert.Equal("verifyC3", service.GetName(newId));
        }

        [Fact]
        public void AssignNewId_ReturnsNull_WhenTheFileDoesNotExist()
        {
            Assert.Null(NewService().AssignNewId("missing"));
            Assert.Null(NewService().AssignNewId(null));
        }

        [Fact]
        public void TheDuplicateBox_AssignsANewIdRightAfterTheCopy_BeforeNotifyingTheSave()
        {
            string path = SourceFileLocator.Find("DS4Windows/UI/DupBox.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("DupBox.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            text = Regex.Replace(text, @"\s+", " ");

            int copy = text.IndexOf("System.IO.File.Copy(", StringComparison.Ordinal);
            int assign = text.IndexOf("profileIdentity.AssignNewId(profile);", StringComparison.Ordinal);
            int save = text.IndexOf("Save?.Invoke(this, profile);", StringComparison.Ordinal);
            Assert.True(copy >= 0 && assign > copy && save > assign, "File.Copy → AssignNewId → Save の順になっていない");
        }
    }
}
