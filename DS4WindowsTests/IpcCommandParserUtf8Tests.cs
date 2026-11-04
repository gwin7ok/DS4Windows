using System.Text;
using DS4Windows.UI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>CLI の -command で、非 ASCII（日本語）のプロファイル名が壊れずに届くこと（UTF-8）。</summary>
    public class IpcCommandParserUtf8Tests
    {
        [Fact]
        public void Parse_KeepsJapaneseProfileName()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("LoadProfile.1.マイクラダンジョンズxbox");
            string[] parts = IpcCommandParser.Parse(bytes);
            Assert.Equal(new[] { "loadprofile", "1", "マイクラダンジョンズxbox" }, parts);
        }

        [Fact]
        public void Parse_AsciiIsUnchanged()
        {
            string[] parts = IpcCommandParser.Parse(Encoding.ASCII.GetBytes("ChangeLedColor.1.255.0.0"));
            Assert.Equal(new[] { "changeledcolor", "1", "255", "0", "0" }, parts);
        }
    }
}
