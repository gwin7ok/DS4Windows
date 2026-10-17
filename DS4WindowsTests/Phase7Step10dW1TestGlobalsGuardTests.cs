using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W1（Q2＝A）: テストが <c>Global.store</c>／<c>appdatapath</c>／<c>exedirpath</c>／<c>ProfileSettingsServiceInstance</c> を
    /// 直接参照せず、<see cref="TestGlobals"/> を使うことの回帰ガード。コメントと、文字列を含む行（ソース照合のテスト）は対象外。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10dW1TestGlobalsGuardTests
    {
        private static readonly Regex Direct = new Regex(
            @"(?<![\w.])(?:DS4Windows\.)?Global\.(store|appdatapath|exedirpath|ProfileSettingsServiceInstance)\b", RegexOptions.Compiled);

        [Fact]
        public void Tests_DoNotReferenceGlobalDirectly_ForTheFourWindows()
        {
            string helper = SourceFileLocator.Find("DS4WindowsTests/TestGlobals.cs");
            if (helper == null) return;
            string dir = Path.GetDirectoryName(helper);

            var offenders = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && Path.GetFileName(f) != "TestGlobals.cs")
                .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i + 1, l)))
                .Where(x =>
                {
                    string t = x.l.TrimStart();
                    if (t.StartsWith("//") || t.StartsWith("*") || t.StartsWith("///")) return false;
                    int c = x.l.IndexOf("//", StringComparison.Ordinal);
                    string code = c >= 0 ? x.l.Substring(0, c) : x.l;
                    return !code.Contains('"') && Direct.IsMatch(code);
                })
                .Select(x => Path.GetFileName(x.f) + ":" + x.Item2)
                .ToList();

            Assert.True(offenders.Count == 0, "TestGlobals を使うこと: " + string.Join(", ", offenders));
        }
    }
}
