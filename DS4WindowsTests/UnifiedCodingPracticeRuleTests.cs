#nullable enable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 書き方を 1 つに揃えた箇所が、また 2 通りに戻らないことの検査。
    /// null 検査は <c>ArgumentNullException.ThrowIfNull</c>、変更通知は CommunityToolkit.Mvvm の <c>ObservableObject</c> に統一している。
    /// </summary>
    public class UnifiedCodingPracticeRuleTests
    {
        private static readonly Regex StatementNullCheck = new Regex(
            @"^[ \t]*(?:this\.)?\w+ = (\w+) \?\? throw new ArgumentNullException\(nameof\(\1\)\);", RegexOptions.Multiline);

        private static readonly Regex DirectNotifyImplementation = new Regex(
            @"class\s+\w+[^{\r\n]*:\s*[^{\r\n]*\bINotifyPropertyChanged\b");

        [Fact]
        public void NullChecksOfParameters_UseThrowIfNull()
        {
            var offenders = SourceText.ProductionFiles()
                .Where(f => StatementNullCheck.IsMatch(File.ReadAllText(f)))
                .Select(Path.GetFileName).ToList();
            Assert.True(offenders.Count == 0, "?? throw new ArgumentNullException の文が残っている: " + string.Join(", ", offenders));
        }

        [Fact]
        public void ChangeNotification_UsesTheToolkitObservableObject()
        {
            var offenders = SourceText.ProductionFiles()
                .Where(f => DirectNotifyImplementation.IsMatch(File.ReadAllText(f)))
                .Select(Path.GetFileName).ToList();
            Assert.True(offenders.Count == 0, "INotifyPropertyChanged の直接実装がある（ObservableObject を使う）: " + string.Join(", ", offenders));
        }
    }
}
