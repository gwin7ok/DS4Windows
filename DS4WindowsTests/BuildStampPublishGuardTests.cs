using System.IO;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// build_timestamp.txt がリリースのアーカイブに含まれるようにする修正の回帰ガード。
    /// <c>dotnet publish -o &lt;別フォルダー&gt;</c> では、ビルド出力（OutDir）だけに書かれて公開先（PublishDir）に入らなかったため、
    /// csproj に公開先へ複製するターゲットを置いた。そのターゲットが消えないことを検査する。
    /// </summary>
    public class BuildStampPublishGuardTests
    {
        [Fact]
        public void Csproj_CopiesTheBuildStampToThePublishDir()
        {
            string csproj = SourceFileLocator.Find("DS4Windows/DS4WinWPF.csproj");
            if (csproj == null) return;

            string text = File.ReadAllText(csproj);
            Assert.Contains("Name=\"CopyBuildStampToPublishDir\"", text);
            Assert.Contains("AfterTargets=\"Publish\"", text);
            Assert.Contains("SourceFiles=\"$(OutDir)build_timestamp.txt\"", text);
            Assert.Contains("DestinationFolder=\"$(PublishDir)\"", text);
        }
    }
}
