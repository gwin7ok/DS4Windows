using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7（実機確認で、インストール済みのビルドが古いことに気づいたため）: 起動時のログの「This binary was built at …」に、ビルドのコミットのハッシュも出す。
    /// ビルド（<c>DS4WinWPF.csproj</c> の <c>UpdateLatestBuildSummary</c>）が <c>build_timestamp.txt</c> に「commit=ハッシュ（未コミットの変更があれば -dirty）」を書き、
    /// 起動（<c>App.xaml.cs</c>）がそれを読んでログに出す。ここでは、その契約（書式と、読み取りの正規表現）をソースの照合で守る。
    /// </summary>
    public class Phase8Step7BuildCommitLogGuardTests
    {
        [Fact]
        public void TheBuildWritesTheCommitIntoTheTimestampFile()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4WinWPF.csproj");
            if (path == null) return;
            string text = File.ReadAllText(path);

            Assert.Contains("git rev-parse --short=8 HEAD", text);
            Assert.Contains("git status --porcelain", text);
            Assert.Contains("commit=$(BuildCommit)", text);
            Assert.Contains("$(BuildCommit)-dirty", text);
            Assert.Contains(">unknown</BuildCommit>", text);   // git がないときも、ビルドは失敗しない
        }

        [Fact]
        public void TheStartupLogShowsTheCommit_AndStillReadsTheOldFormat()
        {
            string path = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);

            Assert.Contains("commit=(?<hash>", text);
            Assert.Contains("(commit {commit.Groups[\"hash\"].Value})", text);
            Assert.Contains("This binary was built at {m.Groups[\"date\"].Value}{commitText}", text);
        }

        [Theory]
        [InlineData("2026-10-19 14:12:30 commit=95e2b630", "2026-10-19 14:12:30", "95e2b630")]
        [InlineData("2026-10-19 14:12:30 commit=95e2b630-dirty", "2026-10-19 14:12:30", "95e2b630-dirty")]
        [InlineData("2026-10-19 14:12:30 commit=unknown", "2026-10-19 14:12:30", "unknown")]
        [InlineData("2026-10-19 13:40:13", "2026-10-19 13:40:13", null)]   // 古い形式（コミットなし）
        public void TheFileFormat_IsParsedByTheStartupRegexes(string single, string date, string hash)
        {
            Match m = Regex.Match(single, @"(?<date>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})");
            Match commit = Regex.Match(single, @"commit=(?<hash>\S+)");

            Assert.True(m.Success);
            Assert.Equal(date, m.Groups["date"].Value);
            Assert.Equal(hash != null, commit.Success);
            if (hash != null) Assert.Equal(hash, commit.Groups["hash"].Value);
        }
    }
}
