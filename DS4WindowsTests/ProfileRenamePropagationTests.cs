using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-5d: プロファイルの名前変更が、プロファイル切替のスペシャルアクションの切替先へ伝わること。
    /// </summary>
    // Global.store（静的）を一時的に差し替えるため、同じコレクションで直列に実行する
    [Collection("ServiceProviderHolder")]
    public class ProfileRenamePropagationTests
    {
        [Fact]
        public void RefreshProfileSwitchTargets_ReplacesTheNameOfAnActionWithoutAnId_OnlyWhenItMatches_AndSaves()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w-step10-4-5d-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var store = TestGlobals.Store;
            string originalPath = store.m_Actions;
            var backup = new List<SpecialAction>(store.actions);
            try
            {
                store.m_Actions = Path.Combine(dir, "Actions.xml");
                store.actions.Clear();
                var match = new SpecialAction("switch-old", "Cross", "Profile", "OldProfile", 0, "");
                var other = new SpecialAction("switch-other", "Circle", "Profile", "AnotherProfile", 0, "");
                store.actions.Add(match);
                store.actions.Add(other);

                var repo = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());

                // Phase10-Step4d W7: ID のない SA（この SA は profileId を持たない）は、従来どおり、旧名との一致（大文字小文字を区別しない）で置き換える
                Assert.Equal(0, repo.RefreshProfileSwitchTargets(" ", "x"));
                Assert.Equal(1, repo.RefreshProfileSwitchTargets("oldprofile", "NewProfile"));
                Assert.Equal("NewProfile", match.details);
                Assert.Equal("AnotherProfile", other.details);
                Assert.True(File.Exists(store.m_Actions));
                Assert.Equal(0, repo.RefreshProfileSwitchTargets("oldprofile", "NewProfile"));
            }
            finally
            {
                store.actions.Clear();
                store.actions.AddRange(backup);
                store.m_Actions = originalPath;
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
