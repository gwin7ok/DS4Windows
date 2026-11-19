#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IChangelogService"/> の偽物。新しい版の有無と、例外を差し込める。</summary>
    public sealed class FakeChangelogService : IChangelogService
    {
        public bool Newer;
        public Version? Latest;
        public bool Throw;

        public bool CheckNewerVersionExists(out Version version, string lastVersionChecked, bool allowCached = true, bool respectSkip = true)
        {
            if (Throw) throw new InvalidOperationException();
            version = Latest!;
            return Newer;
        }

        public Task<Dictionary<Version, string>> GetChangelog(bool allVersions = false) => Task.FromResult(new Dictionary<Version, string>());

        public Task<string> GetChangelogMarkdown(bool allVersions = false) => Task.FromResult(string.Empty);
    }
}
