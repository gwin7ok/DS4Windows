using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7c W1（Phase7c-Plan.md §3 W1）: <c>Mapping.cs</c> が <c>GetProfileActionsRaw</c> の返す生の <c>List&lt;string&gt;</c> を
    /// 直接列挙・添字参照しないことの構造ガード。
    /// 背景: プロファイルの読込（<c>LegacyProfileXmlSerializer</c>）が同じリストを <c>Clear()</c>→<c>Add()</c> するため、
    /// Stage3（Button の特殊アクションの同期処理）で "Collection was modified" が起き、そのフレームの出力が打ち切られていた。
    /// 実走行の並行変更テストは、W2 の <c>MapCustomAction</c> ハーネスの追加後に加える。
    /// </summary>
    public class Phase7cW1Stage3ProfileActionsSnapshotGuardTests
    {
        [Fact]
        public void Mapping_EveryGetProfileActionsRawCall_CopiesTheListWithToArray()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            int calls = Regex.Matches(text, @"GetProfileActionsRaw\(").Count;
            int copied = Regex.Matches(text, @"GetProfileActionsRaw\([^)]*\)\s*\?\.ToArray\(\)").Count;

            Assert.True(calls >= 3, $"GetProfileActionsRaw の呼び出しが想定より少ない: {calls}");
            Assert.Equal(calls, copied);
        }
    }
}
