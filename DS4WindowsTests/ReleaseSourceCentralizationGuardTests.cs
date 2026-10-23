using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using DS4WinWPF;

namespace DS4WindowsTests
{
    /// <summary>
    /// リポジトリ移行を <see cref="ReleaseSource"/> の書き換えだけで済ませるための回帰ガード。
    /// - <see cref="ReleaseSource"/> の各 URL が <see cref="ReleaseSource.Repository"/> /
    ///   <see cref="ReleaseSource.UpdaterRepository"/> から組み立てられていること。
    /// - アプリのソース（.cs / .xaml）に、GitHub リポジトリの URL がベタ書きで再混入していないこと。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class ReleaseSourceCentralizationGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ReleaseSourceCentralizationGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ReleaseSource_Uris_AreDerivedFromRepositoryConstants()
        {
            string repo = ReleaseSource.Repository;
            Assert.Matches(@"^[^/\s]+/[^/\s]+$", repo);
            Assert.Matches(@"^[^/\s]+/[^/\s]+$", ReleaseSource.UpdaterRepository);

            Assert.Equal(ReleaseSource.RepositoryPageUri, "https://github.com/" + repo);
            Assert.Equal(ReleaseSource.ReleasesApiUri, "https://api.github.com/repos/" + repo + "/releases");
            Assert.Equal(ReleaseSource.LatestReleaseApiUri, "https://api.github.com/repos/" + repo + "/releases/latest");
            Assert.Equal(ReleaseSource.LatestReleasePageUri, "https://github.com/" + repo + "/releases/latest");
            Assert.StartsWith(ReleaseSource.RepositoryPageUri + "/", ReleaseSource.ContributorsPageUri);
            Assert.StartsWith(ReleaseSource.RepositoryPageUri + "/", ReleaseSource.MoonlightDocPageUri);
            Assert.Equal(ReleaseSource.UpdaterLatestReleaseApiUri,
                "https://api.github.com/repos/" + ReleaseSource.UpdaterRepository + "/releases/latest");
        }

        [Fact]
        public void AppSources_DoNotHardCodeReleaseRepositories()
        {
            string marker = SourceFileLocator.Find("DS4Windows/Updater/ReleaseSource.cs");
            if (marker == null)
            {
                _output.WriteLine("ReleaseSource.cs が見つからないため検査を省略した。");
                return;
            }

            string appDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(marker), ".."));
            string sep = Path.DirectorySeparatorChar.ToString();
            var offenders = Directory.EnumerateFiles(appDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                .Where(f => !f.Contains(sep + "bin" + sep) && !f.Contains(sep + "obj" + sep))
                .Where(f => !f.EndsWith("ReleaseSource.cs", StringComparison.Ordinal))
                .SelectMany(f => File.ReadAllLines(f)
                    .Select((line, i) => (f, n: i + 1, line))
                    .Where(x => !x.line.TrimStart().StartsWith("//") && !x.line.TrimStart().StartsWith("*"))
                    .Where(x => x.line.Contains("gwin7ok/DS4") || x.line.Contains("api.github.com/repos/")))
                .Select(x => $"{x.f}:{x.n}: {x.line.Trim()}")
                .ToList();

            Assert.True(offenders.Count == 0,
                "GitHub リポジトリの URL は ReleaseSource に集約する:\n" + string.Join("\n", offenders));
        }
    }
}
