using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-1: WindowPlacementHelper／ProfileList／ProfileEntity の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-4-Plan.md</c> §11）。
    /// </summary>
    public class Step10FourOneGlobalReferenceGuardTests
    {

        [Fact]
        public void ProfileEntity_HasNoUnusedSaveMembers()
        {
            Assert.Null(typeof(ProfileEntity).GetMethod("SaveProfile"));
            Assert.Null(typeof(ProfileEntity).GetMethod("FireSaved"));
            Assert.Null(typeof(ProfileEntity).GetEvent("ProfileSaved"));
        }

        [Fact]
        public void ProfileList_AndProfileEntity_RequirePathService()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfileList(null));
            Assert.Throws<ArgumentNullException>(() => new ProfileEntity(null));
        }

        [Fact]
        public void ProfileEntity_RenameAndDelete_UseInjectedPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w-step10-4-1-" + Guid.NewGuid().ToString("N"));
            string profiles = Path.Combine(dir, "Profiles");
            Directory.CreateDirectory(profiles);
            try
            {
                File.WriteAllText(Path.Combine(profiles, "a.xml"), "<x/>");
                var entity = new ProfileEntity(new PathService(dir)) { Name = "a" };

                entity.RenameProfile("b");
                Assert.False(File.Exists(Path.Combine(profiles, "a.xml")));
                Assert.True(File.Exists(Path.Combine(profiles, "b.xml")));
                Assert.Equal("b", entity.Name);

                entity.DeleteFile();
                Assert.False(File.Exists(Path.Combine(profiles, "b.xml")));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void ProfileList_Refresh_UsesInjectedPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w-step10-4-1-" + Guid.NewGuid().ToString("N"));
            string profiles = Path.Combine(dir, "Profiles");
            Directory.CreateDirectory(profiles);
            try
            {
                File.WriteAllText(Path.Combine(profiles, "p1.xml"), "<x/>");
                File.WriteAllText(Path.Combine(profiles, "p2.xml"), "<x/>");
                var list = new ProfileList(new PathService(dir));

                list.Refresh();

                Assert.Equal(new[] { "p1", "p2" }, list.ProfileListCol.Select(e => e.Name).ToArray());
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
