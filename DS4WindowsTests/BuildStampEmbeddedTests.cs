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
            Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2} commit=\S+$", stamp);

            string message = BuildStampReader.BuildLogMessage(stamp);
            Assert.StartsWith("This binary was built at ", message);
            Assert.Contains("(commit ", message);
        }

        [Theory]
        [InlineData("Tokyo Standard Time", "2026-11-04T12:54:01Z", "2026-11-04 21:54:01 +09:00")]
        [InlineData("UTC", "2026-11-04T12:54:01Z", "2026-11-04 12:54:01 +00:00")]
        [InlineData("Pacific Standard Time", "2026-11-04T12:54:01Z", "2026-11-04 04:54:01 -08:00")]
        public void ToLocalTimeText_ConvertsUtcToTheZoneWithOffset(string zoneId, string utc, string expected)
        {
            var zone = System.TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            Assert.Equal(expected, BuildStampReader.ToLocalTimeText(utc, zone));
        }

        [Fact]
        public void ToLocalTimeText_ReturnsTheTextAsIs_WhenItIsNotADate()
        {
            Assert.Equal("unknown", BuildStampReader.ToLocalTimeText("unknown", System.TimeZoneInfo.Utc));
        }

        [Fact]
        public void FromAssembly_ReturnsNull_WhenNothingIsEmbedded()
        {
            Assert.Null(BuildStampReader.FromAssembly(typeof(string).Assembly));
            Assert.Null(BuildStampReader.FromAssembly(null));
        }
    }
}
