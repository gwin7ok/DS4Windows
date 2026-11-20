using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.Core.Model;

namespace DS4WindowsTests
{
    /// <summary>
    /// ProfileEditor のホバー表示用インデックスに、タッチパッド 4 領域がすべて登録されていること
    /// （Left Touch の登録欠落の回帰防止。2026-10-15）。
    /// </summary>
    public class ProfileEditorHoverIndexTests
    {
        private static string LoadSource()
        {
            // ホバーの表（HoverRow の配列）は、同じ部分クラスの別ファイル（ProfileEditor.HoverTables.cs）へ移した
            string path = SourceFileLocator.Find("DS4Windows/UI/ProfileEditor.xaml.cs");
            string tablesPath = SourceFileLocator.Find("DS4Windows/UI/ProfileEditor.HoverTables.cs");
            if (path == null || tablesPath == null) return null;
            string text = SourceText.ReadFileWithPartials(path);   // HoverTables を含む、同じ部分クラスの別ファイルがすべて続く
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void HoverIndexes_RegisterAllFourTouchpadRegions()
        {
            string text = LoadSource();
            if (text == null) return;

            Assert.Matches(@"new HoverRow\(leftTouchConBtn,\s*Touch\(DS4Controls\.TouchLeft\)", text);
            Assert.Matches(@"new HoverRow\(rightTouchConBtn,\s*Touch\(DS4Controls\.TouchRight\)", text);
            Assert.Matches(@"new HoverRow\(multiTouchConBtn,\s*Touch\(DS4Controls\.TouchMulti\)", text);
            Assert.Matches(@"new HoverRow\(topTouchConBtn,\s*Touch\(DS4Controls\.TouchUpper\)", text);
        }

        [Fact]
        public void HoverIndexes_BrpConBtn_IsAssignedOnlyOnce()
        {
            string text = LoadSource();
            if (text == null) return;

            Assert.Single(Regex.Matches(text, @"new HoverRow\(brpConBtn,"));
        }
    }
}
