using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W2（Phase8-Step1-Plan.md 項目 B-4）: プロファイル適用後の掃除の順序の構造ガード。
    /// <c>ProfileApplicationService.CompleteProfileApplication</c> は、次の順序で呼ぶ（順序を入れ替えると、アクションの実体の再作成や、
    /// 復帰の合図の退避・復元［Phase7b W1］が壊れる）:
    ///   ClearAllEntries → ClearForDevice → ClearDeviceState → PreallocateForProfileApply
    /// 静的な <c>ActionManager</c> を呼ぶため、呼び出しの実行順の記録ではなく、ソースの順序で固定する。
    /// Phase8 で呼び出しをコンストラクター注入へ置き換えるときは、この順序（意味）を維持して、本テストを更新すること。
    /// </summary>
    public class Phase8Step1ApplyProfileCleanupOrderGuardTests
    {
        [Fact]
        public void CompleteProfileApplication_CleansUpInTheFixedOrder()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileApplicationService.cs");
            if (path == null) return;

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            int start = text.IndexOf("private void CompleteProfileApplication(", System.StringComparison.Ordinal);
            Assert.True(start >= 0, "CompleteProfileApplication が見つからない");
            string body = text.Substring(start);

            int clearAll = body.IndexOf("ActionManager.ClearAllEntries(", System.StringComparison.Ordinal);
            int clearForDevice = body.IndexOf("_keyButtonControllers.ClearForDevice(", System.StringComparison.Ordinal);
            int clearDeviceState = body.IndexOf("ActionManager.ClearDeviceState(", System.StringComparison.Ordinal);
            int preallocate = body.IndexOf("ActionManager.PreallocateForProfileApply(", System.StringComparison.Ordinal);

            Assert.True(clearAll >= 0, "ClearAllEntries の呼び出しが見つからない");
            Assert.True(clearForDevice > clearAll, "ClearForDevice は ClearAllEntries の後");
            Assert.True(clearDeviceState > clearForDevice, "ClearDeviceState は ClearForDevice の後");
            Assert.True(preallocate > clearDeviceState, "PreallocateForProfileApply は ClearDeviceState の後");
        }
    }
}
