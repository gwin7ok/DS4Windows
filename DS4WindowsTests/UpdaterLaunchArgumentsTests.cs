using DS4WinWPF;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS4Updater の起動引数に、<c>--ds4windows-repo</c>（<see cref="ReleaseSource.RepositoryPageUri"/>）が必ず入ること。
    /// 利用者の手元の古い DS4Updater は、引数がなければ移行前のリポジトリを見るため、これが無いと、移行後に更新が完了しない。
    /// </summary>
    public class UpdaterLaunchArgumentsTests
    {
        [Fact]
        public void Build_AlwaysPassesTheReleaseSourceRepository()
        {
            string args = UpdaterLaunchArguments.Build(@"C:\DS4Windows", @"C:\DS4Windows\DS4Updater", @"C:\DS4Windows\DS4Windows.exe");

            Assert.Contains($"--ds4windows-repo \"{ReleaseSource.RepositoryPageUri}\"", args);
        }

        [Fact]
        public void Build_KeepsTheExistingArguments()
        {
            string args = UpdaterLaunchArguments.Build(@"C:\D", @"C:\D\U", @"C:\D\DS4Windows.exe", "admin");

            Assert.Contains("--ds4windows-path \"C:\\D\"", args);
            Assert.Contains("--ds4updater-path \"C:\\D\\U\"", args);
            Assert.Contains("-autolaunch", args);
            Assert.Contains("--launchExe \"C:\\D\\DS4Windows.exe\"", args);
            Assert.EndsWith("--launch-mode=admin", args);
        }

        [Fact]
        public void Build_WithoutLaunchMode_OmitsIt()
        {
            string args = UpdaterLaunchArguments.Build("a", "b", "c");

            Assert.DoesNotContain("--launch-mode", args);
        }
    }
}
