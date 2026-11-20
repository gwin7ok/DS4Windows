using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>Mapping.cs</c> が <c>GetProfileActionsRaw</c> の返す生の <c>List&lt;string&gt;</c> を
    /// 直接列挙・添字参照しないことの構造ガード。
    /// 背景: プロファイルの読込（<c>LegacyProfileXmlSerializer</c>）が同じリストを <c>Clear()</c>→<c>Add()</c> するため、
    /// Stage3（Button のスペシャルアクションの同期処理）で "Collection was modified" が起き、そのフレームの出力が打ち切られていた。
    /// 実走行の並行変更テストは、W2 の <c>MapCustomAction</c> ハーネスの追加後に加える。
    /// </summary>
    public class Stage3ProfileActionsSnapshotGuardTests
    {
        [Fact]
        public void Mapping_EveryGetProfileActionsRawCall_CopiesTheList()
        {
            string path = SourceFileLocator.Find("DS4Windows/Processing/Mapping.cs");
            if (path == null) return;
            string text = SourceText.ReadFileWithPartials(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            int calls = Regex.Matches(text, @"GetProfileActionsRaw\(").Count;
            int copied = Regex.Matches(text, @"CopyProfileActionNames\(\s*[^;]*GetProfileActionsRaw\(").Count;

            Assert.True(calls >= 1,   // Stage3 などの旧経路を削除し、MapCustomAction の 1 か所になった
                 $"GetProfileActionsRaw の呼び出しが想定より少ない: {calls}");
            Assert.Equal(calls, copied);
        }
    }
}
