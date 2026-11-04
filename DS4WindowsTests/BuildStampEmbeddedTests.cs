using System.Reflection;
using DS4Windows.Composition.Startup;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ビルド日時とコミットは、別ファイル（build_timestamp.txt）ではなく DS4Windows.exe（アセンブリ）に埋め込む。
    /// ビルドが埋め込んだ値を <see cref="BuildStampReader.FromAssembly"/> が読み、起動時のログの文言になることを検査する。
    /// </summary>
    public class BuildStampEmbeddedTests
    {
        [Fact]
        public void TheAssembly_CarriesTheBuildTimestampAndCommit()
        {
            Assembly assembly = typeof(BuildStampReader).Assembly;
            string stamp = BuildStampReader.FromAssembly(assembly);
            Assert.NotNull(stamp);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} commit=\S+$", stamp);

            string message = BuildStampReader.BuildLogMessage(stamp);
            Assert.StartsWith("This binary was built at ", message);
            Assert.Contains("(commit ", message);
        }

        [Fact]
        public void FromAssembly_ReturnsNull_WhenNothingIsEmbedded()
        {
            Assert.Null(BuildStampReader.FromAssembly(typeof(string).Assembly));
            Assert.Null(BuildStampReader.FromAssembly(null));
        }
    }
}
