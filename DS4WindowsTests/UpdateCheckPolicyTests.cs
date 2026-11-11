using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 更新の確認の判断（<see cref="UpdateCheckPolicy"/>）。元は <c>MainWindow.Check_Version</c> にあった分岐を固定する。
    /// </summary>
    public class UpdateCheckPolicyTests
    {
        private sealed class FakeChangelog : IChangelogService
        {
            public bool Newer;
            public Version Latest;
            public bool Throw;
            public int Calls;

            public bool CheckNewerVersionExists(out Version version, string lastVersionChecked, bool allowCached = true, bool respectSkip = true)
            {
                Calls++;
                if (Throw) throw new InvalidOperationException();
                version = Latest;
                return Newer;
            }

            public Task<Dictionary<Version, string>> GetChangelog(bool allVersions = false) => Task.FromResult(new Dictionary<Version, string>());
            public Task<string> GetChangelogMarkdown(bool allVersions = false) => Task.FromResult(string.Empty);
        }

        private static UpdateCheckPolicy Create(FakeChangelog changelog)
            => new UpdateCheckPolicy(changelog, TestRepositoryFactory.CreateAppSettingsService());

        [Fact]
        public void DifferentVersion_AndNewerExists_IsUpdateAvailable()
        {
            var c = new FakeChangelog { Newer = true, Latest = new Version(5, 0, 0) };
            var outcome = Create(c).Evaluate("4.2.4", "5.0.0", out var latest);
            Assert.Equal(UpdateCheckOutcome.UpdateAvailable, outcome);
            Assert.Equal(new Version(5, 0, 0), latest);
        }

        [Fact]
        public void DifferentVersion_ButNoNewer_IsUpToDate()
        {
            var c = new FakeChangelog { Newer = false };
            Assert.Equal(UpdateCheckOutcome.UpToDate, Create(c).Evaluate("4.2.4", "5.0.0", out _));
            Assert.Equal(1, c.Calls);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("4.2.4")]
        public void NoRecord_OrSameVersion_IsUpToDate_WithoutAskingChangelog(string recorded)
        {
            var c = new FakeChangelog();
            Assert.Equal(UpdateCheckOutcome.UpToDate, Create(c).Evaluate("4.2.4", recorded, out _));
            Assert.Equal(0, c.Calls);
        }

        [Fact]
        public void UnparsableRecord_IsSilent()
        {
            var c = new FakeChangelog();
            Assert.Equal(UpdateCheckOutcome.Silent, Create(c).Evaluate("4.2.4", "abc", out _));
            Assert.Equal(0, c.Calls);
        }

        [Fact]
        public void ChangelogThrows_IsUpToDate()
        {
            var c = new FakeChangelog { Throw = true };
            Assert.Equal(UpdateCheckOutcome.UpToDate, Create(c).Evaluate("4.2.4", "5.0.0", out var latest));
            Assert.Null(latest);
        }
    }
}
